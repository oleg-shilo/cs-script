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
            PrepareCodeDomCompilers();

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

        static void PrepareCodeDomCompilers()
        {
            // The default C# compiler that comes with Windows supports only C# 5 syntax.
            // For compiling C# 7.3 syntax you will need to install a newer one.
            // You can install the newer compiler by either installing .NET SDK or downloading SDK tools NuGet package.
            // If you want to download the compiler from nuget.org either manually from
            // https://api.nuget.org/v3-flatcontainer/microsoft.net.compilers.toolset/5.0.0/microsoft.net.compilers.toolset.5.0.0.nupkg
            // or by using CS-Script's own downloader: Uncomment next two lines:
            //
            // NugetPackageDownloader.OnProgressOutput = Console.WriteLine;
            // NugetPackageDownloader.DownloadLatestFrameworkCompiler(includePrereleases: false);
            //
            // This sample works even if you do not uncomment the code above because the compiling tools package is added to this
            // project.

            // Globals.csc is initialized internally the same way. Providing it here for demo purposes only.
            Globals.csc =
                Globals.FindFrameworkToolsetPackageCompiler() ?? // lookup the installed nuget package with the compiler
                Globals.FindDefaultFrameworkCompiler(); // the default compiler, which is a part of Windows OS

            // ========================================================
            // RUNTIME: Note, when running the first time the loading overhead of csc.exe is noticeable but on the subsequent runs it becomes up to 10
            // times faster as csc.exe is already loaded in memory. It stays loaded even after the host application is restarted.
            // It is .NET own caching mechanism that keeps the compiler loaded in memory and it is not related to CS-Script.
            //
            // DEPLOYMENT: If you need to deploy your application to an environment where Microsoft.Net.Compilers.Toolset package is not available you can
            // copy csc.exe and its dependencies to the same folder as your application and set the path to csc.exe in Globals.csc.
        }
    }
}