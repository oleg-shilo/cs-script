using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using csscript;

namespace CSScripting.CodeDom
{
    static public class RoslynService
    {
        static (string file, int line) Translate(this Dictionary<(int, int), (string, int)> mapping, int line)
        {
            foreach ((int start, int end) range in mapping.Keys)
                if (range.start <= line && line <= range.end)
                {
                    (string file, int lineOffset) = mapping[range];
                    return (file, line - range.start + lineOffset);
                }

            return ("", 0);
        }

        static string[] SeparateUsingsFromCode(this string code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            int pos = root.Usings.FullSpan.End;

            return new[] { code.Substring(0, pos).TrimEnd(), code.Substring(pos) };
        }

        public static CompilerResults CompileAssemblyFromFileBatch_with_roslyn(CompilerParameters options, string[] fileNames, bool inprocess)
        {
            // setting up build folder
            string projectName = fileNames.First().GetFileName();

            var engine_dir = typeof(RoslynService).Assembly.Location.GetDirName();
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = cache_dir.PathJoin(".build", projectName);

            build_dir.DeleteDir()
                     .EnsureDir();

            string firstScript = fileNames.First();
            string attr_file = fileNames.FirstOrDefault(x => x.EndsWith(Globals.InjectedAttributesPrefix) || x.EndsWith(".attr.g.vb"));
            string dbg_inject_file = fileNames.FirstOrDefault(x => x.GetFileName().StartsWith("dbg.inject.", StringComparison.OrdinalIgnoreCase));

            string single_source = build_dir.PathJoin(firstScript.GetFileName());

            if (attr_file != null)
            {
                // Roslyn scripting does not support attributes (0,2): error CS7026: Assembly and
                // module attributes are not allowed in this context

#pragma warning disable S125 // Sections of code should not be commented out
                // writer.WriteLine(File.ReadAllText(attr_file));
#pragma warning restore S125 // Sections of code should not be commented out
            }

            // As per dotnet.exe v2.1.26216.3 the pdb get generated as PortablePDB, which is the
            // only format that is supported by both .NET debugger (VS) and .NET Core debugger (VSCode).

            // However PortablePDB does not store the full source path but file name only (at least
            // for now). It works fine in typical .Core scenario where the all sources are in the
            // root directory but if they are not (e.g. scripting or desktop app) then debugger
            // cannot resolve sources without user input.

            // The only solution (ugly one) is to inject the full file path at startup with #line directive

            // merge all scripts into a single source move all scripts' usings to the file header
            // append the first script whole content append all imported scripts bodies at the
            // bottom of the first script ensure all scripts' content is separated by debugger
            // directive `#line...`

            var importedSources = new Dictionary<string, (int, string[])>(); // file, usings count, code lines

            var combinedScript = new List<string>();

            // exclude dbg_inject_file because it has extension methods, which are not permitted in
            // Roslyn scripts exclude attr_file because it has assembly attribute, which is not
            // permitted in Roslyn scripts
            var imported_sources = fileNames.Where(x => x != attr_file && x != firstScript && x != dbg_inject_file);

            var mapping = new Dictionary<(int, int), (string, int)>();

            combinedScript.Add($"#define NETCORE");
            combinedScript.Add($"#define CS_SCRIPT");
            combinedScript.Add($"#define CS_SCRIPT_ROSLYN");

            foreach (string file in imported_sources)
            {
                var parts = File.ReadAllText(file).SeparateUsingsFromCode();
                var usings = parts[0].GetLines();
                var code = parts[1].GetLines();

                importedSources[file] = (usings.Count(), code);
                add_code(file, usings, 0);
            }

            void add_code(string file, string[] codeLines, int lineOffset)
            {
                int start = combinedScript.Count;
                combinedScript.AddRange(codeLines);
                int end = combinedScript.Count;
                mapping[(start, end)] = (file, lineOffset);
            }

            combinedScript.Add($"#line 1 \"{firstScript}\"");
            add_code(firstScript, File.ReadAllLines(firstScript), 0);

            foreach (string file in importedSources.Keys)
            {
                (var usings_count, var code) = importedSources[file];

                combinedScript.Add($"#line {usings_count + 1} \"{file}\""); // zos
                add_code(file, code, usings_count);
            }

            File.WriteAllLines(single_source, combinedScript.ToArray());

            // See CSharpCompiler.CreateProject for facade asm exclusion reasoning
            bool not_facade_asm_in_engine_dir(string asm)
                => !(asm.GetDirName() == engine_dir && asm.IsPossibleFacadeAssembly());

            // prepare for compiling
            var ref_assemblies = options.ReferencedAssemblies.Where(x => !x.IsSharedAssembly())
                                                             .Where(Path.IsPathRooted)
                                                             .Where(not_facade_asm_in_engine_dir)
                                                             .ToList();

            if (CSExecutor.options.enableDbgPrint)
                ref_assemblies.Add(Assembly.GetExecutingAssembly().Location());

            var refs = new StringBuilder();
            var assembly = build_dir.PathJoin(projectName + ".dll");

            var result = new CompilerResults();

            //pseudo-gac as .NET core does not support GAC but rather common assemblies.
            var gac = typeof(string).Assembly.Location.GetDirName();

            Profiler.get("compiler").Restart();

            //----------------------------

            var all_refs = Directory.GetFiles(gac, "System.*.dll")
                                    .Where(x => !x.Contains("Native")).ToArray()
                                    .ConcatWith(ref_assemblies);

            bool useSingleSourceFile = Environment.GetEnvironmentVariable("css_cli_roslyn_legacy_algorithm")?.ToLower() == "true";

            BuildResult emitResult;
            if (useSingleSourceFile)
                emitResult = Build([single_source], assembly, all_refs, options, inprocess);
            else
                emitResult = Build(fileNames, assembly, all_refs, options, inprocess);

            Profiler.EngineContext = "Building with Roslyn engine...";

            if (!emitResult.Success)
            {
                var message = new StringBuilder();

                IEnumerable<BuildResult.Diagnostic> failures = emitResult.Diagnostics.Where(d => d.IsWarningAsError ||
                                                                                      d.Severity == DiagnosticSeverity.Error);
                foreach (var diagnostic in failures)
                {
                    string error_location = "";
                    if (diagnostic.Location_IsInSource)
                    {
                        int error_line = diagnostic.Location_StartLinePosition_Line;
                        int error_column = diagnostic.Location_StartLinePosition_Character;

                        var source = diagnostic.Location_FilePath;
                        if (source == "")
                        {
                            (source, error_line) = mapping.Translate(error_line);
                        }

                        error_line++;
                        error_location = $"{source}({error_line},{error_column}): ";
                    }
                    message.AppendLine($"{error_location}error {diagnostic.Id}: {diagnostic.Message}");
                }

                if (combinedScript.Any(x => x.TrimStart().StartsWith("global using ")))
                    message.Insert(0, "<script>(0,0): warning: It looks like you are using global using statement that is not " +
                        $"supported by Roslyn. Either remove it or use different compiler engine.{Environment.NewLine}");

                var errors = message.ToString();

                throw new CompilerException(errors);
            }

            //----------------------------
            Profiler.get("compiler").Stop();

            // Console.WriteLine(" roslyn: " + Profiler.get("compiler").Elapsed);

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
                    if (result.Output.Any())
                    {
                        result.Errors.Add(new CompilerError { ErrorText = result.Output.JoinBy("\n") });
                    }
                    else
                    {
                        // unknown error; e.g. invalid compiler params
                        result.Errors.Add(new CompilerError { ErrorText = "Unknown compiler error" });
                    }
                }
            }

