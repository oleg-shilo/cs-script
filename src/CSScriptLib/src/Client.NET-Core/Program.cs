using System;
using System.Diagnostics;
using System.Linq;
using CSScripting;
using CSScriptLib;

namespace ConsoleApp1
{
    static class ttt
    {
        public static string GetGenericTypeBaseName(this Type type)
        {
            if (type.IsGenericType)
                return type.FullName.Split('`').FirstOrDefault();
            return null;
        }
    }

    class CustomDialogContent { }

    class CustomDialogContent2 { }

    class CustomDialog<T> { }

    class Program
    {
        static void Main(string[] args)
        {
            var type = typeof(CustomDialog<CustomDialogContent>);
            var type2 = typeof(CustomDialog<CustomDialogContent2>);

            var n = type2.GetGenericTypeBaseName();

            var t1 = type.GetGenericArguments();
            var t2 = type2.GetGenericArguments();
            Type d4 = type2.GetGenericTypeDefinition();

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