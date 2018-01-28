using CSScriptLib;
using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System.Runtime.Loader;

namespace CSScriptLib.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // test();
            CSScript.EvaluatorConfig.DebugBuild = true;

            Test.CompileCode();
            // Test.CompileMethod();
            // Test.CompileCSharp_7();
            // Test.CompileDelegate();
            // Test.CompileDelegate1();
            // Test.LoadCode();
            // Test.LoadCode2();
        }

        static void test()
        {
            var path = @"E:\Galos\Projects\CS-Script\GitHub\cs-script\Source\.NET Core\CSScriptLib\src\CSScriptLib\bin\Debug\netstandard2.0\script.txt";
            var lines = File.ReadAllLines(path);
            var sb = new StringBuilder();
            sb.AppendLine($"#line 1 \"{path}\"");
            foreach (var l in lines)
            {
                if (l.StartsWith("write"))
                {
                    var res = l.Substring(l.IndexOf(" ", StringComparison.Ordinal)).Trim();
                    sb.AppendLine($"System.Console.WriteLine(\"{res}\");");
                }
                else
                    sb.AppendLine(l);
            }

            var script = sb.ToString();
            var options = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default;
            var roslynScript = CSharpScript.Create(script, options);
            var compilation = roslynScript.GetCompilation();

            compilation = compilation.WithOptions(compilation.Options
               .WithOptimizationLevel(OptimizationLevel.Debug)
               .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            using (var assemblyStream = new MemoryStream())
            {
                using (var symbolStream = new MemoryStream())
                {
                    var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);
                    var result = compilation.Emit(assemblyStream, symbolStream, options: emitOptions);
                    if (!result.Success)
                    {
                        var errors = string.Join(Environment.NewLine, result.Diagnostics.Select(x => x));
                        Console.WriteLine(errors);
                        return;
                    }

                    //Execute the script
                    assemblyStream.Seek(0, SeekOrigin.Begin);
                    symbolStream.Seek(0, SeekOrigin.Begin);

                    AssemblyLoadContext context = AssemblyLoadContext.Default;
                    Assembly assembly = context.LoadFromStream(assemblyStream, symbolStream);

                    // var assembly = Assembly.Load(assemblyStream.ToArray(), symbolStream.ToArray());
                    var type = assembly.GetType("Submission#0");
                    var method = type.GetMethod("<Factory>", BindingFlags.Static | BindingFlags.Public);

                    method.Invoke(null, new object[] { new object[2] });
                }
            }
        }
    }
}