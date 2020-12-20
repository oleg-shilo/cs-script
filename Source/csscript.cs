#region Licence...

//-----------------------------------------------------------------------------
// Date:	17/10/04	Time: 2:33p
// Module:	csscript.cs
// Classes:	CSExecutor
//			ExecuteOptions
//
// This module contains the definition of the CSExecutor class. Which implements
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
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

//using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.CSharp;
using CSScriptLibrary;

namespace csscript
{
    /// <summary>
    /// Delegate implementing source file probing algorithm.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="searchDirs">The extra dirs.</param>
    /// <param name="throwOnError">if set to <c>true</c> [throw on error].</param>
    /// <returns></returns>
    public delegate string[] ResolveSourceFileAlgorithm(string file, string[] searchDirs, bool throwOnError);

    /// <summary>
    /// Delegate implementing assembly file probing algorithm.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="searchDirs">The extra dirs.</param>
    /// <returns></returns>
    public delegate string[] ResolveAssemblyHandler(string file, string[] searchDirs);

    internal class Profiler
    {
        static public Stopwatch Stopwatch = new Stopwatch();
    }

    internal interface IScriptExecutor
    {
        void ShowHelpFor(string arg);

        void ShowProjectFor(string arg);

        void ShowHelp(string helpTyp, params object[] context);

        void DoCacheOperations(string command);

        void ShowVersion();

        void ShowPrecompilerSample();

        void CreateDefaultConfigFile();

        void PrintDefaultConfig();

        void PrintDecoratedAutoclass(string script);

        void ProcessConfigCommand(string command);

        void ShowSample(string version);

        ExecuteOptions GetOptions();

        string WaitForInputBeforeExit { get; set; }
    }

    /// <summary>
    /// CSExecutor is an class that implements execution of *.cs files.
    /// </summary>
    internal class CSExecutor : IScriptExecutor
    {
        #region Public interface...

        /// <summary>
        /// Force caught exceptions to be re-thrown.
        /// </summary>
        public bool Rethrow
        {
            get { return rethrow; }
            set { rethrow = value; }
        }

        string waitForInputBeforeExit;

        public string WaitForInputBeforeExit
        {
            get { return waitForInputBeforeExit; }
            set { waitForInputBeforeExit = value; }
        }

        internal static void HandleUserNoExecuteRequests(ExecuteOptions options)
        {
            var request = (options.nonExecuteOpRquest as string);

            if (request == AppArgs.proj || request == AppArgs.proj_dbg)
            {
                var project = Project.GenerateProjectFor(options.scriptFileName);

                foreach (string file in project.Files)
                    print("file:" + file);

                if (request == AppArgs.proj_dbg && options.enableDbgPrint)
                    print("file:" + CSSUtils.CreateDbgInjectionInterfaceCode(null));

                foreach (string file in project.Refs)
                    print("ref:" + file);

                foreach (string file in project.SearchDirs)
                    print("searchDir:" + file);
            }
        }

        internal static Settings LoadSettings(ExecuteOptions options)
        {
            Settings settings = null;

            if (options != null && options.noConfig)
            {
                if (options.altConfig != "")
                    settings = Settings.Load(Path.GetFullPath(options.altConfig));
                else
                    settings = Settings.Load(null, true);
            }
            else
            {
                if (!Assembly.GetExecutingAssembly().IsDynamic())
                    settings = Settings.LoadDefault();
            }
            return settings;
        }

        Settings GetPersistedSettings(List<string> appArgs)
        {
            //read persistent settings from configuration file
            Settings settings = LoadSettings(options);

            if (settings != null)
            {
                options.hideTemp = settings.HideAutoGeneratedFiles;
                if (options.preCompilers == "") //it may be set from command-line args, which have higher precedence
                    options.preCompilers = settings.Precompiler;
                options.altCompiler = settings.ExpandUseAlternativeCompiler();
                options.roslynDir = Environment.ExpandEnvironmentVariables(settings.RoslynDir);
                options.defaultRefAssemblies = settings.ExpandDefaultRefAssemblies();
                options.postProcessor = settings.ExpandUsePostProcessor();
                options.apartmentState = settings.DefaultApartmentState;
                options.reportDetailedErrorInfo = settings.ReportDetailedErrorInfo;
                options.openEndDirectiveSyntax = settings.OpenEndDirectiveSyntax;
                options.consoleEncoding = settings.ConsoleEncoding;
                options.decorateAutoClassAsCS6 = settings.AutoClass_DecorateAsCS6;
                options.enableDbgPrint = settings.EnableDbgPrint;
                options.cleanupShellCommand = settings.ExpandCleanupShellCommand();
                options.doCleanupAfterNumberOfRuns = settings.DoCleanupAfterNumberOfRuns;
                options.inMemoryAsm = settings.InMemoryAssembly;

                //options.useSurrogateHostingProcess = settings.UseSurrogateHostingProcess;
                options.concurrencyControl = settings.ConcurrencyControl;
                options.hideCompilerWarnings = settings.HideCompilerWarnings;
                options.TargetFramework = settings.TargetFramework;

                //process default command-line arguments
                string[] defaultCmdArgs = settings.DefaultArguments
                                                  .Split(" ".ToCharArray())
                                                  .Where(Utils.NotEmpty)
                                                  .ToArray();

                int firstDefaultScriptArg = CSSUtils.ParseAppArgs(defaultCmdArgs, this);
                if (firstDefaultScriptArg != defaultCmdArgs.Length)
                {
                    options.scriptFileName = defaultCmdArgs[firstDefaultScriptArg];
                    for (int i = firstDefaultScriptArg + 1; i < defaultCmdArgs.Length; i++)
                        if (defaultCmdArgs[i].Trim().Length != 0)
                            appArgs.Add(defaultCmdArgs[i]);
                }

                //if (options.suppressExternalHosting)
                //    options.useSurrogateHostingProcess = settings.UseSurrogateHostingProcess = false;
            }
            return settings;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public void Execute(string[] args, PrintDelegate printDelg, string primaryScript)
        {
            try
            {
                print = printDelg != null ? printDelg : VoidPrint;

                if (args.Length > 0)
                {
                    #region Parse command-line arguments...

                    //Here we need to separate application arguments from script ones.
                    //Script engine arguments are always followed by script arguments
                    //[appArgs][scriptFile][scriptArgs][//x]
                    List<string> appArgs = new List<string>();

                    //The following will also update corresponding "options" members from "settings" data
                    Settings settings = GetPersistedSettings(appArgs);

                    int firstScriptArg = CSSUtils.ParseAppArgs(args, this);

                    options.resolveAutogenFilesRefs = settings.ResolveAutogenFilesRefs;
                    if (!options.processFile)
                    {
                        // No further processing is required.
                        // Some primitive request (e.g. print help) has been already dispatched
                        // though some non-processing request cannot be done without using options
                        // so let them to be handled here.
                        if (options.nonExecuteOpRquest == null)
                            return;
                    }

                    if (args.Length <= firstScriptArg)
                    {
                        Environment.ExitCode = 1;
                        print("No script file was specified.");
                        return; //no script, no script arguments
                    }

                    //process original command-line arguments
                    if (options.scriptFileName == "")
                    {
                        options.scriptFileName = args[firstScriptArg];
                        firstScriptArg++;
                    }

                    for (int i = firstScriptArg; i < args.Length; i++)
                    {
                        if (i == args.Length - 1 && string.Compare(args[args.Length - 1], "//x", true, CultureInfo.InvariantCulture) == 0)
                        {
                            options.startDebugger = true;
                            options.DBG = true;
                        }
                        else
                            appArgs.Add(args[i]);
                    }

                    scriptArgs = appArgs.ToArray();

                    //searchDirs[0] is the script file directory. Set it only after
                    //the script file resolved because it can be:
                    //	dir defined by the absolute/relative script file path
                    //
                    //	current dir
                    //	local dir
                    //	host dir
                    //	dirs from command args (-dir:<path>)
                    //  dirs form code (//css_dir <path>)
                    //	settings.SearchDirs
                    //  CacheDir

                    List<string> dirs = new List<string>();

                    if (!Settings.ProbingLegacyOrder)
                    {
                        dirs.Add(Settings.local_dirs_section);
                        dirs.Add(Settings.cmd_dirs_section);
                        dirs.Add(Settings.code_dirs_section);
                        dirs.Add(Settings.config_dirs_section);
                        dirs.Add(Settings.internal_dirs_section);
                    }

                    var host_dir = this.GetType().Assembly.GetAssemblyDirectoryName();
                    var local_dir = Path.GetDirectoryName(Path.GetFullPath(options.scriptFileName));

                    using (IDisposable currDir = new CurrentDirGuard())
                    {
                        if (options.local)
                            Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(options.scriptFileName));

                        dirs.AddIfNotThere(Environment.CurrentDirectory, Settings.local_dirs_section);
                        dirs.AddIfNotThere(local_dir, Settings.local_dirs_section);

                        foreach (string dir in options.searchDirs) //some directories may be already set from command-line
                        {
                            // command line dirs resolved against current dir
                            if (!dir.IsDirSectionSeparator())
                                dirs.AddIfNotThere(Path.GetFullPath(dir), Settings.cmd_dirs_section);
                        }

                        if (settings != null)
                            foreach (string dir in Environment.ExpandEnvironmentVariables(settings.SearchDirs).Split(",;".ToCharArray()))
                                if (dir.Trim() != "")
                                    dirs.AddIfNotThere(Path.GetFullPath(dir), Settings.config_dirs_section);

                        dirs.AddIfNotThere(host_dir, Settings.local_dirs_section);
                    }

                    options.scriptFileName = FileParser.ResolveFile(options.scriptFileName, dirs.ToArray());

                    if (primaryScript != null)
                        options.scriptFileNamePrimary = primaryScript;
                    else
                        options.scriptFileNamePrimary = options.scriptFileName;

                    if (Environment.GetEnvironmentVariable("EntryScript") == null)
                        Environment.SetEnvironmentVariable("EntryScript", Path.GetFullPath(options.scriptFileNamePrimary));

                    if (CSExecutor.ScriptCacheDir == "")
                        CSExecutor.SetScriptCacheDir(options.scriptFileName);

                    if (settings != null && settings.HideAutoGeneratedFiles != Settings.HideOptions.DoNotHide)
                    {
                        if (!Settings.ProbingLegacyOrder)
                        {
                            dirs.AddIfNotThere(CSExecutor.ScriptCacheDir, Settings.internal_dirs_section);
                        }
                    }

                    options.searchDirs = dirs.ToArray();
                    CSharpParser.CmdScriptInfo[] cmdScripts = new CSharpParser.CmdScriptInfo[0];

                    //do quick parsing for pre/post scripts, ThreadingModel and embedded script arguments
                    CSharpParser parser = new CSharpParser(options.scriptFileName, true, null, options.searchDirs);

                    if (parser.AutoClassMode != null)
                    {
                        options.autoClass = true;
                    }

                    if (parser.Inits.Length != 0)
                        options.initContext = parser.Inits[0];

                    if (parser.HostOptions.Length != 0)
                    {
                        if (Environment.Version.Major >= 4)
                        {
                            foreach (string optionsSet in parser.HostOptions)
                                foreach (string option in optionsSet.Split(' '))
                                    if (option == "/platform:x86")
                                        options.compilerOptions += " " + option;
                                    else if (option.StartsWith("/version:"))
                                        options.TargetFramework = option.Replace("/version:", "");

                            options.useSurrogateHostingProcess = true;
                        }
                    }

                    //analyses ThreadingModel to use it with execution thread
                    if (File.Exists(options.scriptFileName))
                    {
                        if (parser.ThreadingModel != ApartmentState.Unknown)
                            options.apartmentState = parser.ThreadingModel;

                        List<string> newSearchDirs = new List<string>(options.searchDirs);

                        using (IDisposable currDir = new CurrentDirGuard())
                        {
                            Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(options.scriptFileName));

                            var code_probing_dirs = parser.ExtraSearchDirs.Select<string, string>(Path.GetFullPath);

                            foreach (string dir in code_probing_dirs)
                                newSearchDirs.AddIfNotThere(dir, Settings.code_dirs_section);

                            foreach (string file in parser.RefAssemblies)
                            {
                                string path = file.Replace("\"", "");
                                string dir = Path.GetDirectoryName(path);
                                if (dir != "")
                                {
                                    dir = Path.GetFullPath(dir);
                                    newSearchDirs.AddIfNotThere(dir, Settings.code_dirs_section);
                                }
                            }
                            options.searchDirs = newSearchDirs.ToArray();
                        }

                        List<CSharpParser.CmdScriptInfo> preScripts = new List<CSharpParser.CmdScriptInfo>(parser.CmdScripts);
                        foreach (CSharpParser.ImportInfo info in parser.Imports)
                        {
                            try
                            {
                                string[] files = FileParser.ResolveFiles(info.file, options.searchDirs);
                                foreach (string file in files)
                                    if (file.IndexOf(".g.cs") == -1) //non auto-generated file
                                    {
                                        using (IDisposable currDir = new CurrentDirGuard())
                                        {
                                            CSharpParser impParser = new CSharpParser(file, true, null, options.searchDirs);
                                            Environment.CurrentDirectory = Path.GetDirectoryName(file);

                                            string[] packageAsms = NuGet.Resolve(impParser.NuGets, true, file);
                                            foreach (string asmName in packageAsms)
                                            {
                                                var packageDir = Path.GetDirectoryName(asmName);
                                                newSearchDirs.Add(packageDir);
                                            }

                                            foreach (string dir in impParser.ExtraSearchDirs)
                                                newSearchDirs.Add(Path.GetFullPath(dir));

                                            options.searchDirs = newSearchDirs.ToArray();
                                        }
                                        preScripts.AddRange(new CSharpParser(file, true, null, options.searchDirs).CmdScripts);
                                    }
                            }
                            catch
                            {
                                //some files may not be generated yet
                            }
                        }

                        cmdScripts = preScripts.ToArray();

                        if (primaryScript == null)//this is a primary script
                        {
                            int firstEmbeddedScriptArg = CSSUtils.ParseAppArgs(parser.Args, this);
                            if (firstEmbeddedScriptArg != -1)
                            {
                                for (int i = firstEmbeddedScriptArg; i < parser.Args.Length; i++)
                                    appArgs.Add(parser.Args[i]);
                            }
                            scriptArgs = appArgs.ToArray();
                        }
                    }

                    #endregion Parse command-line arguments...

                    ExecuteOptions originalOptions = (ExecuteOptions)options.Clone(); //preserve master script options
                    string originalCurrDir = Environment.CurrentDirectory;

                    //run prescripts
                    //Note: during the secondary script execution static options will be modified (this is required for
                    //browsing in CSSEnvironment with reflection). So reset it back with originalOptions after the execution is completed
                    foreach (CSharpParser.CmdScriptInfo info in cmdScripts)
                        if (info.preScript)
                        {
                            if (info.args.Any(x => x.ToLower() == "elevate" && options.syntaxCheck))
                                continue; //do not interrupt syntax checking with the elevation

                            Environment.CurrentDirectory = originalCurrDir;
                            info.args[1] = FileParser.ResolveFile(info.args[1], originalOptions.searchDirs);

                            CSExecutor exec = new CSExecutor(info.abortOnError, originalOptions);

                            if (originalOptions.DBG)
                            {
                                List<string> newArgs = new List<string>();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "dbg");
                                info.args = newArgs.ToArray();
                            }
                            if (originalOptions.verbose)
                            {
                                List<string> newArgs = new List<string>();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "verbose");
                                info.args = newArgs.ToArray();
                            }

                            if (info.abortOnError)
                                exec.Execute(info.args, printDelg, originalOptions.scriptFileName);
                            else
                                exec.Execute(info.args, null, originalOptions.scriptFileName);
                        }

