using System;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using CSScripting;
using CSScriptLib;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            NetCompiler.EnableLatestSyntax();
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
        }

        static void Test_CodeDom()
        {
            dynamic script = CSScript.CodeDomEvaluator
                                     .LoadMethod(@"public object func()
                                                   {
                                                       // return (0,5);   // C# latest syntax
                                                       return new[]{0,5}; // C# 5 syntax
                                                   }");

            var result = script.func();
        }
    }
}