using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using csscript;
using CSScripting;
using CSScriptLib;
using Scripting;
using Testing;
using Xunit;

#pragma warning disable MethodDocumentationHeader // The method must have a documentation header.
#pragma warning disable ClassDocumentationHeader // The class must have a documentation header.
// #pragma warning disable ConstructorDocumentationHeader // The constructor must have a documentation header.

namespace EvaluatorTests
{
    [Collection("Sequential")]
    public class API_CodeDom : API_Roslyn
    {
        public API_CodeDom()
        {
            base.GetEvaluator = () => CSScript.CodeDomEvaluator;
            // CSScript.EvaluatorConfig.DebugBuild = true;
            CodeDomEvaluator.CompileOnServer = true;
        }

        [Fact]
        public void CompileCode_CompileInfo_CodeDom()
        {
            var info = new CompileInfo { RootClass = "test" };

            var ex = Assert.Throws<CSScriptException>(() =>
            {
                Assembly asm = new_evaluator.CompileCode(@"using System;
                                                               public class Script
                                                               {
                                                                   public int Sum(int a, int b)
                                                                   {
                                                                       return a+b;
                                                                   }
                                                               }",
                                                         info);
            });

            Assert.StartsWith("CompileInfo.RootClass property should only be used with Roslyn evaluator", ex.Message);
        }
    }

    [Collection("Sequential")]
    public class API_Roslyn
    {
        public string GetTempFileName(string seed)
            => $"{this.GetHashCode()}.{seed}".GetFullPath();

        public string GetTempScript(string seed, string content)
        {
            var script = GetTempFileName(seed);
            File.WriteAllText(script, content);
            return script;
        }

        public Func<IEvaluator> GetEvaluator = () => CSScript.RoslynEvaluator;
        public IEvaluator new_evaluator => GetEvaluator();

