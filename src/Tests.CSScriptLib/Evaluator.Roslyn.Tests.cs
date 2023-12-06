using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Scripting;
using csscript;
using CSScripting;
using CSScriptLib;
using Testing;
using Xunit;

public interface IPrinter
{
    void Print();
}

// static class extensions { public class UnloadableAssemblyLoadContext : AssemblyLoadContext {
// public UnloadableAssemblyLoadContext(string name = null) : base(name ??
// Guid.NewGuid().ToString(), isCollectible: true) { } } }

namespace EvaluatorTests
{
    [Collection("Sequential")]
    public class Generic_Roslyn
    {
        [Fact(Skip = "xUnit runtime is incompatible. But test is valid")]
        public void call_UnloadAssembly()
        {
            // There is something strange happening under xUnit runtime. This very test runs fine
            // from a console app but under a test runner the assembly stays in the memory. Possibly
            // because xUnit uses dynamic types. See "Test_Unloading" method for details (https://github.com/oleg-shilo/cs-script/blob/master/src/CSScriptLib/src/Client.NET-Core/Program.cs)

            int? count = null;

            for (int i = 0; i < 10; i++)
            {
                call_SuccessfulUnloadAssembly();
                var newCount = AppDomain.CurrentDomain.GetAssemblies().Count();

                if (count.HasValue)
                    Assert.Equal(count, newCount);

                GC.Collect();

                count = newCount;
            }
        }

        [Fact(Skip = "xUnit runtime is incompatible. But test is valid")]
        public void call_SuccessfulUnloadAssembly()
        {
            ICalc script = CSScript.RoslynEvaluator
                                   .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                   .LoadMethod<ICalc>(@"public int Sum(int a, int b) { return a+b; }");

            var result = script.Sum(1, 2);

            script.GetType().Assembly.Unload();
        }

