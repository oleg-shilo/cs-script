using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using csscript;
using CSScripting;
using CSScriptLib;
using Xunit;

namespace EvaluatorTests
{
    [Collection("Sequential")]
    public class Generic_CodeDom
    {
        public static string root = Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("test", "TestFolder", "TestData").EnsureDir();

        string testTempFile(string fileName, [CallerMemberName] string caller = null)
        {
            var rootDir = root.PathJoin(nameof(Generic_CodeDom), caller).GetFullPath().EnsureDir();
            return Path.Combine(rootDir, fileName);
        }

        [Fact]
        public void using_CompilerOptions()
        {
            // CodeDomEvaluator.CompileOnServer = false;

            // Note if you assign AssemblyFile to the path from the "local directory" xUnit runtime will lock the file simply
            // because it was present in the local dir. So hide the assembly from xUnit by moving it in a separate folder
            var info = new CompileInfo
            {
                CompilerOptions = "-define:test",
                AssemblyFile = testTempFile("test.dll"),
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
        public void referencing_script_types_from_another_codedom_script()
        {
            CSScript.EvaluatorConfig.DebugBuild = true;
            CSScript.EvaluatorConfig.ReferenceDomainAssemblies = false;

            var info = new CompileInfo { AssemblyFile = testTempFile("utils_asm.dll") };

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
            var respFile = testTempFile($"{nameof(use_resp_file)}.resp");
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
            finally
            {
                CSScript.EvaluatorConfig.CompilerOptions = "";
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
        public void use_interfaces_between_scripts2()
        {
            IPrinter printer = CSScript.CodeDomEvaluator
                                       .ReferenceAssemblyOf<IPrinter>()
                                       .LoadCode<IPrinter>(@"using System;
                                                             public class Printer : IPrinter
                                                             {
                                                                public void Print()
                                                                    => Console.Write(""Printing..."");
                                                             }");

            var ttt = printer.GetType().Assembly.Location();

            dynamic script = CSScript.CodeDomEvaluator
                                     .ReferenceAssemblyOf<IPrinter>()
                                     .LoadMethod(@"void Test(IPrinter printer)
                                               {
                                                   printer.Print();
                                               }");
            script.Test(printer);

            // does not throw :)
        }

        [Fact]
        public void import_script_from_another_scripts_Issue_459()
        {
            var oldLoad = Settings.Load;
            try
            {
                var settings = new Settings();
                settings.AddSearchDir("A");
                settings.AddSearchDir("B");
                settings.DefaultRefAssemblies = "System.IO.Pipes;System.IO.Pipelines";
                Settings.Load = (file) => settings;

                ProjectBuilder.DefaultSearchDirs = "C;D";
                ProjectBuilder.DefaultNamespaces = "System.IO";
                ProjectBuilder.DefaultRefAsms = "System.Xml.dll;System.Net.dll";

                var script_math = testTempFile("math.cs");
                var script_calc = testTempFile("calc.cs");

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

                var calc = CSScript.CodeDomEvaluator
                                   .LoadFile(script_calc);

                Project proj = calc.GetType().Assembly.GetAttached<Project>();

                Assert.Contains("A", proj.SearchDirs);
                Assert.Contains("B", proj.SearchDirs);
                Assert.Contains("C", proj.SearchDirs);
                Assert.Contains("D", proj.SearchDirs);

                // from ProjectBuilder.DefaultRefAsms
                Assert.Contains(proj.Refs, x => x.EndsWith("System.Xml.dll"));
                Assert.Contains(proj.Refs, x => x.EndsWith("System.Net.dll"));

                // from settings.DefaultRefAssemblies
                Assert.Contains(proj.Refs, x => x.EndsWith("System.IO.Pipes.dll"));
                Assert.Contains(proj.Refs, x => x.EndsWith("System.IO.Pipelines.dll"));
            }
            finally
            {
                Settings.Load = oldLoad;
                ProjectBuilder.DefaultSearchDirs = default;
                ProjectBuilder.DefaultNamespaces = default;
                ProjectBuilder.DefaultRefAsms = default;
            }
        }

        [Fact]
        public void import_script_from_another_scripts()
        {
            var script_math = testTempFile("math.cs");
            var script_calc = testTempFile("calc.cs");

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

        [Fact]
        public void Precompiler_for_script_code()
        {
            var pre = testTempFile("precompiler.cs");

            File.WriteAllText(pre, @"using System;
                                     using System.Collections;
                                     public class Sample_Precompiler
                                     {
                                         public static bool Compile(ref string scriptCode, string scriptFile, bool isPrimaryScript, Hashtable context)
                                         {
                                             scriptCode = scriptCode.Replace(""Hello World"", ""Hello World!!!"");
                                             return true;
                                         }
                                     }");

            dynamic script = CSScript.CodeDomEvaluator
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
        public void Precompiler_alt_signature()
        {
            var pre = testTempFile("precompiler.cs");

            File.WriteAllText(pre, $@"//css_ref {typeof(PrecompilationContext).Assembly.Location}
                                     using System;
                                     using System.Collections;
                                     public class Sample_Precompiler
                                     {{
                                        public bool Compile(csscript.PrecompilationContext context)
                                        {{
                                            context.Content = context.Content.Replace(""Hello World"", ""Hello World!!!"");
                                            return true;
                                        }}
                                      }}");

            CSScript.EvaluatorConfig.DebugBuild = true;

            dynamic script = CSScript.CodeDomEvaluator
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
        public void Precompiler_alt_signature2()
        {
            var pre = testTempFile("precompiler.cs");

            File.WriteAllText(pre, @"using System;
                                     using System.Collections;
                                     public class Sample_Precompiler
                                     {
                                        public bool Compile(dynamic context)
                                        {
                                            context.Content = context.Content.Replace(""Hello World"", ""Hello World!!!"");
                                            return true;
                                        }
                                      }");

            CSScript.EvaluatorConfig.DebugBuild = true;

            dynamic script = CSScript.CodeDomEvaluator
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
                       public bool Compile(csscript.PrecompilationContext context)
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

            dynamic script = CSScript.CodeDomEvaluator
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
                       public static bool Compile(ref string code, string scriptFile, bool isPrimaryScript, Hashtable context)
                       {
                           code = code.Replace(""Hello World"", ""Hello World!!!"");
                           return true;
                       }
                   }");

            File.WriteAllText(importedScript,
                @"public class Utils
                  {
                      public static string foo()=> ""Hello World"";
                  }");

            // CSScript.EvaluatorConfig.DebugBuild = true;

            dynamic script = CSScript.CodeDomEvaluator
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

            dynamic script = CSScript.CodeDomEvaluator
                                     .LoadFile(scriptFile);

            var result = script.foo();
            Assert.Contains("Hello World!!!", result);

            var scriptAsm = CSScript.CodeDomEvaluator
                                    .CompileAssemblyFromFile(scriptFile, scriptFile + ".dll");
            dynamic script2 = Assembly.LoadFrom(scriptAsm).CreateObject("*");

            result = script2.foo();
            Assert.Contains("Hello World!!!", result);
        }
    }
}