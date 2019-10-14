using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CSScriptLibrary;

// The VB.NET samples can be found here: https://github.com/oleg-shilo/cs-script/tree/master/Source/NuGet/content/vb

namespace CSScriptNativeApi
{
    public class CodeDom_Roslyn
    {
        public static void Test()
        {
            CSScript.GlobalSettings.UseAlternativeCompiler = LocateRoslynCSSProvider();
            CSScript.GlobalSettings.RoslynDir = LocateRoslynCompilers();

            var sayHello = CSScript.LoadMethod(@"static void SayHello(string greeting)
                                                 {
                                                     var tuple = (1,2);
                                                     void test()
                                                     {
                                                         Console.WriteLine(""Hello from C#7!"");
                                                         Console.WriteLine(tuple);
                                                     }
                                                     test();

                                                     Console.WriteLine(greeting);
                                                 }")
                                   .GetStaticMethod();

            sayHello("Hello World!");
        }
        static string Root { get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); } }

        static string LocateRoslynCompilers()
        {
            try
            {
                // Roslyn compilers are distributed as a NuGet package but the binaries are never copied into the output folder.
                // Thus the path tho the compilers needs to be discovered dynamically.
                // The algorithm below is simplistic and does not check for highest version if multiple packages are found.

                string packageDir = Path.Combine(Root, "..", "..", "..", "packages");
                string roslynDir = Path.Combine(Directory.GetDirectories(packageDir, "Microsoft.Net.Compilers.*").First(), "tools");
                return roslynDir;
            }
            catch
            {
                throw new Exception("Cannot locate Roslyn compiler (csc.exe). You can set it manually ");
            }
        }

        static string LocateRoslynCSSProvider()
        {
            return Path.Combine(Root, "CSSRoslynProvider.dll");
        }
    }
}