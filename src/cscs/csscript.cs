using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CSScripting;
using CSScripting.CodeDom;
using CSScriptLib;

namespace csscript
{
    /// <summary>
    /// CSExecutor is an class that implements execution of *.cs files.
    /// </summary>
    internal partial class CSExecutor : IScriptExecutor
    {
        /// <summary>
        /// Force caught exceptions to be re-thrown.
        /// </summary>
        public bool Rethrow { get; set; }

        public string WaitForInputBeforeExit { get; set; }

        internal static void HandleUserNoExecuteRequests(ExecuteOptions options)
        {
            var request = (options.nonExecuteOpRquest as string);

            if (request.StartsWith(AppArgs.publish + "|"))
            {
                var destination = request.Split('|').Last();

                if (destination.IsEmpty())
                    // destination = options.scriptFileName.GetFileNameWithoutExtension();
                    destination = options.scriptFileName.GetDirName().PathJoin("publish");

                destination.EnsureDir().DeleteDirContent();

                var project = Project.GenerateProjectFor(options.scriptFileName);

                foreach (string srcFile in project.Files.Concat(project.Refs))
                {
                    if (!srcFile.IsSharedAssembly())
                    {
                        var destFile = Path.Combine(destination, Path.GetFileName(srcFile));
                        File.Copy(srcFile, destFile, true);
                    }
                }
                print("Published: " + destination);
            }
            else if (request == AppArgs.vs || request == AppArgs.vscode)
            {
                var project = Project.GenerateProjectFor(options.scriptFileName);

                var compileParams = new CompilerParameters();
                compileParams.ReferencedAssemblies.AddRange(project.Refs);
                compileParams.GenerateExecutable = true;

                var projectFile = CSharpCompiler.CreateProject(compileParams, project.Files, ScriptVsDir);

                var envarName = request == AppArgs.vs ? "CSSCRIPT_VSEXE" : "CSSCRIPT_VSCODEEXE";
                var vs_exe = Environment.GetEnvironmentVariable(envarName);

                Process p = null;
                if (request == AppArgs.vs)
                {
                    print("Opening project: " + projectFile);
                    if (vs_exe.IsEmpty())
                    {
                        try
                        {
                            p = Process.Start("devenv", $"\"{projectFile}\"");
                        }
                        catch
                        {
                            print($"Error: you need to set environment variable '{envarName}' to the valid path to Visual Studio executable devenv.exe.");
                        }
                    }
                    else
                        p = Process.Start(vs_exe, $"\"{projectFile}\"");
                }
                else
                {
                    print("Opening script: " + options.scriptFileName);
                    if (vs_exe.IsEmpty())
                    {
                        var vscode_exe = (Runtime.IsWin ? "code.exe" : "code");
                        string ide = null;
                        if (Runtime.IsWin)
                        {
                            // C:\Program Files\Microsoft VS Code\Code.exe
                            ide = Directory.GetDirectories(SpecialFolder.ProgramFiles.GetPath(), "*")
                                           .Where(d => d.GetFileName().StartsWith("Microsoft", true))
                                           .Select(d =>
                                                   {
                                                       // current user may not have permission to read some folders
                                                       try { return Directory.GetFiles(d, vscode_exe, SearchOption.AllDirectories).FirstOrDefault(); }
                                                       catch { return null; }
                                                   })
                                           .FirstOrDefault(f => f != null);
                        }
                        else
                        {
                            "which".Run(vscode_exe, onOutput: line => ide = line);
                        }

                        try
                        {
                            // RunAsync works better than Process.Start as it stops VSCode diagnostics STD output
                            p = (ide ?? "<unknown>").RunAsync($"\"{options.scriptFileName}\"");
                            print($"Opening with auto-detected VSCode '{ide}'. You can set an alternative location with environment variable '{envarName}'");
                        }
                        catch (Exception)
                        {
                            print($"Error: you need to set environment variable '{envarName}' to the valid path " +
                                  $"to Visual Studio Code executable code{vscode_exe}.");
                        }
                    }
                    else
                        p = vs_exe.RunAsync($"\"{options.scriptFileName}\"");
                }

                if (p != null)
                    File.WriteAllText(ScriptVsDir.PathJoin("pid"), p.Id.ToString());

                Task.Run(() =>
                    Utils.CleanAbandonedProcessDirs(ScriptVsDir));
            }
            else if (request == AppArgs.proj || request == AppArgs.proj_dbg || request == AppArgs.proj_csproj)
            {
                var project = Project.GenerateProjectFor(options.scriptFileName);

                if (request == AppArgs.proj_csproj)
                {
                    var compileParams = new CompilerParameters();
                    compileParams.ReferencedAssemblies.AddRange(project.Refs);
                    compileParams.GenerateExecutable = true;

                    var projectFile = CSharpCompiler.CreateProject(compileParams, project.Files);
                    print("project:" + projectFile);
                }
                else
                {
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

        Settings LoadSettings(List<string> appArgs)
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
                options.reportDetailedErrorInfo = settings.ReportDetailedErrorInfo;
                options.openEndDirectiveSyntax = settings.OpenEndDirectiveSyntax;
                options.consoleEncoding = settings.ConsoleEncoding;
                options.compilerEngine = settings.DefaultCompilerEngine;
                options.enableDbgPrint = settings.EnableDbgPrint;
                options.inMemoryAsm = settings.InMemoryAssembly;

                //options.useSurrogateHostingProcess = settings.UseSurrogateHostingProcess;
                options.concurrencyControl = settings.ConcurrencyControl;
                options.hideCompilerWarnings = settings.HideCompilerWarnings;

                //process default command-line arguments
                string[] defaultCmdArgs = settings.DefaultArguments
                                                  .Split(" ".ToCharArray())
                                                  .Where(Utils.NotEmpty)
                                                  .ToArray();

                int firstDefaultScriptArg = this.ParseAppArgs(defaultCmdArgs);
                if (firstDefaultScriptArg != defaultCmdArgs.Length)
                {
                    options.scriptFileName = defaultCmdArgs[firstDefaultScriptArg];
                    for (int i = firstDefaultScriptArg + 1; i < defaultCmdArgs.Length; i++)
                        if (defaultCmdArgs[i].Trim().Length != 0)
                            appArgs?.Add(defaultCmdArgs[i]);
                }
            }
            return settings;
        }

        public string[] PreprocessInlineCodeArgs(string[] args)
        {
            // the first arg is warranted to be '-code'
            if (args.Last().IsHelpRequest())
                return args.ToArray();

            if (args.Count() == 1 && args.First() != $"-{AppArgs.speed}")
                return "-code ?".Split(' ');

            List<string> newArgs;

            // need to get raw unparsed CLI line so cannot use args
            var code = Environment.CommandLine;

            if (Environment.CommandLine.EndsWith($"-{AppArgs.speed}")) // speed test
            {
                // simulate `{engine_file} -code "/**/"`
                code = $@"{this.GetType().Assembly.Location} -{AppArgs.code} /**/";

                var engineArg = args.FirstOrDefault(x => x.StartsWith("-ng:") || x.StartsWith("-engine:"));

                newArgs = new List<string>();
                if (engineArg != null)
                    newArgs.Add(engineArg);
            }
            else // all args before -code
                newArgs = args.TakeWhile(a => !a.StartsWith($"-{AppArgs.code}")).ToList();

            newArgs.Add("-l:0"); // ensure the current dir is not changed to the location of the script, which is in
                                 // this case always the "snippets" directory

            int pos = code.IndexOf(AppArgs.code);

            if (pos < 0)
            {
                Console.WriteLine("Invalid input parameters. Expected '-code' argument is missing.");
                return newArgs.ToArray();
            }

            bool attchDebugger = code.EndsWith("//x", StringComparison.OrdinalIgnoreCase);
            if (attchDebugger)
                code = code.Substring(0, code.Length - 3).TrimEnd();

            code = code.Replace("-code:show", "-code")
                       .Substring(pos + (AppArgs.code.Length + 1))
                       .Replace("#``", "\"")
                       .Replace("#''", "\"")
                       .Replace("#'", "'")
                       .Replace("''", "\"")
                       .Replace("``", "\"")
                       .Replace("`", "\"")
                       .Replace("`n", "\n")
                       .Replace("#n", "\n")
                       .Replace("`r", "\r")
                       .Replace("#r", "\r")
                       .Trim(" \"".ToCharArray())
                       .Expand();

            var commonHeader =
            "   using System;" + NewLine +
            "   using System.IO;" + NewLine +
            "   using System.Collections;" + NewLine +
            "   using System.Collections.Generic;" + NewLine +
            "   using System.Linq;" + NewLine +
            "   using System.Reflection;" + NewLine +
            "   using System.Diagnostics;" + NewLine +
            "   using static dbg;" + NewLine +
            "   using static System.Environment;" + NewLine;

            var customHeaderFile = this.GetType().Assembly.Location.GetDirName().PathJoin("-code.header");

            if (File.Exists(customHeaderFile))
            {
                commonHeader = File.ReadAllLines(customHeaderFile)
                                   .Select(x => "   " + x.Trim())
                                   .JoinBy(NewLine)
                                   + $"{NewLine}// --------------------{NewLine}";
            }
            else
                try { File.WriteAllText(customHeaderFile, commonHeader); }
                catch { /* there is always a chance we can fail (e.g. because of dir permissions) */}

            code = commonHeader + code;

            if (!code.EndsWith(";"))
                code += ";";

            var script = Runtime.GetScriptTempDir()
                                .PathJoin("snippets")
                                .EnsureDir()
                                .PathJoin($"{code.GetHashCodeEx()}.{Process.GetCurrentProcess().Id}.cs");

            if (args.Contains("-code:show"))
            {
                if (args.Contains("-code:show"))
                {
                    Console.WriteLine("/*--------------------");
                    Console.WriteLine("CS-Script args:");
                    for (int i = 0; i < args.Length; i++)
                        Console.WriteLine($"  {i}: {args[i].Replace("-code:show", "-code")}");
                    Console.WriteLine("--------------------*/");
                }
                Console.WriteLine("// Interpreted C# code:");
                if (File.Exists(customHeaderFile))
                {
                    Console.WriteLine("// --------------------");
                    Console.WriteLine("// common header: " + customHeaderFile);
                }
                Console.WriteLine(code);
                Console.WriteLine("// --------------------");
                throw new CLIExitRequest();
            }

            File.WriteAllText(script, code);

            newArgs.Add(script);
            if (attchDebugger)
                newArgs.Add("//x");

            return newArgs.ToArray();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public void Execute(string[] args, PrintDelegate printDelg, string primaryScript)
        {
            try
            {
                print = printDelg ?? VoidPrint;

                if (args.Length > 0)
                {
                    // Here we need to separate application arguments from script ones.
                    // Script engine arguments are always followed by script arguments
                    // [appArgs][scriptFile][scriptArgs][//x]
                    List<string> appArgs = new List<string>();

                    // load settings from file and then process user cli args as some settings values may need to be replaced
                    // upon user request. So
                    // 1 - load settings
                    // 2 - process args and possibly update settings

                    // The following will also update corresponding "options" members from "settings" data
                    Settings settings = LoadSettings(appArgs);

                    int firstScriptArg = this.ParseAppArgs(args);

                    if (options.altConfig.HasText()) // user requested to use a non default config file, so start again.
                    {
                        appArgs.Clear();
                        settings = LoadSettings(appArgs); // will use options.altConfig as a file source
                        firstScriptArg = this.ParseAppArgs(args);
                    }

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
                        var isLastArg = i == args.Length - 1;

                        if (isLastArg && args.Last().SameAs("//x"))
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

                    using (new CurrentDirGuard())
                    {
                        if (options.local)
                            Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(options.scriptFileName));

                        dirs.AddPathIfNotThere(Environment.CurrentDirectory, Settings.local_dirs_section);
                        dirs.AddPathIfNotThere(local_dir, Settings.local_dirs_section);

                        foreach (string dir in options.searchDirs) //some directories may be already set from command-line
                        {
                            // command line dirs resolved against current dir
                            if (!dir.IsDirSectionSeparator())
                                dirs.AddPathIfNotThere(Path.GetFullPath(dir), Settings.cmd_dirs_section);
                        }

                        if (settings != null)
                            foreach (string dir in Environment.ExpandEnvironmentVariables(settings.SearchDirs).Split(",;".ToCharArray()))
                                if (dir.Trim() != "")
                                    dirs.AddPathIfNotThere(Path.GetFullPath(dir), Settings.config_dirs_section);

                        dirs.AddPathIfNotThere(host_dir, Settings.local_dirs_section);
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

                    if (settings != null)
                    {
                        if (!Settings.ProbingLegacyOrder)
                        {
                            dirs.AddPathIfNotThere(CSExecutor.ScriptCacheDir, Settings.internal_dirs_section);
                        }
                    }

                    options.searchDirs = dirs.ToArray();
                    var cmdScripts = new CSharpParser.CmdScriptInfo[0];

                    var compilerDirective = "//css_engine";
                    var compilerDirective2 = "//css_ng";
                    //do quick parsing for pre/post scripts, ThreadingModel and embedded script arguments

                    CSharpParser parser = new CSharpParser(options.scriptFileName, true, new[] { compilerDirective, compilerDirective2 }, options.searchDirs);

                    // it is either '' or 'freestyle', but not 'null' if '//css_ac' was specified
                    if (parser.AutoClassMode != null)
                    {
                        options.autoClass = true;
                    }

                    var compilerDirectives = parser.GetDirective(compilerDirective).Concat(parser.GetDirective(compilerDirective2));
                    if (compilerDirectives.Any())
                    {
                        options.compilerEngine = compilerDirectives.Last();
                    }

                    if (parser.Inits.Length != 0)
                        options.initContext = parser.Inits[0];

                    //analyses ThreadingModel to use it with execution thread
                    if (File.Exists(options.scriptFileName))
                    {
                        List<string> newSearchDirs = new List<string>(options.searchDirs);

                        using (new CurrentDirGuard())
                        {
                            Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(options.scriptFileName));

                            var code_probing_dirs = parser.ExtraSearchDirs.Select<string, string>(Path.GetFullPath);

                            foreach (string dir in code_probing_dirs)
                                newSearchDirs.AddPathIfNotThere(dir, Settings.code_dirs_section.Expand());

                            foreach (string file in parser.RefAssemblies)
                            {
                                string path = file.Replace("\"", "");
                                string dir = Path.GetDirectoryName(path);
                                if (dir != "")
                                {
                                    dir = Path.GetFullPath(dir);
                                    newSearchDirs.AddPathIfNotThere(dir, Settings.code_dirs_section);
                                }
                            }
                            options.searchDirs = newSearchDirs.ToArray();
                        }

                        var preScripts = new List<CSharpParser.CmdScriptInfo>(parser.CmdScripts);

                        foreach (CSharpParser.ImportInfo info in parser.Imports)
                        {
                            try
                            {
                                string[] files = FileParser.ResolveFiles(info.file, options.searchDirs);
                                foreach (string file in files)
                                    if (file.IndexOf(".g.cs") == -1) //non auto-generated file
                                    {
                                        using (new CurrentDirGuard())
                                        {
                                            var impParser = new CSharpParser(file, true, null, options.searchDirs);
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

                        if (primaryScript == null)//this is a primary script
                        {
                            int firstEmbeddedScriptArg = this.ParseAppArgs(parser.Args);
                            if (firstEmbeddedScriptArg != -1)
                            {
                                for (int i = firstEmbeddedScriptArg; i < parser.Args.Length; i++)
                                    appArgs.Add(parser.Args[i]);
                            }
                            scriptArgs = appArgs.ToArray();
                        }
                    }

                    var originalOptions = (ExecuteOptions)options.Clone(); //preserve master script options
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

                            var exec = new CSExecutor(info.abortOnError, originalOptions);

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

                            exec.Execute(info.args,
                                         info.abortOnError ? printDelg : null,
                                         originalOptions.scriptFileName);
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
                    ExecuteImpl();

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
                                var newArgs = new List<string>();
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
                    Settings settings = LoadSettings((List<string>)null);
                    ShowVersion();
                }
            }
            catch (CLIException)
            {
                throw;
            }
            catch (Exception e)
            {
                Exception ex = e;
                if (e is System.Reflection.TargetInvocationException)
                    ex = e.InnerException;

                if (Rethrow)
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
        /// Dummy 'print' to suppress displaying application messages.
        /// </summary>
        static void VoidPrint(string msg)
        {
        }

        /// <summary>
        /// This method implements compiling and execution of the script.
        /// </summary>
        public Exception lastException;

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
                    var initInfo = options.initContext as CSharpParser.InitInfo;

                    if (options.local)
                        Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(options.scriptFileName));

                    if (options.verbose)
                    {
                        Console.WriteLine("> ----------------");
                        Console.WriteLine("  TragetFramework: " + options.TargetFramework);
                        Console.WriteLine("  Provider: " + options.altCompiler);
                        try
                        {
                            Console.WriteLine("  Script engine: " + Assembly.GetExecutingAssembly().Location);
                        }
                        catch { }

                        if (options.compilerEngine == "csc")
                            Console.WriteLine($"  Compiler engine: {options.compilerEngine} ({Globals.csc})");
                        else
                            Console.WriteLine($"  Compiler engine: {options.compilerEngine} ({Globals.dotnet})");

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
                    // Synchronising the stages via the lock object that is based on the assembly file name seems like the natural and best option. However the actual assembly name
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

                    using (var validatingFileLock = new SystemWideLock(options.scriptFileName, "v"))
                    using (var compilingFileLock = new SystemWideLock(options.scriptFileName, "c"))
                    using (var executingFileLock = new SystemWideLock(options.scriptFileName, "e"))
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
                        foreach (string s in options.searchDirs.Except(Settings.PseudoDirItems).Distinct())
                            path += ";" + s;

                        Environment.SetEnvironmentVariable("PATH", path);

                        // --- COMPILE ---
                        if (options.buildExecutable || !options.useCompiled || (options.useCompiled && assemblyFileName == null) || options.forceCompile)
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
                                Profiler.Stopwatch.Restart();

                                assemblyFileName = Compile(options.scriptFileName);

                                if (Runtime.IsLinux && assemblyFileName.EndsWith(".exe"))
                                    assemblyFileName = assemblyFileName.RemoveAssemblyExtension();

                                Profiler.Stopwatch.Stop();

                                if (options.verbose || Utils.IsSpeedTest || options.profile)
                                {
                                    TimeSpan compilationTime = Profiler.Stopwatch.Elapsed;

                                    var pureCompilerTime = (compilationTime - initializationTime);
                                    if (Profiler.has("compiler"))
                                        pureCompilerTime = Profiler.get("compiler").Elapsed;

                                    if (options.verbose || options.profile)
                                        Console.WriteLine("> ----------------");

                                    Console.WriteLine(Profiler.EngineContext);
                                    Console.WriteLine($"Initialization time: {initializationTime.TotalMilliseconds} msec");
                                    Console.WriteLine($"Compilation time:    {pureCompilerTime.TotalMilliseconds} msec");
                                    Console.WriteLine($"Total load time:     {compilationTime.TotalMilliseconds} msec");

                                    if (options.verbose || options.profile)
                                    {
                                        Console.WriteLine("> ----------------");
                                        Console.WriteLine("");
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                if (!(e is CLIExitRequest))
                                {
                                    if (!CSSUtils.IsRuntimeErrorReportingSuppressed)
                                    {
                                        print($"Error: Specified file could not be compiled.{NewLine}");
                                        if (NuGet.newPackageWasInstalled)
                                        {
                                            print($"> -----{NewLine}A new NuGet package has been installed. If some of it's components are not found you may need to restart the script again.{NewLine}> -----{NewLine}");
                                        }
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
                            if (options.verbose || options.profile)
                            {
                                Console.WriteLine("  Loading script from cache...");
                                if (options.verbose)
                                    Console.WriteLine($"  Cache file: {NewLine}       " + assemblyFileName);
                                Console.WriteLine("> ----------------");
                                Console.WriteLine("Initialization time: " + Profiler.Stopwatch.Elapsed.TotalMilliseconds + " msec");
                                Console.WriteLine("> ----------------");
                            }
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
                                if (options.startDebugger)
                                {
                                    SaveDebuggingMetadata(options.scriptFileName);

                                    Debugger.Launch();
                                    if (Debugger.IsAttached)
                                    {
                                        Debugger.Break();
                                    }
                                }

                                if (options.useCompiled)
                                {
                                    AssemblyResolver.CacheProbingResults = true; //it is reasonable safe to do the aggressive probing as we are executing only a single script (standalone execution not a script hosting model)

                                    var executor = new LocalExecutor(options.searchDirs);
                                    executor.ExecuteAssembly(assemblyFileName, scriptArgs, executingFileLock);
                                }
                                else
                                {
                                    var executor = new AssemblyExecutor(assemblyFileName, "AsmExecution");
                                    executor.Execute(scriptArgs);
                                }
                            }
                            catch
                            {
                                if (!CSSUtils.IsRuntimeErrorReportingSuppressed)
                                    print("Error: Specified file could not be executed." + NewLine);
                                throw;
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

                if (Rethrow)
                {
                    lastException = ex;
                }
                else
                {
                    Environment.ExitCode = 1;
                    if (!CSSUtils.IsRuntimeErrorReportingSuppressed)
                    {
                        string message;

                        if (options.reportDetailedErrorInfo)
                            message = ex.ToString();
                        else
                            message = ex.Message; //Mono friendly

                        if (Runtime.IsWin && IsWpfHostingException(ex) && Assembly.GetExecutingAssembly().GetName().Name == "cscs") // console app)
                        {
                            message += $"{NewLine}{NewLine}NOTE: If you are trying to use WPF ensure you have enabled WPF support " +
                                       $"with `{Environment.GetEnvironmentVariable("ENTRY_ASM")} -wpf:enable`";
                        }

                        print(message);
                    }
                }
            }
        }

        static bool IsWpfHostingException(Exception e)
        {
            var message = e.ToString();

            if (e is System.Reflection.ReflectionTypeLoadException && (message.Contains("'System.Windows.DependencyObject'") || message.Contains("'WindowsBase,")))
                return true;
            else if (Runtime.IsWin && e.GetType().ToString().Contains("System.Windows.Markup.XamlParseException") && message.Contains("'System.Windows.Controls.UIElementCollection'"))
                return true;
            else
                return false;
        }

        static void SaveDebuggingMetadata(string scriptFile)
        {
            var dir = Path.Combine(Runtime.GetScriptTempDir(), "DbgAttach");
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
                string cacheFile = Path.Combine(CSExecutor.GetCacheDirectory(scriptFile), Path.GetFileName(scriptFile) + ".dll");
                options.forceOutputAssembly = cacheFile;
            }
            if (debugBuild)
                options.DBG = true;
            return Compile(scriptFile);
        }

        /// <summary>
        /// C# Script arguments array (sub array of application arguments array).
        /// </summary>
        string[] scriptArgs;

        /// <summary>
        /// Callback to print application messages to appropriate output.
        /// </summary>
        internal static PrintDelegate print;

        /// <summary>
        /// Container for parsed command line arguments
        /// </summary>
        static internal ExecuteOptions options = new ExecuteOptions();

        public CSExecutor()
        { }

        public CSExecutor(bool rethrow, ExecuteOptions optionsBase)
        {
            this.Rethrow = rethrow;

            //force to read all relative options data from the config file
            options.noConfig = optionsBase.noConfig;
            options.altConfig = optionsBase.altConfig;
        }

        public ExecuteOptions GetOptions() => options;

        /// <summary>
        /// Checks/returns if compiled C# script file (ScriptName + ".dll") available and valid.
        /// </summary>
        internal string GetAvailableAssembly(string scripFileName)
        {
            string retval = null;

            string asmFileName = options.forceOutputAssembly;

            if (asmFileName == null || asmFileName == "")
            {
                asmFileName = CSExecutor.ScriptCacheDir.PathJoin(scripFileName.GetFileName() + ".dll");
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

        ICodeCompiler LoadDefaultCompiler()
        {
            return CSharpCompiler.Create();
        }

        static string ExistingFile(string dir, params string[] paths)
        {
            var file = dir.PathJoin(paths);
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

                    var asm = Assembly.LoadFile(compilerAsmFile);
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
                        var errorMessage = $"{NewLine}Cannot find alternative compiler (" + options.altCompiler + "). Loading default compiler instead.";

                        if (!File.Exists(options.altCompiler)) //Invalid alt-compiler configured
                            print(errorMessage);

                        options.altCompiler = "";
                        return LoadDefaultCompiler();
                    }
                    catch { }

                    // Debug.Assert(false);
                    throw new ApplicationException($"Cannot use alternative compiler (" + options.altCompiler + $"). You may want to adjust 'CSSCRIPT_ROOT' environment variable or disable alternative compiler by setting 'useAlternativeCompiler' to empty value in the css_config.xml file.{NewLine}{NewLine}Error Details:", ex);
                }
            }
            return compiler;
        }

        internal static string LookupAltCompilerFile(string altCompiler) => LookupAltCompilerFile(altCompiler, null);

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

            void addByAsmName(string asmName)
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
            }

            //add assemblies referenced from command line
            string[] cmdLineAsms = options.refAssemblies;

            string[] defaultAsms = options.defaultRefAssemblies.Split(";,".ToCharArray()).Select(x => x.Trim()).ToArray();

            foreach (string asmName in defaultAsms.Concat(cmdLineAsms))
                if (asmName != "")
                    addByAsmName(asmName);

            if (options.enableDbgPrint)
            {
                if (Utils.IsNet40Plus())
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

            //add assemblies/packages referenced from code
            foreach (string asmName in parser.ResolvePackages())
            {
                requestedRefAsms.AddAssembly(asmName);
                options.AddSearchDir(asmName.GetDirName(), Settings.code_dirs_section);
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
            // if no request to build executable or dll is made then use exe format as it is the only format that allows
            // top-level statements (classless scripts)
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
                                           .Distinct();
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

            if (options.DBG && !(Runtime.IsCore || CSharpCompiler.DefaultCompilerRuntime == DefaultCompilerRuntime.Standard))
                Utils.AddCompilerOptions(compilerParams, "/d:DEBUG /d:TRACE");

            compilerParams.IncludeDebugInformation = options.DBG;
            compilerParams.GenerateExecutable = !options.compileDLL; // user asked to execute script but we still need to generate the exe assembly before
                                                                     // the execution so top-level classes are supported
            compilerParams.BuildExe = options.buildExecutable; // user asked to build exe
            compilerParams.GenerateInMemory = false;
            compilerParams.WarningLevel = (options.hideCompilerWarnings ? -1 : 4);

            string[] filesToCompile = parser.FilesToCompile.Distinct();
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
                    if (Runtime.IsLinux)
                        assemblyFileName = Path.Combine(scriptDir, Path.GetFileNameWithoutExtension(scriptFileName));
                    else
                        assemblyFileName = Path.Combine(scriptDir, Path.GetFileNameWithoutExtension(scriptFileName) + ".exe");
                }
                else if (options.useCompiled || options.compileDLL)
                {
                    if (options.compileDLL)
                        assemblyFileName = Path.Combine(scriptDir, Path.GetFileNameWithoutExtension(scriptFileName) + ".dll");
                    else
                        assemblyFileName = Path.Combine(CSExecutor.ScriptCacheDir, Path.GetFileName(scriptFileName) + ".dll");
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

                CSSUtils.VerbosePrint($"  Output file: {NewLine}       " + assemblyFileName, options);
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
                                    // yep we can get here as Mono 1.2.4 on Windows never ever releases the assembly
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

            foreach (var new_dir in results.ProbingDirs)
                options.AddSearchDir(new_dir, Settings.internal_dirs_section);

            if (options.syntaxCheck && File.Exists(compilerParams.OutputAssembly))
                Utils.FileDelete(compilerParams.OutputAssembly, false);

            ProcessCompilingResult(results, compilerParams, parser, scriptFileName, assemblyFileName, additionalDependencies);

            return assemblyFileName;
        }

        CompilerResults CompileAssembly(ICodeCompiler compiler, CompilerParameters compilerParams, string[] filesToCompile)
        {
            return compiler.CompileAssemblyFromFileBatch(compilerParams, filesToCompile);
        }

        void ProcessCompilingResult(CompilerResults results, CompilerParameters compilerParams, ScriptParser parser, string scriptFileName, string assemblyFileName, string[] additionalDependencies)
        {
            LastCompileResult = new CompilingInfo() { ScriptFile = scriptFileName, ParsingContext = parser.GetContext(), Result = results, Input = compilerParams };

            if (results.Errors.HasErrors())
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
                    Console.WriteLine("Compile: {0} error(s)" + NewLine + "{1}", ex.ErrorCount, ex.Message.Trim());
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
                    if (results.Errors.Any())
                        Console.WriteLine("    Compiler Output: ");
                    foreach (CompilerError err in results.Errors)
                    {
                        string file = err.FileName;
                        int line = err.Line;
                        if (options.resolveAutogenFilesRefs)
                            CoreExtensions.NormaliseFileReference(ref file, ref line);
                        Console.WriteLine("  {0}({1},{2}):{3} {4} {5}", file, line, err.Column, (err.IsWarning ? "warning" : "error"), err.ErrorNumber, err.ErrorText);
                    }
                    Console.WriteLine("> ----------------");
                }

                string pdbFileName = Utils.DbgFileOf(assemblyFileName);

                if (!options.DBG) //.pdb and imported files might be needed for the debugger
                {
                    parser.DeleteImportedFiles();
                    Utils.FileDelete(pdbFileName);
                }

                if (options.useCompiled)
                {
                    var scriptFile = new FileInfo(scriptFileName);

                    if (options.useSmartCaching)
                    {
                        var depInfo = new MetaDataItems();

                        string[] searchDirs = options.searchDirs.RemovePathDuplicates();

                        //add entry script info
                        depInfo.AddItem(scriptFileName, scriptFile.LastWriteTimeUtc, false);

                        //save imported scripts info
                        depInfo.AddItems(parser.ImportedFiles, false, searchDirs);

                        //additionalDependencies (precompilers) are warranted to be as absolute path so no need to pass searchDirs or isAssembly
                        depInfo.AddItems(additionalDependencies, false, new string[0]);

                        //save referenced local assemblies info
                        string[] newProbingDirs = depInfo.AddItems(compilerParams.ReferencedAssemblies.ToArray(), true, searchDirs);

                        //needed to be added at Compilation for further resolving during the Invoking stage
                        foreach (string dir in newProbingDirs)
                            options.AddSearchDir(dir, Settings.code_dirs_section);

                        //save new probing dirs found by compilation (e.g. nuget)
                        string[] extraProbingDirs = depInfo.AddItems(results.ProbingDirs.Select(x => "package_dir:" + x).ToArray(), true, searchDirs);

                        depInfo.StampFile(assemblyFileName);
                    }

                    if (Settings.legacyTimestampCaching)
                    {
                        var asmFile = new FileInfo(assemblyFileName);
                        var pdbFile = new FileInfo(pdbFileName);

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

        /// <summary>
        /// Returns the name of the temporary file in the CSSCRIPT subfolder of Path.GetTempPath().
        /// </summary>
        /// <returns>Temporary file name.</returns>
        static public string GetScriptTempFile()
        {
            lock (typeof(CSExecutor))
            {
                return Path.Combine(Runtime.GetScriptTempDir(), string.Format("{0}.{1}.tmp", Process.GetCurrentProcess().Id, Guid.NewGuid()));
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
            => Runtime.GetScriptTempDir();

        static string tempDir = null;

        /// <summary>
        /// Sets the location for the CS-Script temporary files directory.
        /// </summary>
        /// <param name="path">The path for the temporary directory.</param>
        static public void SetScriptTempDir(string path) => tempDir = path;

        /// <summary>
        /// Generates the name of the cache directory for the specified script file.
        /// </summary>
        /// <param name="file">Script file name.</param>
        /// <returns>Cache directory name.</returns>
        public static string GetCacheDirectory(string file)
        {
            string commonCacheDir = Path.Combine(Runtime.GetScriptTempDir(), "cache");

            string cacheDir;
            string directoryPath = Path.GetDirectoryName(Path.GetFullPath(file));
            string dirHash;
            if (Runtime.IsWin)
            {
                //Win is not case-sensitive so ensure, both lower and capital case path yield the same hash
                dirHash = directoryPath.ToLower().GetHashCodeEx().ToString();
            }
            else
            {
                dirHash = directoryPath.GetHashCodeEx().ToString();
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
                    using var sw = new StreamWriter(infoFile);
                    sw.Write(Environment.Version.ToString() + NewLine + directoryPath + NewLine);
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
        static public string ScriptCacheDir { get; set; } = "";

        // "<temp_dir>\csscript.core\cache\<script_hash>"
        static internal string ScriptVsDir => CSExecutor.ScriptCacheDir.Replace("cache", ".vs");

        /// <summary>
        /// Generates the name of the temporary cache folder in the CSSCRIPT subfolder of Path.GetTempPath(). The cache folder is specific for every script file.
        /// </summary>
        /// <param name="scriptFile">script file</param>
        static public void SetScriptCacheDir(string scriptFile)
        {
            string newCacheDir = GetCacheDirectory(scriptFile); //this will also create the directory if it does not exist
            ScriptCacheDir = newCacheDir;
        }
    }
}