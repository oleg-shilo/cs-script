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
// Copyright (c) 2016 Oleg Shilo
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
using System.IO;

#if !net1

using System.Linq;

#endif

using System.Reflection;

#if net1
using System.Collections;
#else

using System.Collections.Generic;

#endif

using System.Text;
using CSScriptLibrary;
using System.Runtime.InteropServices;
using System.Threading;
using System.CodeDom.Compiler;

//using System.Windows.Forms;
using System.Globalization;
using System.Diagnostics;
using Microsoft.CSharp;
namespace csscript
{
    /// <summary>
    /// Delegate implementing source file probing algorithm.
    /// <para>The method is deprecated because it only allows returning a single file. While in reality it can be many. 
    /// For example the file and its auto-generated derivatives (e.g. *.g.cs). </para>
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="searchDirs">The extra dirs.</param>
    /// <param name="throwOnError">if set to <c>true</c> [throw on error].</param>
    /// <returns></returns>
    [Obsolete("This type is obsolete. Use ResolveSourceFileAlgorithm instead.", true)]
    public delegate string ResolveSourceFileHandler(string file, string[] searchDirs, bool throwOnError);

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

        void ShowSample();

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
        /// Force caught exceptions to be rethrown.
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
            if (AppArgs.proj == (options.nonExecuteOpRquest as string))
            {
                var project = Project.GenerateProjectFor(options.scriptFileName);
                foreach (string file in project.Files)
                    print("file:"+ file);

                foreach (string file in project.Refs)
                    print("ref:"+ file);

                foreach (string file in project.SearchDirs)
                    print("searcDir:"+ file);
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
                if (!CSSUtils.IsDynamic(Assembly.GetExecutingAssembly()))
                    settings = Settings.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "css_config.xml"));
            }
            return settings;
        }
#if net1
        Settings GetPersistedSettings(ArrayList appArgs)
#else

        Settings GetPersistedSettings(List<string> appArgs)