            build_dir.DeleteDir();

            return result;
        }

        static AssemblyMetadata ToMetadataOnCore(string assemblyFile)
        {
            try
            {
                return AssemblyMetadata.CreateFromFile(assemblyFile);
            }
            catch
            {
                unsafe
                {
                    try
                    {
                        var asm = Assembly.LoadFile(assemblyFile);
                        return ToMetadataOnCore(asm);
                    }
                    catch { }
                }
                return null;
            }
        }

        static AssemblyMetadata ToMetadataOnCore(Assembly asm)
        {
            // this way of loading metadata is faster than the one based on reading the assembly file
            // but TryGetRawMetadata is not available on .NET Framework
            unsafe
            {
                if (asm.TryGetRawMetadata(out var blob, out var length))
                    return AssemblyMetadata.Create(ModuleMetadata.CreateFromMetadata((IntPtr)blob, length));
                return null;
            }
        }

        static BuildResult Build(string[] sourceFiles, string assemblyFile, string[] refs, CompilerParameters compileInfo, bool buildLocally)
        {
            bool useServer = !buildLocally;
            bool IsDebug = compileInfo.IncludeDebugInformation;

            try
            {
                if (useServer)
                    return build_remotelly(sourceFiles, assemblyFile, refs, compileInfo);
            }
            catch { }

            var legacyAlgorithm = Environment.GetEnvironmentVariable("css_cli_roslyn_legacy_algorithm")?.ToLower() == "true";

            var outputKind = OutputKind.DynamicallyLinkedLibrary;

            Compilation compilation;
            if (legacyAlgorithm)
            {
                string mscorelib = 333.GetType().Assembly.Location.GetFileName();
                var scriptOptions = ScriptOptions.Default;
                foreach (string file in refs)
                {
                    try
                    {
                        // NOTE: It is important to avoid loading the runtime itself (mscorelib) as it
                        // will break the code evaluation (compilation).
                        if (file.GetFileName() != mscorelib)
                            scriptOptions = scriptOptions.AddReferences(Assembly.LoadFile(file));
                    }
                    catch { }
                }
                var scriptText = File.ReadAllText(sourceFiles.FirstOrDefault());
                compilation = CSharpScript.Create(scriptText, scriptOptions)
                                          .GetCompilation();
            }
            else
            {
                var references = new List<MetadataReference>();
                foreach (var asm in refs)
                {
                    var metadata = ToMetadataOnCore(asm);
                    if (metadata != null)
                        references.Add(metadata.GetReference());
                }

                var compileSymbols = compileInfo.CompilerOptions.GetCompilerOptonsSymbols();
                if (compileInfo.IncludeDebugInformation)
                    compileSymbols = [.. compileSymbols, "DEBUG", "TRACE"];

                SyntaxTree createTree(string code, string path) =>
                    SyntaxFactory.ParseSyntaxTree(code, new CSharpParseOptions(
                        kind: SourceCodeKind.Regular,
                            preprocessorSymbols: compileSymbols,
                            languageVersion: LanguageVersion.Latest), path, encoding: Encoding.Default);

                var syntaxTrees = new List<SyntaxTree>();
                foreach (var file in sourceFiles)
                {
                    var scriptText = File.ReadAllText(file);
                    syntaxTrees.Add(createTree(scriptText, file));
                }

                if (syntaxTrees.First().IsTopLevelStatement())
                    outputKind = OutputKind.ConsoleApplication;

                compilation = CSharpCompilation.Create(
                                                assemblyName: "Script" + Guid.NewGuid(),
                                                syntaxTrees: syntaxTrees.ToArray(),
                                                references: references,
                                                options: new CSharpCompilationOptions(outputKind));
            }

            if (IsDebug)
                compilation = compilation.WithOptions(compilation.Options
                                         .WithOptimizationLevel(OptimizationLevel.Debug)
                                         .WithOutputKind(outputKind)); // change this line if you need to build excutable

            return build_locally(compilation, assemblyFile, IsDebug);
        }

        static BuildResult build_remotelly(string[] sourceFiles, string assemblyFile, string[] refs, CompilerParameters options)
        {
            //Console.WriteLine("Building remotely");
            var request = new BuildRequest
            {
                Source = sourceFiles.First(),
                ImportedSources = sourceFiles.Skip(1).ToArray(),
                Assembly = assemblyFile,
                IsDebug = options.IncludeDebugInformation,
                References = refs,
                CompileSymbols = options.CompilerOptions.GetCompilerOptonsSymbols()
            };

            try
            {
                var message = request.Serialize();

                // Profiler.get("server").Restart();
                try
                {
                    return Roslyn.BuildServer.SentRequest(message).Deserialize<BuildResult>();
                }
                finally
                {
                    // Console.WriteLine(" server: " + Profiler.get("server").Elapsed);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name.EndsWith("SocketException"))
                {
                    var exe = Assembly.GetEntryAssembly().Location();
                    "dotnet".RunAsync($"\"{exe}\" -server_r:start");
                }

                if (Environment.GetCommandLineArgs().Contains(AppArgs.verbose))
                    Console.WriteLine("Build server problem:" + e.Message);
                throw;
            }
        }

        public static string process_build_remotelly_request(string request_data)
        {
            BuildRequest request = request_data.Deserialize<BuildRequest>();

            ScriptOptions scriptOptions = ScriptOptions.Default;

            foreach (string file in request.References)
            {
                try
                {
                    var asm = Assembly.LoadFile(file); // may fail for some gac assemblies
                    scriptOptions = scriptOptions.AddReferences(asm);
                }
                catch { }
            }

            SyntaxTree createTree(string code, string path) =>
                SyntaxFactory.ParseSyntaxTree(code, new CSharpParseOptions(
                    kind: SourceCodeKind.Regular,
                        preprocessorSymbols: request.CompileSymbols,
                        languageVersion: LanguageVersion.Latest), path, encoding: Encoding.Default);

            var syntaxTrees = new List<SyntaxTree>();
            syntaxTrees.Add(createTree(File.ReadAllText(request.Source), request.Source));

            foreach (var file in request.ImportedSources)
                syntaxTrees.Add(createTree(File.ReadAllText(file), file));

            var references = new List<MetadataReference>();
            foreach (var asm in request.References)
            {
                var metadata = ToMetadataOnCore(asm);
                if (metadata != null)
                    references.Add(metadata.GetReference());
            }

            var outputKind = syntaxTrees.First().IsTopLevelStatement() ?
                OutputKind.ConsoleApplication :
                OutputKind.DynamicallyLinkedLibrary;

            var compilation = CSharpCompilation.Create(
                                                assemblyName: "Script" + Guid.NewGuid(),
                                                syntaxTrees: syntaxTrees.ToArray(),
                                                references: references,
                                                options: new CSharpCompilationOptions(outputKind));

            if (request.IsDebug)
                compilation = compilation.WithOptions(compilation.Options
                                         .WithOptimizationLevel(OptimizationLevel.Debug)
                                         .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);

            BuildResult result = build_locally(compilation, request.Assembly, request.IsDebug);

            return result.Serialize();
        }

        public static object Init()
        {
            try
            {
                var code = @"
using System;
class Script
{
    static public void Main()
    {
        (int a, int b) t = (1, 2);
        Console.WriteLine(""hello..."");
    }
}";
                var scriptOptions = ScriptOptions.Default;

                var gac = typeof(string).Assembly.Location.GetDirName();
                foreach (string file in Directory.GetFiles(gac, "System.*.dll"))
                    try
                    {
                        scriptOptions = scriptOptions.AddReferences(Assembly.LoadFile(file));
                    }
                    catch { }

                var compilation = CSharpScript.Create(code, scriptOptions)
                                              .GetCompilation();

                using var pdb = new MemoryStream();
                using var asm = new MemoryStream();

                var emitResult = compilation.Emit(asm, pdb, options: new EmitOptions(false, DebugInformationFormat.PortablePdb));
                return BuildResult.From(emitResult);
            }
            catch { }
            return null;
        }

        static BuildResult build_locally(Compilation compilation, string assemblyFile, bool IsDebug, bool doNotSave = false)
        {
            var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);

            using var asm = new MemoryStream();
            using var pdb = new MemoryStream();

            var emitResult = compilation.Emit(asm, pdb, options: emitOptions);

            if (emitResult.Success && !doNotSave)
            {
                asm.Seek(0, SeekOrigin.Begin);
                byte[] buffer = asm.GetBuffer();

                File.WriteAllBytes(assemblyFile, buffer);

                if (IsDebug)
                {
                    pdb.Seek(0, SeekOrigin.Begin);
                    byte[] pdbBuffer = pdb.GetBuffer();

                    File.WriteAllBytes(assemblyFile.ChangeExtension(".pdb"), pdbBuffer);
                }
            }
            return BuildResult.From(emitResult);
        }
    }
}