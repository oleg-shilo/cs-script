#region Licence...

//-----------------------------------------------------------------------------
// Date:	17/10/04	Time: 2:33p
// Module:	CSExecutionClient.cs
// Classes:	CSExecutionClient
//			AppInfo
//
// This module contains the definition of the CSExecutionClient class. Which implements
// compiling C# code and executing 'Main' method of compiled assembly
//
// Written by Oleg Shilo (oshilo@gmail.com)
//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2004-2018 Oleg Shilo
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

#endregion Licence...

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace csscript
{
    delegate void PrintDelegate(string msg);

    /// <summary>
    /// Wrapper class that runs CSExecutor within console application context.
    /// </summary>
    public class CSExecutionClient
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] rawArgs)
        {
#if DEBUG
            Environment.SetEnvironmentVariable("CSS_RESGEN", @"E:\Galos\cs-script.distro\lib\ResGen.exe");
#endif
            // Debug.Assert(false);
            main(rawArgs);
        }

        static void main(string[] rawArgs)
        {
            Runtime.SetMonoRootDirEnvvar();

            if (rawArgs.Contains("-preload"))
                rawArgs = SchedulePreloadCompiler(rawArgs);

            for (int i = 0; i < rawArgs.Length; i++)
                rawArgs[i] = Environment.ExpandEnvironmentVariables(rawArgs[i]);

            HostConsole.OnStart();
            try
            {
                //work around of nasty Win7x64 problem.
                //http://superuser.com/questions/527728/cannot-resolve-windir-cannot-modify-path-or-path-being-reset-on-boot
                if (Environment.GetEnvironmentVariable("windir") == null)
                    Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("SystemRoot"));

                Environment.SetEnvironmentVariable("pid", Process.GetCurrentProcess().Id.ToString());

                Profiler.Stopwatch.Start();

                string[] args = rawArgs;

                // if (args.Contains("-check"))
                // Debug.Assert(false);

                if (Runtime.IsLinux)
                {
                    //because Linux shebang does not properly split arguments we need to take care of this
                    //http://www.daniweb.com/software-development/c/threads/268382
                    List<string> tempArgs = new List<string>();
                    foreach (string arg in rawArgs)
                        if (arg == CSSUtils.Args.DefaultPrefix)
                        {
                            foreach (string subArg in arg.Split(CSSUtils.Args.DefaultPrefix[0]))
                                if (subArg.Trim() != "")
                                    tempArgs.Add(CSSUtils.Args.DefaultPrefix + subArg.Trim());
                        }
                        else
                            tempArgs.Add(arg);

                    args = tempArgs.ToArray();
                }

                try
                {
                    Utils.SetEnvironmentVariable("CSScriptRuntime", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    Utils.SetEnvironmentVariable("CSScriptRuntimeLocation", System.Reflection.Assembly.GetExecutingAssembly().Location);
                    Utils.SetEnvironmentVariable("cscs_exe_dir", Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                    if (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") == null && Runtime.IsLinux)
                    {
                        // GetExecutingAssembly().Location may be empty even for the entry assembly
                        var cscs_exe_dir = Environment.GetEnvironmentVariable("cscs_exe_dir");
                        if (cscs_exe_dir != null && cscs_exe_dir.StartsWith("/usr/local/"))
                            Utils.SetEnvironmentVariable("CSSCRIPT_DIR", cscs_exe_dir);
                    }
                }
                catch { } //SetEnvironmentVariable may throw an exception on Mono

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
                    CSSUtils.DbgInjectionCode = embedded_strings.dbg_source;
#endif
                    AppInfo.appName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                    exec.Execute(args, Print, null);
                }
                catch (Surrogate86ProcessRequiredException)
                {
#if !net4
                    throw new ApplicationException("Cannot build surrogate host application because this script engine is build against early version of CLR.");
#else
                    try
                    {
                        string thisAssembly = Assembly.GetExecutingAssembly().Location;
                        string runner = Path.Combine(Path.GetDirectoryName(thisAssembly), "lib\\runasm32.exe");

                        if (!File.Exists(runner))
                            runner = Path.Combine(Path.GetDirectoryName(thisAssembly), "runasm32.exe");

                        if (!File.Exists(runner))
                            runner = Environment.ExpandEnvironmentVariables("%CSSCRIPT_32RUNNER%");

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
#endif
                }
                catch (CLIException e)
                {
                    if (!(e is CLIExitRequest))
                    {
                        Console.WriteLine(e.Message);
                        Environment.ExitCode = e.ExitCode;
                    }
                }
                catch (SurrogateHostProcessRequiredException e)
                {
#if !net4
                    object dummy = e;
                    throw new ApplicationException("Cannot build surrogate host application because this script engine is build against early version of CLR.");
#else

                    try
                    {
                        string assemblyHost = ScriptLauncherBuilder.GetLauncherName(e.ScriptAssembly);
                        string appArgs = CSSUtils.Args.DefaultPrefix + "css_host_parent:" + Process.GetCurrentProcess().Id + " \"" + CSSUtils.Args.DefaultPrefix + "css_host_asm:" + e.ScriptAssembly + "\" " + GenerateCommandLineArgumentsString(e.ScriptArgs);
                        if (e.StartDebugger)
                            appArgs = CSSUtils.Args.DefaultPrefix + "css_host_dbg:true " + appArgs;

                        RunConsoleApp(assemblyHost, appArgs);
                    }
                    catch (Exception e1)
                    {
                        Console.WriteLine("Cannot execute Surrogate Host Process: " + e1);
                    }
#endif
                }

                if (exec.WaitForInputBeforeExit != null)
                {
                    Console.WriteLine(exec.WaitForInputBeforeExit);
                    Console.ReadKey();
                }
            }
            finally
            {
                HostConsole.OnExit();
            }
        }

        /// <summary>
        /// Implementation of displaying application messages.
        /// </summary>
        static void Print(string msg)
        {
            Console.WriteLine(msg);
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
                sb.Append("\" ");
            }

            return sb.ToString();
        }

        static string[] SchedulePreloadCompiler(string[] args)
        {
            var tmp = Path.GetTempFileName();
            Utils.FileDelete(tmp);

            var script = Path.Combine(Path.GetDirectoryName(tmp), "css_load.cs");

            try
            {
                File.WriteAllText(script, @"using System;
                                                  class Script
                                                  {
                                                      static public void Main(string[] args)
                                                      {
                                                        Console.WriteLine(""Compiler is loaded..."");
                                                      }
                                                  }");
            }
            catch { }

            return args.Where(x => x != "-preload").Concat(new[] { "-c:0", script }).ToArray();
        }

#if net4

        static void RunConsoleApp(string app, string args)
        {
            var process = new Process();
            process.StartInfo.FileName = app;
            process.StartInfo.Arguments = args;
            process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            ManualResetEvent outputThreadDone = new ManualResetEvent(false);
            ManualResetEvent errorOutputThreadDone = new ManualResetEvent(false);

            Action<StreamReader, Stream, ManualResetEvent> redirect = (src, dest, doneEvent) =>
            {
                try
                {
                    while (true)
                    {
                        char[] buffer = new char[1000];
                        int size = src.Read(buffer, 0, 1000);
                        if (size == 0)
                            break;

                        var data = new string(buffer, 0, size);
                        var bytes = src.CurrentEncoding.GetBytes(data);
                        dest.Write(bytes, 0, bytes.Length);
                        dest.Flush();
                    }
                }
                finally
                {
                    doneEvent.Set();
                }
            };

            ThreadPool.QueueUserWorkItem(x =>
                redirect(process.StandardOutput, Console.OpenStandardOutput(), outputThreadDone));

            ThreadPool.QueueUserWorkItem(x =>
                redirect(process.StandardError, Console.OpenStandardError(), errorOutputThreadDone));

            ThreadPool.QueueUserWorkItem(x =>
            {
                while (true)
                {
                    int nextChar = Console.Read();
                    process.StandardInput.Write((char)nextChar);
                    process.StandardInput.Flush();
                }
            });

            process.WaitForExit();
            Environment.ExitCode = process.ExitCode;

            //the output buffers may still contain some data just after the process exited
            outputThreadDone.WaitOne();
            errorOutputThreadDone.WaitOne();
        }

#endif
    }

    /// <summary>
    /// Repository for application specific data
    /// </summary>
    class AppInfo
    {
        public static string appName = Assembly.GetExecutingAssembly().GetName().Name;
        public static bool appConsole = true;

        public static string appLogo
        {
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\nCopyright (C) 2004-2018 Oleg Shilo.\n"; }
        }

        public static string appLogoShort
        {
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\n"; }
        }
    }

    class HostConsole
    {
        static Encoding originalEncoding;

        public static void OnExit()
        {
            try
            {
                if (originalEncoding != null)
                    Console.OutputEncoding = originalEncoding;

                //collect abandoned temp files
                if (Environment.GetEnvironmentVariable("CSScript_Suspend_Housekeeping") == null)
                    Utils.CleanUnusedTmpFiles(CSExecutor.GetScriptTempDir(), "*????????-????-????-????-????????????.dll", false);
            }
            catch { }
        }

        public static void SetEncoding(string encoding)
        {
            try
            {
                Encoding oldEncoding = Console.OutputEncoding;

                Console.OutputEncoding = System.Text.Encoding.GetEncoding(encoding);

                Utils.IsDefaultConsoleEncoding = false;

                if (originalEncoding == null)
                    originalEncoding = oldEncoding;
            }
            catch { }
        }

        public static void OnStart()
        {
            Utils.ProcessNewEncoding = ProcessNewEncoding;

            ProcessNewEncoding(null);
        }

        public static string ProcessNewEncoding(string requestedEncoding)
        {
            string consoleEncodingOverwrite = NormaliseEncodingName(Environment.GetEnvironmentVariable("CSSCRIPT_CONSOLE_ENCODING_OVERWRITE"));

            string encodingToSet = consoleEncodingOverwrite ?? NormaliseEncodingName(requestedEncoding);

            if (encodingToSet != null)
            {
                if (encodingToSet != Settings.DefaultEncodingName)
                    SetEncoding(encodingToSet);
            }
            return encodingToSet;
        }

        public static string NormaliseEncodingName(string name)
        {
            if (string.Compare(name, Settings.DefaultEncodingName, true) == 0)
                return Settings.DefaultEncodingName;
            else
                return name;
        }
    }
}