#endif

        {
            //read persistent settings from configuration file
            Settings settings = LoadSettings(options);

            if (settings != null)
            {
                options.hideTemp = settings.HideAutoGeneratedFiles;
                if (options.preCompilers == "") //it may be set from command-line args, which have higher precedence
                    options.preCompilers = settings.Precompiler;
                options.altCompiler = settings.ExpandUseAlternativeCompiler();
                options.defaultRefAssemblies = settings.ExpandDefaultRefAssemblies();
                options.postProcessor = settings.ExpandUsePostProcessor();
                options.apartmentState = settings.DefaultApartmentState;
                options.reportDetailedErrorInfo = settings.ReportDetailedErrorInfo;
                options.openEndDirectiveSyntax = settings.OpenEndDirectiveSyntax;
                options.consoleEncoding = settings.ConsoleEncoding;
                options.cleanupShellCommand = settings.ExpandCleanupShellCommand();
                options.doCleanupAfterNumberOfRuns = settings.DoCleanupAfterNumberOfRuns;
                options.inMemoryAsm = settings.InMemoryAssembly;

                //options.useSurrogateHostingProcess = settings.UseSurrogateHostingProcess;
                options.concurrencyControl = settings.ConcurrencyControl;
                options.hideCompilerWarnings = settings.HideCompilerWarnings;
                options.TargetFramework = settings.TargetFramework;

                //process default command-line arguments
                string[] defaultCmdArgs = settings.DefaultArguments.Split(" ".ToCharArray());
                defaultCmdArgs = Utils.RemoveEmptyStrings(defaultCmdArgs);

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
                print = printDelg != null ? printDelg : new PrintDelegate(VoidPrint);

                if (args.Length > 0)
                {
                    #region Parse command-line arguments...

                    //Here we need to separate application arguments from script ones.
                    //Script engine arguments are always followed by script arguments
                    //[appArgs][scriptFile][scriptArgs][//x]
#if net1
                    ArrayList appArgs = new ArrayList();
#else
                    List<string> appArgs = new List<string>();
#endif
                    //The following will also update corresponding "options" members from "settings" data
                    Settings settings = GetPersistedSettings(appArgs);

                    int firstScriptArg = CSSUtils.ParseAppArgs(args, this);

                    if (!options.processFile)
                    {
                        // No further processing is required. 
                        // Some primitive request (e.g. print help) has been already dispatched
                        // though some non-processing request cannot be done without using options
                        // so let them to be handleed here.
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
#if net1
                    scriptArgs = (string[])appArgs.ToArray(typeof(string));
#else
                    scriptArgs = appArgs.ToArray();
#endif

                    //searchDirs[0] is the script file directory. Set it only after
                    //the script file resolved because it can be:
                    //	dir defined by the absolute/relative script file path
                    //	"%CSSCRIPT_DIR%\lib
                    //	settings.SearchDirs
                    //  CacheDir
#if net1
                    ArrayList dirs = new ArrayList();
#else
                    List<string> dirs = new List<string>();
#endif

                    using (IDisposable currDir = new CurrentDirGuard())
                    {
                        if (options.local)
                            Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(options.scriptFileName));

                        foreach (string dir in options.searchDirs) //some directories may be already set from command-line
                            dirs.Add(Path.GetFullPath(dir));

                        if (settings != null)
                            foreach (string dir in Environment.ExpandEnvironmentVariables(settings.SearchDirs).Split(",;".ToCharArray()))
                                if (dir.Trim() != "")
                                    dirs.Add(Path.GetFullPath(dir));
                    }

                    dirs.Add(this.GetType().Assembly.GetAssemblyDirectoryName());
#if net1
                    options.scriptFileName = FileParser.ResolveFile(options.scriptFileName, (string[])dirs.ToArray(typeof(string)));
#else
                    options.scriptFileName = FileParser.ResolveFile(options.scriptFileName, dirs.ToArray());
#endif
                    if (primaryScript != null)
                        options.scriptFileNamePrimary = primaryScript;
                    else
                        options.scriptFileNamePrimary = options.scriptFileName;

                    if (CSExecutor.ScriptCacheDir == "")
                        CSExecutor.SetScriptCacheDir(options.scriptFileName);

                    dirs.Insert(0, Path.GetDirectoryName(Path.GetFullPath(options.scriptFileName)));

                    if (settings != null && settings.HideAutoGeneratedFiles != Settings.HideOptions.DoNotHide)
                        dirs.Add(CSExecutor.ScriptCacheDir);

#if net1
                    options.searchDirs = (string[])dirs.ToArray(typeof(string));
#else
                    options.searchDirs = dirs.ToArray();
#endif
                    CSharpParser.CmdScriptInfo[] cmdScripts = new CSharpParser.CmdScriptInfo[0];

                    //do quick parsing for pre/post scripts, ThreadingModel and embedded script arguments
                    CSharpParser parser = new CSharpParser(options.scriptFileName, true, null, options.searchDirs);

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
#if net1

                        ArrayList preScripts = new ArrayList(parser.CmdScripts);
                        foreach (CSharpParser.ImportInfo info in parser.Imports)
                        {
                            try
                            {
                                string file = FileParser.ResolveFile(info.file, options.searchDirs);
                                if (file.IndexOf(".g.cs") == -1) //non auto-generated file
                                    preScripts.AddRange(new CSharpParser(file, true, options.searchDirs).CmdScripts);
                            }
                            catch { } //some files may not be generated yet
                        }

                        cmdScripts = (CSharpParser.CmdScriptInfo[])preScripts.ToArray(typeof(CSharpParser.CmdScriptInfo));
#else
                        List<string> newSearchDirs = new List<string>(options.searchDirs);

                        using (IDisposable currDir = new CurrentDirGuard())
                        {
                            Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(options.scriptFileName));

                            foreach (string dir in parser.ExtraSearchDirs)
                                newSearchDirs.Add(Path.GetFullPath(dir));

                            foreach (string file in parser.RefAssemblies)
                            {
                                string path = file.Replace("\"", "");
                                string dir = Path.GetDirectoryName(path);
                                if (dir != "")
                                    newSearchDirs.Add(Path.GetFullPath(dir));
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
                            catch { } //some files may not be generated yet
                        }

                        cmdScripts = preScripts.ToArray();
#endif
                        if (primaryScript == null)//this is a primary script
                        {
                            int firstEmbeddedScriptArg = CSSUtils.ParseAppArgs(parser.Args, this);
                            if (firstEmbeddedScriptArg != -1)
                            {
                                for (int i = firstEmbeddedScriptArg; i < parser.Args.Length; i++)
                                    appArgs.Add(parser.Args[i]);
                            }
#if net1
                            scriptArgs = (string[])appArgs.ToArray(typeof(string));
#else
                            scriptArgs = appArgs.ToArray();
#endif
                        }
                    }

                    #endregion Parse command-line arguments...

                    ExecuteOptions originalOptions = (ExecuteOptions) options.Clone(); //preserve master script options
                    string originalCurrDir = Environment.CurrentDirectory;

                    //run prescripts
                    //Note: during the secondary script execution static options will be modified (this is required for
                    //browsing in CSSEnvironment with reflection). So reset it back with originalOptions after the execution is completed
                    foreach (CSharpParser.CmdScriptInfo info in cmdScripts)
                        if (info.preScript)
                        {
                            Environment.CurrentDirectory = originalCurrDir;
                            info.args[1] = FileParser.ResolveFile(info.args[1], originalOptions.searchDirs);

                            CSExecutor exec = new CSExecutor(info.abortOnError, originalOptions);

                            if (originalOptions.DBG)
                            {
#if net1
                                ArrayList newArgs = new ArrayList();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "dbg");
                                info.args = (string[])newArgs.ToArray(typeof(string));
#else
                                List<string> newArgs = new List<string>();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "dbg");
                                info.args = newArgs.ToArray();
#endif
                            }
                            if (originalOptions.verbose)
                            {
#if net1
                                ArrayList newArgs = new ArrayList();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "verbose");
                                info.args = (string[])newArgs.ToArray(typeof(string));
#else
                                List<string> newArgs = new List<string>();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "verbose");
                                info.args = newArgs.ToArray();
