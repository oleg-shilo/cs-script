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
using static CSScripting.Globals;
using static CSScripting.PathExtensions;

#endif

namespace CSScripting.CodeDom
{
    public partial class CSharpCompiler
    {
        CompilerResults CompileAssemblyFromFileBatch_with_Build(CompilerParameters options, string[] fileNames)
        {
            var projectFile = CreateProject(options, fileNames);

            var output = "bin";
            var build_dir = projectFile.GetDirName();

            var assembly = build_dir.PathJoin(output, projectFile.GetFileNameWithoutExtension() + ".dll");

            var result = new CompilerResults();

            if (!Runtime.IsSdkInstalled())
                Console.WriteLine("WARNING: .NET SDK is not installed. It is required for CS-Script (with `csc` engine) to function properly.");

            var config = options.IncludeDebugInformation ? "--configuration Debug" : "--configuration Release";
            var cmd = $"build {config} -o {output} {options.CompilerOptions.Replace("/target:winexe", "")}"; // dotnet build command gets "console vs win" from the project file, not the CLI param

            Profiler.get("compiler").Start();
            assembly.DeleteIfExists();
            result.NativeCompilerReturnValue = dotnet.Run(cmd, build_dir,
                                                          onOutput: x => result.Output.Add(x),
                                                          onError: x => Console.Error.WriteLine("error> " + x),
                                                          timeout: 20000,
                                                          assembly);
            Profiler.get("compiler").Stop();
            Thread.Sleep(50);
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
                    // strangely enough on some Linux distro (e.g. WSL2) the access denied error is
                    // raised after the files are successfully copied so ignore
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

            //build_dir.DeleteDir(handleExceptions: true);

            return result;
        }

