using System;
using System.Diagnostics;
using CSScripting;
using CSScriptLib;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            CSScript.StopBuildServer();
            CSScript.EvaluatorConfig.DebugBuild = true;

            var sw = Stopwatch.StartNew();

            Console.WriteLine($"Hosting runtime: .NET { (Runtime.IsCore ? "Core" : "Framework")}");
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
            CodeDomEvaluator.CompileOnServer = true;

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

            (int, int) result = script.func();
        }
    }
}