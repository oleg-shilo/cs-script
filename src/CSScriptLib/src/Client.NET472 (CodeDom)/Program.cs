using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CSScripting;
using CSScriptLib;

namespace Client.NET472
{
    class Program
    {
        // Note, csc.exe compiler references some assemblies by default so we need
        // to use WithRefAssembliesFilter to avoid "referenced assembly duplication" compiler error
        static IEnumerable<string> FileterOutDefaultAssemblies(IEnumerable<string> asms)
            => asms.Where(a => !a.EndsWith("System.dll") &&
                               !a.EndsWith("System.Core.dll") &&
                               !a.EndsWith("Microsoft.CSharp.dll"));

        static void Main(string[] args)
        {
            CSScript.EvaluatorConfig.DebugBuild = true;

            Console.WriteLine($"Hosting runtime: .NET {(Runtime.IsCore ? "Core" : "Framework")}");

            Test();
            Test_CSharp7();
            Test_GAC();
        }

        static void Test()
        {
            Globals.csc = Globals.DefaultNetFrameworkCompiler;

            dynamic script = CSScript.CodeDomEvaluator
                                     .WithRefAssembliesFilter(FileterOutDefaultAssemblies)
                                     .LoadMethod(@"public object func()
                                                   {
                                                       return new[]{0,5}; // C# 5 syntax
                                                   }");

            var result = script.func();
            Console.WriteLine(result);
        }

        static void Test_CSharp7()
        {
            // The C# compiler that comes with .NET Framework 4.7.2 does not support C# 7.3 syntax so we need to use the one (csc.exe)
            // from Microsoft.Net.Compilers.Toolset package.
            // Note, when running the first time the load overhead of csc.exe is noticeable but on subsequent runs it is up to 10 times faster
            // as csc.exe is already loaded in memory. It stays loaded even after the host application is restarted.
            // It is .NET own caching mechanism that keeps it loaded in memory and it is not related to CS-Script.

            Globals.csc = Globals.MsNetComilersToolsetCompiler;

            dynamic script = CSScript.CodeDomEvaluator
                                     .WithRefAssembliesFilter(FileterOutDefaultAssemblies)
                                     .LoadMethod(@"public object func()
                                                   {
                                                        return (0,5);   // C# 7.3 syntax
                                                   }");

            var result = script.func();
            Console.WriteLine(result);
        }

        static void Test_GAC()
        {
            // System.Net.Http.dll needs t be referenced from GAC so we need to add its location to the probing dir

            dynamic script = CSScript.CodeDomEvaluator
                                     .WithRefAssembliesFilter(FileterOutDefaultAssemblies)
                                     .LoadCode(@"
                                                //css_dir C:\Windows\Microsoft.NET\assembly\GAC_MSIL\**
                                                //css_ref System.Net.Http.dll
                                                using System;
                                                using System.Net.Http;

                                                public class Test
                                                {
                                                    public void Foo()
                                                    {
                                                        using (var client = new HttpClient())
                                                        {
                                                            Console.WriteLine(""Test.Foo()"");
                                                        }
                                                    }
                                                }");

            script.Foo();
        }
    }
}