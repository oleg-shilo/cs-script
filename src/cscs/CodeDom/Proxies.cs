using System;
using System.Collections.Generic;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.StringComparison;
using System.Text;
using System.Threading;
using System.Xml.Linq;

#if class_lib
using CSScriptLib;
#else

using csscript;
using static csscript.CoreExtensions;
using static CSScripting.Directives;
using static CSScripting.Globals;
using static CSScripting.PathExtensions;

#endif

namespace CSScripting.CodeDom
{
    public enum DefaultCompilerRuntime
    {
        Standard,
        Host
    }

    public partial class CSharpCompiler : ICodeCompiler
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

        static string InitBuildTools(string fileType)
        {
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var cache_root = cache_dir.GetDirName();
            var build_root = cache_root.GetDirName().PathJoin("build").EnsureDir();

            (string projectName, string language) = fileType.MapValue((".cs", to => ("build.csproj", "C#")),
                                                                      (".vb", to => ("build.vbproj", "VB")));

            var proj_template = build_root.PathJoin($"build.{Environment.Version}.{fileType}proj");

            if (!File.Exists(proj_template))
            {
                var defaultProjFileName = build_root.PathJoin($"build{fileType}proj");
                defaultProjFileName.DeleteIfExists();

                dotnet.Run($"new console -lang {language}", build_root);
                // Program.cs
                build_root.PathJoin($"Program{fileType}").DeleteIfExists();
                build_root.PathJoin("obj").DeleteIfExists(recursive: true);

                if (defaultProjFileName.FileExists())
                    File.Move(defaultProjFileName, proj_template);
            }

            if (!File.Exists(proj_template)) // sdk may not be available so this is the last line of defense
            {
                File.WriteAllLines(proj_template, new[]
                {
                    "<Project Sdk=\"Microsoft.NET.Sdk\">",
                    "  <PropertyGroup>",
                    "    <OutputType>Exe</OutputType>",
                    $"    <TargetFramework>net{Environment.Version.Major}.0</TargetFramework>",
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
            {
                var engine = CSExecutor.options.compilerEngine;

                if (!Runtime.IsSdkInstalled() &&
                    engine != compiler_roslyn &&
                    engine != compiler_roslyn_inproc)
                {
                    Console.WriteLine(
                        $"WARNING: Cannot find .NET {Environment.Version.Major} SDK required for `{engine}` compiler engine. " +
                        $"Switching compiler to `{compiler_roslyn}` instead.{NewLine}" +
                        $"To remove this warning either install .NET {Environment.Version.Major} SDK " +
                        $"or set the default compiler engine to `{compiler_roslyn}` with {NewLine}" +
                        $"  css -config:set:DefaultCompilerEngine={compiler_roslyn}.{NewLine}" +
                        $"--------");
                    engine = compiler_roslyn;
                }

                switch (engine)
                {
                    case Directives.compiler_dotnet:
                        return CompileAssemblyFromFileBatch_with_Build(options, fileNames);

                    case Directives.compiler_csc_inproc:
                        return CompileAssemblyFromFileBatch_with_Csc(options, fileNames, false);

                    case Directives.compiler_csc:
                        return CompileAssemblyFromFileBatch_with_Csc(options, fileNames, true);

                    case Directives.compiler_roslyn:
                        return RoslynService.CompileAssemblyFromFileBatch_with_roslyn(options, fileNames, false);

                    case Directives.compiler_roslyn_inproc:
                        return RoslynService.CompileAssemblyFromFileBatch_with_roslyn(options, fileNames, true);

                    default:
                        return CompileAssemblyFromFileBatch_with_Build(options, fileNames);
                }
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

        internal static string CreateProject(CompilerParameters options, string[] fileNames, string outDir = null, bool isNetFx = false, string platform = null)
        {
            // if project file starts with the '-' so dotnet interprets it as a command line switch
            string assemblyName = fileNames.First().GetFileNameWithoutExtension();
            string projectShortName = assemblyName.TrimStart('-');

            if (ExecuteOptions.options.runExternal)
            {
                projectShortName = options.OutputAssembly.GetFileNameWithoutExtension();
                // assemblyName = projectShortName;
            }

            string projectName = projectShortName;
            string fileType = "";

            // to allow compilation of scripts like script.csx
            if (fileNames.First().GetExtension().ToLower().StartsWith(".vb"))
            {
                fileType = ".vbs";
                projectName += ".vbproj";
            }
            else
            {
                fileType = ".cs";
                projectName += ".csproj";
            }

            var template = InitBuildTools(fileType);

            var out_dir = outDir ?? CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = out_dir.PathJoin(".build", fileNames.First().GetFileName());

            if (!build_dir.DirExists())
                build_dir.EnsureDir();
            else
                build_dir.DeleteDir(doNotDeletеRoot: true); // dotnet has tendency to lock the folder so delete only content in case of recompilation

            // <Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>netcoreapp3.1</TargetFramework></PropertyGroup></Project>
            var project_element = XElement.Parse(File.ReadAllText(template));

            var compileConstantsDelimiter = ";";
            if (projectName.GetExtension().SameAs(".vbproj"))
                compileConstantsDelimiter = ",";

            string[] constants = ["TRACE", "NETCORE", "CS_SCRIPT"];

            if (isNetFx)
                constants = ["NETFRAMEWORK", .. constants];

            project_element.Add(new XElement("PropertyGroup",
                                    new XElement("DefineConstants", constants.JoinBy(compileConstantsDelimiter))));

            if (options.AppType.HasText())
                project_element.Attribute("Sdk").SetValue($"Microsoft.NET.Sdk.{options.AppType}");

            if (!options.GenerateExecutable || !Runtime.IsCore || DefaultCompilerRuntime == DefaultCompilerRuntime.Standard)
            {
                project_element.Element("PropertyGroup")
                               .Element("OutputType")
                               .Remove();
            }

            // In .NET all references including GAC assemblies must be passed to the compiler. In
            // .NET Core this creates a problem as the compiler does not expect any default (shared)
            // assemblies to be passed. So we do need to exclude them.
            // Note: .NET project that uses 'standard' assemblies brings facade/full .NET Core
            // assemblies in the working folder (engine dir)
            //
            // Though we still need to keep shared assembly resolving in the host as the future
            // compiler require ALL ref assemblies to be pushed to the compiler.

            bool not_in_engine_dir(string asm) => (asm.GetDirName() != Assembly.GetExecutingAssembly().Location.GetDirName());

            var ref_assemblies = options.ReferencedAssemblies.Where(x => !x.IsSharedAssembly())
                                                             .Where(Path.IsPathRooted)
                                                             .Where(not_in_engine_dir)
                                                             .ToList();

            void setTargetFremeworkWin(string framework) => project_element.Element("PropertyGroup")
                                                                           .SetElementValue("TargetFramework", framework);

            bool refWinForms = ref_assemblies.Any(x => x.EndsWith("System.Windows.Forms") ||
                                                       x.EndsWith("System.Windows.Forms.dll"));

            var framework = $"net{Environment.Version.Major}.0-windows";

            project_element.Element("PropertyGroup")
                           .Add(new XElement("AssemblyName", assemblyName));

            if (platform.HasText())
            {
                project_element.Element("PropertyGroup")
                               .Add(new XElement("PlatformTarget", platform));
            }

            if (isNetFx)
            {
                framework = "net472";
                project_element.Element("PropertyGroup")
                               .Element("Nullable")
                               .Remove();
                project_element.Element("PropertyGroup")
                               .Element("ImplicitUsings")
                               .Remove();

                setTargetFremeworkWin(framework);
            }

            if (refWinForms)
            {
                setTargetFremeworkWin(framework);
                project_element.Element("PropertyGroup")
                               .Add(new XElement("UseWindowsForms", "true"));
            }

            var refWpf = options.ReferencedAssemblies.Any(x => x.EndsWith("PresentationFramework") ||
                                                               x.EndsWith("PresentationFramework.dll"));
            if (refWpf)
            {
                setTargetFremeworkWin(framework);
                Environment.SetEnvironmentVariable("UseWPF", "true");
                project_element.Element("PropertyGroup")
                               .Add(new XElement("UseWPF", "true"));
            }

            if (CSExecutor.options.enableDbgPrint)
                ref_assemblies.Add(Assembly.GetExecutingAssembly().Location());

            void CopySourceToBuildDir(string source)
            {
                // As per dotnet.exe v2.1.26216.3 the pdb get generated as PortablePDB, which is the
                // only format that is supported by both .NET debugger (VS) and .NET Core debugger (VSCode).

                // However PortablePDB does not store the full source path but file name only (at
                // least for now). It works fine in typical .Core scenario where the all sources are
                // in the root directory but if they are not (e.g. scripting or desktop app) then
                // debugger cannot resolve sources without user input.

                // The only solution (ugly one) is to inject the full file path at startup with
                // #line directive. And loose the possibility to use path-based source files in the
                // project file instead of copying all files in the build dir as we do.

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
                    // <Compile Include="..\..\..\cscs\fileparser.cs" Link="fileparser.cs"/>

                    var sourceFile = x.GetFullPath();

                    if (x.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                    {
                        includs.Add(new XElement("Page",
                                        new XAttribute("Include", x),
                                        new XAttribute("Link", sourceFile),
                                        new XElement("Generator", "MSBuild:Compile")));
                    }
                    else
                    {
                        if (isNetFx && x == fileNames.First()) // entry script with 'static main'
                        {
                            var newCode = CSSUtils.InjectModuleInitializer(File.ReadAllText(sourceFile), "HostingRuntime.Init();");

                            sourceFile = build_dir.PathJoin(x.GetFileNameWithoutExtension() + ".g.cs");

                            File.WriteAllText(sourceFile, newCode);
                        }

                        includs.Add(new XElement("Compile",
                                        new XAttribute("Include", sourceFile),
                                        new XAttribute("Link", Path.GetFileName(x))));
                    }
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
        public string AppType { get; set; }
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

        // controls if the compiled assembly has static main and supports top level class
        public bool GenerateExecutable { get; set; }

        // Controls if the actual executable needs to be build
        public bool BuildExe { get; set; }

        public string CoreAssemblyFileName { get; set; }
        internal TempFileCollection TempFiles { get; set; }
    }
}