using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;
using CSScripting;
using CSScriptLib;

namespace Client.NET472
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Hosting runtime: .NET {(Runtime.IsCore ? "Core" : "Framework")}");

            Test_CodeDom();
            Test_CodeDom_GAC();
            Test_CodeDom_CSharp7();
            Test_Roslyn_Eval();
            Test_Roslyn();
        }

        static void Test_CodeDom()
        {
            dynamic script = CSScript.CodeDomEvaluator
                                     .LoadMethod(@"public object func()
                                                   {
                                                       return new[]{0,5}; // C# 5 syntax
                                                   }");

            var result = script.func();
            Console.WriteLine($"CodeDom:           {result}");
        }

        static void Test_CodeDom_GAC()
        {
            // System.Net.Http.dll needs t be referenced from GAC so we need to add its location to the probing dir

            dynamic script = CSScript.CodeDomEvaluator
                                     .LoadCode(@"//css_dir C:\Windows\Microsoft.NET\assembly\GAC_MSIL\**
                                                 //css_ref System.Net.Http.dll
                                                 using System;
                                                 using System.Net.Http;

                                                 public class Test_CodeDom
                                                 {
                                                     public void Foo()
                                                     {
                                                         using (var client = new HttpClient())
                                                         {
                                                             Console.WriteLine(""CodeDom + GAC:     Test_CodeDom.Foo()"");
                                                         }
                                                     }
                                                 }");

            script.Foo();
        }

        static void Test_CodeDom_CSharp7()
        {
            // The C# compiler (csc.exe) that comes with .NET Framework 4.7.2 does not support C# 7.3 syntax so we need to use the one
            // from Microsoft.Net.Compilers.Toolset package. This package is added to this project because it is required for the
            // CodeDom scripts. You can also use the csc.exe from .NET Core SDK if you have it installed.
            //
            // RUNTIME: Note, when running the first time the loading overhead of csc.exe is noticeable but on the subsequent runs it becomes up to 10
            // times faster as csc.exe is already loaded in memory. It stays loaded even after the host application is restarted.
            // It is .NET own caching mechanism that keeps the compiler loaded in memory and it is not related to CS-Script.
            //
            // DEPLOYMENT: If you need to deploy your application to an environment where Microsoft.Net.Compilers.Toolset package is not available you can
            // copy csc.exe and its dependencies to the same folder as your application and set the path to csc.exe in Globals.csc.
            // The Microsoft.Net.Compilers.Toolset package can be downloaded from NuGet: https://www.nuget.org/packages/Microsoft.Net.Compilers.Toolset/

            Globals.csc = CodeDomEvaluator.MsNetComilersToolsetCompiler;

            dynamic script = CSScript.CodeDomEvaluator
                                     .LoadMethod(@"public object func()
                                                   {
                                                        return (0,5);    // C# 7.3 syntax
                                                   }");

            var result = script.func();
            Console.WriteLine($"CodeDom + C# 7.3:  {result}");
        }

        static void Test_Roslyn_Eval()
        {
            // This approach is rather simplistic. Use it only if you have to.

            int sum = CSScript.RoslynEvaluator.Eval("6 + 3");

            Console.WriteLine($"Roslyn + Eval:     {sum}");
        }

        static void Test_Roslyn()
        {
            dynamic script = CSScript.RoslynEvaluator
                                     .LoadMethod(@"public (int, int) func()
                                                   {
                                                       return (0,5);
                                                   }");

            (int, int) result = script.func();

            Console.WriteLine($"Roslyn:            {result}");
        }
    }
}