#endif
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
#if net1
                    newThread.ApartmentState = options.apartmentState;
#else
                    newThread.SetApartmentState(options.apartmentState);
#endif
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
#if net1
                                ArrayList newArgs = new ArrayList();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "dbg");
                                info.args = (string[])newArgs.ToArray(typeof(string));
#else

                                List<string> newArgs = new List<string>();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "dbg");
                                info.args = newArgs.ToArray();
#endif
                            }
                            if (originalOptions.verbose)
                            {
#if net1
                                ArrayList newArgs = new ArrayList();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "verbose");
                                info.args = (string[])newArgs.ToArray(typeof(string));
#else

                                List<string> newArgs = new List<string>();
                                newArgs.AddRange(info.args);
                                newArgs.Insert(0, CSSUtils.Args.DefaultPrefix + "verbose");
                                info.args = newArgs.ToArray();
#endif
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
                    ShowHelpFor(null);
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

                    if (!CSSUtils.IsRuntimeErrorReportingSupressed)
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
                                settings = Settings.Load(Path.GetFullPath(options.altConfig)); //read persistent settings from configuration file
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(Assembly.GetExecutingAssembly().Location))
                                settings = Settings.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "css_config.xml"));
                        }
                        if (!options.useScriptConfig && (settings == null || settings.DefaultArguments.IndexOf(CSSUtils.Args.DefaultPrefix + "sconfig") == -1))
                            return "";

                        string script = args[firstScriptArg];
#if net1
                        ArrayList dirs = new ArrayList();
#else
                        List<string> dirs = new List<string>();
#endif
                        string libDir = Environment.ExpandEnvironmentVariables("%CSSCRIPT_DIR%" + Path.DirectorySeparatorChar + "lib");
                        if (!libDir.StartsWith("%"))
                            dirs.Add(libDir);

                        if (settings != null)
                            dirs.AddRange(Environment.ExpandEnvironmentVariables(settings.SearchDirs).Split(",;".ToCharArray()));

                        dirs.Add(Assembly.GetExecutingAssembly().GetAssemblyDirectoryName());

#if net1
                        string[] searchDirs = (string[])dirs.ToArray(typeof(string));
#else
                        string[] searchDirs = dirs.ToArray();
