using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        public static string root = Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("test", "TestFolder", "TestData").EnsureDir();

        string testTempFile(string fileName, [CallerMemberName] string caller = null)
        {
            var rootDir = root.PathJoin(nameof(Generic_Roslyn), caller).GetFullPath().EnsureDir();
            return Path.Combine(rootDir, fileName);
        }

        public Generic_Roslyn()
        {
            // force to load the assembly in the current appdomain so the scripts don't have to reference it explicitly
            // Debug.WriteLine(typeof(Console).Assembly.FullName);
            // Globals.DefaultRoslynCompilationToScript = true;
        }

        [Fact(Skip = "xUnit runtime is incompatible. But the test is valid")]
        public void call_UnloadAssembly()
        {
            // There is something strange happening under xUnit runtime. This very test runs fine
            // from a console app but under a test runner the assembly stays in the memory. Possibly
            // because xUnit uses dynamic types. See "Test_Unloading" method for details (https://github.com/oleg-shilo/cs-script/blob/master/src/CSScriptLib/src/Client.NET/Program.cs)

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

        [Fact(Skip = "xUnit runtime is incompatible. But the test is valid")]
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

            object obj = script;

            var ttt = obj.GetType().GetMethods()[0].Invoke(obj, new object[0]);

            var result = (int[])script.func();

            var asm_type = (Type)script.GetType();

            var asm = asm_type.Assembly.Location();

            Assert.Equal(0, result[0]);
            Assert.Equal(5, result[1]);
        }

        [Fact]
        public void Precompiler_for_script_code()
        {
            var pre = testTempFile("precompiler.cs");

            File.WriteAllText(pre, $@"//css_ref {typeof(PrecompilationContext).Assembly.Location}
                                     using System;
                                     using System.Collections;
                                     public class Sample_Precompiler
                                     {{
                                         public static bool Compile(ref string scriptCode, string scriptFile, bool isPrimaryScript, Hashtable context)
                                         {{
                                             scriptCode = scriptCode.Replace(""Hello World"", ""Hello World!!!"");
                                             return true;
                                         }}
                                     }}");

            dynamic script = CSScript.RoslynEvaluator
                                     .LoadCode($@"//css_precompiler {pre}
                                                  public class Script
                                                  {{
                                                      public string foo()
                                                          => ""Hello World"";
                                                  }}");

            var result = script.foo();

            Assert.Equal("Hello World!!!", result);
        }

        [Fact]
        public void Precompiler_alt_signature2()
        {
            var pre = testTempFile("precompiler.cs");

            File.WriteAllText(pre, $@"using System;
                                      using System.Collections;
                                      public class Sample_Precompiler
                                      {{
                                          public bool Compile(dynamic context)
                                          {{
                                              context.Content = context.Content.Replace(""Hello World"", ""Hello World!!!"");
                                              return true;
                                          }}
                                      }}");

            CSScript.EvaluatorConfig.DebugBuild = true;

            dynamic script = CSScript.RoslynEvaluator
                                     .LoadCode($@"//css_precompiler {pre}
                                                  public class Script
                                                  {{
                                                      public string foo()
                                                          => ""Hello World"";
                                                  }}");

            var result = script.foo();

            Assert.Contains("Hello World!!!", result);
        }

        [Fact]
        public void Precompiler_alt_signature()
        {
            var pre = testTempFile("precompiler.cs");

            File.WriteAllText(pre, $@"using System;
                                      using System.Collections;
                                      public class Sample_Precompiler
                                      {{
                                          public bool Compile(dynamic context)
                                          {{
                                              context.Content = context.Content.Replace(""Hello World"", ""Hello World!!!"");
                                              return true;
                                          }}
                                      }}");

            CSScript.EvaluatorConfig.DebugBuild = true;

            dynamic script = CSScript.RoslynEvaluator
                                     .LoadCode($@"//css_precompiler {pre}
                                                  public class Script
                                                  {{
                                                      public string foo()
                                                          => ""Hello World"";
                                                  }}");

            var result = script.foo();

            Assert.Contains("Hello World!!!", result);
        }

        [Fact]
        public void Precompiler_imported_scripts()
        {
            var pre = testTempFile("precompiler.cs");
            var importedScript = testTempFile("imported.cs");

            File.WriteAllText(pre,
                 @"using System;
                   using System.Collections;
                   public class Sample_Precompiler
                   {
                       public bool Compile(dynamic context)
                       {
                           context.Content = context.Content.Replace(""Hello World"", ""Hello World!!!"");
                           return true;
                       }
                   }");

            File.WriteAllText(importedScript,
                @"public class Utils
                  {
                      public static string foo()=> ""Hello World"";
                  }");

            dynamic script = CSScript.RoslynEvaluator
                                     .LoadCode($@"//css_precompiler {pre}
                                                  //css_include {importedScript}
                                                  public class Script
                                                  {{
                                                      public string foo()
                                                          => ""Hello World"";
                                                      public string foo2()
                                                          => Utils.foo();
                                                  }}");

            var result = script.foo();
            Assert.Contains("Hello World!!!", result);

            result = script.foo2();
            Assert.Contains("Hello World!!!", result);
        }

        [Fact]
        public void Precompiler_imported_scripts2()
        {
            var pre = testTempFile("precompiler.cs");
            var importedScript = testTempFile("imported.cs");

            File.WriteAllText(pre,
                 @"using System;
                   using System.Collections;
                   public class Sample_Precompiler
                   {
                       public static bool Compile(ref string scriptCode, string scriptFile, bool isPrimaryScript, Hashtable context)
                       {
                           scriptCode = scriptCode.Replace(""Hello World"", ""Hello World!!!"");
                           return true;
                       }
                   }");

            File.WriteAllText(importedScript,
                @"public class Utils
                  {
                      public static string foo()=> ""Hello World"";
                  }");

            // CSScript.EvaluatorConfig.DebugBuild = true;

            dynamic script = CSScript.RoslynEvaluator
                                     .LoadCode($@"//css_precompiler {pre}
                                                  //css_include {importedScript}
                                                  public class Script
                                                  {{
                                                      public string foo()
                                                          => ""Hello World"";
                                                      public string foo2()
                                                          => Utils.foo();
                                                  }}");

            var result = script.foo();
            Assert.Contains("Hello World!!!", result);

            result = script.foo2();
            Assert.Contains("Hello World!!!", result);
        }

        [Fact]
        public void Precompiler_for_script_file()
        {
            var preFile = testTempFile("precompiler.cs");
            var scriptFile = testTempFile("primary_script.cs");

            File.WriteAllText(preFile, $@"using System;
                                      using System.Collections;
                                      public class Sample_Precompiler
                                      {{
                                          public static bool Compile(ref string scriptCode, string scriptFile, bool isPrimaryScript, Hashtable context)
                                          {{
                                              scriptCode = scriptCode.Replace(""Hello World"", ""Hello World!!!"");
                                              return true;
                                          }}
                                      }}");

            File.WriteAllText(scriptFile, $@"//css_precompiler {preFile}
                                             public class Script
                                             {{
                                                 public string foo()
                                                     => ""Hello World"";
                                             }}");

            dynamic script = CSScript.RoslynEvaluator
                                     .LoadFile(scriptFile);

            var result = script.foo();
            Assert.Contains("Hello World!!!", result);

            var scriptAsm = CSScript.RoslynEvaluator
                                    .CompileAssemblyFromFile(scriptFile, scriptFile + ".dll");
            dynamic script2 = Assembly.LoadFrom(scriptAsm).CreateObject("*");

            result = script2.foo();
            Assert.Contains("Hello World!!!", result);
        }

        public class Host : IScriptHost
        {
            public void WriteLine(string message)
            {
                Debug.WriteLine(message);
            }
        }

        [Fact]
        public void issue_460()
        {
            var calcAsm = testTempFile("calc.issue_460.dll");
            var scriptFile = testTempFile("script.cs");

            var scriptCode = $@"//css_ref {calcAsm.GetFileName()}
                                using System;
                                public class Script
                                {{
                                    public int Sum(int a, int b) => Calc.Sum(a, b);
                                }}";

            var calcCode = @"using System;
                             public class Calc
                             {
                                 static public int Sum(int a, int b) => a + b;
                             }";

            if (!calcAsm.FileExists()) // try to avoid unnecessary compilations as xUnint keeps locking the loaded assemblies
                CSScript.CodeDomEvaluator.CompileAssemblyFromCode(calcCode, calcAsm);

            File.WriteAllText(scriptFile, scriptCode);

            var eval = CSScript.RoslynEvaluator.LoadFile(scriptFile);
        }

        [Fact]
        public void issue_461()
        {
            var calcAsm = testTempFile("script.dll");
            var scriptFile = testTempFile("script.cs");

            var scriptCode = $@"using System;
                                namespace Demo
                                {{
                                    public class Script
                                    {{
                                        public int Sum(int a, int b) => a + b;
                                    }}
                                }}";

            File.WriteAllText(scriptFile, scriptCode);

            CSScript.RoslynEvaluator.CompileAssemblyFromFile(scriptFile, calcAsm);
        }

        [Fact]
        public void issue_462()
        {
            var scriptFile = testTempFile("script.cs");

            var scriptCode = @"using System;
					            public class Script
					            {
					                public (int, int) CreatePoint(int x, int y)
					                {
					                    return (x, y);
					                }
					            }";

            dynamic script = CSScript.Evaluator
                                     .LoadCode(scriptCode);

            // Call method
            var p = script.CreatePoint(3, 5);
        }

        [Fact]
        public void issue_417()
        {
            Assembly asm = CSScript.RoslynEvaluator
                                   .ReferenceAssemblyOf<Testing.IScriptHost>()
                                   .CompileCode(@"using System;
                                                  public class Script
                                                  {
                                                      Testing.IScriptHost host;
                                                      public void SetHost(Testing.IScriptHost host) { this.host = host; }
                                                      public void foo()
                                                      {
                                                          host.WriteLine(""Hello from script!"");
                                                      }
                                                  }");

            dynamic script = asm.CreateObject("*");

            script.SetHost(new Host());
            script.foo();
        }

        [Fact]
        public void issue_417_static()
        {
            Assembly asm = CSScript.RoslynEvaluator
                                   .ReferenceAssemblyOf<Testing.IScriptHost>()
                                   .CompileCode(@"using System;
                                                  public class Script
                                                  {
                                                      static Testing.IScriptHost host;
                                                      public static void SetHost(Testing.IScriptHost host) { Script.host = host; }
                                                      public static void foo()
                                                      {
                                                          host.WriteLine(""Hello from script!"");
                                                      }
                                                  }");

            var script = (Globals.DefaultRoslynCompilationToScript ? asm.GetType("css_root+Script") : asm.GetType("Script"));

            var setHost = (Action<IScriptHost>)script.GetMethod("SetHost").CreateDelegate(typeof(Action<IScriptHost>));
            var foo = (Action)script.GetMethod("foo").CreateDelegate(typeof(Action));

            setHost(new Host());
            foo();
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
                                                   public class Script_issue_259
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
            try
            {
                Globals.AlwaysEmitRoslynProject = true;

                string[] unfilteredRefAssemblies = null;
                var thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

                var eval = CSScript.RoslynEvaluator;

                object script = eval.ReferenceDomainAssemblies()
                                    .SetRefAssemblyFilter(asms =>
                                        {
                                            unfilteredRefAssemblies = asms.Select(a => a.Location())
                                                                          .ToArray();

                                            return asms.Where(a => a.FullName != Assembly.GetExecutingAssembly().FullName);
                                        })
                                    .LoadMethod(@"public object func()
                                              {
                                                  return new[] {0,5};
                                              }");

                var project = script.GetType().Assembly.GetAttached<Project>();

                // skip next assertions if Globals.DefaultRoslynCompilationToScript as it forces creation of an artificial single script
                // file (even including imported scripts) what makes the concept of Project incompatible.
                if (!Globals.DefaultRoslynCompilationToScript)
                {
                    Assert.Contains(thisAssemblyPath, unfilteredRefAssemblies);
                    Assert.DoesNotContain(thisAssemblyPath, project.Refs);
                }
            }
            finally
            {
                Globals.AlwaysEmitRoslynProject = false;
            }
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
                AssemblyFile = testTempFile("script_a_asm2.dll"),
                CodeKind = SourceCodeKind.Script
            };

            try
            {
                var code2 = @"using System;
                      using System.Collections.Generic;
                      using System.Linq;

                      public class Utils
                      {
                          public string Name => ""Utils"";
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
                        .ReferenceAssemblyOf(typeof(Console).Assembly)
                        .With(e => e.IsCachingEnabled = false) // required to not interfere with xUnit
                        .CompileCode(code2, info);

                dynamic script = CSScript.RoslynEvaluator
                                         .With(e => e.IsCachingEnabled = false)
                                         .ReferenceAssembly(info.AssemblyFile)
                                         .CompileMethod(@"using static script_a;
                                                  Utils Test()
                                                  {
                                                      return new Utils();
                                                  }")
                                         .CreateObject("*");

                dynamic utils = script.Test();
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
                AssemblyFile = testTempFile("AccountingScript.dll"),
                CodeKind = SourceCodeKind.Script // for demo purposes only, wrap everything in the RootClass. Otherwise use SourceCodeKind.Regular with a normal class declaration
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
            Assert.Equal("1", r);
        }

        public static string log = "";

        [Fact]
        public void Issue_354()
        {
            var info = new CompileInfo { RootClass = "PrintingClass", AssemblyFile = testTempFile("Printer.dll"), CodeKind = SourceCodeKind.Script };
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
                                     .Eval("PrintingClass.Printer.Print();");
            // .LoadMethod(@"public void TestPrint() => Printing.Printer.Print();");

            // Assert.Equal(3, statements.Count());
        }

        [Fact]
        public void Issue_448()
        {
            var code = @"using System;
                         using System.Diagnostics;
                         public class Util
                         {
                             public string foo()
                             {
                                 #if test
                                 return ""test"";
                                 #else
                                 return ""not-test"";
                                 #endif
                             }
                         }";

            // =================================
            var info = new CompileInfo { CodeKind = SourceCodeKind.Script, CompilerOptions = "-define:test" };

            dynamic util = CSScript.Evaluator.CompileCode(code, info).CreateObject("*");
            var result = util.foo();
            Assert.Equal("test", result);

            // =================================
            info = new CompileInfo { CodeKind = SourceCodeKind.Regular, CompilerOptions = "-define:test" };

            util = CSScript.Evaluator.CompileCode(code, info).CreateObject("*");
            result = util.foo();
            Assert.Equal("test", result);

            // =================================
            // if we do not specify dll file to create, the assembly will be created with the default name that is the same as for
            // one of the prev tests And since it is loaded in the appdomain from the file the file will be locked.
            // This is specific to SourceCodeKind.Regular, which triggers the initialization of CompileInfo.AssemblyFile to the default
            // name `$"{RootClass}.dll"`.

            info = new CompileInfo { CodeKind = SourceCodeKind.Regular, AssemblyFile = testTempFile("asm1.dll") };

            util = CSScript.Evaluator.CompileCode(code, info).CreateObject("*");
            result = util.foo();
            Assert.Equal("not-test", result);

            // =================================

            // skip next assertions because DefaultRoslynCompilationToScript when no CompileInfo is passed will trigger the compilation
            // with CSharpScript.Create, which dows not allow preprocessor symbols
            if (!Globals.DefaultRoslynCompilationToScript)
            {
                CSScript.EvaluatorConfig.CompilerOptions = "-define:test";
                util = CSScript.Evaluator.LoadCode(code);
                result = util.foo();
                Assert.Equal("test", result);

                // =================================

                var scriptFile = testTempFile("script1.cs");
                File.WriteAllText(scriptFile, code);
                var scriptAsmFile = testTempFile("script1.cs.dll");
                var resultAsm = CSScript.Evaluator.CompileAssemblyFromFile(scriptFile, CompileInfo.For(scriptAsmFile));
                util = Assembly.LoadFrom(resultAsm).CreateObject("*");

                result = util.foo();
                Assert.Equal("test", result);
            }
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
            var info = new CompileInfo
            {
                RootClass = root_class_name,
                PreferLoadingFromFile = true,
                AssemblyFile = testTempFile(root_class_name + ".dll"),
                CodeKind = SourceCodeKind.Script // for demo purposes only, wrap everything in the RootClass. Otherwise use SourceCodeKind.Regular with a normal class declaration
            };

            try
            {
                // need to reference Console's assembly since the script implicitly references the current appdomain assemblies
                // but console asm is not loaded in the test environment
                var printer_asm = CSScript.RoslynEvaluator
                                          .Reset(false) // to prevent appdomain assemblies being loaded
                                          .ReferenceAssembly(typeof(Console).Assembly)
                                          .CompileCode(@"using System;
                                                 public class Printer
                                                 {
                                                     public void Print() => Console.Write(""Printing..."");
                                                 }", info);

                // as part of #448 work I have discovered that in the TestEnvironment when AppDomain has all the assemblies of the various
                // tests loaded the next LoadMethod will trigger file not found exception for the *.tmp.cs.dll file that was generated for the
                // CodeDom.use_interfaces_between_scripts.
                // This is because the next LoadMethod will try to reference domain assemblies. `Reset(false)` somehow does not prevent this

                dynamic script = CSScript.RoslynEvaluator
                                         .Reset(false) // to control what assemblies are referenced
                                         .ReferenceAssembly(printer_asm)
                                         .LoadMethod($"using static {root_class_name};" + @"
                                               void Test()
                                               {
                                                   new Printer().Print();
                                               }");
                script.Test();
            }
            catch (Exception)
            {
                // Debugger.Launch();
                throw;
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
    public interface IScriptHost
    {
        void WriteLine(string message);
    }

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