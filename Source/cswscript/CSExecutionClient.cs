//-----------------------------------------------------------------------------
// Date:	17/10/04	Time: 2:33p
// Module:	CSExecutionClient.cs
// Classes:	CSExecutionClient
//
// This module contains the definition of the CSExecutor class. Which implements
// compiling C# code and executing 'Main' method of compiled assembly
//
// Written by Oleg Shilo (oshilo@gmail.com)
//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2017 Oleg Shilo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;
using System.Collections.Generic;

namespace csscript
{
    delegate void PrintDelegate(string msg);

    /// <summary>
    /// Wrapper class that runs CSExecutor within windows application context.
    /// </summary>
    class CSExecutionClient
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] rawArgs)
        {
            Utils.SetMonoRootDirEnvvar();

            //Debug.Assert();
            for (int i = 0; i < rawArgs.Length; i++)
                rawArgs[i] = Environment.ExpandEnvironmentVariables(rawArgs[i]);

            string[] args = rawArgs;

            //work around of nasty Win7x64 problem.
            //http://superuser.com/questions/527728/cannot-resolve-windir-cannot-modify-path-or-path-being-reset-on-boot
            if (Environment.GetEnvironmentVariable("windir") == null)
                Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("SystemRoot"));
            Environment.SetEnvironmentVariable("pid", Process.GetCurrentProcess().Id.ToString());

            if (Utils.IsLinux())
            {
                //because Linux shebang does not properly split arguments we need to take care of this
                //http://www.daniweb.com/software-development/c/threads/268382
                List<string> tempArgs = new List<string>();
                foreach (string arg in rawArgs)
                    if (arg.StartsWith(CSSUtils.Args.DefaultPrefix))
                    {
                        foreach (string subArg in arg.Split(CSSUtils.Args.DefaultPrefix.ToCharArray()))
                            if (subArg.Trim() != "")
                                tempArgs.Add(CSSUtils.Args.DefaultPrefix + subArg.Trim());
                    }
                    else
                        tempArgs.Add(arg);

                args = tempArgs.ToArray();
            }

            try
            {
                SetEnvironmentVariable("CSScriptRuntime", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                SetEnvironmentVariable("CSScriptRuntimeLocation", System.Reflection.Assembly.GetExecutingAssembly().Location);
                SetEnvironmentVariable("cscs_exe_dir", Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            }
            catch { } //SetEnvironmentVariable will always throw an exception on Mono

            CSExecutor.print = new PrintDelegate(Print);

            CSExecutor exec = new CSExecutor();

            try
            {
                if (AppDomain.CurrentDomain.FriendlyName != "ExecutionDomain") // AppDomain.IsDefaultAppDomain is more appropriate but it is not available in .NET 1.1
                {
                    string configFile = exec.GetCustomAppConfig(args);
                    if (configFile != "")
                    {
                        AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;
                        setup.ConfigurationFile = configFile;

                        AppDomain appDomain = AppDomain.CreateDomain("ExecutionDomain", null, setup);
#if !net4
                        appDomain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location, null, args);
#else
                        appDomain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location, args);
#endif
                        return;
                    }
                }
#if net4
                //CSSUtils.DbgInjectionCode = cswscript.Resources.dbg;
                CSSUtils.DbgInjectionCode = embedded_strings.dbg_source;
#endif
                AppInfo.appName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                exec.Execute(args, Print, null);
            }
            catch (CLIException e)
            {
                if (!(e is CLIExitRequest))
                {
                    Console.WriteLine(e.Message);
                    Environment.ExitCode = e.ExitCode;
                }
            }
            catch (Surrogate86ProcessRequiredException)
            {
                try
                {
                    string thisAssembly = Assembly.GetExecutingAssembly().Location;
                    string runner = Path.Combine(Path.GetDirectoryName(thisAssembly), "lib\\runasm32.exe");

                    if (!File.Exists(runner))
                        runner = Path.Combine(Path.GetDirectoryName(thisAssembly), "runasm32.exe");

                    if (!File.Exists(runner))
                        runner = Environment.ExpandEnvironmentVariables("CSSCRIPT_32RUNNER");

                    if (!File.Exists(runner))
                    {
                        Print("This script requires to be executed as x86 process but no runner (e.g. runasm32.exe) can be found.");
                    }
                    else
                    {
                        RunConsoleApp(runner, "\"" + thisAssembly + "\" " + GetCommandLineArgumentsStringFromEnvironment());
                    }
                }
                catch { } //This will always throw an exception on Mono
            }
            catch (SurrogateHostProcessRequiredException e)
            {
                try
                {
                    string assemblyHost = ScriptLauncherBuilder.GetLauncherName(e.ScriptAssembly);
                    string appArgs = CSSUtils.Args.DefaultPrefix + "css_host_parent:" + Process.GetCurrentProcess().Id + " \"" + CSSUtils.Args.DefaultPrefix + "css_host_asm:" + e.ScriptAssembly + "\" " + GenerateCommandLineArgumentsString(e.ScriptArgs);
                    if (e.StartDebugger)
                        appArgs = "/css_host_dbg:true " + appArgs;

                    int exitCode = RunConsoleApp(assemblyHost, appArgs);
                }
                catch (Exception e1)
                {
                    Print("Cannot execute Surrogate Host Process: " + e1);
                }
            }

            OnExit();
        }

        public static void OnExit()
        {
            //collect abandoned temp files
            if (Environment.GetEnvironmentVariable("CSScript_Suspend_Housekeeping") == null)
                Utils.CleanUnusedTmpFiles(CSExecutor.GetScriptTempDir(), "*????????-????-????-????-????????????.dll", false);
        }

        /// <summary>
        /// Implementation of displaying application messages.
        /// </summary>
        static void Print(string msg)
        {
            try
            {
                string file = Path.Combine(CSExecutor.GetScriptTempDir(), "help." + Assembly.GetExecutingAssembly().GetName().Version + ".txt");
                File.WriteAllText(file, msg);
                Process.Start(file);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static string GetCommandLineArgumentsStringFromEnvironment()
        {
            if (Environment.CommandLine.StartsWith("\""))
            {
                return Environment.CommandLine.Substring(Environment.CommandLine.IndexOf('"', 1) + 1).TrimStart();
            }
            else
            {
                return Environment.CommandLine.Substring(Environment.CommandLine.IndexOf(' ') + 1).TrimStart();
            }
        }

        static string GenerateCommandLineArgumentsString(string[] args)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string arg in args)
            {
                sb.Append("\"");
                sb.Append(arg);
                sb.Append("\"");
            }

            return sb.ToString();
        }

        static int RunConsoleApp(string app, string args)
        {
            Process process = new Process();
            process.StartInfo.FileName = app;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();

            return process.ExitCode;
        }
    }

    /// <summary>
    /// Repository for application specific data
    /// </summary>
    static class AppInfo
    {
        public static string appName = "cswscript";
        public static bool appConsole = false;

        public static string appLogo
        {
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\nCopyright (C) 2004-2017 Oleg Shilo.\n"; }
        }

        public static string appLogoShort
        {
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\n"; }
        }

        public static string appParamsHelp = "";
    }
}