        void call_FailingUnloadAssembly()
        {
            // dynamic will trigger an accidental referencing the assembly under the hood of CLR and
            // it will not be collected.
            dynamic script = CSScript.RoslynEvaluator
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
        public void use_ScriptCaching()
        {
            var code = "object func() => new[] { 0, 5 }; // " + Guid.NewGuid();

            // cache is created and the compilation result is saved
            var script = CSScript.RoslynEvaluator
                    .With(eval => eval.IsCachingEnabled = true)
                    .LoadMethod(code);

            CSScript.RoslynEvaluator
                    .With(eval => eval.IsCachingEnabled = true)
                    .LoadMethod(code);

            // cache is used instead of recompilation
            var sw = Stopwatch.StartNew();

            CSScript.RoslynEvaluator
                    .With(eval => eval.IsCachingEnabled = true)
                    .LoadMethod(code);
            var cachedLoadingTime = sw.ElapsedMilliseconds;
            sw.Restart();

            // cache is not used and the script is recompiled again
            CSScript.RoslynEvaluator
                    .With(eval => eval.IsCachingEnabled = false)
                    .LoadMethod(code);

            var noncachedLoadingTime = sw.ElapsedMilliseconds;

            Assert.True(cachedLoadingTime < noncachedLoadingTime);
            return;
        }

        [Fact]
        public void call_LoadMethod()
        {
            dynamic script = CSScript.RoslynEvaluator
                                     .LoadMethod(@"public object func()
                                               {
                                                   return new[] {0,5};
                                               }");

            var result = (int[])script.func();

            var asm_type = (Type)script.GetType();

            var asm = asm_type.Assembly.Location();

            Assert.Equal(0, result[0]);
            Assert.Equal(5, result[1]);
        }

        [Fact]
        public void issue_251()
        {
            var calc = CSScript
                .Evaluator
                .LoadCode<Testing.ICalc>(
                                  @"using System;
                                    public class Script : Testing.ICalc
                                    {
                                        public int Sum(int a, int b)
                                        {
                                            return a+b;
                                        }
                                    }");
            var result = calc.Sum(1, 2);
            Console.WriteLine(result);
        }

        [Fact]
        public void issue_259()
        {
            var before = AppDomain.CurrentDomain.GetAssemblies().Count();
            // -----

            Assembly asm = CSScript.Evaluator
                                   .With(e => e.IsCachingEnabled = true)
                                   .CompileCode(@"using System;
                                                   public class Script
                                                   {
                                                       void Log(string message)
                                                       {
                                                           Console.WriteLine(message);
                                                       }
                                                   }");
            asm.CreateObject("*");

            var asmFile = asm.Location();

            var after = AppDomain.CurrentDomain.GetAssemblies().Count();
        }

        [Fact]
        public void use_AssembliesFilter()
        {
            string[] refAssemblies = null;

            var eval = CSScript.RoslynEvaluator;

            dynamic script = eval.ReferenceDomainAssemblies()
                                 .SetRefAssemblyFilter(asms =>
                                     {
                                         refAssemblies = asms.Select(a => a.Location)
                                                             .Distinct()
                                                             .ToArray();

                                         return asms.Where(a => a.FullName != Assembly.GetExecutingAssembly().FullName);
                                     })
                                 .LoadMethod(@"public object func()
                                               {
                                                   return new[] {0,5};
                                               }");

            var filteresAssemblies = eval.GetReferencedAssemblies()
                                         .Select(a => a.Location)
                                         .ToArray();

            Assert.Equal(1, refAssemblies.Count() - filteresAssemblies.Count());
        }

        [Fact]
        public void call_CompileMethod()
        {
            dynamic script = CSScript.RoslynEvaluator
                                     .CompileMethod(@"public object func() => new[] {0,5}; ")
                                     .CreateObject("*.DynamicClass");

            var result = (int[])script.func();

            Assert.Equal(0, result[0]);
            Assert.Equal(5, result[1]);
        }

        [Fact]
        public void referencing_script_types_from_another_script()
        {
            CSScript.EvaluatorConfig.ReferenceDomainAssemblies = false; // to avoid an accidental referencing
            CSScript.EvaluatorConfig.DebugBuild = true;

            var info = new CompileInfo
            {
                RootClass = "script_a",
                AssemblyName = "script_a",
                AssemblyFile = "script_a_asm2\\script_a_asm2.dll".EnsureFileDir()
            };

            try
            {
                var code2 = @"using System;
                      using System.Collections.Generic;
                      using System.Linq;

                      public class Utils
                      {
                          static void Main(string[] args)
                          {
                              var x = new List<int> {1, 2, 3, 4, 5};
                              var y = Enumerable.Range(0, 5);

                              x.ForEach(Console.WriteLine);
                              var z = y.First();
                              Console.WriteLine(z);
                          }
                      }";

                var asm = CSScript.RoslynEvaluator
                        .With(e => e.IsCachingEnabled = false) // required to not interfere with xUnit
                        .CompileCode(code2, info);

                dynamic script = CSScript.RoslynEvaluator
                                         .With(e => e.IsCachingEnabled = false)
                                         // .With(e => e.ReferenceDomainAssemblies = false)

                                         .ReferenceAssembly(info.AssemblyFile)
                                         .CompileMethod(@"using static script_a;
                                                  Utils Test()
                                                  {
                                                      return new Utils();
                                                  }")
                                         .CreateObject("*");
                object utils = script.Test();

                Assert.Equal("script_a+Utils", utils.GetType().ToString());
            }
            finally
            {
                info.AssemblyFile.FileDelete(rethrow: false);
            }
        }

        [Fact]
        public void use_interfaces_between_scripts()
        {
            IPrinter printer = CSScript.RoslynEvaluator
                                       .ReferenceAssemblyOf<IPrinter>()
                                       .LoadCode<IPrinter>(@"using System;
                                                         public class Printer : IPrinter
                                                         {
                                                            public void Print()
                                                                => Console.Write(""Printing..."");
                                                         }");

            dynamic script = CSScript.RoslynEvaluator
                                     .ReferenceAssemblyOf<IPrinter>()
                                     .LoadMethod(@"void Test(IPrinter printer)
                                               {
                                                   printer.Print();
                                               }");
            script.Test(printer);
        }

        // [Fact(Skip = "VB is not supported yet")] // hiding it from xUnit public void
        // VB_Generic_Test() { Assembly asm = CSScript.RoslynEvaluator .CompileCode(@"' //css_ref
        // System Imports System Class Script

        // Function Sum(a As Integer, b As Integer) Sum = a + b End Function

        // End Class");

        // dynamic script = asm.CreateObject("*"); var result = script.Sum(7, 3); }

        [Fact]
        public void Issue_337()
        {
            var info = new CompileInfo
            {
                RootClass = "AccountingScript",
                AssemblyFile = "D:\\AccountingScript.dll",
            };

            var accounting_assm2 = CSScript.Evaluator
                                   .With(e => e.IsCachingEnabled = false)
                                   .CompileCode(@"public class TXBase
                                                  {
                                                      public string TranId { get; set; }
                                                      public string HashKey { get; set; }
                                                  }", info);

            dynamic script1 = CSScript.Evaluator
                              .ReferenceAssembly(accounting_assm2)
                              .LoadCode(@"public class TXPayment
                            {
                                public string Reason { get; set; }
                                public string Test()
                                {
                                    var tx = new AccountingScript.TXBase();
                                    tx.TranId = ""1"";
                                    return tx.TranId;
                                }
                            }");

            string r = script1.Test().ToString();
            // Assert.Equal(3, statements.Count());
        }

        public static string log = "";

        [Fact]
        public void Issue_354()
        {
            var info = new CompileInfo { RootClass = "Printing", AssemblyFile = "Printer.dll" };

            var printer_asm = CSScript.Evaluator
                                      .ReferenceAssemblyOf(this)
                                      .CompileCode(@"using System;
                                                     using System.Diagnostics;
                                                     public class Printer
                                                     {
                                                         public static void Print()
                                                         {
                                                             EvaluatorTests.Generic_Roslyn.log = ""test"";
                                                             Debug.WriteLine(""Printing..."");
                                                         }
                                                     }", info);

            dynamic script = CSScript.Evaluator
                                     .ReferenceAssembly(printer_asm)
                                     .Eval("Printing.Printer.Print();");
            // .LoadMethod(@"public void TestPrint() => Printing.Printer.Print();");

            // Assert.Equal(3, statements.Count());
        }

        [Fact]
        public void Issue_297()
        {
            var code =
                "//css_imp ..\\Common\\V1\\Common1.cs\r\n" +
                "//css_imp ..\\Common\\V1\\Common2.cs\r\n" +
                "//css_imp ..\\Common\\V1\\Common3.cs";

            var parser = new CSharpParser(code);
            string[] statements = parser.GetRawStatements(code, "//css_imp", code.Length, false);

            Assert.Equal(3, statements.Count());
        }

        [Fact]
        public void Issue_291()
        {
            var code =
                "//css_include WorldRenderGameComponent\n" +
                "\n" +
                "using System;";

            var parser = new CSharpParser(code);
            string[] statements = parser.GetRawStatements(code, "//css_include", code.Length, false);

            Assert.Single(statements);

            code = @"//css_inc D:\dev\Galos\cs-script\src\Tests.CSScriptLib\bin\Debug\net6.0\depCompileCodeWithNestedImports2.cs";
            parser = new CSharpParser(code);
            statements = parser.GetRawStatements(code, "//css_inc", code.Length, false);

            Assert.Single(statements);
        }

        [Fact]
        public void Issue_185_Referencing()
        {
            var root_class_name = $"script_{System.Guid.NewGuid()}".Replace("-", "");

            var info = new CompileInfo { RootClass = root_class_name, PreferLoadingFromFile = true };
            try
            {
                var printer_asm = CSScript.RoslynEvaluator
                                          .CompileCode(@"using System;
                                                 public class Printer
                                                 {
                                                     public void Print() => Console.Write(""Printing..."");
                                                 }", info);

                dynamic script = CSScript.RoslynEvaluator
                                         .ReferenceAssembly(printer_asm)
                                         .LoadMethod($"using static {root_class_name};" + @"
                                               void Test()
                                               {
                                                   new Printer().Print();
                                               }");
                script.Test();
            }
            finally
            {
                info.AssemblyFile.FileDelete(rethrow: false);
            }
        }
    }
}

namespace Testing
{
    public interface ICalc
    {
        int Sum(int a, int b);
    }

    public interface ICalc<TResult, TParam>
    {
        TResult Sum(TParam a, TParam b);
    }

    public class Wrapped<T>
    {
        public Wrapped(T value)
        {
            Value = value;
        }

        public T Value { get; set; }
    }
}