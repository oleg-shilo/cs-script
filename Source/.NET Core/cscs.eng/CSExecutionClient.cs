#region Licence...

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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

/// <summary>
/// CS-Script engine class library assembly.
/// Runtime: .NET Standard 2.0
/// File name: cscs.eng.dll
/// Implements light/refined business logic of the standard CS-Script. 
/// </summary>


/*
 Limitations comparing to CS-Script for .NET

 CS-Script todo:
    - Refactoring
        - Major refactoring to meet C# 7 standards
        - Share source modules between cscs and CSScriptLib
    - Functionality
        - Decide which NuGet engine to invoke 
        - Handle //css_nuget -rt:<name> directive arg in .NET Full NuGet.exe use-case
        + Full support for std in, out and error in css launcher
        + remove all code (and config) for -inmem:0 use-case 
        + Process CompilerParameters during compilation
        + Ensure the cached script is not used/shared between .NET Full and Core launchers
        + Ensure ".NET standard" class libraries can be referenced from .NET Full cscs.exe 
        + In -ver output handle/reflect absent config 
        + Ensure C# 7 syntax
        + Ensure inmem loading
        + NuGet support
        + Ensure default '-l:1'
        + Describe //css_nuget -rt:<name> directive arg

 CS-Script limitations:
    - No support for script app.config file
    - No building "*.exe"
    - No NuGet inter-package dependencies resolving. All packages (including dependency packages) must be specified in the script 
    - Huge compilation startup delay (.NET Core offers no VBCSCompiler.exe optimisation)
      There may be some hope as VS actually runs "dotnet VBCSCompiler.dll -namedpipe:..." 
      The side signs are indicating that MS is working on this problem. Thus a call "dotnet build ..."
      forks an addition long standing process 
      "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\2.1.300-preview1-008174\Roslyn\bincore\VBCSCompiler.dll" "-pipename:<user_name>.F.QF+8Z+bcVzTAZf2vxEt85UoKv"

    - Support for custom app.config files is not available for .NET Core due to the API limitations
      See in full version source "...if (AppDomain.CurrentDomain.FriendlyName != "ExecutionDomain")...")

 CS-Script obsolete features and limitations:
    - All builds are "Debug" builds
    - Surrogate process support for x86 vs. x64 execution
    - Support for Settings.DefaultApartmentState (still needs to be tested)
    - Support for Settings.UsePostProcessor
    - Support for Settings.TargetFramework
    - Support for Settings.CleanupShellCommand
    - Support for Settings.DoCleanupAfterNumberOfRuns
    - Support for `//css_host`
    - No automatic elevation via arg '-elevate'
    // https://github.com/aspnet/RoslynCodeDomProvider/issues/37
*/

namespace csscript
{
    internal delegate void PrintDelegate(string msg);

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
            // Debug.Assert(false);
            main(PreprocessArgs(rawArgs));
        }

        private static string[] PreprocessArgs(string[] rawArgs)
        {
            string[] args = rawArgs.Contains("-preload") ?
                                SchedulePreloadCompiler(rawArgs) :
                                rawArgs.Select(Environment.ExpandEnvironmentVariables).ToArray();

            if (!Utils.IsWin)
            {
                // because Linux shebang does not properly split arguments we need to take care of this
                // http://www.daniweb.com/software-development/c/threads/268382
                args = args.SplitMergedArgs();
            }

            return args;
        }

        private static void main(string[] args)
        {
            try
            {
                var exec = new CSExecutor();
                Profiler.Stopwatch.Start();

                Host.OnStart();

                try
                {
                    exec.Execute(args, Console.WriteLine, null);
                }
                catch (CLIException e)
                {
                    if (!(e is CLIExitRequest))
                    {
                        Console.WriteLine(e.Message);
                        Environment.ExitCode = e.ExitCode;
                    }
                }

                if (exec.WaitForInputBeforeExit != null)
                    try
                    {

                        Console.WriteLine(exec.WaitForInputBeforeExit);
                        Console.Read();
                    }
                    catch { }
            }
            finally
            {
                Host.OnExit();
            }
        }

        // static string GetCommandLineArgumentsStringFromEnvironment1()
        // {
        //     if (Environment.CommandLine.StartsWith("\""))
        //     {
        //         return Environment.CommandLine.Substring(Environment.CommandLine.IndexOf('"', 1) + 1).TrimStart();
        //     }
        //     else
        //     {
        //         return Environment.CommandLine.Substring(Environment.CommandLine.IndexOf(' ') + 1).TrimStart();
        //     }
        // }

        private static string GenerateCommandLineArgumentsString(string[] args)
        {
            var sb = new StringBuilder();

            foreach (string arg in args)
            {
                sb.Append("\"");
                sb.Append(arg);
                sb.Append("\" ");
            }

            return sb.ToString();
        }

        private static string[] SchedulePreloadCompiler(string[] args)
        {
            var tmp = Path.GetTempFileName();
            Utils.FileDelete(tmp);

            var script = Path.Combine(Path.GetDirectoryName(tmp), "css_load.cs");

            try
            {
                File.WriteAllText(script, @"using System;
                                            class Script
                                            {
                                                static public void Main()
                                                {
                                                  Console.WriteLine(""Compiler is loaded..."");
                                                }
                                            }");
            }
            catch { }

            return args.Where(x => x != "-preload").Concat(new[] { "-c:0", script }).ToArray();
        }

        private static void RunConsoleApp(string app, string args)
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
    }

    /// <summary>
    /// Repository for application specific data
    /// </summary>
    internal class AppInfo
    {
        public static string appName = Assembly.GetExecutingAssembly().GetName().Name;
        public static bool appConsole = true;

        public static string appLogo
        {
            get { return "C# Script execution engine (.NET Core). Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\nCopyright (C) 2004-2018 Oleg Shilo.\n"; }
        }

        public static string appLogoShort
        {
            get { return "C# Script execution engine (.NET Core). Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\n"; }
        }
    }

    internal class Host
    {
        private static Encoding originalEncoding;

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

                Console.OutputEncoding = Encoding.GetEncoding(encoding);

                Utils.IsDefaultConsoleEncoding = false;

                if (originalEncoding == null)
                    originalEncoding = oldEncoding;
            }
            catch { }
        }

        public static void OnStart()
        {
            //work around of nasty Win7x64 problem.
            //http://superuser.com/questions/527728/cannot-resolve-windir-cannot-modify-path-or-path-being-reset-on-boot
            if (Environment.GetEnvironmentVariable("windir") == null)
                Utils.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("SystemRoot"));

            Utils.SetEnvironmentVariable("pid", Process.GetCurrentProcess().Id.ToString());
            Utils.SetEnvironmentVariable("CSScriptRuntime", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Utils.SetEnvironmentVariable("CSScriptRuntimeLocation", Assembly.GetExecutingAssembly().Location);
            Utils.SetEnvironmentVariable("cscs_exe_dir", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            if (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") == null && !Utils.IsWin)
            {
                // GetExecutingAssembly().Location may be empty even for the entry assembly
                var cscs_exe_dir = Environment.GetEnvironmentVariable("cscs_exe_dir");
                if (cscs_exe_dir != null && cscs_exe_dir.StartsWith("/usr/local/"))
                    Utils.SetEnvironmentVariable("CSSCRIPT_DIR", cscs_exe_dir);
            }

            Utils.ProcessNewEncoding = ProcessNewEncoding;

            ProcessNewEncoding(null);

            AppInfo.appName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            // CSSUtils.DbgInjectionCode = embedded_strings.dbg_source;

            CSExecutor.print = Console.WriteLine;
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