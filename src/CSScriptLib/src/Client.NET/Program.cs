using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CSScripting;
using CSScriptLib;

namespace Client.NET
{
    public class Program
    {
        static void Main(string[] args)
        {
            PrepareCodeDomCompilers();

            Console.WriteLine("================\n");
            Console.WriteLine($"Loading and unloading script 20 times");
            Test_Unloading();
            Console.WriteLine("================\n");

            CSScript.StopBuildServer();
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

        static void call_UnloadAssembly()
        {
            CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
            CSScript.EvaluatorConfig.DebugBuild = true;

            var script = CSScript.Evaluator
                                 .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                 .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                     { return a+b; }");

            script.Sum(1, 2);

            script.GetType().Assembly.Unload();
        }

        static Assembly printer_asm;

        static void call_UnloadAssemblyWithDependency()
        {
            var info = new CompileInfo { RootClass = "Printing", AssemblyFile = "Printer.dll", AssemblyName = "PrintAsm" };

            if (printer_asm == null)
                printer_asm = CSScript.Evaluator
                                      .CompileCode(@"using System;
                                                     public class Printer
                                                     {
                                                         public static void Print() =>
                                                             Console.WriteLine(""Printing..."");
                                                     }", info);

            var script = CSScript.Evaluator
                                 .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                 .ReferenceAssembly(printer_asm)
                                 .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                     {
                                                         Printing.Printer.Print();
                                                         return a+b;
                                                     }");
            script.Sum(1, 2);
            script.GetType().Assembly.Unload();
        }

        static void call_UnloadAssembly_Failing()
        {
            // using 'dynamic` completely breaks CLR unloading mechanism. Most likely it triggers an
            // accidental referencing of the assembly or System.Runtime.Loader.AssemblyLoadContext.
            dynamic script = CSScript.Evaluator
                                     .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                     .LoadMethod(@"public int Sum(int a, int b)
                                                { return a+b; }");

            script.Sum(1, 2);

            (script as object).GetType().Assembly.Unload();
        }

        static void call_UnloadAssembly_Crashing_CLR()
        {
            CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
            CSScript.EvaluatorConfig.DebugBuild = true;

            var script = CSScript.Evaluator
                                 .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                 .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                     { return a+b; }");

            script.Sum(1, 2);

            GC.Collect(); // see https://github.com/oleg-shilo/cs-script/issues/301 for details

            script.GetType().Assembly.Unload();
        }

        static void Test_Unloading()
        {
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine("Loaded assemblies count: " + AppDomain.CurrentDomain.GetAssemblies().Count());

                call_UnloadAssembly();
                //call_UnloadAssemblyWithDependency(); // also works OK; provided just for demo

                // call_UnloadAssembly_Failing();
                // call_UnloadAssembly_Crashing_CLR();
                GC.Collect();
            }
        }

        static void PrepareCodeDomCompilers()
        {
            // If you are using CodeDom evaluator and your hosting environment does not have .NET SDK installed
            // you will need to install the compiler by downloading SDK tools NuGet package.
            // Either manually from
            // https://api.nuget.org/v3-flatcontainer/microsoft.net.sdk.compilers.toolset/10.0.103/microsoft.net.sdk.compilers.toolset.10.0.103.nupkg
            // or by using CS-Script's own downloader. Uncomment next two lines:
            //
            // NugetPackageDownloader.OnProgressOutput = Console.WriteLine;
            // NugetPackageDownloader.DownloadLatestSdkCompiler(includePrereleases: false);
            // This sample works even if you do not uncomment the code above because the compiling tools package is added to this
            // project.

            // Globals.csc is internally initialized the same way. Providing it here for demo purposes only.
            Globals.csc =
                Globals.FindSdkCompilersPackageCompiler(includePrereleases: false) ?? // from the installed Microsoft.Net.Sdk.Compilers.Toolset package
                Globals.FindSdKCompiler(); // or from .NET SDK installed on OS
        }
    }

    public interface ICalc
    {
        int Sum(int a, int b);
    }
}