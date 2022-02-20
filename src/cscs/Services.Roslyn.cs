using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            string attr_file = fileNames.FirstOrDefault(x => x.EndsWith(".attr.g.cs", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".attr.g.vb", StringComparison.OrdinalIgnoreCase));
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

            // prepare for compiling
            var ref_assemblies = options.ReferencedAssemblies.Where(x => !x.IsSharedAssembly())
                                                             .Where(Path.IsPathRooted)
                                                             .Where(asm => asm.GetDirName() != engine_dir)
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
                                    .ConcatWith(ref_assemblies);

            BuildResult emitResult = Build(single_source, assembly, all_refs, options.IncludeDebugInformation, inprocess);

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
                        error_location = $"{source}({error_line},{ error_column}): ";
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

        static BuildResult Build(string sourceFile, string assemblyFile, string[] refs, bool IsDebug, bool buildLocally)
        {
            bool useServer = !buildLocally;
            // useServer = false;
            var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);
            try
            {
                if (useServer)
                    return build_remotelly(sourceFile, assemblyFile, refs, IsDebug, emitOptions);
            }
            catch { }

            var scriptText = File.ReadAllText(sourceFile);
            var scriptOptions = ScriptOptions.Default;

            foreach (string file in refs)
                try { scriptOptions = scriptOptions.AddReferences(Assembly.LoadFile(file)); }
                catch { }

            Compilation compilation = CSharpScript.Create(scriptText, scriptOptions)
                                                  .GetCompilation();

            if (IsDebug)
                compilation = compilation.WithOptions(compilation.Options
                                         .WithOptimizationLevel(OptimizationLevel.Debug)
                                         .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            return build_locally(compilation, assemblyFile, IsDebug, emitOptions);
        }

        static BuildResult build_remotelly(string sourceFile, string assemblyFile, string[] refs, bool IsDebug, EmitOptions emitOptions)
        {
            //Console.WriteLine("Building remotely");
            var request = new BuildRequest
            {
                Source = sourceFile,
                Assembly = assemblyFile,
                IsDebug = IsDebug,
                References = refs
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

            var scriptText = File.ReadAllText(request.Source);
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

            var compilation = CSharpScript.Create(scriptText, scriptOptions)
                                          .GetCompilation();

            if (request.IsDebug)
                compilation = compilation.WithOptions(compilation.Options
                                         .WithOptimizationLevel(OptimizationLevel.Debug)
                                         .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);

            BuildResult result = build_locally(compilation, request.Assembly, request.IsDebug, emitOptions);

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

        static BuildResult build_locally(Compilation compilation, string assemblyFile, bool IsDebug, EmitOptions emitOptions, bool doNotSave = false)
        {
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