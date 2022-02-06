using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CSScripting;
using CSScriptLib;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("================\n");
            Console.WriteLine($"Loading and unloading script 20 times");
            Test_Unloading();
            Console.WriteLine("================\n");

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

        static void call_UnloadAssembly()
        {
            var script = CSScript.Evaluator
                                 .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                 .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                     { return a+b; }");

            script.Sum(1, 2);

            script.GetType().Assembly.Unload();
        }

        static void call_UnloadAssembly_Failing()
        {
            // using 'dynamic` completely breaks CLR unloading mechanism. Most likely it triggers an
            // accidental referencing of the assembly or System.Runtime.Loader.AssemblyLoadContext.
            dynamic script = CSScript.Evaluator
                                     .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                     .LoadMethod(@"public int Sum(int a, int b)
                                                { return a+b; }");

            script.Sum(1, 2);

            (script as object).GetType().Assembly.Unload();
        }

        static void Test_Unloading()
        {
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine("Loaded assemblies count: " + AppDomain.CurrentDomain.GetAssemblies().Count());
                call_UnloadAssembly();
                // call_UnloadAssembly_Failing();
                GC.Collect();
            }
        }
    }

    public interface ICalc
    {
        int Sum(int a, int b);
    }
}