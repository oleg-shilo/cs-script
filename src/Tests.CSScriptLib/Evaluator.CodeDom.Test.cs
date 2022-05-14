using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using csscript;
using CSScripting;
using CSScriptLib;
using Xunit;

namespace EvaluatorTests
{
    public class Generic_CodeDom
    {
        [Fact]
        public void using_CompilerOptions()
        {
            // Note if you give AssemblyFile the name with the extension .dll xUnit runtime will lock the file simply
            // because it was present in the local dir. So hide the assembly by dropping the file extension and giving the name "using_CompilerOptions".

            var info = new CompileInfo
            {
                CompilerOptions = "-define:test",
                AssemblyFile = nameof(using_CompilerOptions).GetFullPath()
            };

            Assembly asm = CSScript.CodeDomEvaluator.CompileCode(
                     @"using System;
                      public class Script
                      {
                          public int Sum(int a, int b)
                          {
                              #if test
                                  return -(a+b);
                              #else
                                  return a+b;
                              #endif
                          }
                      }",
                     info);

            dynamic script = asm.CreateObject("*");
            var result = script.Sum(7, 3);
            Assert.Equal(-10, result);
        }

        [Fact]
        public void call_UnloadAssembly()
        {
            dynamic script = CSScript.CodeDomEvaluator
                                     .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                     .LoadMethod(@"public object func()
                                               {
                                                   return new[] {0,5};
                                               }");

            var result = (int[])script.func();

            var asm_type = (Type)script.GetType();

            asm_type.Assembly.Unload();
        }

        [Fact]
        public void call_LoadMethod()
        {
            CSScript.EvaluatorConfig.DebugBuild = true;
            CodeDomEvaluator.CompileOnServer = true;

            dynamic script = CSScript.CodeDomEvaluator
                                     .LoadMethod(@"public object func()
                                                {
                                                    return new[] {0,5};
                                                }");

            var result = (int[])script.func();

            Profiler.Stopwatch.Start();
            script = CSScript.CodeDomEvaluator
                             .LoadMethod(@"public object func()
                                           {
                                               return 77;
                                           }");

            var resultsum = (int)script.func();

            var time = Profiler.Stopwatch.ElapsedMilliseconds;

            Assert.Equal(0, result[0]);
            Assert.Equal(5, result[1]);
        }

        [Fact]
        public void call_CompileMethod()
        {
            dynamic script = CSScript.CodeDomEvaluator
                                     .CompileMethod(@"public object func() => new[] {0,5}; ")
                                     .CreateObject("*.DynamicClass");

            var result = (int[])script.func();

            Assert.Equal(0, result[0]);
            Assert.Equal(5, result[1]);
        }

        [Fact]
        public void referencing_script_types_from_another_script()
        {
            CSScript.EvaluatorConfig.DebugBuild = true;
            CSScript.EvaluatorConfig.ReferenceDomainAssemblies = false;

            var info = new CompileInfo { AssemblyFile = "utils_asm" };

            try
            {
                var utils_code = @"using System;
                               using System.Collections.Generic;
                               using System.Linq;

                               public class Utils
                               {
                                   public class Printer { }

                                   static void Main(string[] args)
                                   {
                                       var x = new List<int> {1, 2, 3, 4, 5};
                                       var y = Enumerable.Range(0, 5);

                                       x.ForEach(Console.WriteLine);
                                       var z = y.First();
                                       Console.WriteLine(z);
                                   }
                               }";

                var asm = CSScript.CodeDomEvaluator
                                  .CompileCode(utils_code, info);

                dynamic script = CSScript.CodeDomEvaluator
                                         .ReferenceAssembly(info.AssemblyFile)
                                         .CompileMethod(@"public Utils NewUtils() => new Utils();
                                                      public Utils.Printer NewPrinter() => new Utils.Printer();")
                                         .CreateObject("*");

                object utils = script.NewUtils();
                object printer = script.NewPrinter();

                Assert.Equal("Utils", utils.GetType().ToString());
                Assert.Equal("Utils+Printer", printer.GetType().ToString());
            }
            finally
            {
                info.AssemblyFile.FileDelete(rethrow: false); // assembly is locked so only showing the intention
            }
        }

        [Fact]
        public void use_resp_file()
        {
            var respFile = $"{nameof(use_resp_file)}.resp".GetFullPath();
            File.WriteAllText(respFile, "/r:Foo.dll");

            CSScript.EvaluatorConfig.CompilerOptions = $"\"@{respFile}\"";

            try
            {
                dynamic script = CSScript.CodeDomEvaluator
                                         .LoadMethod(@"public (int, int) func()
                                                   {
                                                       return (0,5);
                                                   }");
            }
            catch (CompilerException e)
            {
                Assert.Contains("Metadata file 'Foo.dll' could not be found", e.Message);
            }
        }

        [Fact]
        public void use_ScriptCaching()
        {
            var code = "object func() => new[] { 0, 5 };";

            // cache is created and the compilation result is saved
            CSScript.CodeDomEvaluator
                    .With(eval => eval.IsCachingEnabled = true)
                    .LoadMethod(code);

            // cache is used instead of recompilation
            var sw = Stopwatch.StartNew();

            CSScript.CodeDomEvaluator
                    .With(eval => eval.IsCachingEnabled = true)
                    .LoadMethod(code);

            var cachedLoadingTime = sw.ElapsedMilliseconds;
            sw.Restart();

            // cache is not used and the script is recompiled again
            CSScript.CodeDomEvaluator
                    .With(eval => eval.IsCachingEnabled = false)
                    .LoadMethod(code);

            var noncachedLoadingTime = sw.ElapsedMilliseconds;

            Assert.True(cachedLoadingTime < noncachedLoadingTime);
        }

        [Fact]
        public void use_interfaces_between_scripts()
        {
            IPrinter printer = CSScript.CodeDomEvaluator
                                       .ReferenceAssemblyOf<IPrinter>()
                                       .LoadCode<IPrinter>(@"using System;
                                                         public class Printer : IPrinter
                                                         {
                                                            public void Print()
                                                                => Console.Write(""Printing..."");
                                                         }");

            dynamic script = CSScript.Evaluator
                                     .ReferenceAssemblyOf<IPrinter>()
                                     .LoadMethod(@"void Test(IPrinter printer)
                                               {
                                                   printer.Print();
                                               }");
            script.Test(printer);

            // does not throw :)
        }

        [Fact]
        public void import_script_from_another_scripts()
        {
            var script_math = "math.cs".GetFullPath();
            var script_calc = "calc.cs".GetFullPath();

            File.WriteAllText(script_math,
                              @"using System;

                              public class math
                              {
                                  public static int add(int a, int b) => a+b;
                              }");

            File.WriteAllText(script_calc,
                              $@"//css_inc {script_math}
                               using System;

                               public class Calc
                               {{
                                    public int Add(int a, int b)
                                        => math.add(a, b);
                               }}");

            dynamic calc = CSScript.CodeDomEvaluator
                                   .LoadFile(script_calc);

            var result = calc.Add(1, 4);

            Assert.Equal(5, result);
        }

        // [Fact(Skip = "VB is not supported yet")]
        // public void VB_Generic_Test()
        // {
        //     Assembly asm = CSScript.CodeDomEvaluator
        //                            .CompileCode(@"' //css_ref System
        //                                       Imports System
        //                                       Class Script

        //                                         Function Sum(a As Integer, b As Integer)
        //                                             Sum = a + b
        //                                         End Function

        //                                     End Class");

        //     dynamic script = asm.CreateObject("*");
        //     var result = script.Sum(7, 3);
        // }
    }
}