                    options = originalOptions;
                    ExecuteOptions.options = originalOptions; //update static members as well
                    Environment.CurrentDirectory = originalCurrDir;

                    options.compilationContext = CSSUtils.GenerateCompilationContext(parser, options);

                    if (options.nonExecuteOpRquest != null)
                    {
                        HandleUserNoExecuteRequests(options);
                        if (!options.processFile)
                            return;
                    }

                    //Run main script
                    //We need to start the execution in a new thread as it is the only way
                    //to set desired ApartmentState under .NET 2.0
                    Thread newThread = new Thread(new ThreadStart(this.ExecuteImpl));
                    newThread.SetApartmentState(options.apartmentState);
                    newThread.Start();
                    newThread.Join();
                    if (lastException != null)
                        if (lastException is SurrogateHostProcessRequiredException)
                            throw lastException;
                        else
                            throw new ApplicationException("Script " + options.scriptFileName + " cannot be executed.", lastException);

                    //run postscripts
                    foreach (CSharpParser.CmdScriptInfo info in cmdScripts)
                        if (!info.preScript)
                        {
                            Environment.CurrentDirectory = originalCurrDir;
                            info.args[1] = FileParser.ResolveFile(info.args[1], originalOptions.searchDirs);

                            CSExecutor exec = new CSExecutor(info.abortOnError, originalOptions);

                            if (originalOptions.DBG)
                            {
                                List<string> newArgs = new List<string>();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "dbg");
                                info.args = newArgs.ToArray();
                            }

