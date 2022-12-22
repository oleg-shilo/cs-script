using CSScripting;
using CSScriptLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            NetCompiler.EnableLatestSyntax();
            CSScript.EvaluatorConfig.DebugBuild = true;

            Console.WriteLine("CodeDOM");
            Test_CodeDom();
            Test_CodeDom2();

            Console.WriteLine("\nRoslyn");
            Test_Roslyn();
            Test_Roslyn2();
            Console.WriteLine("\nDone...");
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
        static void Test_CodeDom2()
        {
            dynamic script = CSScript.CodeDomEvaluator
                                     .CompileCode(@"using System;
                                                    namespace testing
                                                    {
                                                        public class Script
                                                        {
                                                            public (int, int) func()
                                                            {
                                                                return (0,5);
                                                            }
                                                        }
                                                    }")
                                        .CreateObject("*");

            (int, int) result = script.func();
        }

        static void Test_Roslyn2()
        {
            dynamic script = CSScript.RoslynEvaluator
                                     .CompileCode(@"using System;
                                                    class Script
                                                    {
                                                        public (int, int) func()
                                                        {
                                                            return (0,5);
                                                        }
                                                    }")
                                     .CreateObject("*");
            (int, int) result = script.func();
        }
    }
}