        /// <summary>
        /// Compiles the code with imports.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void CompileFileWithImports()
        {
            var rootDir = Environment.CurrentDirectory;
            var primaryScript = rootDir.PathJoin($"{nameof(CompileCodeWithImports)}.cs");
            var dependencyScript = rootDir.PathJoin($"dep{nameof(CompileCodeWithImports)}.cs");

            File.WriteAllText(primaryScript, $@"//css_inc {dependencyScript}
                                               using System;
                                               public class Script
                                               {{
                                                   public int Sum(int a, int b) => Calc.Sum(a ,b);
                                               }}");

            File.WriteAllText(dependencyScript, @"using System;
                                               public class Calc
                                               {
                                                   static public int Sum(int a, int b)
                                                   {
                                                       return a+b;
                                                   }
                                               }");

            // CSScript.EvaluatorConfig.DebugBuild = true;
            dynamic script = new_evaluator
                .LoadFile(primaryScript);

            var result = script.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void CompileCodeWithImports()
        {
            var dependencyScript = $"dep{nameof(CompileCodeWithImports)}.cs".GetFullPath();

            File.WriteAllText(dependencyScript, @"using System;
                                            public class Calc
                                            {
                                                static public int Sum(int a, int b)
                                                {
                                                    return a+b;
                                                }
                                            }");

            dynamic script = new_evaluator.LoadCode($@"//css_inc {dependencyScript}
                                            using System;
                                            public class Script
                                            {{
                                                public int Sum(int a, int b) => Calc.Sum(a ,b);
                                            }}");

            var result = script.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CompileCodeWithNestedImports(bool isRoslyn)
        {
            var dependencyScript1 = $"dep{nameof(CompileCodeWithNestedImports)}1.cs".GetFullPath();
            var dependencyScript2 = $"dep{nameof(CompileCodeWithNestedImports)}2.cs".GetFullPath();

            File.WriteAllText(dependencyScript2, @"
                                            using System;
                                            public class Calc
                                            {
                                                static public int Sum(int a, int b)
                                                {
                                                    return a+b;
                                                }
                                            }");

            File.WriteAllText(dependencyScript1, $"//css_inc {dependencyScript2}");

            var evaluator = isRoslyn ? (IEvaluator)CSScript.RoslynEvaluator : (IEvaluator)CSScript.CodeDomEvaluator;

            dynamic script = evaluator.LoadCode($@"//css_inc {dependencyScript1}
                                                  using System;
                                                  public class Script
                                                  {{
                                                      public int Sum(int a, int b) => Calc.Sum(a ,b);
                                                  }}");

            var result = script.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void CompileCodeWithRefs()
        {
            var tempDir = ".\\dependencies".EnsureDir();
            var calcAsm = tempDir.PathJoin("calc.v1.dll").GetFullPath();

            if (!calcAsm.FileExists()) // try to avoid unnecessary compilations as xUnint keeps locking the loaded assemblies
                CSScript.CodeDomEvaluator
                        .CompileAssemblyFromCode(@"using System;
                                                   public class Calc
                                                   {
                                                       static public int Sum(int a, int b) => a + b;
                                                   }",
                                                 calcAsm);

            // NOTE!!! Roslyn evaluator will inject class in the extra root class "css_root" Very
            // annoying, but even `css_root.Calc.Sum` will not work when referenced in scrips.
            // Roslyn does some crazy stuff. The assembly produced by Roslyn cannot be easily used.
            //
            // So using CodeDom (csc.exe) instead. It does build proper assemblies

            var code = @"using System;
                         public class Script
                         {
                             public int Sum(int a, int b) => Calc.Sum(a, b);
                         }";

            try
            {
                new_evaluator.LoadCode(code);

                Assert.True(false);
            }
            catch (Exception e)
            {
                Assert.Contains("The name 'Calc' does not exist in the current context", e.Message);
            }

            new_evaluator.LoadCode($"//css_ref {calcAsm}" + Environment.NewLine + code);
        }

        [Fact]
        public void LoadCode_detect_error_in_imported_script()
        {
            var dependencyScript = $"dep{nameof(LoadCode_detect_error_in_imported_script)}.cs".GetFullPath();

            try
            {
                File.WriteAllText(dependencyScript, @"using System;
                                                      public class Calc
                                                      {
                                                          static public int Sum(int a, int b)
                                                          {
                                                              return a+b
                                                          }
                                                      }");

                new_evaluator.LoadCode($@"//css_inc {dependencyScript}
                                      using System;
                                      public class Script
                                      {{
                                          public int Sum(int a, int b) => Calc.Sum(a ,b);
                                      }}");

                Assert.Fail("Expected compile error was not detected");
            }
            catch (Exception e)
            {
                Assert.Contains($"{dependencyScript}(6,73): error CS1002: ; expected", e.Message);
            }
        }

        [Fact]
        public void LoadFile_detect_error_in_imported_script()
        {
            var primaryScript = $"{nameof(LoadFile_detect_error_in_imported_script)}.cs".GetFullPath();
            var dependencyScript = $"dep{nameof(LoadFile_detect_error_in_imported_script)}.cs".GetFullPath();

            try
            {
                File.WriteAllText(primaryScript, $@"//css_inc {dependencyScript}
                                                    using System;
                                                    public class Script
                                                    {{
                                                        public int Sum(int a, int b) => Calc.Sum(a ,b);
                                                    }}");

                File.WriteAllText(dependencyScript, @"using System;
                                                      public class Calc
                                                      {
                                                          static public int Sum(int a, int b)
                                                          {
                                                              return a+b
                                                          }
                                                      }");
                new_evaluator.LoadFile(primaryScript);
                Assert.Fail("Expected compile error was not detected");
            }
            catch (Exception e)
            {
                Assert.Contains($"{dependencyScript}(6,73): error CS1002: ; expected", e.Message);
            }
        }

        [Fact]
        public void LoadCode_detect_error_in_primary_script()
        {
            var dependencyScript = $"dep{nameof(LoadCode_detect_error_in_primary_script)}.cs".GetFullPath();

            try
            {
                File.WriteAllText(dependencyScript, @"using System;
                                                      public class Calc
                                                      {
                                                          static public int Sum(int a, int b)
                                                          {
                                                              return a+b;
                                                          }
                                                      }");

                new_evaluator.LoadCode($@"//css_inc {dependencyScript}
                                      using System;
                                      public class Script
                                      {{
                                          public int Sum(int a, int b) => Calc.Sum(a ,b)
                                      }}");
                Assert.Fail("Expected compile error was not detected");
            }
            catch (Exception e)
            {
                Assert.Contains($"(5,89): error CS1002: ; expected", e.Message);
            }
        }

        [Fact]
        public void LoadCode_detect_error_in_script()
        {
            try
            {
                new_evaluator.LoadCode($@"using System;
                                      public class Script
                                      {{
                                          public int Sum(int a, int b) => a = b
                                      }}");

                Assert.Fail("Expected compile error was not detected");
            }
            catch (Exception e)
            {
                Assert.Contains($"(4,80): error CS1002: ; expected", e.Message);
            }
        }

        [Fact]
        public void LoadFiule_detect_error_in_script()
        {
            var script = $"{nameof(LoadFiule_detect_error_in_script)}.cs".GetFullPath();

            try
            {
                File.WriteAllText(script, @"using System;
                                            public class Script
                                            {
                                                 public int Sum(int a, int b) => a + b
                                            }");

                new_evaluator.LoadFile(script);

                Assert.Fail("Expected compile error was not detected");
            }
            catch (Exception e)
            {
                Assert.Contains($"{script}(4,87): error CS1002: ; expected", e.Message);
            }
        }

        [Fact]
        public void LoadFile_detect_error_in_primary_script()
        {
            var primaryScript = $"{nameof(LoadFile_detect_error_in_primary_script)}.cs".GetFullPath();
            var dependencyScript = $"dep{nameof(LoadFile_detect_error_in_primary_script)}.cs".GetFullPath();

            try
            {
                File.WriteAllText(primaryScript, $@"//css_inc {dependencyScript}
                                                    using System;
                                                    public class Script
                                                    {{
                                                        public int Sum(int a, int b) => Calc.Sum(a ,b)
                                                    }}");

                File.WriteAllText(dependencyScript, @"using System;
                                                      public class Calc
                                                      {
                                                          static public int Sum(int a, int b)
                                                          {
                                                              return a+b;
                                                          }
                                                      }");
                new_evaluator.LoadFile(primaryScript);
                Assert.Fail("Expected compile error was not detected");
            }
            catch (Exception e)
            {
                Assert.Contains($"{primaryScript}(5,103): error CS1002: ; expected", e.Message);
            }
        }

        [Fact]
        public void CompileCode()
        {
            Assembly asm = new_evaluator.CompileCode(@"using System;
                                                   public class Script
                                                   {
                                                       public int Sum(int a, int b)
                                                       {
                                                           return a+b;
                                                       }
                                                   }");

            dynamic script = asm.CreateObject("*");
            var result = script.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void CompileCode_CompileInfo()
        {
            // Note if you give AssemblyFile the name with the extension .dll xUnit runtime will
            // lock the file simply because it was present in the local dir. So hide the assembly by
            // dropping the file extension.

            var asm_file = GetTempFileName(nameof(CompileCode_InmemAsmLocation));

            var info = new CompileInfo { AssemblyFile = asm_file };

            Assembly asm = new_evaluator.CompileCode(@"using System;
                                                   public class Script
                                                   {
                                                       public int Sum(int a, int b)
                                                       {
                                                           return a+b;
                                                       }
                                                   }",
                                                     info);

            dynamic script = asm.CreateObject("*");
            var result = script.Sum(7, 3);

            Assert.Equal(10, result);
            Assert.Equal(asm_file, asm.Location());
        }

        [Fact]
        public void CompileCode_InmemAsmLocation()
        {
            if (new_evaluator is RoslynEvaluator) // Roslyn cannot work with C# files (but in memory streams)
                return;                       // So asm location does not make sense.

            var asm_file = GetTempFileName(nameof(CompileCode_InmemAsmLocation));

            var info = new CompileInfo { AssemblyFile = asm_file, PreferLoadingFromFile = false };

            Assembly asm = new_evaluator.CompileCode(@"using System;
                                                    public class Script
                                                    {
                                                        public int Sum(int a, int b)
                                                        {
                                                            return a+b;
                                                        }
                                                    }",
                                         info);

            var css_injected_location = asm.Location();
            var clr_standard_location = asm.Location;

            Assert.Equal(asm_file, css_injected_location);
            Assert.Equal("", clr_standard_location);
        }

        [Fact]
        public void Check_InvalidScript()
        {
            var ex = Assert.Throws<CompilerException>(() =>
            {
                new_evaluator.Check(@"using System;
                                  public class Script ??");
            });

            Assert.Contains("Invalid expression term '??'", ex.Message);
        }

        [Fact]
        public void Check_ValidScript()
        {
            new_evaluator.Check(@"using System;
                              public class Script
                              {
                                 public int Sum(int a, int b)
                                 {
                                     return a+b;
                                 }
                              }");
            // does not throw
        }

        [Fact]
        public void CompileAssemblyFromCode()
        {
            string asmFile = new_evaluator.CompileAssemblyFromCode(
                                           @"using System;
                                           public class Script
                                           {
                                               public int Sum(int a, int b)
                                               {
                                                   return a+b;
                                               }
                                           }",
                                           "MyScript.asm");

            Assert.True(File.Exists(asmFile));
        }

        [Fact]
        public void CompileAssemblyFromFile()
        {
            var script = GetTempScript(nameof(CompileAssemblyFromFile),
                                       @"using System;
                                       public class Calc
                                       {
                                           public int Sum(int a, int b) => a+b;
                                       }");

            string asmFile = new_evaluator.CompileAssemblyFromFile(script, "MyScript.asm");

            Assert.True(File.Exists(asmFile));
        }

        [Fact]
        public void CompileMethod()
        {
            dynamic calc = new_evaluator.CompileMethod("int Sum(int a, int b) => a+b;")
                                        .CreateObject("*");

            var result = calc.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void LoadMethod()
        {
            dynamic calc = new_evaluator.LoadMethod("int Sum(int a, int b) => a+b;");
            var result = calc.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void LoadMethod_T()
        {
            ICalc calc = new_evaluator.LoadMethod<ICalc>("int Sum(int a, int b) => a+b;");
            var result = calc.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void LoadMethod_T_Generic()
        {
            var calc = new_evaluator.LoadMethod<ICalc<int, int>>("int Sum(int a, int b) => a+b;");
            var result = calc.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void LoadMethod_T_Generic_Nested()
        {
            var script = @"using Testing;
                           Wrapped<int> Sum(int a, int b) => new Wrapped<int>(a+b);";
            var calc = new_evaluator.LoadMethod<ICalc<Wrapped<int>, int>>(script);
            var result = calc.Sum(7, 3).Value;

            Assert.Equal(10, result);
        }

        [Fact]
        public void CreateDelegate()
        {
            var sum = new_evaluator.CreateDelegate(@"int Sum(int a, int b) => a + b;");

            int result = (int)sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void CreateDelegate_T()
        {
            var sum = new_evaluator.CreateDelegate<int>(@"int Sum(int a, int b)
                                                    => a + b;");

            int result = sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void LoadCode()
        {
            dynamic script = new_evaluator
                                 .LoadCode(@"using System;
                                              public class Script
                                              {
                                                  public int Sum(int a, int b)
                                                  {
                                                      return a+b;
                                                  }
                                              }");
            int result = script.Sum(1, 2);

            Assert.Equal(3, result);
        }

        [Fact]
        public void LoadCode_T()
        {
            ICalc calc = new_evaluator.LoadCode<ICalc>(@"using System;
                                                     public class Script : Testing.ICalc
                                                     {
                                                         public int Sum(int a, int b)
                                                         {
                                                             return a+b;
                                                         }
                                                     }");

            int result = calc.Sum(1, 2);

            Assert.Equal(3, result);
        }

        [Fact]
        public void LoadFile()
        {
            var script = GetTempScript(nameof(LoadFile),
                                       @"using System;
                                        public class Calc
                                        {
                                            public int Sum(int a, int b) => a+b;
                                        }");

            dynamic calc = new_evaluator.LoadFile(script);
            int result = calc.Sum(1, 2);

            Assert.Equal(3, result);
        }

        [Fact]
        public void LoadFile_Params()
        {
            var script = GetTempScript(nameof(LoadFile),
                                       @"using System;
                                        public class Calc
                                        {
                                            public string Name;
                                            public Calc(string name) { Name = name; }
                                            public int Sum(int a, int b) => a+b;
                                        }");

            dynamic calc = new_evaluator.LoadFile(script, "test");
            int result = calc.Sum(1, 2);

            Assert.Equal(3, result);
            Assert.Equal("test", calc.Name);
        }

        [Fact]
        public void LoadFile_T()
        {
            var script = GetTempScript(nameof(LoadFile),
                                       @"using System;
                                        public class Calc : Testing.ICalc
                                        {
                                            public int Sum(int a, int b) => a+b;
                                        }");

            ICalc calc = new_evaluator.ReferenceDomainAssemblies()
                                      .LoadFile<ICalc>(script);

            int result = calc.Sum(1, 2);

            Assert.Equal(3, result);
        }

        [Fact]
        public void CompileCode_with_toplevel_class()
        {
        }
    }
}