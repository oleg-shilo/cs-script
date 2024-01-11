﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CSScripting;
using CSScriptLib;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
#pragma warning ("This solution will no longer work on .NET4.8 for the CS-Script versions starting from v4.8.13. The problem is caused by the latest Roslyn stopping supporting `System.Runtime.Loader` on .NET Framework.")
            // this solution only provided for the demo purposes.

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

            (int, int) result = script.func();
        }
    }
}