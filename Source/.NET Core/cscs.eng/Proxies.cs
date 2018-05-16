using csscript;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace System.CodeDom.Compiler
{
    internal class CSharpCompiler : ICodeCompiler
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


        static string _dotnet = Process.GetCurrentProcess().MainModule.FileName;
        static string dotnet { get { return _dotnet; } }

        static string InitBuildTools()
        {
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var cache_root = cache_dir.GetDirName();
            var build_root = cache_root.GetDirName().PathJoin("build").EnsureDir();

            var proj_template = build_root.PathJoin("build.csproj");

            if (!File.Exists(proj_template))
            {
                Run("dotnet.exe", "new console", build_root);
                build_root.PathJoin("Program.cs").DeleteIfExists();
            }

            return proj_template;
        }

        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            string projectName = fileNames.First().GetFileName();
            string projectShortName = Path.GetFileNameWithoutExtension(projectName);

            var template = InitBuildTools();

            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = cache_dir.PathJoin(".build", projectName);

            build_dir.DeleteDir()
                     .EnsureDir();

            var project_content = File.ReadAllText(template);

            if (!options.GenerateExecutable)
                project_content = project_content.Replace("<OutputType>Exe</OutputType>", "");

            // In .NET all references including GAC assemblies must be passed to the compiler.
            // In .NET Core this creates a problem as the compiler does not expect any default (shared)
            // assemblies to be passed. So we do need to exclude them.
            // Though we still need to keep shared assembly resolving in the host as the future compiler 
            // require ALL ref assemblies to be pushed to the compiler. 

            var refs = new StringBuilder();
            var ref_assemblies = options.ReferencedAssemblies.Where(x => !x.IsSharedAssembly() && Path.IsPathRooted(x));

            foreach (string asm in ref_assemblies)
                refs.AppendLine($@"<Reference Include=""{asm.GetFileName()}""><HintPath>{asm}</HintPath></Reference>");

            project_content = project_content.Replace("</Project>",
                                                    $@"  <ItemGroup>
                                                        {refs.ToString()}
                                                        </ItemGroup>
                                                     </Project>");

            File.WriteAllText(build_dir.PathJoin(projectShortName + ".csproj"), project_content);

            fileNames.ForEach(x => x.CopyFileTo(build_dir));

            var assembly = build_dir.PathJoin("exe", projectShortName + ".dll");

            var result = new CompilerResults();

            result.NativeCompilerReturnValue = Run(dotnet, "build -o exe", build_dir, x => result.Output.Add(x));

            result.ProcessErrors();

            result.Errors
                  .ForEach(x=>
                  {
                      // by default x.FileName is a file name only 
                      x.FileName = fileNames.FirstOrDefault(f => f.EndsWith(x.FileName)) ?? x.FileName;
                  });

            if (result.NativeCompilerReturnValue == 0 && File.Exists(assembly))
            {
                result.PathToAssembly = options.OutputAssembly;
                File.Copy(assembly, result.PathToAssembly, true);
            }

            build_dir.DeleteDir();

            return result;
        }

        private static Thread StartMonitor(StreamReader stream, Action<string> action = null)
        {
            var thread = new Thread(x =>
            {
                try
                {
                    string line = null;
                    while (null != (line = stream.ReadLine()))
                        action?.Invoke(line);
                }
                catch { }
            });
            thread.Start();
            return thread;
        }

        private static int Run(string exe, string args, string dir = null, Action<string> onOutput = null, Action<string> onError = null)
        {
            var process = new Process();

            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = args;
            process.StartInfo.WorkingDirectory = dir;

            // hide terminal window
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            var error = StartMonitor(process.StandardError, onError);
            var output = StartMonitor(process.StandardOutput, onOutput);

            process.WaitForExit();

            try { error.Abort(); } catch { }
            try { output.Abort(); } catch { }

            return process.ExitCode;
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

    public class CompilerResults
    {
        public TempFileCollection TempFiles { get; set; } = new TempFileCollection();
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
                    if (line.StartsWith("Build FAILED.") || line.StartsWith("Build succeeded."))
                        isErrroSection = true;
                }
                else
                {
                    if (!line.IsEmpty())
                    {
                        var error = CompilerError.Parser(line);
                        if(error != null)
                            Errors.Add(error);
                    }
                }
            }

        }
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
            // script.cs(11,8): error CS1029: #error: 'this is the error...' [C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            // script.cs(10,10): warning CS1030: #warning: 'DEBUG is defined' [C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            bool isError = compilerOutput.Contains("): error ");
            bool isWarning = compilerOutput.Contains("): warning ");

            if (isWarning || isError)
            {
                var result = new CompilerError();

                var items = compilerOutput.Split(":".ToArray(), 3);
                try
                {
                    var location_items = items[0].Split("(,)".ToArray());

                    result.ErrorText = items[2];
                    result.ErrorNumber = items[1].Split(' ').Last();
                    result.IsWarning = isWarning;
                    result.FileName = location_items[0];
                    result.Line = int.Parse(location_items[1]);
                    result.Column = int.Parse(location_items[2]);

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
            return null;
        }
    }

    public class CompilerParameters
    {
        private List<string> linkedResources = new List<string>();
        private List<string> embeddedResources = new List<string>();
        private List<string> referencedAssemblies = new List<string>();

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