using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CSScripting;
using CSScriptLib;

namespace Client.NET472
{
    class Program
    {
        static void Main(string[] args)
        {
            // This solution only provided for the demo purposes.
            // note that CSScriptLib is compiled against the latest `Microsoft.CodeAnalysis.dll`. However .NET Framework does not
            // support this version of `Microsoft.CodeAnalysis.dll` so the project packages are referencing older version of Microsoft.CodeAnalysis.dll
            // but we need to use `SimpleAsmProbing` to load the compatible version of `Microsoft.CodeAnalysis.dll` at runtime.

            using (SimpleAsmProbing.For(Assembly.GetExecutingAssembly().Location.GetDirName()))
            {
                main(args);
            }
        }

        static void main(string[] args)
        {
            // note that csc.exe compiler references some assemblies by default so we need
            // to use WithRefAssembliesFilter to avoid "referenced assembly duplication" compiler error

            NetCompiler.EnableLatestSyntax();
            CSScript.EvaluatorConfig.DebugBuild = true;

            var sw = Stopwatch.StartNew();

            Console.WriteLine($"Hosting runtime: .NET {(Runtime.IsCore ? "Core" : "Framework")}");
            Console.WriteLine("================\n");

            Console.WriteLine("CodeDOM");
            Test_CodeDom();
            Console.WriteLine("  first run: " + sw.ElapsedMilliseconds);
            sw.Restart();

            Test_CodeDom();
            Console.WriteLine("  next run: " + sw.ElapsedMilliseconds);
            sw.Restart();

            Test_CodeDom_GAC();
            Console.WriteLine("  next run: " + sw.ElapsedMilliseconds);
        }

        // private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        // {
        //     var ttt = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == args.Name);

        //     throw new NotImplementedException();
        // }

        static void Test_CodeDom()
        {
            dynamic script = CSScript.CodeDomEvaluator
                                     .WithRefAssembliesFilter(asms => asms.Where(a => !a.EndsWith("System.Core.dll")))
                                     .LoadMethod(@"public object func()
                                                   {
                                                       // return (0,5);   // C# latest syntax
                                                       return new[]{0,5}; // C# 5 syntax
                                                   }");

            var result = script.func();
        }

        static void Test_CodeDom_GAC()
        {
            // System.Net.Http.dll needs t be referenced from GAC so we need to add its location to the probing dir

            dynamic script = CSScript.CodeDomEvaluator
                                     .WithRefAssembliesFilter(asms => asms.Where(a => !a.EndsWith("System.Core.dll") &&
                                                                                      !a.EndsWith("System.dll")))
                                     .LoadCode(@"
                                                //css_dir C:\Windows\Microsoft.NET\assembly\GAC_MSIL\**
                                                //css_ref System.Net.Http.dll
                                                using System;
                                                using System.Net.Http;

                                                public class Test
                                                {
                                                    public void Foo()
                                                    {
                                                        using (var client = new HttpClient()) { }
                                                    }
                                                }");

            script.Foo();
        }
    }
}