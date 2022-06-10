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
            sw.Restart();
            Test_CodeDom_GAC();
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

        static void Test_CodeDom_GAC()
        {
            // System.Net.Http.dll needs t be referenced from GAC so we need to add its location to the probing dir

            dynamic script = CSScript.CodeDomEvaluator
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