using CSScriptLib;
using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.IO;
using System.Text;

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System.Runtime.Loader;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace EvalTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var ttt = "".Cast<char>();
            // CSScript.EvaluatorConfig.DebugBuild = true;

            // Test.ReferencingPackagesCode(); //return;
            // Test.CompileCode();
            // Test.CompileMethod();
            // Test.CompileCSharp_7();
            // Test.CompileDelegate();
            // Test.CompileDelegate1();
            // Test.LoadCode();
            // Test.LoadCode2();
            // Test.CrossReferenceCode();

            // dynamic func1 = CSScript.Evaluator.LoadMethod(
            //       @"public object Func()
            //         {
            //             return 1;
            //         }");
            // Console.WriteLine("Result: " + func1.Func().ToString());

            // dynamic func2 = CSScript.Evaluator.LoadMethod(@"
            //         public object Func()
            //         {
            //             return EvalTest.Program.CallMe();
            //         }");
            // Console.WriteLine("Result: " + func2.Func().ToString());

            dynamic func3 = CSScript.Evaluator.LoadMethod(@"
                    using EvalTest;
                    public object Func()
                    {
                        return 3;
                    }");
            Console.WriteLine("Result: " + func3.Func().ToString());
        }

        public static int CallMe() => 2;
    }
}