#endif
                        script = FileParser.ResolveFile(script, searchDirs);
                        if (options.customConfigFileName != "")
                            return Path.Combine(Path.GetDirectoryName(script), options.customConfigFileName);
                        if (File.Exists(script + ".config"))
                            return script + ".config";
                        else if (File.Exists(Path.ChangeExtension(script, ".exe.config")))
                            return Path.ChangeExtension(script, ".exe.config");
                    }
                }
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

                    if (!options.noLogo)
                    {
                        Console.WriteLine(AppInfo.appLogo);
                    }

                    if (Environment.GetEnvironmentVariable("EntryScript") == null)
                        Environment.SetEnvironmentVariable("EntryScript", Path.GetFullPath(options.scriptFileName));

                    {
                        CSSUtils.VerbosePrint("> ----------------", options);
                        CSSUtils.VerbosePrint("  TragetFramework: " + options.TargetFramework, options);
                        CSSUtils.VerbosePrint("  Provider: " + options.altCompiler, options);
                        try
                        {
                            CSSUtils.VerbosePrint("  Engine: " + Assembly.GetExecutingAssembly().Location, options);
                        }
                        catch { }

                        try
                        {
                            CSSUtils.VerbosePrint(string.Format("  Console Encoding: {0} ({1}) - {2}", Console.OutputEncoding.WebName, Console.OutputEncoding.EncodingName, (Utils.IsDefaultConsoleEncoding ? "system default" : "set by engine")), options);
                        }
                        catch { } //will fail for windows app

                        CSSUtils.VerbosePrint("  CurrentDirectory: " + Environment.CurrentDirectory, options);

                        if (!Utils.IsLinux() && options.verbose)
                        {
                            CSSUtils.VerbosePrint("  NuGet manager: " + NuGet.NuGetExeView, options);
                            CSSUtils.VerbosePrint("  NuGet cache: " + NuGet.NuGetCacheView, options);
                        }
                        CSSUtils.VerbosePrint("  Executing: " + Path.GetFullPath(options.scriptFileName), options);
                        CSSUtils.VerbosePrint("  Script arguments: ", options);
                        for (int i = 0; i < scriptArgs.Length; i++)
                            CSSUtils.VerbosePrint("    " + i + " - " + scriptArgs[i], options);
                        CSSUtils.VerbosePrint("  SearchDirectories: ", options);

                        {
                            int offset = 0;
                            if (Path.GetFullPath(options.searchDirs[0]) != Environment.CurrentDirectory)
                            {
                                CSSUtils.VerbosePrint("    0 - " + Environment.CurrentDirectory, options);
                                offset++;
                            }
                            for (int i = 0; i < options.searchDirs.Length; i++)
                                CSSUtils.VerbosePrint("    " + (i + offset) + " - " + options.searchDirs[i], options);
                        }
                        CSSUtils.VerbosePrint("> ----------------", options);
                        CSSUtils.VerbosePrint("", options);
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
                    // Next, if assembly is valid (the script hasn't been changed since last compilation) the it is loaded for further execution without recompilation. 
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
                    //      controlled by a single synch object compilingFileLock. 
                    //      This happens to be a good default choice for Windows as well.
                    //
                    // * ConcurrencyControl.None 
                    //      All synchronization is the responsibility of the hosting environment.

                    using (SystemWideLock validatingFileLock = new SystemWideLock(options.scriptFileName, "v"))
                    using (SystemWideLock compilingFileLock = new SystemWideLock(options.scriptFileName, null))
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
#if net1
                            SetEnvironmentVariable("PATH", path);
#else
                        Environment.SetEnvironmentVariable("PATH", path);
#endif
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

                            if (!options.inMemoryAsm && !Utils.IsLinux())
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
                                if (!CSSUtils.IsRuntimeErrorReportingSupressed)
                                {
                                    print("Error: Specified file could not be compiled.\n");
                                    if (NuGet.newPackageWasInstalled)
                                    {
                                        print("> -----\nA new NuGet package has been installed. If some of it's components are not found you may need to restart the script again.\n> -----\n");
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
                            Profiler.Stopwatch.Stop();
                            CSSUtils.VerbosePrint("  Loading script from cache...", options);
                            CSSUtils.VerbosePrint("", options);
                            CSSUtils.VerbosePrint("  Cache file: \n       " + assemblyFileName, options);
                            CSSUtils.VerbosePrint("> ----------------", options);
                            CSSUtils.VerbosePrint("Initialization time: " + Profiler.Stopwatch.Elapsed.TotalMilliseconds + " msec", options);
                            CSSUtils.VerbosePrint("> ----------------", options);
                        }

                        // --- EXECUTE ---
                        if (!options.supressExecution)
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
                                if (!CSSUtils.IsRuntimeErrorReportingSupressed)
                                    print("Error: Specified file could not be executed.\n");
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
                    if (!CSSUtils.IsRuntimeErrorReportingSupressed)
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
#if !net1
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
#endif
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
        static PrintDelegate print;

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
                asmFileName = options.hideTemp != Settings.HideOptions.DoNotHide ? Path.Combine(CSExecutor.ScriptCacheDir, Path.GetFileName(scripFileName) + ".compiled") : scripFileName + ".c";

            if (File.Exists(asmFileName) && File.Exists(scripFileName))
            {
                FileInfo scriptFile = new FileInfo(scripFileName);
                FileInfo asmFile = new FileInfo(asmFileName);

                if (asmFile.LastWriteTime == scriptFile.LastWriteTime &&
                    asmFile.LastWriteTimeUtc == scriptFile.LastWriteTimeUtc)
                {
                    retval = asmFileName;
                }
            }
            return retval;
        }

        class UniqueAssemblyLocations
        {
            public static explicit operator string[] (UniqueAssemblyLocations obj)
            {
                string[] retval = new string[obj.locations.Count];
                obj.locations.Values.CopyTo(retval, 0);
                return retval;
            }

            public void AddAssembly(string location)
            {
                string assemblyID = Path.GetFileName(location).ToUpperInvariant();
                if (!locations.ContainsKey(assemblyID))
                    locations[assemblyID] = location;
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
#if net1
                return new CSharpCodeProvider().CreateCompiler();
#else
            IDictionary<string, string> providerOptions = new Dictionary<string, string>();
            providerOptions["CompilerVersion"] = options.TargetFramework;
            return new CSharpCodeProvider(providerOptions).CreateCompiler();
#endif
#pragma warning restore 618
        }

        ICodeCompiler LoadCompiler(string scriptFileName, ref string[] filesToInject)
        {
            ICodeCompiler compiler;


            if (options.InjectScriptAssemblyAttribute &&
                (options.altCompiler == "" || scriptFileName.EndsWith(".cs"))) //injection code syntax is C# compatible
            {
                //script may be loaded from in-memory string/code
                bool isRealScriptFile = !scriptFileName.Contains(@"CSSCRIPT\dynamic");
                if (isRealScriptFile)
                    filesToInject = Utils.Concat(filesToInject, CSSUtils.GetScriptedCodeAttributeInjectionCode(scriptFileName));
            }

            if (options.altCompiler == "")
            {
                compiler = LoadDefaultCompiler();
            }
            else
            {
                try
                {
                    Assembly asm = null;
                    if (Path.IsPathRooted(options.altCompiler))
                    {
                        //absolute path
                        if (File.Exists(options.altCompiler))
                            asm = Assembly.LoadFrom(options.altCompiler);
                    }
                    else
                    {
                        //look in the following folders
                        // 1. Script location
                        // 2. Executable location
                        // 3. Executable location + "Lib"
                        // 4. CSScriptLibrary.dll location
                        string probingDir = Path.GetDirectoryName(Path.GetFullPath(scriptFileName));
                        string altCompilerFile = Path.Combine(probingDir, options.altCompiler);
                        if (File.Exists(altCompilerFile))
                        {
                            asm = Assembly.LoadFrom(altCompilerFile);
                        }
                        else
                        {
                            probingDir = Path.GetFullPath(Assembly.GetExecutingAssembly().GetAssemblyDirectoryName());
                            altCompilerFile = Path.Combine(probingDir, options.altCompiler);
                            if (File.Exists(altCompilerFile))
                            {
                                asm = Assembly.LoadFrom(altCompilerFile);
                            }
                            else
                            {
                                probingDir = Path.Combine(probingDir, "Lib");
                                altCompilerFile = Path.Combine(probingDir, options.altCompiler);
                                if (File.Exists(altCompilerFile))
                                {
                                    asm = Assembly.LoadFrom(altCompilerFile);
                                }
                                else
                                {
                                    //in case of CSScriptLibrary.dll "this" is not defined in the main executable
                                    probingDir = Path.GetFullPath(this.GetType().Assembly.GetAssemblyDirectoryName());
                                    altCompilerFile = Path.Combine(probingDir, options.altCompiler);
                                    if (File.Exists(altCompilerFile))
                                    {
                                        asm = Assembly.LoadFrom(altCompilerFile);
                                    }
                                    else
                                    {
                                        throw new ApplicationException("Cannot find alternative compiler \"" + options.altCompiler + "\"");
                                    }
                                }
                            }
                        }
                    }
                    Type[] types = asm.GetModules()[0].FindTypes(Module.FilterTypeName, "CSSCodeProvider");

#if net1
                    MethodInfo method = types[0].GetMethod("CreateCompiler");
                    compiler = (ICodeCompiler)method.Invoke(null, new object[] { scriptFileName });  //the script file name may influence what compiler will be created (e.g. *.vb vs. *.cs)
#else
                    MethodInfo method = types[0].GetMethod("CreateCompilerVersion");
                    if (method != null)
                    {
                        compiler = (ICodeCompiler) method.Invoke(null, new object[] { scriptFileName, options.TargetFramework });  //the script file name may influence what compiler will be created (e.g. *.vb vs. *.cs)
                    }
                    else
                    {
                        method = types[0].GetMethod("CreateCompiler");
                        compiler = (ICodeCompiler) method.Invoke(null, new object[] { scriptFileName });  //the script file name may influence what compiler will be created (e.g. *.vb vs. *.cs)
                    }
#endif
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
                                if (Directory.Exists(sccssdir) && !File.Exists(options.altCompiler)) //Invalid alt-compiler configured
                                    print("\nCannot find alternative compiler (" + options.altCompiler + "). Loading default compiler instead.");

                                options.altCompiler = "";
                                return LoadDefaultCompiler();
                            }
                        }
                    }
                    catch { }
                    throw new ApplicationException("Cannot use alternative compiler (" + options.altCompiler + "). You may want to adjust 'CSSCRIPT_DIR' environment variable or disable alternative compiler by setting 'useAlternativeCompiler' to empty value in the css_config.xml file.\n\nError Details:", ex);
                }
            }
            return compiler;
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

