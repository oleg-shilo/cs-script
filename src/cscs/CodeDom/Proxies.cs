using System;
using System.Collections.Generic;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.StringComparison;
using static CSScripting.PathExtensions;
using System.Text;
using System.Threading;
using System.Xml.Linq;

#if class_lib
using CSScriptLib;
#else

using csscript;
using static csscript.CoreExtensions;

#endif

namespace CSScripting.CodeDom
{
    public enum DefaultCompilerRuntime
    {
        Standard,
        Host
    }

    public class CSharpCompiler : ICodeCompiler
    {
        public static ICodeCompiler Create()
        {
            return new CSharpCompiler();
        }

        public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit)
        {
            throw new NotImplementedException();
        }

        public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits)
        {
            throw new NotImplementedException();
        }

        public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName)
        {
            return CompileAssemblyFromFileBatch(options, new[] { fileName });
        }

        public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
        {
            throw new NotImplementedException();
        }

        static string dotnet { get; } = "dotnet";

        static string InitBuildTools(string fileType)
        {
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var cache_root = cache_dir.GetDirName();
            var build_root = cache_root.GetDirName().PathJoin("build").EnsureDir();

            (string projectName, string language) = fileType.MapValue((".cs", to => ("build.csproj", "C#")),
                                                                      (".vb", to => ("build.vbproj", "VB")));

            var proj_template = build_root.PathJoin($"build{fileType}proj");

            if (!File.Exists(proj_template))
            {
                dotnet.Run($"new console -lang {language}", build_root);
                build_root.PathJoin($"Program{fileType}").DeleteIfExists();
                Directory.Delete(build_root.PathJoin("obj"), true);
            }

            if (!File.Exists(proj_template)) // sdk may not be available
            {
                File.WriteAllLines(proj_template, new[]
                {
                    "<Project Sdk=\"Microsoft.NET.Sdk\">",
                    "  <PropertyGroup>",
                    "    <OutputType>Exe</OutputType>",
                    "    <TargetFramework>net5.0</TargetFramework>",
                    "  </PropertyGroup>",
                    "</Project>"
                });
            }

            return proj_template;
        }

        public static DefaultCompilerRuntime DefaultCompilerRuntime = DefaultCompilerRuntime.Host;

        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            if (options.BuildExe)
                return CompileAssemblyFromFileBatch_with_Build(options, fileNames); // csc.exe does not support building self sufficient executables
            else
                switch (CSExecutor.options.compilerEngine)
                {
                    case Directives.compiler_dotnet:
                        return CompileAssemblyFromFileBatch_with_Build(options, fileNames);

                    case Directives.compiler_csc:
                        return CompileAssemblyFromFileBatch_with_Csc(options, fileNames);

                    default:
                        // return CompileAssemblyFromFileBatch_with_Csc(options, fileNames);
                        return CompileAssemblyFromFileBatch_with_Build(options, fileNames);
                }
        }

        class FileWatcher
        {
            public static string WaitForCreated(string dir, string filter, int timeout)
            {
                var result = new FileSystemWatcher(dir, filter).WaitForChanged(WatcherChangeTypes.Created, timeout);
                return result.TimedOut ? null : Path.Combine(dir, result.Name);
            }
        }

        static public string BuildOnServer(string[] args)
        {
            string jobQueue = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                   "cs-script", "compiler", "1.0.0.0", "queue");

            Directory.CreateDirectory(jobQueue);
            var requestName = $"{Guid.NewGuid()}.rqst";
            var responseName = Path.ChangeExtension(requestName, ".resp");

            Directory.CreateDirectory(jobQueue);
            var request = Path.Combine(jobQueue, requestName);

            // first arg is the compiler identifier: csc|vbc
            File.WriteAllLines(request, args.Skip(1));

            string responseFile = FileWatcher.WaitForCreated(jobQueue, responseName, timeout: 5 * 60 * 1000);

            if (responseFile != null)
                return File.ReadAllText(responseFile);
            else
                return "Error: cannot process compile request on CS-Script build server ";
        }

        CompilerResults CompileAssemblyFromFileBatch_with_Csc(CompilerParameters options, string[] fileNames)
        {
            if (fileNames.Any() && fileNames.First().GetExtension().SameAs(".vb"))
                throw new CompilerException("Executing VB scripts is only supported on dotnet engine. Please either set it:" + NewLine +
                    " - as global setting with [css -config:set:DefaultCompilerEngine=dotnet]" + NewLine +
                    " - from CLI parameters with [css -ng:dotnet <scriupt.vb>]" + NewLine +
                    " - from your VB script code with [' //css_ng dotnet]");

            string projectName = fileNames.First().GetFileName();

            var engine_dir = this.GetType().Assembly.Location.GetDirName();
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = cache_dir.PathJoin(".build", projectName);

            build_dir.DeleteDir(handleExceptions: true)
                     .EnsureDir();

            var sources = new List<string>();

            fileNames.ForEach((string source) =>
                {
                    // As per dotnet.exe v2.1.26216.3 the pdb get generated as PortablePDB, which is the only format that is supported
                    // by both .NET debugger (VS) and .NET Core debugger (VSCode).

                    // However PortablePDB does not store the full source path but file name only (at least for now). It works fine in typical
                    // .Core scenario where the all sources are in the root directory but if they are not (e.g. scripting or desktop app) then
                    // debugger cannot resolve sources without user input.

                    // The only solution (ugly one) is to inject the full file path at startup with #line directive

                    var new_file = build_dir.PathJoin(source.GetFileName());
                    var sourceText = $"#line 1 \"{source}\"{Environment.NewLine}" + File.ReadAllText(source);
                    File.WriteAllText(new_file, sourceText);
                    sources.Add(new_file);
                });

            var ref_assemblies = options.ReferencedAssemblies.Where(x => !x.IsSharedAssembly())
                                                             .Where(Path.IsPathRooted)
                                                             .Where(asm => asm.GetDirName() != engine_dir)
                                                             .ToList();

            if (CSExecutor.options.enableDbgPrint)
                ref_assemblies.Add(Assembly.GetExecutingAssembly().Location());

            var refs = new StringBuilder();
            var assembly = build_dir.PathJoin(projectName + ".dll");

            var result = new CompilerResults();

            var needCompileXaml = fileNames.Any(x => x.EndsWith(".xaml", OrdinalIgnoreCase));
            if (needCompileXaml)
            {
                result.Errors.Add(new CompilerError
                {
                    ErrorText = $"In order to compile XAML you need to use 'dotnet' compiler. " + NewLine +
                                $"You can set it by any of this methods:" + NewLine +
                                $"- for a specific script from code with \"//css_engine dotnet\" directive" + NewLine +
                                $"- for the process with a CLI argument \"dotnet .{Path.DirectorySeparatorChar}cscs.dll -engine:dotnet <script>\"" + NewLine +
                                $"- globally as a config value \"dotnet .{Path.DirectorySeparatorChar}cscs.dll -config:set:DefaultCompilerEngine=dotnet\""
                }); ;
                return result;
            }

            if (!options.GenerateExecutable || !Runtime.IsCore || DefaultCompilerRuntime == DefaultCompilerRuntime.Standard)
            {
                // todo
                // nothing for now
            }

            //----------------------------

            //pseudo-gac as .NET core does not support GAC but rather common assemblies.
            var gac = typeof(string).Assembly.Location.GetDirName();

            var refs_args = new List<string>();
            var source_args = new List<string>();
            var common_args = new List<string>();

            common_args.Add("/utf8output");
            common_args.Add("/nostdlib+");

            common_args.Add("/t:exe"); // need always build exe so "top-class" feature is supported even when building dlls

            if (options.IncludeDebugInformation)
                common_args.Add("/debug:portable");  // on .net full it is "/debug+"

            if (options.CompilerOptions.HasText())
                common_args.Add(options.CompilerOptions);

            common_args.Add("-define:TRACE;NETCORE;CS_SCRIPT");

            var gac_asms = Directory.GetFiles(gac, "System.*.dll").ToList();
            gac_asms.AddRange(Directory.GetFiles(gac, "netstandard.dll"));
            // Microsoft.DiaSymReader.Native.amd64.dll is a native dll
            gac_asms.AddRange(Directory.GetFiles(gac, "Microsoft.*.dll").Where(x => !x.Contains("Native")));

            foreach (string file in gac_asms.Concat(ref_assemblies))
                refs_args.Add($"/r:\"{file}\"");

            foreach (string file in sources)
                source_args.Add($"\"{file}\"");

            // running build server on Linux is problematic as if it is started from here it will be killed when the
            // parent process (this) exits

            // bool compile_on_server = Runtime.IsWin;
            bool compile_on_server = true;
            string cmd = "";
            string std_err = "";

            Profiler.get("compiler").Start();
            if (compile_on_server)
                compile_on_server = Globals.BuildServerIsDeployed;

            var cmpl_cmd = "";

            if (compile_on_server)
            {
                Profiler.EngineContext = "Building with csc engine server (Build server)...";

                // using sockets directly

                var compiler = $"{sources.FirstOrDefault()?.GetExtension().Trim('.')}c";

                var request = $@"{compiler} {common_args.JoinBy(" ")}  /out:""{assembly}"" {refs_args.JoinBy(" ")} {source_args.JoinBy(" ")}"
                              .SplitCommandLine();

                cmpl_cmd = request.JoinBy(" ");
                // ensure server running
                // it will gracefully exit if another instance is running
                var startBuildServerCommand = $"\"{Globals.build_server}\" -listen -port:{BuildServer.serverPort} -csc:\"{Globals.csc}\"";

                dotnet.RunAsync(startBuildServerCommand);
                Thread.Sleep(30);

                (string response, int exitCode) = BuildServer.SendBuildRequest(request, BuildServer.serverPort);

                bool buildServerNotRunning() => response.GetLines()
                                                        .FirstOrDefault()?
                                                        .Contains("System.Net.Internals.SocketExceptionFactory+ExtendedSocketException (10061)")
                                                        == true;

                for (int i = 0; i < 10 && buildServerNotRunning(); i++)
                {
                    Thread.Sleep(100);
                    (response, exitCode) = BuildServer.SendBuildRequest(request, BuildServer.serverPort);
                }

                if (buildServerNotRunning())
                    throw new CompilerException("CS-Script build server is not running:\n" +
                                                $"Try to start server manually with 'cscs -server:start'");

                result.NativeCompilerReturnValue = exitCode;
                result.Output.AddRange(response.GetLines());
            }
            else
            {
                Profiler.EngineContext = "Building with local csc engine...";
                cmd = $@"""{Globals.GetCompilerFor(sources.FirstOrDefault())}"" {common_args.JoinBy(" ")} /out:""{assembly}"" {refs_args.JoinBy(" ")} {source_args.JoinBy(" ")}";
                cmpl_cmd = cmd;
                result.NativeCompilerReturnValue = dotnet.Run(cmd, build_dir, x => result.Output.Add(x), x => std_err += x);
            }

            if (std_err.HasText())
                result.Output.Add($"cmpl_stde: {std_err}");

            Profiler.get("compiler").Stop();

            if (CSExecutor.options.verbose)
            {
                if (Environment.GetEnvironmentVariable("echo_compiler_cli") == null)
                {
                    Console.WriteLine("    csc.dll: " + Profiler.get("compiler").Elapsed);
                }
                else
                {
                    // File.WriteAllTe();
                    Console.WriteLine("> ================");
                    Console.WriteLine("csc.dll run: ");
                    Console.WriteLine($"  current_dir: {build_dir}");
                    Console.WriteLine($"  cmd: dotnet \"{Globals.csc}\" {cmpl_cmd}");
                    Console.WriteLine($"  output: {NewLine}{result.Output.JoinBy(NewLine)}");
                    Console.WriteLine("> ================");
                }
            }

            result.ProcessErrors();

            result.Errors
                  .ForEach(x =>
                  {
                      // by default x.FileName is a file name only
                      x.FileName = fileNames.FirstOrDefault(f => f.EndsWith(x.FileName ?? "")) ?? x.FileName;
                  });

            if (result.NativeCompilerReturnValue == 0 && File.Exists(assembly))
            {
                if (Runtime.IsLinux)
                {
                    result.PathToAssembly = options.OutputAssembly;
                    File.Copy(assembly, result.PathToAssembly, true);
                }
                else
                {
                    result.PathToAssembly = options.OutputAssembly;
                    try { File.Copy(assembly, result.PathToAssembly, true); } catch { }
                }

                if (options.GenerateExecutable)
                {
                    var runtimeconfig = "{'runtimeOptions': {'framework': {'name': 'Microsoft.NETCore.App', 'version': '{version}'}}}"
                            .Replace("'", "\"")
                                     .Replace("{version}", Environment.Version.ToString());

                    File.WriteAllText(result.PathToAssembly.ChangeExtension(".runtimeconfig.json"), runtimeconfig);
                    try
                    {
                        // CSUtils.
                        if (Runtime.IsLinux)
                            File.Copy(assembly, result.PathToAssembly.RemoveAssemblyExtension(), true);
                        else
                            File.Copy(assembly, result.PathToAssembly.ChangeExtension(".exe"), true);
                    }
                    catch { }
                }
                else
                {
                    File.Copy(assembly, result.PathToAssembly, true);
                }

                if (options.IncludeDebugInformation)
                    File.Copy(assembly.ChangeExtension(".pdb"),
                              result.PathToAssembly.ChangeExtension(".pdb"),
                              true);
            }
            else
            {
                if (result.Errors.Any(x => x.ErrorNumber == "CS2012") && Runtime.IsLinux && compile_on_server)
                {
                    // When running on Linux as sudo CS-Script creates temp folders (e.g. cache) that automatically
                    // get write-protected if accessed by not sudo-process. On Windows these folders are always not protected.
                    // Meaning that if the build server is running as non root (sudo) it will fail to place the output to these folders.
                    // The solution is to either restart build server as sudo or use dotnet engine as it always starts and stops with the
                    // cscs.exe executable and always inherits its root context.

                    var outputDir = assembly.GetDirName();

                    bool serverCanWrite = BuildServer.Request($"-is_writable_dir:{outputDir}", BuildServer.serverPort) == true.ToString();
                    bool hostCanWrite = outputDir.IsWritable();

                    if (hostCanWrite && !serverCanWrite)
                        result.Errors.Add(new CompilerError
                        {
                            ErrorText = "The build server have less permissions to write to the temporary output directories " +
                            "than host process (cs-script process). " + NewLine +
                            "permissions comparing to the build server. " + NewLine + NewLine +
                            "If you are running cs-script with root privileges you may need to " +
                            "restart the build server with root privileges too:" + NewLine + NewLine +
                            "  sudo css -server:restart" + NewLine + NewLine +
                            "Alternatively you can switch to dotnet engine, which is always " +
                            "aligned with the script engine permissions context. " + NewLine +
                            "(e.g. `cscs -ng:dotnet <script_path>`)"
                        });
                }

                if (result.Errors.IsEmpty())
                {
                    // unknown error; e.g. invalid compiler params
                    result.Errors.Add(new CompilerError { ErrorText = "Unknown compiler error" });
                }
            }

            build_dir.DeleteDir(handleExceptions: true);

            return result;
        }

        internal static string CreateProject(CompilerParameters options, string[] fileNames, string outDir = null)
        {
            string projectName = fileNames.First().GetFileName();
            string projectShortName = Path.GetFileNameWithoutExtension(projectName);

            var template = InitBuildTools(Path.GetExtension(fileNames.First().GetFileName()));

            var out_dir = outDir ?? CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = out_dir.PathJoin(".build", projectName);

            build_dir.DeleteDir()
                     .EnsureDir();

            //  <Project Sdk ="Microsoft.NET.Sdk">
            //    <PropertyGroup>
            //      <OutputType>Exe</OutputType>
            //      <TargetFramework>netcoreapp3.1</TargetFramework>
            //    </PropertyGroup>
            //  </Project>
            var project_element = XElement.Parse(File.ReadAllText(template));

            var compileConstantsDelimiter = ";";
            if (projectName.GetExtension().SameAs(".vb"))
                compileConstantsDelimiter = ",";

            project_element.Add(new XElement("PropertyGroup",
                                    new XElement("DefineConstants", new[] { "TRACE", "NETCORE", "CS_SCRIPT" }.JoinBy(compileConstantsDelimiter))));

            if (!options.GenerateExecutable || !Runtime.IsCore || DefaultCompilerRuntime == DefaultCompilerRuntime.Standard)
            {
                project_element.Element("PropertyGroup")
                               .Element("OutputType")
                               .Remove();
            }

            // In .NET all references including GAC assemblies must be passed to the compiler.
            // In .NET Core this creates a problem as the compiler does not expect any default (shared)
            // assemblies to be passed. So we do need to exclude them.
            // Note: .NET project that uses 'standard' assemblies brings facade/full .NET Core assemblies in the working folder (engine dir)
            //
            // Though we still need to keep shared assembly resolving in the host as the future compiler
            // require ALL ref assemblies to be pushed to the compiler.

            bool not_in_engine_dir(string asm) => (asm.GetDirName() != Assembly.GetExecutingAssembly().Location.GetDirName());

            var ref_assemblies = options.ReferencedAssemblies.Where(x => !x.IsSharedAssembly())
                                                             .Where(Path.IsPathRooted)
                                                             .Where(not_in_engine_dir)
                                                             .ToList();

            void setTargetFremeworkWin() => project_element.Element("PropertyGroup")
                                                           .SetElementValue("TargetFramework", "net5.0-windows");

            bool refWinForms = ref_assemblies.Any(x => x.EndsWith("System.Windows.Forms") ||
                                                       x.EndsWith("System.Windows.Forms.dll"));
            if (refWinForms)
            {
                setTargetFremeworkWin();
                project_element.Element("PropertyGroup")
                               .Add(new XElement("UseWindowsForms", "true"));
            }

            var refWpf = options.ReferencedAssemblies.Any(x => x.EndsWith("PresentationFramework") ||
                                                               x.EndsWith("PresentationFramework.dll"));
            if (refWpf)
            {
                setTargetFremeworkWin();
                Environment.SetEnvironmentVariable("UseWPF", "true");
                project_element.Element("PropertyGroup")
                               .Add(new XElement("UseWPF", "true"));
            }

            if (CSExecutor.options.enableDbgPrint)
                ref_assemblies.Add(Assembly.GetExecutingAssembly().Location());

            void CopySourceToBuildDir(string source)
            {
                // As per dotnet.exe v2.1.26216.3 the pdb get generated as PortablePDB, which is the only format that is supported
                // by both .NET debugger (VS) and .NET Core debugger (VSCode).

                // However PortablePDB does not store the full source path but file name only (at least for now). It works fine in typical
                // .Core scenario where the all sources are in the root directory but if they are not (e.g. scripting or desktop app) then
                // debugger cannot resolve sources without user input.

                // The only solution (ugly one) is to inject the full file path at startup with #line directive. And loose the possibility
                // to use path-based source files in the project file instead of copying all files in the build dir as we do.

                var new_file = build_dir.PathJoin(source.GetFileName());
                var sourceText = File.ReadAllText(source);
                if (!source.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                    sourceText = $"#line 1 \"{source}\"{Environment.NewLine}" + sourceText;
                File.WriteAllText(new_file, sourceText);
            }

            if (ref_assemblies.Any())
            {
                var refs1 = new XElement("ItemGroup");
                project_element.Add(refs1);

                foreach (string asm in ref_assemblies)
                {
                    if (!asm.EndsWith("System.Windows.Forms.dll") &&
                        !asm.EndsWith("PresentationFramework.dll"))
                        refs1.Add(new XElement("Reference",
                                      new XAttribute("Include", asm.GetFileName()),
                                      new XElement("HintPath", asm)));
                }
            }

            var linkSources = true;
            if (linkSources)
            {
                var includs = new XElement("ItemGroup");
                project_element.Add(includs);
                fileNames.ForEach(x =>
                {
                    // <Compile Include="..\..\..\cscs\fileparser.cs" Link="fileparser.cs" />

                    if (x.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                        includs.Add(new XElement("Page",
                                        new XAttribute("Include", x),
                                        new XAttribute("Link", Path.GetFileName(x)),
                                        new XElement("Generator", "MSBuild:Compile")));
                    else
                        includs.Add(new XElement("Compile",
                                        new XAttribute("Include", x),
                                        new XAttribute("Link", Path.GetFileName(x))));
                });
            }
            else
                fileNames.ForEach(CopySourceToBuildDir);

            var projectFile = build_dir.PathJoin(projectShortName + Path.GetExtension(template));
            File.WriteAllText(projectFile, project_element.ToString());

            var working_dir = fileNames.FirstOrDefault()?.GetDirName().Replace(@"\", @"\\");
            var props_file = projectFile.GetDirName().PathJoin("Properties")
                                                     .EnsureDir()
                                                     .PathJoin("launchSettings.json");
            var props = @"{""profiles"": {
                              ""Start"": {
                                  ""commandName"": ""Project"",
                                  ""workingDirectory"": """ + working_dir + @""" } } }";

            File.WriteAllText(props_file, props);

            var solution = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.30608.117
MinimumVisualStudioVersion = 10.0.40219.1
Project(`{9A19103F-16F7-4668-BE54-9A1E7A4F7556}`) = `{proj_name}`, `{proj_file}`, `{03A7169D-D1DD-498A-86CD-7C9587D3DBDD}`
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {03A7169D-D1DD-498A-86CD-7C9587D3DBDD}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {03A7169D-D1DD-498A-86CD-7C9587D3DBDD}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {03A7169D-D1DD-498A-86CD-7C9587D3DBDD}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {03A7169D-D1DD-498A-86CD-7C9587D3DBDD}.Release|Any CPU.Build.0 = Release|Any CPU
    EndGlobalSection
    GlobalSection(ExtensibilityGlobals) = postSolution
        SolutionGuid = {629108FC-1E4E-4A2B-8D8E-159E40FF5950}
    EndGlobalSection
EndGlobal"
.Replace("`", "\"")
.Replace("    ", "\t")
.Replace("{proj_name}", projectFile.GetFileNameWithoutExtension())
.Replace("{proj_file}", projectFile.GetFileName());
            File.WriteAllText(projectFile.ChangeExtension(".sln"), solution);

            return projectFile;
        }

        CompilerResults CompileAssemblyFromFileBatch_with_Build(CompilerParameters options, string[] fileNames)
        {
            var projectFile = CreateProject(options, fileNames);

            var output = "bin";
            var build_dir = projectFile.GetDirName();

            var assembly = build_dir.PathJoin(output, projectFile.GetFileNameWithoutExtension() + ".dll");

            var result = new CompilerResults();

            var config = options.IncludeDebugInformation ? "--configuration Debug" : "--configuration Release";
            var cmd = $"build {config} -o {output} {options.CompilerOptions}";

            Profiler.get("compiler").Start();
            result.NativeCompilerReturnValue = dotnet.Run(cmd, build_dir, x => result.Output.Add(x), x => Console.WriteLine("error> " + x));
            Profiler.get("compiler").Stop();

            if (CSExecutor.options.verbose)
            {
                if (Environment.GetEnvironmentVariable("echo_compiler_cli") == null)
                {
                    var timing = result.Output.FirstOrDefault(x => x.StartsWith("Time Elapsed"));
                    if (timing != null)
                        Console.WriteLine("    dotnet: " + timing);
                }
                else
                {
                    Console.WriteLine("> ================");
                    Console.WriteLine("dotnet.exe run: ");
                    Console.WriteLine($"  current_dir: {build_dir}");
                    Console.WriteLine($"  cmd: dotnet {cmd}");
                    Console.WriteLine($"  output: {NewLine}{result.Output.JoinBy(NewLine)}");
                    Console.WriteLine("> ================");
                }
            }

            Profiler.EngineContext = "Building with dotnet engine...";

            result.ProcessErrors();

            result.Errors
                  .ForEach(x =>
                  {
                      // by default x.FileName is a file name only
                      x.FileName = fileNames.FirstOrDefault(f => f.EndsWith(x.FileName ?? "")) ?? x.FileName;
                  });

            if (result.NativeCompilerReturnValue == 0 && File.Exists(assembly))
            {
                result.PathToAssembly = options.OutputAssembly;
                if (options.GenerateExecutable)
                {
                    // strangely enough on some Linux distro (e.g. WSL2) the access denied error is raised after the files are successfully copied
                    // so ignore
                    PathExtensions.FileCopy(
                        assembly.ChangeExtension(".runtimeconfig.json"),
                        result.PathToAssembly.ChangeExtension(".runtimeconfig.json"),
                        ignoreErrors: Runtime.IsLinux);

                    if (Runtime.IsLinux) // on Linux executables are without extension
                        PathExtensions.FileCopy(
                            assembly.RemoveAssemblyExtension(),
                            result.PathToAssembly.RemoveAssemblyExtension(),
                            ignoreErrors: Runtime.IsLinux);
                    else
                        PathExtensions.FileCopy(
                            assembly.ChangeExtension(".exe"),
                            result.PathToAssembly.ChangeExtension(".exe"));

                    PathExtensions.FileCopy(
                        assembly.ChangeExtension(".dll"),
                        result.PathToAssembly.ChangeExtension(".dll"),
                        ignoreErrors: Runtime.IsLinux);
                }
                else
                {
                    File.Copy(assembly, result.PathToAssembly, true);
                }

                if (options.IncludeDebugInformation)
                    File.Copy(assembly.ChangeExtension(".pdb"),
                              result.PathToAssembly.ChangeExtension(".pdb"),
                              true);
            }
            else
            {
                if (result.Errors.IsEmpty())
                {
                    // unknown error; e.g. invalid compiler params
                    result.Errors.Add(new CompilerError { ErrorText = "Unknown compiler error" });
                }
            }

            build_dir.DeleteDir(handleExceptions: true);

            return result;
        }

        public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
        {
            return CompileAssemblyFromFileBatch(options, new[] { source });
        }
    }

    public interface ICodeCompiler
    {
        CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit);

        CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits);

        CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName);

        CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames);

        CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source);

        CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources);
    }

    public static class ProxyExtensions
    {
        public static bool HasErrors(this List<CompilerError> items) => items.Any(x => !x.IsWarning);
    }

    public class CodeCompileUnit
    {
    }

    public class CompilerParameters
    {
        public List<string> LinkedResources { get; } = new List<string>();
        public List<string> EmbeddedResources { get; } = new List<string>();
        public string Win32Resource { get; set; }
        public string CompilerOptions { get; set; }
        public int WarningLevel { get; set; }
        public bool TreatWarningsAsErrors { get; set; }
        public bool IncludeDebugInformation { get; set; }
        public string OutputAssembly { get; set; }
        public IntPtr UserToken { get; set; }
        public string MainClass { get; set; }
        public List<string> ReferencedAssemblies { get; } = new List<string>();
        public bool GenerateInMemory { get; set; }

        // controls if the compiled assembly has static mainand supports top level class
        public bool GenerateExecutable { get; set; }

        // Controls if the actiual executable needs to be build
        public bool BuildExe { get; set; }

        public string CoreAssemblyFileName { get; set; }
        internal TempFileCollection TempFiles { get; set; }
    }
}