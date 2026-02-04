using System;
using System.Diagnostics;
using System.IO;
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
            Test_Roslyn();

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

            Console.WriteLine("\nRoslyn");
            sw.Restart();
            Test_Roslyn();
            Console.WriteLine("  first run: " + sw.ElapsedMilliseconds);
            sw.Restart();
            Test_Roslyn();
            Console.WriteLine("  next run: " + sw.ElapsedMilliseconds);
        }

        static void Test_CodeDom()
        {
            dynamic script = CSScript.CodeDomEvaluator
                                     .LoadMethod(@"public (int, int) func()
                                                   {
                                                       return (0,5);
                                                   }");

            (int, int) result = script.func();
        }

        static void Test_Roslyn()
        {
            dynamic script = CSScript.RoslynEvaluator
                                     .LoadMethod(@"public (int, int) func()
                                                   {
                                                       return (0,5);
                                                   }");

            int sum = CSScript.RoslynEvaluator.Eval("6 + 3");
            (int, int) result = script.func();
        }
    }
}