#if net1
            ArrayList refAssemblies = new ArrayList();
#else
            List<string> refAssemblies = new List<string>();
#endif
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

            //add assemblies referenced from command line
            string[] cmdLineAsms = options.refAssemblies;
            if (!options.useSurrogateHostingProcess)
            {
                string[] defaultAsms = options.defaultRefAssemblies.Replace(" ", "").Split(";,".ToCharArray());

                foreach (string asmName in Utils.Concat(defaultAsms, cmdLineAsms))
                {
                    if (asmName == "")
                        continue;

                    string[] files = AssemblyResolver.FindAssembly(asmName, options.searchDirs);
                    if (files.Length > 0)
                    {
                        foreach (string asm in files)
                            requestedRefAsms.AddAssembly(NormalizeGacAssemblyPath(asm));
                    }
                    else
                    {
                        requestedRefAsms.AddAssembly(asmName);
                    }
                }
            }

            AssemblyResolver.ignoreFileName = Path.GetFileNameWithoutExtension(parser.ScriptPath) + ".dll";

            //add assemblies referenced from code
            foreach (string asmName in parser.ResolvePackages())
                requestedRefAsms.AddAssembly(asmName);

            //add assemblies referenced from code
            foreach (string asmName in parser.ReferencedAssemblies)
            {
                string asm = asmName.Replace("\"", "");

                if (Path.IsPathRooted(asm)) //absolute path
                {
                    //not-searchable assemblies
                    if (File.Exists(asm))
                        requestedRefAsms.AddAssembly(NormalizeGacAssemblyPath(asm));
                }
                else
                {
                    string[] files = AssemblyResolver.FindAssembly(asm, options.searchDirs);
                    if (files.Length > 0)
                    {
                        foreach (string asmFile in files)
                            requestedRefAsms.AddAssembly(NormalizeGacAssemblyPath(asmFile));
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
                    bool ignore = false; //user may nominate namespaces to be excluded fro namespace-to-asm resolving
                    foreach (string ignoreNamespace in parser.IgnoreNamespaces)
                        if (ignoreNamespace == nmSpace)
                            ignore = true;

                    if (!ignore)
                    {
                        bool alreadyFound = requestedRefAsms.ContainsAssembly(nmSpace);
                        if (!alreadyFound)
                            foreach (string asm in AssemblyResolver.FindAssembly(nmSpace, options.searchDirs))
                                requestedRefAsms.AddAssembly(NormalizeGacAssemblyPath(asm));
                    }
                }
            }

            return (string[]) requestedRefAsms;
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
            options.searchDirs = Utils.RemoveDuplicates(
                                 Utils.Concat(
                                        parser.SearchDirs, //parser.searchDirs may be updated as result of script parsing
                                        Assembly.GetExecutingAssembly().GetAssemblyDirectoryName()));

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
                Utils.AddCompilerOptions(compilerParams, "/d:DEBUG /d:TRACE");

            compilerParams.IncludeDebugInformation = options.DBG;
            compilerParams.GenerateExecutable = generateExe;
            compilerParams.GenerateInMemory = false;
            compilerParams.WarningLevel = (options.hideCompilerWarnings ? -1 : 4);

            string[] filesToCompile = Utils.RemoveDuplicates(parser.FilesToCompile);
            PrecompilationContext context = CSSUtils.Precompile(scriptFileName, filesToCompile, options);

            if (context.NewIncludes.Count > 0)
            {
                for (int i = 0; i < context.NewIncludes.Count; i++)
                {
                    context.NewIncludes[i] = FileParser.ResolveFile(context.NewIncludes[i], options.searchDirs);
                }
                filesToCompile = Utils.Concat(filesToCompile, context.NewIncludes.ToArray());
                context.NewDependencies.AddRange(context.NewIncludes);
            }

            string[] additionalDependencies = context.NewDependencies.ToArray();

            AddReferencedAssemblies(compilerParams, scriptFileName, parser);

            //add resources referenced from code
            foreach (string resFile in parser.ReferencedResources)
            {
                string file = null;
                foreach (string dir in options.searchDirs)
                {
                    file = Path.IsPathRooted(resFile) ? Path.GetFullPath(resFile) : Path.Combine(dir, resFile);
                    if (File.Exists(file))
                        break;
                }

                if (file == null)
                    file = resFile;

                Utils.AddCompilerOptions(compilerParams, "\"/res:" + file + "\""); //e.g. /res:C:\\Scripting.Form1.resources";
            }

            if (options.forceOutputAssembly != "")
            {
                assemblyFileName = options.forceOutputAssembly;
            }
            else
            {
                if (generateExe)
                    assemblyFileName = Path.Combine(scriptDir, Path.GetFileNameWithoutExtension(scriptFileName) + ".exe");
                else if (options.useCompiled || options.DLLExtension)
                {
                    if (options.DLLExtension)
                        assemblyFileName = Path.Combine(scriptDir, Path.GetFileNameWithoutExtension(scriptFileName) + ".dll");
                    else if (options.hideTemp != Settings.HideOptions.DoNotHide)
                        assemblyFileName = Path.Combine(CSExecutor.ScriptCacheDir, Path.GetFileName(scriptFileName) + ".compiled");
                    else
                        assemblyFileName = scriptFileName + ".compiled";
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

            string dbgSymbols = Path.ChangeExtension(assemblyFileName, ".pdb");
            if (options.DBG && File.Exists(dbgSymbols))
                Utils.FileDelete(dbgSymbols);

            compilerParams.OutputAssembly = assemblyFileName;

            string outDir = Path.GetDirectoryName(Path.GetFullPath(compilerParams.OutputAssembly));
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            //compilerParams.ReferencedAssemblies.Add(this.GetType().Assembly.Location);

            CompilerResults results;
            if (generateExe)
            {
                results = CompileAssembly(compiler, compilerParams, filesToCompile);
            }
            else
            {
                if (filesToInject.Length != 0)
                {
                    filesToCompile = Utils.Concat(filesToCompile, filesToInject);
                }

                CSSUtils.VerbosePrint("  Output file: \n       " + assemblyFileName, options);
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

                    postProcessor.Invoke(null, new object[] {
                                            compilerParams.OutputAssembly,
                                            refAsms,
                                            options.searchDirs
                                            });
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Cannot post-process compiled script (set UsePostProcessor to \"null\" if the problem persist).\n" + e.Message);
                }
            }

            return retval;
        }

        void ProcessCompilingResult(CompilerResults results, CompilerParameters compilerParams, ScriptParser parser, string scriptFileName, string assemblyFileName, string[] additionalDependencies)
        {
            LastCompileResult = results;

            if (results.Errors.HasErrors)
            {
                CompilerException ex = CompilerException.Create(results.Errors, options.hideCompilerWarnings);
                if (options.syntaxCheck)
                {
                    Console.WriteLine("Compile: {0} error(s)\n{1}", ex.ErrorCount, ex.Message.Trim());
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
                        Console.WriteLine("  {0}({1},{2}):{3} {4} {5}", err.FileName, err.Line, err.Column, (err.IsWarning ? "warning" : "error"), err.ErrorNumber, err.ErrorText);
                    Console.WriteLine("> ----------------", options);
                }

                if (!options.DBG) //.pdb and imported files might be needed for the debugger
                {
                    parser.DeleteImportedFiles();
                    string pdbFile = Path.Combine(Path.GetDirectoryName(assemblyFileName), Path.GetFileNameWithoutExtension(assemblyFileName) + ".pdb");
                    Utils.FileDelete(pdbFile);
                }

                if (options.useCompiled)
                {
                    if (options.useSmartCaching)
                    {
                        MetaDataItems depInfo = new MetaDataItems();

                        string[] searchDirs = Utils.RemovePathDuplicates(options.searchDirs);

                        //save imported scripts info
                        depInfo.AddItems(parser.ImportedFiles, false, searchDirs);

                        //additionalDependencies (precompilers) are warranted to be as absolute path so no need to pass searchDirs or isAssembly
                        depInfo.AddItems(additionalDependencies, false, new string[0]);

                        //save referenced local assemblies info
                        string[] newProbingDirs = depInfo.AddItems(compilerParams.ReferencedAssemblies, true, searchDirs);
                        foreach (string dir in newProbingDirs)
                            options.AddSearchDir(dir); //needed to be added at Compilation for further resolving during the Invoking stage

                        depInfo.StampFile(assemblyFileName);
                    }

                    FileInfo scriptFile = new FileInfo(scriptFileName);
                    FileInfo asmFile = new FileInfo(assemblyFileName);

                    if (scriptFile != null && asmFile != null)
                    {
                        asmFile.LastWriteTimeUtc = scriptFile.LastWriteTimeUtc;
                    }
                }
            }
        }

        internal CompilerResults LastCompileResult;

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
                tempDir = Path.Combine(Path.GetTempPath(), "CSSCRIPT");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
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
            if (!Utils.IsLinux())
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
                Directory.CreateDirectory(cacheDir);

            string infoFile = Path.Combine(cacheDir, "css_info.txt");
            if (!File.Exists(infoFile))
                try
                {
                    using (StreamWriter sw = new StreamWriter(infoFile))
                        sw.Write(Environment.Version.ToString() + "\n" + directoryPath + "\n");
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
            print(HelpProvider.BuildCommandInterfaceHelp(arg));
        }

        /// <summary>
        /// Prints CS-Script specific C# syntax help info.
        /// </summary>
        public void ShowHelp(string helpType, params object[] context)
        {
            print(HelpProvider.ShowHelp(helpType, context));
        }

        /// <summary>
        /// Show sample C# script file.
        /// </summary>
        public void ShowSample()
        {
            print(HelpProvider.BuildSampleCode());
        }

        /// <summary>
        /// Show sample precompiler C# script file.
        /// </summary>
        public void ShowPrecompilerSample()
        {
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
            string file = Path.GetFullPath("css_config.xml");
            new Settings().Save(file);
            print("The default config file has been created: " + file);
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