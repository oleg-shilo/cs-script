using csscript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

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
            throw new NotImplementedException();
        }

        public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
        {
            throw new NotImplementedException();
        }


        static string dotnet { get; } = Utils.IsCore ? Process.GetCurrentProcess().MainModule.FileName : "dotnet";

        static string InitBuildTools()
        {
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var cache_root = cache_dir.GetDirName();
            var build_root = cache_root.GetDirName().PathJoin("build").EnsureDir();

            var proj_template = build_root.PathJoin("build.csproj");

            if (!File.Exists(proj_template))
            {
                Utils.Run("dotnet", "new console", build_root);
                build_root.PathJoin("Program.cs").DeleteIfExists();
            }

            return proj_template;
        }

        public static DefaultCompilerRuntime DefaultCompilerRuntime = DefaultCompilerRuntime.Host;

        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            // csc();
            // RoslynService.CompileAssemblyFromFileBatch_with_roslyn(options, fileNames);
            return RoslynService.CompileAssemblyFromFileBatch_with_roslyn(options, fileNames);
            // return CompileAssemblyFromFileBatch_with_Csc(options, fileNames);
            // return CompileAssemblyFromFileBatch_with_Build(options, fileNames);
        }

       

    
        CompilerResults CompileAssemblyFromFileBatch_with_Csc(CompilerParameters options, string[] fileNames)
        {
            string projectName = fileNames.First().GetFileName();

            var engine_dir = this.GetType().Assembly.Location.GetDirName();
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = cache_dir.PathJoin(".build", projectName);

            build_dir.DeleteDir()
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
                                                             .ToArray();

            var refs = new StringBuilder();
            var assembly = build_dir.PathJoin(projectName + ".dll");

            var result = new CompilerResults();

            if (!options.GenerateExecutable || !Utils.IsCore || DefaultCompilerRuntime == DefaultCompilerRuntime.Standard)
            {
                // todo
            }

            //----------------------------

            //pseudo-gac as .NET core does not support GAC but rather common assemblies.
            var core_dir = typeof(string).Assembly.Location.GetDirName();

            core_dir = @"C:\Program Files\dotnet\sdk\2.0.3\Roslyn";
            // var gac = @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.1.0-preview1-26216-03";

            var gac = typeof(string).Assembly.Location.GetDirName();

            var refs_args = "";
            var source_args = "";

            var common_args = "/utf8output /nostdlib+ ";
            if (options.GenerateExecutable)
                common_args += "/t:exe ";
            else
                common_args += "/t:library ";

            if (options.IncludeDebugInformation)
                common_args += "/debug+";

            foreach (string file in Directory.GetFiles(gac, "System.*.dll"))
                refs_args += $"/r:\"{file}\" ";

            foreach (string file in ref_assemblies)
                refs_args += $"/r:\"{file}\" ";

            foreach (string file in sources)
                source_args += $"\"{file}\" ";

            var cmd = $@"""{core_dir}\csc.exe"" {common_args} {refs_args} {source_args} /out:""{assembly}""";
            //----------------------------

            Profiler.get("compiler").Start();
            result.NativeCompilerReturnValue = Utils.Run(dotnet, cmd, build_dir, x => result.Output.Add(x));
            Profiler.get("compiler").Stop();

            Console.WriteLine("    csc.exe: " + Profiler.get("compiler").Elapsed);

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
                File.Copy(assembly, result.PathToAssembly, true);

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

            build_dir.DeleteDir();

            return result;
        }

        CompilerResults CompileAssemblyFromFileBatch_with_Build(CompilerParameters options, string[] fileNames)
        {
            string projectName = fileNames.First().GetFileName();
            string projectShortName = Path.GetFileNameWithoutExtension(projectName);

            var template = InitBuildTools();

            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = cache_dir.PathJoin(".build", projectName);

            build_dir.DeleteDir()
                     .EnsureDir();

            var project_content = File.ReadAllText(template);

            if (!options.GenerateExecutable || !Utils.IsCore || DefaultCompilerRuntime == DefaultCompilerRuntime.Standard)
            {
                project_content = project_content.Replace("<OutputType>Exe</OutputType>", "");
                project_content = project_content.Replace("<TargetFramework>netcoreapp2.1</TargetFramework>",
                                                          "<TargetFramework>netstandard2.0</TargetFramework>");
            }

            // In .NET all references including GAC assemblies must be passed to the compiler.
            // In .NET Core this creates a problem as the compiler does not expect any default (shared)
            // assemblies to be passed. So we do need to exclude them.
            // Note: .NET project that uses 'standard' assemblies brings facade/full .NET Core assemblies in the working folder (engine dir)
            // 
            // Though we still need to keep shared assembly resolving in the host as the future compiler 
            // require ALL ref assemblies to be pushed to the compiler. 

            bool not_in_engine_dir(string asm) => (asm.GetDirName() != this.GetType().Assembly.Location.GetDirName());

            var refs = new StringBuilder();
            var ref_assemblies = options.ReferencedAssemblies.Where(x => !x.IsSharedAssembly())
                                                             .Where(Path.IsPathRooted)
                                                             .Where(not_in_engine_dir)
                                                             .ToArray(); // for debugging

            void CopySourceToBuildDir(string source)
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
            }


            if (ref_assemblies.Any())
            {
                // var logger = NLog.LogManager.GetCurrentClassLogger();
                // logger.Info("Hello World");

                foreach (string asm in ref_assemblies)
                    refs.AppendLine($@"<Reference Include=""{asm.GetFileName()}""><HintPath>{asm}</HintPath></Reference>");

                project_content = project_content.Replace("</Project>",
                                                        $@"  <ItemGroup>
                                                                    {refs.ToString()}
                                                                 </ItemGroup>
                                                              </Project>");

            }


            File.WriteAllText(build_dir.PathJoin(projectShortName + ".csproj"), project_content);

            fileNames.ForEach(CopySourceToBuildDir);

            var output = "bin";
            var assembly = build_dir.PathJoin(output, projectShortName + ".dll");

            var result = new CompilerResults();


            var config = options.IncludeDebugInformation ? "--configuration Debug" : "--configuration Release";

            Profiler.get("compiler").Start();
            result.NativeCompilerReturnValue = Utils.Run(dotnet, $"build {config} -o {output} {options.CompilerOptions}", build_dir, x => result.Output.Add(x));
            Profiler.get("compiler").Stop();



            var timing = result.Output.FirstOrDefault(x => x.StartsWith("Time Elapsed"));
            if (timing != null)
                Console.WriteLine("    dotnet: " + timing);

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
                File.Copy(assembly, result.PathToAssembly, true);

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

            build_dir.DeleteDir();

            return result;
        }

        public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
        {
            return CompileAssemblyFromFileBatch(options, new[] { source });
        }

        static void explore_package_dependencies_spike()
        {

            // var package_name = "NLog.Config";
            // var package_ver = "4.5.4";
            // project_content = project_content.Replace("</Project>",
            //                                        $@"  <ItemGroup>
            //                                             <PackageReference Include=""{package_name}""  />
            //                                             </ItemGroup>
            //                                          </Project>");

            // "C:\Users\%username%\.nuget\packages\nlog\4.5.4\lib\netstandard2.0\NLog.dll"
            // var logger = NLog.LogManager.GetCurrentClassLogger();

            // <PackageReference Include=""{package_name}"" Version=""{package_ver}"" />
            /*
            <ItemGroup>
                <PackageReference Include="NLog.Config" Version="4.5.4" />
            </ItemGroup>
            */
            /*
             "targets": {
    ".NETStandard,Version=v2.0": {},
    ".NETStandard,Version=v2.0/": {
      "script/1.0.0": {
        "dependencies": {
          "NETStandard.Library": "2.0.1",
          "NLog.Config": "4.5.4"
        },
        "runtime": {
          "script.dll": {}
        }
      },
      "Microsoft.NETCore.Platforms/1.1.0": {},
      "NETStandard.Library/2.0.1": {
        "dependencies": {
          "Microsoft.NETCore.Platforms": "1.1.0"
        }
      },
      "NLog/4.5.4": {
        "runtime": {
          "lib/netstandard2.0/NLog.dll": {}
        }
      },
             */
            // C:\Users\%username%\.nuget\packages\nlog\4.5.4\lib\netstandard2.0\NLog.dll

            /* "NLog/4.5.4": {
                  "type": "package",
                  "serviceable": true,
                  "sha512": "sha512-sYOzep0pdVat2zkOREy6FQwcH4zRCTimGr4c1Esqc0+t9QYvQ5JXDnW/sdqOsVxbxJ28sa/3MRUGSAfXz7eIGw==",
                  "path": "nlog/4.5.4",
                  "hashPath": "nlog.4.5.4.nupkg.sha512"
                },
                "NLog.Config/4.5.4": {
                  "type": "package",
                  "serviceable": true,
                  "sha512": "sha512-qMqpvqTqUwUuOQB7Y4HKJ/ZWPIsDyt4qD58B+z6KRG1vN0TeGKfz0gF4rGSGXLvOpwlxd6Gqai2u9U6WtbhOgg==",
                  "path": "nlog.config/4.5.4",
                  "hashPath": "nlog.config.4.5.4.nupkg.sha512"
                },
                "NLog.Schema/4.5.4": {
                  "type": "package",
                  "serviceable": true,
                  "sha512": "sha512-DEjVc+GSpUpeR3zInV6GJ6OuHzoM0qgweTbpuQc6wHicENq6ZY4DNi2Kk4AcEGrzdIwyIcyAsvAxYg/+bZHQEQ==",
                  "path": "nlog.schema/4.5.4",
                  "hashPath": "nlog.schema.4.5.4.nupkg.sha512"
                }*/


            // result.ProcessErrors();

            // result.ProbingDirs.Add(@"C:\Users\%username%\.nuget\packages\nlog\4.5.4\lib\netstandard2.0");
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

    public class CompilerResults
    {
        public TempFileCollection TempFiles { get; set; } = new TempFileCollection();
        public List<string> ProbingDirs { get; set; } = new List<string>();
        public Assembly CompiledAssembly { get; set; }
        public List<CompilerError> Errors { get; set; } = new List<CompilerError>();
        public List<string> Output { get; set; } = new List<string>();
        public string PathToAssembly { get; set; }
        public int NativeCompilerReturnValue { get; set; }

        internal void ProcessErrors()
        {
            var isErrroSection = false;
            // Build succeeded.
            foreach (var line in Output)
            {
                if (!isErrroSection)
                {
                    // MSBUILD : error MSB1001: Unknown switch.
                    if (line.StartsWith("Build FAILED.") || line.StartsWith("Build succeeded."))
                        isErrroSection = true;

                    if (line.Contains("MSBUILD : error "))
                    {
                        var error = CompilerError.Parser(line);
                        if (error != null)
                            Errors.Add(error);
                    }
                }
                else
                {
                    if (line.IsNotEmpty())
                    {
                        var error = CompilerError.Parser(line);
                        if (error != null)
                            Errors.Add(error);
                    }
                }
            }

        }
    }

    public static class ProxyExtensions
    {
        public static bool HasErrors(this List<CompilerError> items) => items.Any(x => !x.IsWarning);
    }

    public class CodeCompileUnit
    {
    }

    public class TempFileCollection
    {
        public List<string> Items { get; set; } = new List<string>();
        public void Clear() => Items.ForEach(File.Delete);
    }

    public class CompilerError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string ErrorNumber { get; set; }
        public string ErrorText { get; set; }
        public bool IsWarning { get; set; }
        public string FileName { get; set; }

        public static CompilerError Parser(string compilerOutput)
        {
            // C:\Program Files\dotnet\sdk\2.1.300-preview1-008174\Sdks\Microsoft.NET.Sdk\build\Microsoft.NET.ConflictResolution.targets(59,5): error MSB4018: The "ResolvePackageFileConflicts" task failed unexpectedly. [C:\Users\%username%\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            // script.cs(11,8): error CS1029: #error: 'this is the error...' [C:\Users\%username%\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            // script.cs(10,10): warning CS1030: #warning: 'DEBUG is defined' [C:\Users\%username%\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            // MSBUILD : error MSB1001: Unknown switch.
            bool isError = compilerOutput.Contains("): error ");
            bool isWarning = compilerOutput.Contains("): warning ");
            bool isBuildError = compilerOutput.Contains("MSBUILD : error");

            if (isBuildError)
            {
                var parts = compilerOutput.Replace("MSBUILD : error ", "").Split(":".ToCharArray(), 2);
                return new CompilerError
                {
                    ErrorText = "MSBUILD: " + parts.Last().Trim(),        // MSBUILD error: Unknown switch.
                    ErrorNumber = parts.First()                           // MSB1001
                };
            }
            else if (isWarning || isError)
            {
                var result = new CompilerError();

                var rx = new Regex(@".*\(\d+\,\d+\)\:");
                var match = rx.Match(compilerOutput, 0);
                if (match.Success)
                {
                    try
                    {

                        var m = Regex.Match(match.Value, @"\(\d+\,\d+\)\:");


                        var location_items = m.Value.Substring(1, m.Length - 3).Split(separator: ',').ToArray();
                        var description_items = compilerOutput.Substring(match.Value.Length).Split(":".ToArray(), 2);

                        result.ErrorText = description_items.Last();                            // #error: 'this is the error...'
                        result.ErrorNumber = description_items.First().Split(' ').Last();       // CS1029
                        result.IsWarning = isWarning;
                        result.FileName = match.Value.Substring(0, m.Index);                    // cript.cs
                        result.Line = int.Parse(location_items[0]);                             // 11
                        result.Column = int.Parse(location_items[1]);                           // 8

                        int desc_end = result.ErrorText.LastIndexOf("[");
                        if (desc_end != -1)
                            result.ErrorText = result.ErrorText.Substring(0, desc_end);

                        return result;
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e);
                    }
                }
            }
            return null;
        }
    }

    public class CompilerParameters
    {
        readonly List<string> linkedResources = new List<string>();
        readonly List<string> embeddedResources = new List<string>();
        readonly List<string> referencedAssemblies = new List<string>();

        public List<string> LinkedResources { get => linkedResources; }
        public List<string> EmbeddedResources { get => embeddedResources; }
        public string Win32Resource { get; set; }
        public string CompilerOptions { get; set; }
        public int WarningLevel { get; set; }
        public bool TreatWarningsAsErrors { get; set; }
        public bool IncludeDebugInformation { get; set; }
        public string OutputAssembly { get; set; }
        public IntPtr UserToken { get; set; }
        public string MainClass { get; set; }
        public List<string> ReferencedAssemblies { get => referencedAssemblies; }
        public bool GenerateInMemory { get; set; }
        public bool GenerateExecutable { get; set; }
        public string CoreAssemblyFileName { get; set; }
        public TempFileCollection TempFiles { get; set; }
    }
}