                            if (originalOptions.verbose)
                            {
                                List<string> newArgs = new List<string>();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "verbose");
                                info.args = newArgs.ToArray();
                            }
                            if (info.abortOnError)
                            {
                                exec.Rethrow = true;
                                exec.Execute(info.args, printDelg, originalOptions.scriptFileName);
                            }
                            else
                                exec.Execute(info.args, null, originalOptions.scriptFileName);
                        }
                }
                else
                {
                    ShowVersion();
                }
            }
            catch (Surrogate86ProcessRequiredException)
            {
                throw;
            }
            catch (SurrogateHostProcessRequiredException)
            {
                throw;
            }
            catch (Exception e)
            {
                Exception ex = e;
                if (e is System.Reflection.TargetInvocationException)
                    ex = e.InnerException;

                if (rethrow)
                {
                    throw ex;
                }
                else
                {
                    Environment.ExitCode = 1;

                    if (!CSSUtils.IsRuntimeErrorReportingSuppressed)
                    {
                        if (options.reportDetailedErrorInfo && !(ex is FileNotFoundException))
                            print(ex.ToString());
                        else
                            print(ex.Message); //Mono friendly
                    }
                }
            }
        }

        /// <summary>
        /// Returns custom application config file.
        /// </summary>
        internal string GetCustomAppConfig(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    int firstScriptArg = CSSUtils.ParseAppArgs(args, this);
                    if (args.Length > firstScriptArg)
                    {
                        Settings settings = null;
                        if (options.noConfig)
                        {
                            if (options.altConfig != "")
                                settings = Settings.Load(options.altConfig); //read persistent settings from configuration file
                        }
                        else
                        {
                            settings = Settings.Load(Settings.DefaultConfigFile, true);
                        }
                        if (!options.useScriptConfig && (settings == null || settings.DefaultArguments.IndexOf(CSSUtils.Args.DefaultPrefix + "sconfig") == -1))
                            return "";

                        string script = args[firstScriptArg];
                        List<string> dirs = new List<string>();
                        string libDir = Environment.ExpandEnvironmentVariables("%CSSCRIPT_ROOT%" + Path.DirectorySeparatorChar + "lib");
                        if (!libDir.StartsWith("%"))
                            dirs.Add(libDir);

                        if (settings != null)
                            dirs.AddRange(Environment.ExpandEnvironmentVariables(settings.SearchDirs).Split(",;".ToCharArray()));

                        dirs.Add(Assembly.GetExecutingAssembly().GetAssemblyDirectoryName());

                        string[] searchDirs = dirs.ToArray();
                        script = FileParser.ResolveFile(script, searchDirs);

                        if (options.customConfigFileName == "")
                        {
                            using (var reader = new StreamReader(script)) //quickly check if the app.config was specified in the code as -sconfig argument
                            {
                                string line;
                                while (null != (line = reader.ReadLine()))
                                {
                                    line = line.Trim();
                                    if (line.Any())
                                    {
                                        if (!line.StartsWith("//css"))
                                            break;

                                        if (line.StartsWith("//css_args"))
                                        {
                                            var custom_app_config = line.Substring("//css_args".Length)
                                                                    .SplitCommandLine()
                                                                    .FirstOrDefault(x => x.StartsWith("-" + AppArgs.sconfig + ":")
                                                                                    || x.StartsWith("/" + AppArgs.sconfig + ":"));

                                            if (custom_app_config != null)
                                            {
                                                options.customConfigFileName = custom_app_config.Substring(AppArgs.sconfig.Length + 2);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (options.customConfigFileName == "none")
                            return "";

                        if (options.customConfigFileName != "")
                        {
                            return Path.Combine(Path.GetDirectoryName(script), options.customConfigFileName);
                        }

                        if (File.Exists(script + ".config"))
                        {
                            return script + ".config";
                        }
                        else if (File.Exists(Path.ChangeExtension(script, ".exe.config")))
                        {
                            return Path.ChangeExtension(script, ".exe.config");
                        }
                        else if (File.Exists(Path.ChangeExtension(script, ".config")))
                        {
                            return Path.ChangeExtension(script, ".config");
                        }
                        else
                        {
                            var defaultAppConfig = script.GetDirName().PathCombine("app.config");
                            if (File.Exists(defaultAppConfig))
                                return defaultAppConfig;
                        }
                    }
                }
            }
            catch (CLIException)
            {
                throw;
            }
            catch
            {
                //ignore the exception because it will be raised (again) and handled by the Execute method
            }
            return "";
        }

        /// <summary>
        /// Dummy 'print' to suppress displaying application messages.
        /// </summary>
        static void VoidPrint(string msg)
        {
        }

        /// <summary>
        /// This method implements compiling and execution of the script.
        /// </summary>
        public Exception lastException;

        [DllImport("ole32.dll")]
        public static extern int CoInitializeSecurity(IntPtr pVoid,
                                                      int cAuthSvc,
                                                      IntPtr asAuthSvc,
                                                      IntPtr pReserved1,
                                                      int level,
                                                      int impers,
                                                      IntPtr pAuthList,
                                                      int capabilities,
                                                      IntPtr pReserved3);

        static void ComInitSecurity(int RpcImpLevel, int EoAuthnCap)
        {
            int hr = CoInitializeSecurity(
                IntPtr.Zero,
                -1,
                IntPtr.Zero,
                IntPtr.Zero,
                0, //RpcAuthnLevel.Default
                3, //RpcImpLevel.Impersonate,
                IntPtr.Zero,
                0x40, //EoAuthnCap.DynamicCloaking
                IntPtr.Zero);

            //if (hr != 0)
            //    System.Windows.Forms.MessageBox.Show("CoInitializeSecurity failed. [" + hr + "]", "CS-Script COM Initialization");
            //else
            //    System.Windows.Forms.MessageBox.Show("CoInitializeSecurity succeeded.", "CS-Script COM Initialization");
        }

        /// <summary>
        /// This method implements compiling and execution of the script.
        /// </summary>
        void ExecuteImpl()
        {
            try
            {
                //System.Diagnostics.Debug.Assert(false);
                if (options.processFile)
                {
                    CSharpParser.InitInfo initInfo = options.initContext as CSharpParser.InitInfo;
                    if (initInfo != null && initInfo.CoInitializeSecurity)
                        ComInitSecurity(initInfo.RpcImpLevel, initInfo.EoAuthnCap);

                    if (options.local)
                        Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(options.scriptFileName));

                    if (options.verbose)
                    {
                        Console.WriteLine("> ----------------");
                        Console.WriteLine("  TragetFramework: " + options.TargetFramework);
                        Console.WriteLine("  Provider: " + options.altCompiler);
                        try
                        {
                            Console.WriteLine("  Engine: " + Assembly.GetExecutingAssembly().Location);
                        }
                        catch { }

                        try
                        {
                            Console.WriteLine(string.Format("  Console Encoding: {0} ({1}) - {2}", Console.OutputEncoding.WebName, Console.OutputEncoding.EncodingName, (Utils.IsDefaultConsoleEncoding ? "system default" : "set by engine")));
                        }
                        catch { } //will fail for windows app but pass for console apps

                        Console.WriteLine("  CurrentDirectory: " + Environment.CurrentDirectory);
                        Console.WriteLine("  NuGet manager: " + NuGet.NuGetExeView);
                        Console.WriteLine("  NuGet cache: " + NuGet.NuGetCacheView);
                        Console.WriteLine("  Executing: " + Path.GetFullPath(options.scriptFileName));
                        Console.WriteLine("  Script arguments: ");
                        for (int i = 0; i < scriptArgs.Length; i++)
                            Console.WriteLine("    " + i + " - " + scriptArgs[i]);

                        Console.WriteLine("  SearchDirectories: ");
                        {
                            int index = 0;
                            for (int i = 0; i < options.searchDirs.Length; i++)
                            {
                                var dir = options.searchDirs[i];
                                if (dir.StartsWith(Settings.dirs_section_prefix))
                                    Console.WriteLine("    " + dir, options);
                                else
                                    Console.WriteLine("    " + (index++) + " - " + dir, options);
                            }
                        }
                        Console.WriteLine("> ----------------");
                        Console.WriteLine("");
                    }

                    // The following long comment is also reflected on Wiki

                    // Execution consist of multiple stages and some of them need to be atomic and need to be synchronized system wide.
                    // Note: synchronization (concurrency control) may only be required for execution of a given script by two or more competing
                    // processes. If one process executes script_a.cs and another one executes script_b.cs then there is no need for any synchronization
                    // as the script files are different and their executions do not collide with each other.

                    // ---
                    // VALIDATION
                    // First, script should be validated: assessed for having valid already compiled up to date assembly. Validation is done
                    // by checking if the compiled assembly available at all and then comparing timestamps of the assembly and the script file.
                    // After all on checks are done all script dependencies (imports and ref assemblies) are also validated. Dependency validation is
                    // also timestamp based. For the script the dependencies are identified by parsing the script and for the assembly by extracting the
                    // dependencies metadata injected into assembly during the last compilation by the script engine.
                    // The whole validation stage is atomic and it's synchronized system wide via SystemWideLock validatingFileLock.
                    // SystemWideLock is a decorated Mutex-like system wide synchronization object: Mutex on Windows and file-lock on Linux.
                    // This stage is very fast as there is no heavy lifting to be done just comparing timestamps.
                    // Timeout is infinite as there is very little chance for the stage to hang.
                    // ---
                    // COMPILATION
                    // Next, if assembly is valid (the script hasn't been changed since last compilation) it is loaded for further execution without recompilation.
                    // Otherwise it is compiled again. Compilation stage is also atomic, so concurrent compilations (if happen) do not try to build the assembly potentially
                    // in the same location with the same name (e.g. caching). The whole validation stage is atomic and it's also synchronized system wide via SystemWideLock
                    // 'compilingFileLock'.
                    // This stage is potentially heavy. Some compilers, while being relatively fast may introduce significant startup overhead (like Roslyn).
                    // That is why caching is a preferred execution approach.
                    // Timeout is fixed as there is a chance that the third party compiler can hang.
                    // ---
                    // EXECUTION
                    // Next, the script assembly needs to be loaded/executed.This stage is extremely heavy as the execution may take infinite time depending on business logic.
                    // When the assembly file is loaded it is locked by CLR and none can delete/recreate it. Meaning that if any one is to recompile the assembly then this
                    // cannot be done until the execution is completed and CLR releases the assembly file. System wide synchronization doesn't make much sense in this case
                    // as open end waiting is not practical at all. Thus it's more practical to let the compiler throw an informative locking (access denied) exception.
                    //
                    // Note: if the assembly is loaded as in-memory file copy (options.inMemoryAsm) then the assembly locking is completely eliminated. This is in fact an extremely
                    // attractive execution model as it eliminates any problems associated with the assembly looking during the execution. The only reason why it's not activated by
                    // default is contradicts the traditional .NET loading model when the assembly loaded as a file.
                    //
                    // While execution stage cannot benefit from synchronization it is still using executingFileLock synchronization object. Though the objective is not to wait
                    // when file lock is detected but rather to detect unlocking and start compiling with a little delay. Reason for this is that (on Windows at least) CLR holds
                    // the file lock a little bit longer event after the assembly execution is finished. It is completely undocumented CLR behaver, that is hard to catch and reproduce.
                    // It has bee confirmed by the users that this work around helps in the intense concurrent "border line" scenarios. Though there is no warranty it will be still
                    // valid with any future releases of CLR. There ware no reports about this behaver on Linux.
                    // ---
                    // Synchronizing the stages via the lock object that is based on the assembly file name seems like the natural and best option. However the actual assembly name
                    // (unless caching is active) is only determined during the compilation, which needs to be synchronized (chicken egg problem). Thus script file is a more practical
                    // (and more conservative) approach for basing synchronization objects identity. Though if the need arises the assembly-based approach can be attempted.
                    // ------------------------------------------------------
                    // The concurrency model described above was an unconditional behaver until v3.16
                    // Since v3.16 concurrency model can be chosen based on the user preferences:
                    // * ConcurrencyControl.HighResolution
                    //      The model described above.
                    //
                    // * ConcurrencyControl.Standard
                    //      Due to the limited choices with the system wide named synchronization objects on Linux both Validation and Compilations stages are treated as a single stage,
                    //      controlled by a single sync object compilingFileLock.
                    //      This happens to be a good default choice for Windows as well.
                    //
                    // * ConcurrencyControl.None
                    //      All synchronization is the responsibility of the hosting environment.
                    // ------------------------------------------------------
                    // The CS_Script issue https://github.com/oleg-shilo/cs-script/issues/67 has reported problems on Linux.
                    // The change
                    //          using (SystemWideLock compilingFileLock = new SystemWideLock(options.scrptFileName, null))
                    // to
                    //          using (SystemWideLock compilingFileLock = new SystemWideLock(options.scrptFileName, "c"))
                    // seems to fix the problem.
                    //
                    // While it's not clear how the change can affect the behaver it's safe to implement it nevertheless.
                    // It does not alter the algorithm at all and if there is a chance that it can help on Linux so... be it.
                    // One thing is obvious is that the change eliminates the actual script file from the locking process
                    // and uses it's "lock mirror" instead.

                    using (SystemWideLock validatingFileLock = new SystemWideLock(options.scriptFileName, "v"))
                    using (SystemWideLock compilingFileLock = new SystemWideLock(options.scriptFileName, "c"))
                    using (SystemWideLock executingFileLock = new SystemWideLock(options.scriptFileName, "e"))
                    {
                        bool lockByCSScriptEngine = false;
                        bool lockedByCompiler = false;

                        // --- VALIDATE ---
                        switch (options.concurrencyControl)
                        {
                            case ConcurrencyControl.Standard: lockedByCompiler = !compilingFileLock.Wait(3000); break;
                            case ConcurrencyControl.HighResolution: lockByCSScriptEngine = !validatingFileLock.Wait(-1); break;
                        }

                        //GetAvailableAssembly also checks timestamps
                        string assemblyFileName = options.useCompiled ? GetAvailableAssembly(options.scriptFileName) : null;

                        if (options.useCompiled && options.useSmartCaching)
                        {
                            if (assemblyFileName != null)
                            {
                                if (MetaDataItems.IsOutOfDate(options.scriptFileName, assemblyFileName))
                                {
                                    assemblyFileName = null;
                                }
                            }
                        }

                        if (options.forceCompile && assemblyFileName != null)
                        {
                            switch (options.concurrencyControl)
                            {
                                case ConcurrencyControl.Standard: /*we already acquired compiler lock*/ break;
                                case ConcurrencyControl.HighResolution: lockedByCompiler = !compilingFileLock.Wait(3000); break;
                            }

                            //If file is still locked (lockedByCompiler == true) FileDelete will throw the exception
                            Utils.FileDelete(assemblyFileName, true);
                            assemblyFileName = null;
                        }

                        if (assemblyFileName != null)
                        {
                            //OK, there is a compiled script file and it is current.
                            //Validating stage is over as we are not going to recompile the script
                            //Release the lock immediately so other instances can do their validation if waiting.
                            validatingFileLock.Release();
                        }
                        else
                        {
                            //do not release validation lock as we will need to compile the script and any pending validations
                            //will need to wait until we finish.
                        }

                        //add searchDirs to PATH to support search path for native dlls
                        //need to do this before compilation or execution
                        string path = Environment.GetEnvironmentVariable("PATH");
                        foreach (string s in options.searchDirs)
                            path += ";" + s;

                        Environment.SetEnvironmentVariable("PATH", path);

                        //it is possible that there are fully compiled/cached and up to date script but no host compiled yet
                        string host = ScriptLauncherBuilder.GetLauncherName(assemblyFileName);
                        bool surrogateHostMissing = (options.useSurrogateHostingProcess &&
                                                     (!File.Exists(host) || !CSSUtils.HaveSameTimestamp(host, assemblyFileName)));

                        // --- COMPILE ---
                        if (options.buildExecutable || !options.useCompiled || (options.useCompiled && assemblyFileName == null) || options.forceCompile || surrogateHostMissing)
                        {
                            // Wait for other COMPILATION to complete(if any)

                            // infinite is not good here as it may block forever but continuing while the file is still locked will
                            // throw a nice informative exception
                            switch (options.concurrencyControl)
                            {
                                case ConcurrencyControl.Standard: /*we already acquired compiler lock*/ break;
                                case ConcurrencyControl.HighResolution: lockedByCompiler = !compilingFileLock.Wait(3000); break;
                            }

                            //no need to act on lockedByCompiler/lockedByHost as Compile(...) will throw the exception

                            if (!options.inMemoryAsm && Runtime.IsWin)
                            {
                                // wait for other EXECUTION to complete (if any)
                                bool lockedByHost = !executingFileLock.Wait(1000);

                                if (!lockedByHost)
                                {
                                    //!lockedByHost means that if there is a host executing the assemblyFileName script it has finished the execution
                                    //but the assemblyFileName file itself may still be locked by the IO because it's host process may be just exiting
                                    Utils.WaitForFileIdle(assemblyFileName, 1000);
                                }
                            }

                            try
                            {
                                CSSUtils.VerbosePrint("Compiling script...", options);
                                CSSUtils.VerbosePrint("", options);

                                TimeSpan initializationTime = Profiler.Stopwatch.Elapsed;
                                Profiler.Stopwatch.Reset();
                                Profiler.Stopwatch.Start();

                                assemblyFileName = Compile(options.scriptFileName);

                                if (Profiler.Stopwatch.IsRunning)
                                {
                                    Profiler.Stopwatch.Stop();
                                    TimeSpan compilationTime = Profiler.Stopwatch.Elapsed;
                                    CSSUtils.VerbosePrint("Initialization time: " + initializationTime.TotalMilliseconds + " msec", options);
                                    CSSUtils.VerbosePrint("Compilation time:    " + compilationTime.TotalMilliseconds + " msec", options);
                                    CSSUtils.VerbosePrint("> ----------------", options);
                                    CSSUtils.VerbosePrint("", options);
                                }
                            }
                            catch
                            {
                                if (!CSSUtils.IsRuntimeErrorReportingSuppressed)
                                {
                                    println("Error: Specified file could not be compiled.");
                                    if (NuGet.newPackageWasInstalled)
                                    {
                                        println("> -----");
                                        println("A new NuGet package has been installed. If some of it's " +
                                                "components are not found you may need to restart the script again.");
                                        println("> -----");
                                    }
                                }
                                throw;
                            }
                            finally
                            {
                                //release as soon as possible
                                validatingFileLock.Release();
                                compilingFileLock.Release();
                            }
                        }
                        else
                        {
                            validatingFileLock.Release();
                            compilingFileLock.Release();

                            Profiler.Stopwatch.Stop();
                            CSSUtils.VerbosePrint("  Loading script from cache...", options);
                            CSSUtils.VerbosePrint("", options);
                            CSSUtils.VerbosePrint("  Cache file: " + Environment.NewLine + "       " + assemblyFileName, options);
                            CSSUtils.VerbosePrint("> ----------------", options);
                            CSSUtils.VerbosePrint("Initialization time: " + Profiler.Stopwatch.Elapsed.TotalMilliseconds + " msec", options);
                            CSSUtils.VerbosePrint("> ----------------", options);
                        }

                        // --- EXECUTE ---
                        if (options.suppressExecution)
                        {
                            if (!options.syntaxCheck)
                                print("Created: " + assemblyFileName);
                        }
                        else
                        {
                            try
                            {
                                if (options.useSurrogateHostingProcess)
                                {
                                    throw new SurrogateHostProcessRequiredException(assemblyFileName, scriptArgs, options.startDebugger);
                                }

                                if (options.startDebugger)
                                {
                                    SaveDebuggingMetadata(options.scriptFileName);

                                    System.Diagnostics.Debugger.Launch();
                                    if (System.Diagnostics.Debugger.IsAttached)
                                    {
                                        System.Diagnostics.Debugger.Break();
                                    }
                                }

                                if (options.useCompiled || options.cleanupShellCommand != "")
                                {
                                    AssemblyResolver.CacheProbingResults = true; //it is reasonable safe to do the aggressive probing as we are executing only a single script (standalone execution not a script hosting model)

                                    //despite the name of the class the execution (assembly loading) will be in the current domain
                                    //I am just reusing some functionality of the RemoteExecutor class.

                                    RemoteExecutor executor = new RemoteExecutor(options.searchDirs);
                                    executor.ExecuteAssembly(assemblyFileName, scriptArgs, executingFileLock);
                                }
                                else
                                {
                                    //Load and execute assembly in a different domain to make it possible to unload assembly before clean up
                                    AssemblyExecutor executor = new AssemblyExecutor(assemblyFileName, "AsmExecution");
                                    executor.Execute(scriptArgs);
                                }
                            }
                            catch (SurrogateHostProcessRequiredException)
                            {
                                throw;
                            }
                            catch
                            {
                                if (!CSSUtils.IsRuntimeErrorReportingSuppressed)
                                    println("Error: Specified file could not be executed.");
                                throw;
                            }

                            //cleanup
                            if (File.Exists(assemblyFileName) && !options.useCompiled && options.cleanupShellCommand == "")
                            {
                                Utils.ClearFile(assemblyFileName);
                            }

                            if (options.cleanupShellCommand != "")
                            {
                                try
                                {
                                    string counterFile = Path.Combine(GetScriptTempDir(), "counter.txt");
                                    int prevRuns = 0;

                                    if (File.Exists(counterFile))
                                        using (StreamReader sr = new StreamReader(counterFile))
                                        {
                                            prevRuns = int.Parse(sr.ReadToEnd());
                                        }

                                    if (prevRuns > options.doCleanupAfterNumberOfRuns)
                                    {
                                        prevRuns = 1;
                                        string[] cmd = options.ExtractShellCommand(options.cleanupShellCommand);
                                        if (cmd.Length > 1)
                                            Process.Start(cmd[0], cmd[1]);
                                        else
                                            Process.Start(cmd[0]);
                                    }
                                    else
                                        prevRuns++;

                                    using (StreamWriter sw = new StreamWriter(counterFile))
                                        sw.Write(prevRuns);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Exception ex = e;
                if (e is System.Reflection.TargetInvocationException)
                    ex = e.InnerException;

                if (rethrow || e is SurrogateHostProcessRequiredException)
                {
                    lastException = ex;
                }
                else
                {
                    Environment.ExitCode = 1;
                    if (!CSSUtils.IsRuntimeErrorReportingSuppressed)
                    {
                        if (options.reportDetailedErrorInfo)
                            print(ex.ToString());
                        else
                            print(ex.Message); //Mono friendly
                    }
                }
            }
        }

        static void SaveDebuggingMetadata(string scriptFile)
        {
            var dir = Path.Combine(GetScriptTempDir(), "DbgAttach");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string dataFile = Path.Combine(dir, Process.GetCurrentProcess().Id.ToString() + ".txt");
            File.WriteAllText(dataFile, "source:" + scriptFile);

            //clean old files

            var runningProcesses = Process.GetProcesses().Select(p => p.Id);

            foreach (string file in Directory.GetFiles(dir, "*.txt"))
            {
                int procId = -1;

                if (int.TryParse(Path.GetFileNameWithoutExtension(file), out procId))
                {
                    try
                    {
                        if (!runningProcesses.Contains(procId))
                            Utils.FileDelete(file);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Compiles C# script file into assembly.
        /// </summary>
        public string Compile(string scriptFile, string assemblyFile, bool debugBuild)
        {
            if (assemblyFile != null)
                options.forceOutputAssembly = assemblyFile;
            else
            {
                string cacheFile = Path.Combine(CSExecutor.GetCacheDirectory(scriptFile), Path.GetFileName(scriptFile) + ".compiled");
                options.forceOutputAssembly = cacheFile;
            }
            if (debugBuild)
                options.DBG = true;
            return Compile(scriptFile);
        }

        #endregion Public interface...

        #region Class data...

        /// <summary>
        /// C# Script arguments array (sub array of application arguments array).
        /// </summary>
        string[] scriptArgs;

        /// <summary>
        /// Callback to print application messages to appropriate output.
        /// </summary>
        internal static PrintDelegate print;

        internal static void println(string msg)
        {
            print(msg + Environment.NewLine);
        }

        /// <summary>
        /// Container for parsed command line arguments
        /// </summary>
        static internal ExecuteOptions options = new ExecuteOptions();

        /// <summary>
        /// Flag to force to rethrow critical exceptions
        /// </summary>
        bool rethrow;

        #endregion Class data...

        #region Class methods...

        /// <summary>
        /// Constructor
        /// </summary>
        public CSExecutor()
        {
            rethrow = false;
            options = new ExecuteOptions();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public CSExecutor(bool rethrow, ExecuteOptions optionsBase)
        {
            this.rethrow = rethrow;
            options = new ExecuteOptions();

            //force to read all relative options data from the config file
            options.noConfig = optionsBase.noConfig;
            options.altConfig = optionsBase.altConfig;
        }

        public ExecuteOptions GetOptions()
        {
            return options;
        }

        /// <summary>
        /// Checks/returns if compiled C# script file (ScriptName + ".compiled") available and valid.
        /// </summary>
        internal string GetAvailableAssembly(string scripFileName)
        {
            string retval = null;

            string asmFileName = options.forceOutputAssembly;

            if (asmFileName == null || asmFileName == "")
            {
                var asmExtension = Runtime.IsMono && Runtime.IsLinux ? ".dll" : ".compiled";
                // asmExtension = ".dll"; // testing

                asmFileName = options.hideTemp != Settings.HideOptions.DoNotHide ? Path.Combine(CSExecutor.ScriptCacheDir, Path.GetFileName(scripFileName) + asmExtension) : scripFileName + ".c";
            }

            if (File.Exists(asmFileName) && File.Exists(scripFileName))
            {
                var scriptFile = new FileInfo(scripFileName);
                var asmFile = new FileInfo(asmFileName);

                if (Settings.legacyTimestampCaching)
                {
                    if (asmFile.LastWriteTime == scriptFile.LastWriteTime &&
                        asmFile.LastWriteTimeUtc == scriptFile.LastWriteTimeUtc)
                    {
                        retval = asmFileName;
                    }
                }
                else
                {
                    // the actual timestamps of the script and all its dependencies will be conducted later
                    // by analyzing the assembly script metadata
                    retval = asmFileName;
                }
            }

            return retval;
        }

        class UniqueAssemblyLocations
        {
            public static explicit operator string[](UniqueAssemblyLocations obj)
            {
                string[] retval = new string[obj.locations.Count];
                obj.locations.Values.CopyTo(retval, 0);
                return retval;
            }

            public void AddAssembly(string location)
            {
                string assemblyID = Path.GetFileName(location).ToUpperInvariant();
                if (!locations.ContainsKey(assemblyID))
                    locations[assemblyID] = location.EnsureAsmExtension();
            }

            public bool ContainsAssembly(string name)
            {
                string assemblyID = name.ToUpperInvariant();
                foreach (string key in locations.Keys)
                {
                    if (Path.GetFileNameWithoutExtension(key) == assemblyID)
                        return true;
                }
                return false;
            }

            System.Collections.Hashtable locations = new System.Collections.Hashtable();
        }

        ICodeCompiler LoadDefaultCompiler()
        {
#pragma warning disable 618
            var providerOptions = new Dictionary<string, string>();
            providerOptions["CompilerVersion"] = options.TargetFramework;
            return new CSharpCodeProvider(providerOptions).CreateCompiler();
#pragma warning restore 618
        }

        static string ExistingFile(string dir, params string[] paths)
        {
            var file = dir.PathCombine(paths);
            if (File.Exists(file))
                return file;
            return
                null;
        }

        ICodeCompiler LoadCompiler(string scriptFileName, ref string[] filesToInject)
        {
            ICodeCompiler compiler;

            if (options.altCompiler == "" || scriptFileName.EndsWith(".cs") || Path.GetExtension(scriptFileName) == "") //injection code syntax is C# compatible
            {
                if (options.InjectScriptAssemblyAttribute)
                {
                    //script may be loaded from in-memory string/code
                    bool isRealScriptFile = !scriptFileName.Contains(@"CSSCRIPT\dynamic");
                    if (isRealScriptFile)
                    {
                        filesToInject = filesToInject.Concat(new[] { CSSUtils.GetScriptedCodeAttributeInjectionCode(scriptFileName) })
                                                     .ToArray();
                    }
                }

                if (options.enableDbgPrint)
                {
                    var dbgInjectionFile = CSSUtils.GetScriptedCodeDbgInjectionCode(scriptFileName);
                    if (dbgInjectionFile != null)
                        filesToInject = filesToInject.Concat(new[] { dbgInjectionFile })
                                                     .ToArray();
                }
            }

            if (options.altCompiler == "")
                options.altCompiler = LookupDefaultRoslynCompilerFile();
            else if (options.altCompiler == "none")
                options.altCompiler = "";

            if (options.altCompiler == "")
            {
                compiler = LoadDefaultCompiler();
            }
            else
            {
                string scriptDir = Path.GetDirectoryName(Path.GetFullPath(scriptFileName));

                try
                {
                    var compilerAsmFile = LookupAltCompilerFile(options.altCompiler, scriptDir);

                    var asm = Assembly.LoadFrom(compilerAsmFile);
                    Type[] types = asm.GetModules()[0].FindTypes(Module.FilterTypeName, "CSSCodeProvider");

                    MethodInfo method = types[0].GetMethod("CreateCompilerVersion");
                    if (method != null)
                    {
                        compiler = (ICodeCompiler)method.Invoke(null, new object[] { scriptFileName, options.TargetFramework });  //the script file name may influence what compiler will be created (e.g. *.vb vs. *.cs)
                    }
                    else
                    {
                        method = types[0].GetMethod("CreateCompiler");
                        compiler = (ICodeCompiler)method.Invoke(null, new object[] { scriptFileName });  //the script file name may influence what compiler will be created (e.g. *.vb vs. *.cs)
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        //try to recover from incorrectly configured CS-Script but only if not hosted by another app
                        if (!Assembly.GetExecutingAssembly().Location.ToLower().EndsWith("csscriptlibrary.dll"))
                        {
                            string sccssdir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");

                            if (sccssdir != null)//CS-Script is installed/configured
                            {
                                var errorMessage = Environment.NewLine + "Cannot find alternative compiler (" + options.altCompiler + "). Loading default compiler instead.";

                                if (options.altCompiler.EndsWith("CSSCodeProvider.v4.6.dll"))
                                {
                                    try
                                    {
                                        var roslynProvider = options.altCompiler.Replace("CSSCodeProvider.v4.6.dll", "CSSRoslynProvider.dll");
                                        var compilerAsmFile = LookupAltCompilerFile(roslynProvider, scriptDir);
                                        errorMessage += Environment.NewLine + "However CSSRoslynProvider.dll has been detected. You may consider the latest CS-Script provider " +
                                            "'CSSRoslynProvider.dll' instead of the legacy one 'CSSCodeProvider.v4.6.dll'.";
                                    }
                                    catch { }
                                }

                                if (Directory.Exists(sccssdir) && !File.Exists(options.altCompiler)) //Invalid alt-compiler configured
                                    print(errorMessage);

                                options.altCompiler = "";
                                return LoadDefaultCompiler();
                            }
                        }
                    }
                    catch { }

                    // Debug.Assert(false);
                    throw new ApplicationException(
                        "Cannot use alternative compiler (" + options.altCompiler + "). You may want to adjust 'CSSCRIPT_DIR' " +
                        "environment variable or disable alternative compiler by setting 'useAlternativeCompiler' to empty value " +
                        "in the css_config.xml file." + Environment.NewLine + Environment.NewLine +
                        "Error Details:", ex);
                }
            }
            return compiler;
        }

        internal static string LookupAltCompilerFile(string altCompiler)
        {
            return LookupAltCompilerFile(altCompiler, null);
        }

        internal static string LookupDefaultRoslynCompilerFile()
        {
            if (Assembly.GetEntryAssembly() == Assembly.GetExecutingAssembly()) // cscs.exe but not the host application
            {
                var exeDir = Path.GetFullPath(Assembly.GetEntryAssembly().GetAssemblyDirectoryName());
                return ExistingFile(exeDir, "CSSRoslynProvider.dll") ?? "";
            }
            return "";
        }

        internal static string LookupAltCompilerFile(string altCompiler, string firstProbingDir)
        {
            if (Path.IsPathRooted(altCompiler))
            {
                //absolute path
                if (File.Exists(altCompiler))
                    return altCompiler;
            }
            else
            {
                //look in the following folders
                // 1. Script location
                // 2. Executable location
                // 3. Executable location + "Lib"
                // 4. CSScriptLibrary.dll location

                var exeDir = Path.GetFullPath(Assembly.GetEntryAssembly().GetAssemblyDirectoryName());
                var asmDir = Path.GetFullPath(Assembly.GetExecutingAssembly().GetAssemblyDirectoryName());

                var altCompilerFile = ExistingFile(firstProbingDir, altCompiler) ??
                                      ExistingFile(exeDir, altCompiler) ??
                                      ExistingFile(exeDir, "Lib", altCompiler) ??
                                      ExistingFile(asmDir, altCompiler);

                if (altCompilerFile == null && Path.GetExtension(altCompiler) == "")
                {
                    altCompiler = altCompiler + ".dll";

                    altCompilerFile = ExistingFile(firstProbingDir, altCompiler) ??
                                      ExistingFile(exeDir, altCompiler) ??
                                      ExistingFile(exeDir, "Lib", altCompiler) ??
                                      ExistingFile(asmDir, altCompiler);
                }

                if (altCompilerFile != null)
                    return altCompilerFile;
            }
            throw new ApplicationException("Cannot find alternative compiler \"" + altCompiler + "\"");
        }

        void AddReferencedAssemblies(CompilerParameters compilerParams, string scriptFileName, ScriptParser parser)
        {
            //scriptFileName is obsolete as it is now can be obtained from parser (ScriptParser.ScriptPath)
            string[] asms = AggregateReferencedAssemblies(parser);
            compilerParams.ReferencedAssemblies.AddRange(asms);
        }

        internal string[] AggregateReferencedAssemblies(ScriptParser parser)
        {
            UniqueAssemblyLocations requestedRefAsms = new UniqueAssemblyLocations();

            List<string> refAssemblies = new List<string>();
            if (options.shareHostRefAssemblies)
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        string location = asm.Location();

                        if (!File.Exists(location) || location.Contains("mscorlib"))
                            continue;

                        requestedRefAsms.AddAssembly(location);
                    }
                    catch
                    {
                        //Under ASP.NET some assemblies do not have location (e.g. dynamically built/emitted assemblies)
                        //in such case NotSupportedException will be raised

                        //In fact ignore all exceptions as we should continue if for whatever reason assembly the location cannot be obtained
                    }
                }

            Action<string> addByAsmName = asmName =>
            {
                string[] files = AssemblyResolver.FindAssembly(asmName, options.searchDirs);
                if (files.Any())
                {
                    foreach (string asm in files)
                        requestedRefAsms.AddAssembly(NormalizeGacAssemblyPath(asm));
                }
                else
                {
                    requestedRefAsms.AddAssembly(asmName);
                }
            };

            //add assemblies referenced from command line
            string[] cmdLineAsms = options.refAssemblies;
            if (!options.useSurrogateHostingProcess)
            {
                string[] defaultAsms = options.defaultRefAssemblies.Split(";,".ToCharArray()).Select(x => x.Trim()).ToArray();

                foreach (string asmName in defaultAsms.Concat(cmdLineAsms))
                    if (asmName != "")
                        addByAsmName(asmName);
            }

            if (options.enableDbgPrint)
            {
                if (Runtime.IsNet40Plus() && !Runtime.IsMono)
                {
                    addByAsmName("System.Linq"); // Implementation of System.Linq namespace
                    addByAsmName("System.Core"); // dependency of System.Linq namespace assembly
                    addByAsmName("System"); // dependency of System namespace assembly for regular expressions in dbg.cs
                }
                else
                {
                    addByAsmName("System"); // dependency of System namespace assembly for regular expressions in dbg.cs
                    addByAsmName("System.Core"); // The whole System.Linq namespace assembly
                }
            }

            //add assemblies referenced from code
            foreach (string asmName in parser.ResolvePackages())
            {
                requestedRefAsms.AddAssembly(asmName);
                options.AddSearchDir(asmName.GetDirName(), Settings.nuget_dirs_section);
            }

            AssemblyResolver.ignoreFileName = Path.GetFileNameWithoutExtension(parser.ScriptPath) + ".dll";

            //add assemblies referenced from code
            foreach (string asmName in parser.ReferencedAssemblies)
            {
                string asm = asmName.Replace("\"", "");

                if (Path.IsPathRooted(asm)) //absolute path
                {
                    //non-searchable assemblies
                    if (File.Exists(asm))
                    {
                        requestedRefAsms.AddAssembly(NormalizeGacAssemblyPath(asm));
                    }
                }
                else
                {
                    string[] files = AssemblyResolver.FindAssembly(asm, options.searchDirs);
                    if (files.Length > 0)
                    {
                        foreach (string asmFile in files)
                        {
                            requestedRefAsms.AddAssembly(NormalizeGacAssemblyPath(asmFile));
                        }
                    }
                    else
                    {
                        requestedRefAsms.AddAssembly(asm);
                    }
                }
            }

            bool disableNamespaceResolving = false;
            if (parser.IgnoreNamespaces.Length == 1 && parser.IgnoreNamespaces[0] == "*")
                disableNamespaceResolving = true;

            if (!disableNamespaceResolving)
            {
                //add local and global assemblies (if found) that have the same assembly name as a namespace
                foreach (string nmSpace in parser.ReferencedNamespaces)
                {
                    bool ignore = false; //user may nominate namespaces to be excluded from namespace-to-asm resolving
                    foreach (string ignoreNamespace in parser.IgnoreNamespaces)
                        if (ignoreNamespace == nmSpace)
                            ignore = true;

                    if (!ignore)
                    {
                        bool alreadyFound = requestedRefAsms.ContainsAssembly(nmSpace);
                        if (!alreadyFound)
                            foreach (string asm in AssemblyResolver.FindAssembly(nmSpace, options.searchDirs))
                            {
                                requestedRefAsms.AddAssembly(NormalizeGacAssemblyPath(asm));
                            }
                    }
                }
            }

            return (string[])requestedRefAsms;
        }

        string NormalizeGacAssemblyPath(string asm)
        {
            //e.g. v3.5
            string currentFramework = string.Format("v{0}.{1}", Environment.Version.Major, Environment.Version.MajorRevision);
            if (options.useSurrogateHostingProcess && options.TargetFramework != currentFramework)
            {
                if (asm.IndexOf("\\GAC_MSIL\\") != -1) //GAC assembly
                {
                    //Cannot use full path as the surrogate process may be incompatible with the
                    //found GAC assembly. For example version mismatch because Fusion works with the
                    //.NET 4.0 GAC only. However add the assembly name as the compiler under normal circumstances
                    //can resolve the most common assembly names into locations by itself.mu
                    return Path.GetFileName(asm);
                }
                else
                    return asm;
            }
            else
                return asm;
        }

        /// <summary>
        /// Compiles C# script file.
        /// </summary>
        string Compile(string scriptFileName)
        {
            // ********************************************************************************************
            // * Extremely important to keep the project building algorithm in sync with ProjectBuilder.GenerateProjectFor
            // ********************************************************************************************

            //System.Diagnostics.Debug.Assert(false);
            bool generateExe = options.buildExecutable;
            string scriptDir = Path.GetDirectoryName(scriptFileName);
            string assemblyFileName = "";

            //options may be uninitialized in case we are compiling from CSScriptLibrary
            if (options.searchDirs.Length == 0)
                options.searchDirs = new string[] { scriptDir };

            //parse source file in order to find all referenced assemblies
            //ASSUMPTION: assembly name is the same as namespace + ".dll"
            //if script doesn't follow this assumption user will need to
            //specify assemblies explicitly
            ScriptParser parser = new ScriptParser(scriptFileName, options.searchDirs);

            if (Settings.ProbingLegacyOrder)
            {
                options.searchDirs = parser.SearchDirs //parser.searchDirs may be updated as result of script parsing
                                           .ConcatWith(Assembly.GetExecutingAssembly().GetAssemblyDirectoryName())
                                           .RemoveDuplicates();
            }
            else
            {
                var local_dir = Assembly.GetExecutingAssembly().GetAssemblyDirectoryName();
                options.AddSearchDir(local_dir, Settings.local_dirs_section);

                var newDirsFromCode = parser.SearchDirs.Except(options.searchDirs);

                foreach (string dir in newDirsFromCode)
                {
                    options.AddSearchDir(dir, Settings.code_dirs_section);
                }
            }

            string[] filesToInject = new string[0];

            ICodeCompiler compiler = LoadCompiler(scriptFileName, ref filesToInject);

            CompilerParameters compilerParams = new CompilerParameters();

            foreach (string file in parser.Precompilers)
                if (options.preCompilers == "")
                    options.preCompilers = FileParser.ResolveFile(file, options.searchDirs);
                else
                    options.preCompilers += "," + FileParser.ResolveFile(file, options.searchDirs);

            if (options.compilerOptions != string.Empty)
                Utils.AddCompilerOptions(compilerParams, options.compilerOptions);

            foreach (string option in parser.CompilerOptions)
                Utils.AddCompilerOptions(compilerParams, option);

            if (options.DBG)
                Utils.AddCompilerOptions(compilerParams, "/d:DEBUG");

            Utils.AddCompilerOptions(compilerParams, "/d:CS_SCRIPT");

            compilerParams.IncludeDebugInformation = options.DBG;
            compilerParams.GenerateExecutable = generateExe;
            compilerParams.GenerateInMemory = false;
            compilerParams.WarningLevel = (options.hideCompilerWarnings ? -1 : 4);

            string[] filesToCompile = parser.FilesToCompile.RemoveDuplicates();
            PrecompilationContext context = CSSUtils.Precompile(scriptFileName, filesToCompile, options);

            if (context.NewIncludes.Count > 0)
            {
                for (int i = 0; i < context.NewIncludes.Count; i++)
                {
                    context.NewIncludes[i] = FileParser.ResolveFile(context.NewIncludes[i], options.searchDirs);
                }
                filesToCompile = filesToCompile.Concat(context.NewIncludes).ToArray();
                context.NewDependencies.AddRange(context.NewIncludes);
            }

            Utils.AddCompilerOptions(compilerParams, context.NewCompilerOptions);

            string[] additionalDependencies = context.NewDependencies.ToArray();

            compilerParams.ReferencedAssemblies.AddRange(context.NewReferences.ToArray());
            AddReferencedAssemblies(compilerParams, scriptFileName, parser);

            //add resources referenced from code

            foreach (string item in parser.ReferencedResources)
            {
                string[] tokens = item.Split(',').Select(x => x.Trim()).ToArray();
                string resFile = tokens.First();

                if (tokens.Count() > 2)
                    throw new Exception("The specified referenced resources are in unexpected format: \"" + item + "\"");

                string file = null;
                foreach (string dir in options.searchDirs)
                {
                    file = Path.IsPathRooted(resFile) ? Path.GetFullPath(resFile) : Path.Combine(dir, resFile);
                    if (File.Exists(file))
                        break;
                }

                if (file == null)
                    file = resFile;

                if (file.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                {
                    string out_name = null;
                    if (tokens.Count() > 1)
                        out_name = tokens.LastOrDefault();

                    file = CSSUtils.CompileResource(file, out_name);
                }
                Utils.AddCompilerOptions(compilerParams, "\"/res:" + file + "\""); //e.g. /res:C:\\Scripting.Form1.resources";
            }

            if (options.forceOutputAssembly != "")
            {
                assemblyFileName = options.forceOutputAssembly;
            }
            else
            {
                if (generateExe)
                {
                    assemblyFileName = Path.Combine(scriptDir, Path.GetFileNameWithoutExtension(scriptFileName) + ".exe");
                }
                else if (options.useCompiled || options.DLLExtension)
                {
                    var cachedAsmExtension = ".compiled";

                    if (Runtime.IsMono)
                        cachedAsmExtension = ".dll"; // mono cannot locate the symbols file (*.mbd) unless the assembly file is a .dll one

                    if (options.DLLExtension)
                        assemblyFileName = Path.Combine(scriptDir, Path.GetFileNameWithoutExtension(scriptFileName) + ".dll");
                    else if (options.hideTemp != Settings.HideOptions.DoNotHide)
                        assemblyFileName = Path.Combine(CSExecutor.ScriptCacheDir, Path.GetFileName(scriptFileName) + cachedAsmExtension);
                    else
                        assemblyFileName = scriptFileName + cachedAsmExtension;
                }
                else
                {
                    string tempFile = GetScriptTempFile();
                    assemblyFileName = Path.ChangeExtension(tempFile, ".dll");
                }
            }

            if (generateExe && options.buildWinExecutable)
                Utils.AddCompilerOptions(compilerParams, "/target:winexe");

            if (Path.GetExtension(assemblyFileName).ToLower() == ".pdb")
            {
                throw new ApplicationException("The specified assembly file name cannot have the reserved extension '.pdb'");
            }

            Utils.FileDelete(assemblyFileName, true);

            string dbgSymbols = Utils.DbgFileOf(assemblyFileName);

            if (options.DBG)
                if (File.Exists(dbgSymbols))
                    Utils.FileDelete(dbgSymbols);

            compilerParams.OutputAssembly = assemblyFileName;

            string outDir = Path.GetDirectoryName(Path.GetFullPath(compilerParams.OutputAssembly));
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            compilerParams.CompilerOptions.TunnelConditionalSymbolsToEnvironmentVariables();

            CompilerResults results;
            if (generateExe)
            {
                var exeCompatibleIjections = filesToInject.Where(x => !x.EndsWith(".attr.g.cs")).ToArray();
                filesToCompile = filesToCompile.ConcatWith(exeCompatibleIjections);
                results = CompileAssembly(compiler, compilerParams, filesToCompile);
            }
            else
            {
                if (filesToInject.Any())
                {
                    filesToCompile = filesToCompile.ConcatWith(filesToInject);
                }

                CSSUtils.VerbosePrint("  Output file: " + Environment.NewLine + "       " + assemblyFileName, options);
                CSSUtils.VerbosePrint("", options);

                CSSUtils.VerbosePrint("  Files to compile: ", options);
                int i = 0;
                foreach (string file in filesToCompile)
                    CSSUtils.VerbosePrint("   " + i++ + " - " + file, options);
                CSSUtils.VerbosePrint("", options);

                CSSUtils.VerbosePrint("  References: ", options);
                i = 0;
                foreach (string file in compilerParams.ReferencedAssemblies)
                    CSSUtils.VerbosePrint("   " + i++ + " - " + file, options);
                CSSUtils.VerbosePrint("> ----------------", options);

                string originalExtension = Path.GetExtension(compilerParams.OutputAssembly);
                if (originalExtension != ".dll")
                {
                    //Despite the usage of .dll file name is not required for MS C# compiler we need to do this because
                    //some compilers (Mono, VB) accept only dll or exe file extensions.
                    compilerParams.OutputAssembly = Path.ChangeExtension(compilerParams.OutputAssembly, ".dll");

                    Utils.FileDelete(compilerParams.OutputAssembly, true);

                    results = CompileAssembly(compiler, compilerParams, filesToCompile);

                    if (File.Exists(compilerParams.OutputAssembly))
                    {
                        int attempts = 0;
                        while (true)
                        {
                            //There were reports of MS C# compiler (csc.exe) not releasing OutputAssembly file
                            //after compilation finished. Thus wait a little...
                            //BTW. on Mono 1.2.4 it happens all the time
                            try
                            {
                                attempts++;

                                File.Move(compilerParams.OutputAssembly, Path.ChangeExtension(compilerParams.OutputAssembly, originalExtension));

                                break;
                            }
                            catch
                            {
                                if (attempts > 2)
                                {
                                    //yep we can get here as Mono 1.2.4 on Windows never ever releases the assembly
                                    File.Copy(compilerParams.OutputAssembly, Path.ChangeExtension(compilerParams.OutputAssembly, originalExtension), true);
                                    break;
                                }
                                else
                                    Thread.Sleep(100);
                            }
                        }
                    }
                }
                else
                {
                    Utils.FileDelete(compilerParams.OutputAssembly, true);
                    results = CompileAssembly(compiler, compilerParams, filesToCompile);
                }
            }

            if (options.syntaxCheck && File.Exists(compilerParams.OutputAssembly))
                Utils.FileDelete(compilerParams.OutputAssembly, false);

            ProcessCompilingResult(results, compilerParams, parser, scriptFileName, assemblyFileName, additionalDependencies);

            if (options.useSurrogateHostingProcess)
            {
                new ScriptLauncherBuilder().BuildSurrogateLauncher(assemblyFileName, options.TargetFramework, compilerParams, options.apartmentState, options.consoleEncoding);
            }

            return assemblyFileName;
        }

        CompilerResults CompileAssembly(ICodeCompiler compiler, CompilerParameters compilerParams, string[] filesToCompile)
        {
            //var sw = new Stopwatch();
            //sw.Start();

            // Console.WriteLine("---------------------");
            // Console.WriteLine(compilerParams.OutputAssembly);
            // Console.WriteLine("---------------------");
            // Environment.SetEnvironmentVariable("CSS_PROVIDER_TRACE", "true");

            CompilerResults retval = compiler.CompileAssemblyFromFileBatch(compilerParams, filesToCompile);
            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);

            if (!retval.Errors.HasErrors && options.postProcessor != "")
            {
                string rawAssembly = compilerParams.OutputAssembly + ".raw";
                try
                {
                    MethodInfo postProcessor = Assembly.LoadFrom(options.postProcessor)
                                                       .GetType("CSSPostProcessor", true)
                                                       .GetMethod("Process");

                    string[] refAsms = new string[compilerParams.ReferencedAssemblies.Count];
                    compilerParams.ReferencedAssemblies.CopyTo(refAsms, 0);

                    postProcessor.Invoke(null, new object[]
                    {
                        compilerParams.OutputAssembly,
                        refAsms,
                        options.searchDirs
                    });
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Cannot post-process compiled script (set UsePostProcessor to \"null\" if the problem persist)." + Environment.NewLine + e.Message);
                }
            }

            return retval;
        }

        void ProcessCompilingResult(CompilerResults results, CompilerParameters compilerParams, ScriptParser parser, string scriptFileName, string assemblyFileName, string[] additionalDependencies)
        {
            LastCompileResult = new CompilingInfo() { ScriptFile = scriptFileName, ParsingContext = parser.GetContext(), Result = results, Input = compilerParams };

            if (results.Errors.HasErrors)
            {
                var ex = CompilerException.Create(results.Errors, options.hideCompilerWarnings, options.resolveAutogenFilesRefs);

                if (ex.Message.Contains("error CS0176:") && ex.Message.Contains("'ScriptClass.main()'"))
                {
                    if (options.autoClass)
                        ex = CompilerException.Create(
                                               "Auto-class cannot have method `main(...)` with static modifier. Fix it by declaring the `main(...)` as an instance member.",
                                               scriptFileName,
                                               ex);
                }

                //error CS0121: The call is ambiguous between the following methods or properties: 'dbg_extensions.print<T>(T, params object[])' and 'dbg_extensions.print<T>(T, params object[])'
                if (ex.Message.Contains("error CS0121:") && ex.Message.Contains("dbg_extensions.print<T>(T"))
                {
                    ex = CompilerException.Create(
                                           "The problem most likely is caused by the referenced assemblies being compiled with CS-Script and `EnableDbgPrint` set to `true`. " +
                                           "The easiest way to fix the problem is to compile the assemblies with `-dbgprint:0` argument passed either from the command line or " +
                                           "directly from the script code (e.g. `//css_args -dbgprint:0`).",
                                           scriptFileName,
                                           ex);
                }

                if (options.syntaxCheck)
                {
                    Console.WriteLine("Compile: {0} error(s)", ex.ErrorCount);
                    Console.WriteLine(ex.Message.Trim());
                }
                else
                    throw ex;
            }
            else
            {
                if (options.syntaxCheck)
                {
                    Console.WriteLine("Compile: OK");
                }

                if (options.verbose)
                {
                    Console.WriteLine("  Compiler Output: ", options);
                    foreach (CompilerError err in results.Errors)
                    {
                        string file = err.FileName;
                        int line = err.Line;
                        if (options.resolveAutogenFilesRefs)
                            CSSUtils.NormaliseFileReference(ref file, ref line);
                        Console.WriteLine("  {0}({1},{2}):{3} {4} {5}", file, line, err.Column, (err.IsWarning ? "warning" : "error"), err.ErrorNumber, err.ErrorText);
                    }
                    Console.WriteLine("> ----------------", options);
                }

                string symbFileName = Utils.DbgFileOf(assemblyFileName);
                string pdbFileName = Utils.DbgFileOf(assemblyFileName, false);

                if (!options.DBG) //.pdb and imported files might be needed for the debugger
                {
                    parser.DeleteImportedFiles();
                    Utils.FileDelete(symbFileName);

                    // Roslyn always generates pdb files, even under Mono
                    if (Runtime.IsMono)
                        Utils.FileDelete(pdbFileName);
                }
                else
                {
                    if (Runtime.IsMono)
                    {
                        // Do not do conversion if option 'pdbonly' was specified on Linux. In this case PDB is portable and Linux an
                        // Mono debugger can process it.
                        bool isPdbOnlyMode = compilerParams.CompilerOptions.Contains("debug:pdbonly");

                        if (!Runtime.IsLinux || (!File.Exists(symbFileName) && !isPdbOnlyMode))
                        {
                            // Convert pdb into mdb
                            var process = new Process();
                            try
                            {
                                process.StartInfo.Arguments = "\"" + assemblyFileName + "\"";

                                if (!Runtime.IsLinux)
                                {
                                    // hide terminal window
                                    process.StartInfo.FileName = "pdb2mdb.bat";
                                    process.StartInfo.UseShellExecute = false;
                                    process.StartInfo.ErrorDialog = false;
                                    process.StartInfo.CreateNoWindow = true;
                                }
                                else
                                {
                                    process.StartInfo.FileName = "pdb2mdb";
                                }
                                process.Start();
                                process.WaitForExit();
                            }
                            catch { }

                            if (process.ExitCode == 0)
                                Utils.FileDelete(pdbFileName);
                        }
                    }
                }

                if (options.useCompiled)
                {
                    var scriptFile = new FileInfo(scriptFileName);

                    if (options.useSmartCaching)
                    {
                        var depInfo = new MetaDataItems();

                        string[] searchDirs = Utils.RemovePathDuplicates(options.searchDirs);

                        //add entry script info
                        depInfo.AddItem(scriptFileName, scriptFile.LastWriteTimeUtc, false);

                        //save imported scripts info
                        depInfo.AddItems(parser.ImportedFiles, false, searchDirs);

                        //additionalDependencies (precompilers) are warranted to be as absolute path so no need to pass searchDirs or isAssembly
                        depInfo.AddItems(additionalDependencies, false, new string[0]);

                        //save referenced local assemblies info
                        string[] newProbingDirs = depInfo.AddItems(compilerParams.ReferencedAssemblies, true, searchDirs);
                        foreach (string dir in newProbingDirs)
                            options.AddSearchDir(dir, Settings.code_dirs_section); //needed to be added at Compilation for further resolving during the Invoking stage

                        depInfo.StampFile(assemblyFileName);
                    }

                    if (Settings.legacyTimestampCaching)
                    {
                        var asmFile = new FileInfo(assemblyFileName);
                        var pdbFile = new FileInfo(symbFileName);

                        if (scriptFile.Exists && asmFile.Exists)
                        {
                            asmFile.LastWriteTimeUtc = scriptFile.LastWriteTimeUtc;
                            if (options.DBG && pdbFile.Exists)
                                pdbFile.LastWriteTimeUtc = scriptFile.LastWriteTimeUtc;
                        }
                    }
                }
            }
        }

        internal CompilingInfo LastCompileResult;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetTempFileName(string lpPathName, string lpPrefixString, uint uUnique, [Out] StringBuilder lpTempFileName);

        /// <summary>
        /// Returns the name of the temporary file in the CSSCRIPT subfolder of Path.GetTempPath().
        /// </summary>
        /// <returns>Temporary file name.</returns>
        static public string GetScriptTempFile()
        {
            lock (typeof(CSExecutor))
            {
                return Path.Combine(GetScriptTempDir(), string.Format("{0}.{1}.tmp", Process.GetCurrentProcess().Id, Guid.NewGuid()));
            }
        }

        static internal string GetScriptTempFile(string subDir)
        {
            lock (typeof(CSExecutor))
            {
                string tempDir = Path.Combine(GetScriptTempDir(), subDir);
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                return Path.Combine(tempDir, string.Format("{0}.{1}.tmp", Process.GetCurrentProcess().Id, Guid.NewGuid()));
            }
        }

        /// <summary>
        /// Returns the name of the temporary folder in the CSSCRIPT subfolder of Path.GetTempPath().
        /// <para>Under certain circumstances it may be desirable to the use the alternative location for the CS-Script temporary files.
        /// In such cases use SetScriptTempDir() to set the alternative location.
        /// </para>
        /// </summary>
        /// <returns>Temporary directory name.</returns>
        static public string GetScriptTempDir()
        {
            if (tempDir == null)
            {
                tempDir = Environment.GetEnvironmentVariable("CSS_CUSTOM_TEMPDIR");
                if (tempDir == null)
                {
                    tempDir = Path.Combine(Path.GetTempPath(), "CSSCRIPT");
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }
                }
            }
            return tempDir;
        }

        static string tempDir = null;

        /// <summary>
        /// Sets the location for the CS-Script temporary files directory.
        /// </summary>
        /// <param name="path">The path for the temporary directory.</param>
        static public void SetScriptTempDir(string path)
        {
            tempDir = path;
        }

        /// <summary>
        /// Generates the name of the cache directory for the specified script file.
        /// </summary>
        /// <param name="file">Script file name.</param>
        /// <returns>Cache directory name.</returns>
        public static string GetCacheDirectory(string file)
        {
            string commonCacheDir = Path.Combine(CSExecutor.GetScriptTempDir(), "Cache");

            string cacheDir;
            string directoryPath = Path.GetDirectoryName(Path.GetFullPath(file));
            string dirHash;
            if (!Runtime.IsLinux)
            {
                //Win is not case-sensitive so ensure, both lower and capital case path yield the same hash
                dirHash = CSSUtils.GetHashCodeEx(directoryPath.ToLower()).ToString();
            }
            else
            {
                dirHash = CSSUtils.GetHashCodeEx(directoryPath).ToString();
            }

            cacheDir = Path.Combine(commonCacheDir, dirHash);

            if (!Directory.Exists(cacheDir))
                try
                {
                    Directory.CreateDirectory(cacheDir);
                }
                catch (UnauthorizedAccessException)
                {
                    var parentDir = commonCacheDir;

                    if (!Directory.Exists(commonCacheDir))
                        parentDir = Path.GetDirectoryName(commonCacheDir); // GetScriptTempDir()

                    throw new Exception("You do not have write privileges for the CS-Script cache directory (" + parentDir + "). " +
                                        "Make sure you have sufficient privileges or use an alternative location as the CS-Script " +
                                        "temporary  directory (cscs -config:set=CustomTempDirectory=<new temp dir>)");
                }

            string infoFile = Path.Combine(cacheDir, "css_info.txt");
            if (!File.Exists(infoFile))
                try
                {
                    using (StreamWriter sw = new StreamWriter(infoFile))
                        sw.Write(Environment.Version.ToString() + Environment.NewLine + directoryPath + Environment.NewLine);
                }
                catch
                {
                    //there can be many reasons for the failure (e.g. file is already locked by another writer),
                    //which in most of the cases does not constitute the error but rather a runtime condition
                }

            return cacheDir;
        }

        ///<summary>
        /// Contains the name of the temporary cache folder in the CSSCRIPT subfolder of Path.GetTempPath(). The cache folder is specific for every script file.
        /// </summary>
        static public string ScriptCacheDir
        {
            get
            {
                return cacheDir;
            }
        }

        /// <summary>
        /// Generates the name of the temporary cache folder in the CSSCRIPT subfolder of Path.GetTempPath(). The cache folder is specific for every script file.
        /// </summary>
        /// <param name="scriptFile">script file</param>
        static public void SetScriptCacheDir(string scriptFile)
        {
            string newCacheDir = GetCacheDirectory(scriptFile); //this will also create the directory if it does not exist
            cacheDir = newCacheDir;
        }

        static string cacheDir = "";

        /// <summary>
        /// Prints Help info.
        /// </summary>
        public void ShowHelpFor(string arg)
        {
            if (print != null)
                print(HelpProvider.BuildCommandInterfaceHelp(arg));
        }

        /// <summary>
        /// Prints CS-Script specific C# syntax help info.
        /// </summary>
        public void ShowHelp(string helpType, params object[] context)
        {
            if (print != null)
                print(HelpProvider.ShowHelp(helpType, context));
        }

        /// <summary>
        /// Show sample C# script file.
        /// </summary>
        public void ShowSample(string version)
        {
            if (print != null)
                print(HelpProvider.BuildSampleCode(version));
        }

        /// <summary>
        /// Show sample precompiler C# script file.
        /// </summary>
        public void ShowPrecompilerSample()
        {
            if (print != null)
                print(HelpProvider.BuildPrecompilerSampleCode());
        }

        /// <summary>
        /// Performs the cache operations and shows the operation output.
        /// </summary>
        /// <param name="command">The command.</param>
        public void DoCacheOperations(string command)
        {
            if (print != null)
            {
                if (command == "ls")
                    print(Cache.List());
                else if (command == "trim")
                    print(Cache.Trim());
                else if (command == "clear")
                    print(Cache.Clear());
                else
                    print("Unknown cache command." + Environment.NewLine
                        + "Expected: 'cache:ls', 'cache:trim' or 'cache:clear'" + Environment.NewLine);
            }
        }

        /// <summary>
        /// Creates the default config file in the CurrentDirectory.
        /// </summary>
        public void CreateDefaultConfigFile()
        {
            string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "css_config.xml");
            new Settings().Save(file);
            print("The default config file has been created: " + file);
        }

        /// <summary>
        /// Prints the config file default content.
        /// </summary>
        public void PrintDefaultConfig()
        {
            print(new Settings().ToStringRaw());
        }

        public void PrintDecoratedAutoclass(string script)
        {
            string code = File.ReadAllText(script);

            var decorated = AutoclassPrecompiler.Process(code);

            print(decorated);
        }

        public void ProcessConfigCommand(string command)
        {
            //-config                  - lists/print current settings value
            //-config:raw              - print current config file content
            //-config:ls               - lists/print current settings value (same as simple -config)
            //-config:create           - create config file with default settings
            //-config:default          - print default settings
            //-config:get:name         - print current config file value
            //-config:set:name=value   - set current config file value
            try
            {
                if (command == "create")
                {
                    CreateDefaultConfigFile();
                }
                else if (command == "default")
                {
                    print(new Settings().ToStringRaw());
                }
                else if (command == "ls" || command == null)
                {
                    print(Settings.Load(false).ToString());
                }
                else if (command == "raw" || command == "xml")
                {
                    var currentConfig = Settings.Load(false) ?? new Settings();
                    print(currentConfig.ToStringRaw());
                }
                else if (command.StartsWith("get:"))
                {
                    string name = command.Substring(4);
                    var currentConfig = Settings.Load(false) ?? new Settings();
                    var value = currentConfig.Get(ref name);
                    print(name + ": " + value);
                }
                else if (command.StartsWith("set:"))
                {
                    // set:DefaultArguments=-ac
                    // set:roslyn
                    string name, value;

                    if (string.Compare(command, "set:roslyn", true) == 0)
                    {
                        var asmDir = Assembly.GetExecutingAssembly().GetAssemblyDirectoryName();

                        var providerFile = ExistingFile(asmDir, "CSSRoslynProvider.dll") ??
                                           ExistingFile(asmDir, "Lib", "CSSRoslynProvider.dll");

                        if (providerFile != null)
                        {
                            name = "UseAlternativeCompiler";
                            value = providerFile;
                        }
                        else
                            throw new CLIException("Cannot locate Roslyn provider CSSRoslynProvider.dll");
                    }
                    else
                    {
                        string[] tokens = command.Substring(4).Split(new char[] { '=', ':' }, 2);
                        if (tokens.Length != 2)
                            throw new CLIException("Invalid set config property expression. Must be in name 'set:<name>=<value>' format.");

                        name = tokens[0];
                        value = tokens[1].Trim().Trim('"');
                    }

                    var currentConfig = Settings.Load(true) ?? new Settings();
                    currentConfig.Set(name, value);
                    currentConfig.Save();

                    var new_value = currentConfig.Get(ref name);
                    print("set: " + name + ": " + new_value);
                }
            }
            catch (Exception e)
            {
                throw new CLIException(e.Message); //only a message, stack info for CLI is too verbose
            }
            throw new CLIExitRequest();
        }

        /// <summary>
        /// Show CS-Script version information.
        /// </summary>
        public void ShowVersion()
        {
            print(HelpProvider.BuildVersionInfo());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        public void ShowProjectFor(string arg)
        {
            throw new NotImplementedException();
        }

        #endregion Class methods...
    }
}