        CompilerResults CompileAssemblyFromFileBatch_with_Csc(CompilerParameters options, string[] fileNames, bool buidOnServer = true)
        {
            if (fileNames.Any() && fileNames.First().GetExtension().SameAs(".vb"))
                throw new CompilerException("Executing VB scripts is only supported on dotnet engine. Please either set it:" + NewLine +
                    " - as global setting with [css -config:set:DefaultCompilerEngine=dotnet]" + NewLine +
                    " - from CLI parameters with [css -ng:dotnet <scriupt.vb>]" + NewLine +
                    " - from your VB script code with [' //css_ng dotnet]");

            if (!Runtime.IsSdkInstalled())
                Console.WriteLine("WARNING: .NET SDK is not installed. It is required for CS-Script to function properly.");

            string projectName = fileNames.First().GetFileName();

            var engine_dir = this.GetType().Assembly.Location.GetDirName();
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = cache_dir.PathJoin(".build", projectName);

            build_dir.DeleteDir(handleExceptions: true)
                     .EnsureDir();

            var sources = new List<string>();

            fileNames.ForEach((string source) =>
            {
                // As per dotnet.exe v2.1.26216.3 the pdb get generated as PortablePDB, which is the
                // only format that is supported by both .NET debugger (VS) and .NET Core debugger (VSCode).

                // However PortablePDB does not store the full source path but file name only (at
                // least for now). It works fine in typical .Core scenario where the all sources are
                // in the root directory but if they are not (e.g. scripting or desktop app) then
                // debugger cannot resolve sources without user input.

                // The only solution (ugly one) is to inject the full file path at startup with
                // #line directive

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

            //----------------------------

            //pseudo-gac as .NET core does not support GAC but rather common assemblies.
            // C:\Program Files\dotnet\shared\Microsoft.NETCore.App\5.0.4
            var gac = typeof(string).Assembly.Location.GetDirName();

            // C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\5.0.4
            string gac2 = (options.AppType == "Web" ? gac.Replace("Microsoft.NETCore.App", "Microsoft.AspNetCore.App") : null);

            var refs_args = new List<string>();
            var source_args = new List<string>();
            var common_args = new List<string>();

            common_args.Add("/utf8output");
            common_args.Add("/nostdlib+");
            if (options.GenerateExecutable)
                common_args.Add("/t:exe");
            else
                common_args.Add("/t:library");

            if (options.IncludeDebugInformation)
                common_args.Add("/debug:portable");  // on .net full it is "/debug+"

            if (options.CompilerOptions.HasText())
                common_args.Add(options.CompilerOptions);

            common_args.Add("-define:TRACE;NETCORE;CS_SCRIPT");

            var gac_asms = Directory.GetFiles(gac, "System.*.dll").ToList();
            gac_asms.AddRange(Directory.GetFiles(gac, "netstandard.dll"));
            // Microsoft.DiaSymReader.Native.amd64.dll is a native dll
            gac_asms.AddRange(Directory.GetFiles(gac, "Microsoft.*.dll").Where(x => !x.Contains("Native")));

            if (gac2.HasText())
                gac_asms.AddRange(Directory.GetFiles(gac2, "Microsoft.*.dll").Where(x => !x.Contains("Native")));

            gac_asms.RemoveAll(x => x.Contains(".Native."));

            // need to remove duplicated assemblies leaving GAC as a preferable reference
            // IE System.Linq.dll exists in GAC and in packages where it can be of a different version so it should not be used.
            var new_ref_assemblies = ref_assemblies.Where(x => !gac_asms.Any(y => Path.GetFileName(y) == Path.GetFileName(x)));

            foreach (string file in gac_asms.Concat(new_ref_assemblies))
                refs_args.Add($"/r:\"{file}\"");

            foreach (string file in sources)
                source_args.Add($"\"{file}\"");

            // running build server on Linux is problematic as if it is started from here it will be
            // killed when the parent process (this) exits
            bool compile_on_server = Runtime.IsWin && buidOnServer;

            string cmd = "";
            string std_err = "";

            Profiler.get("compiler").Start();
            if (compile_on_server)
                compile_on_server = Globals.BuildServerIsDeployed;

            var cmpl_cmd = "";

            void log_cmd(string cmd)
            {
                var logFile = Environment.GetEnvironmentVariable("CSSCRIPT_CSC_CMD_LOG");
                if (logFile.HasText())
                    try
                    {
                        File.WriteAllText(logFile, cmd);
                    }
                    catch { } // just ignore as log_cmd is does not reflect any functional requirement
            }

            if (compile_on_server)
            {
                Profiler.EngineContext = "Building with csc engine server (Build server)...";

                // using sockets directly

                var fileType = $"{sources.FirstOrDefault()?.GetExtension().Trim('.')}".ToLower();

                var compiler = (fileType.StartsWith("vb") ? "vbc" : "csc");   // all other scripts will be treated as C#

                var request = $@"{compiler} {common_args.JoinBy(" ")}  /out:""{assembly}"" {refs_args.JoinBy(" ")} {source_args.JoinBy(" ")}"
                              .SplitCommandLine();

                cmpl_cmd = request.JoinBy(" ");

                log_cmd(cmpl_cmd);

                // ensure server running it will gracefully exit if another instance is running
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
                Profiler.EngineContext = "Building with raw csc engine...";
                cmd = $@"""{Globals.GetCompilerFor(sources.FirstOrDefault())}"" {common_args.JoinBy(" ")} /out:""{assembly}"" {refs_args.JoinBy(" ")} {source_args.JoinBy(" ")}";
                cmpl_cmd = cmd;

                log_cmd(cmpl_cmd);

                result.NativeCompilerReturnValue = Globals.dotnet.Run(cmd, build_dir, x => result.Output.Add(x), x => std_err += x);
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

                if (options.BuildExe)
                {
                    var runtimeconfig = "{'runtimeOptions': {'framework': {'name': 'Microsoft.NETCore.App', 'version': '{version}'}}}"
                            .Replace("'", "\"")
                                     .Replace("{version}", Environment.Version.ToString());

                    File.WriteAllText(result.PathToAssembly.ChangeExtension(".runtimeconfig.json"), runtimeconfig);
                    try
                    {
                        // CSUtils.
                        if (Runtime.IsLinux)
                            File.Move(result.PathToAssembly, result.PathToAssembly.RemoveAssemblyExtension(), true);
                        else
                            File.Move(result.PathToAssembly, result.PathToAssembly.ChangeExtension(".exe"), true);
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
                    // When running on Linux as sudo CS-Script creates temp folders (e.g. cache)
                    // that automatically get write-protected if accessed by not sudo-process. On
                    // Windows these folders are always not protected. Meaning that if the build
                    // server is running as non root (sudo) it will fail to place the output to
                    // these folders. The solution is to either restart build server as sudo or use
                    // dotnet engine as it always starts and stops with the cscs.exe executable and
                    // always inherits its root context.

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
    }
}