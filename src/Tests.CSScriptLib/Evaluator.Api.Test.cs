using System;
using System.IO;
using System.Reflection;
using csscript;
using CSScripting;
using CSScriptLib;
using Scripting;
using Testing;
using Xunit;

namespace EvaluatorTests
{
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
                Assembly asm = evaluator.CompileCode(@"using System;
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
        public IEvaluator evaluator => GetEvaluator();

        [Fact]
        public void CompileCode()
        {
            Assembly asm = evaluator.CompileCode(@"using System;
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
            // Note if you give AssemblyFile the name with the extension .dll xUnit runtime will lock the file simply
            // because it was present in the local dir. So hide the assembly by dropping the file extension.

            var asm_file = GetTempFileName(nameof(CompileCode_InmemAsmLocation));

            var info = new CompileInfo { AssemblyFile = asm_file };

            Assembly asm = evaluator.CompileCode(@"using System;
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
            if (evaluator is RoslynEvaluator) // Roslyn cannot work with C# files (but in memory streams)
                return;                       // So asm location does not make sense.

            var asm_file = GetTempFileName(nameof(CompileCode_InmemAsmLocation));

            var info = new CompileInfo { AssemblyFile = asm_file, PreferLoadingFromFile = false };

            Assembly asm = evaluator.CompileCode(@"using System;
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
                evaluator.Check(@"using System;
                                  public class Script ??");
            });

            Assert.Contains("Invalid expression term '??'", ex.Message);
        }

        [Fact]
        public void Check_ValidScript()
        {
            evaluator.Check(@"using System;
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
            string asmFile = evaluator.CompileAssemblyFromCode(
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

            string asmFile = evaluator.CompileAssemblyFromFile(script, "MyScript.asm");

            Assert.True(File.Exists(asmFile));
        }

        [Fact]
        public void CompileMethod()
        {
            dynamic calc = evaluator.CompileMethod("int Sum(int a, int b) => a+b;")
                                    .CreateObject("*");

            var result = calc.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void LoadMethod()
        {
            dynamic calc = evaluator.LoadMethod("int Sum(int a, int b) => a+b;");
            var result = calc.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void LoadMethod_T()
        {
            ICalc calc = evaluator.LoadMethod<ICalc>("int Sum(int a, int b) => a+b;");
            var result = calc.Sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void CreateDelegate()
        {
            var sum = evaluator.CreateDelegate(@"int Sum(int a, int b)
                                                    => a + b;");

            int result = (int)sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void CreateDelegate_T()
        {
            var sum = evaluator.CreateDelegate<int>(@"int Sum(int a, int b)
                                                    => a + b;");

            int result = sum(7, 3);

            Assert.Equal(10, result);
        }

        [Fact]
        public void LoadCode()
        {
            dynamic script = evaluator
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
            ICalc calc = evaluator.LoadCode<ICalc>(@"using System;
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

            dynamic calc = evaluator.LoadFile(script);
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

            dynamic calc = evaluator.LoadFile(script, "test");
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

            ICalc calc = evaluator.LoadFile<ICalc>(script);
            int result = calc.Sum(1, 2);

            Assert.Equal(3, result);
        }

        [Fact]
        public void CompileCode_with_toplevel_class()
        {
        }
    }
}