using System;
using System.Linq;
using System.Reflection;
using CSScriptLibrary;
using Microsoft.CodeAnalysis.Scripting;
using Tests;
using Xunit;

//https://github.com/dotnet/roslyn/wiki/Scripting-API-Samples#expr
public class RoslynEval
{
    static SimpleAsmProbing probing = null;

    public RoslynEval()
    {
        if (probing == null)
        {
            probing = SimpleAsmProbing.For(".", @"..\..\..\Roslyn.Scripting");
        }
    }

    static string classCode = @"public class ScriptedClass
                                    {
                                        public string HelloWorld {get;set;}
                                        public ScriptedClass()
                                        {
                                            HelloWorld = ""Hello Roslyn!"";
                                        }
                                    }";

    [Fact]
    public void CompileCode()
    {
        RoslynEvaluator.LoadCompilers();

        Assembly script = CSScript.RoslynEvaluator.CompileCode(classCode);
        Assert.NotNull(script);
    }

    [Fact]
    public void CompileCode_Error()
    {
        var ex = Assert.Throws<CompilationErrorException>(() =>
                     CSScript.RoslynEvaluator.CompileCode(classCode.Replace("public", "error_word")));

        Assert.Equal("(1,12): error CS1002: ; expected", ex.Message);
    }

    [Fact]
    public void CompileMethodInstance()
    {
        dynamic script = CSScript.RoslynEvaluator
                                 .CompileMethod(@"int Sqr(int data)
                                                  {
                                                       return data * data;
                                                  }")
                                 .CreateObject("*");

        var result = script.Sqr(7);

        Assert.Equal(49, result);
    }

    [Fact]
    public void CompileMethodStatic()
    {
        CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn;

        var script = CSScript.RoslynEvaluator
                             .CompileMethod(@"using Tests;
                                                  static void Test(InputData data)
                                                  {
                                                      data.Index = 7;
                                                  }");
        InputData data = new InputData();
        var Test = script.GetStaticMethod("*.Test", data);

        Test(data);
        Assert.Equal(7, data.Index);
    }

    [Fact]
    public void CreateDelegate()
    {
        var Sqr = CSScript.RoslynEvaluator
                          .CreateDelegate(@"int Sqr(int a)
                                            {
                                                return a * a;
                                            }");

        var r = Sqr(3);

        Assert.Equal(9, r);
    }

    [Fact]
    public void CreateDelegateTyped()
    {
        var Sqr = CSScript.RoslynEvaluator
                          .CreateDelegate<int>(@"int Sqr(int a)
                                                 {
                                                     return a * a;
                                                 }");
        int r = Sqr(3);

        Assert.Equal(9, r);
    }

    [Fact]
    public void LoadCode()
    {
        dynamic script = CSScript.RoslynEvaluator
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
    public void ProcessCodeDirectives()
    {
        dynamic script = CSScript.RoslynEvaluator
                                 .LoadCode(@"//css_ref System.Windows.Forms;
                                             using System;
                                             using System.Xml;
                                             using System.Xml.Linq;
                                             public class Script
                                             {
                                                 public string TestMB()
                                                 {
                                                     return typeof(System.Windows.Forms.MessageBox).ToString();
                                                 }
                                                 public string TestXD()
                                                 {
                                                     return typeof(XDocument).ToString();
                                                 }
                                             }");

        Assert.Equal("System.Windows.Forms.MessageBox", script.TestMB());
        Assert.Equal("System.Xml.Linq.XDocument", script.TestXD());
    }

    [Fact]
    public void LoadCodeWithInterface()
    {
        var script = (ICalc)CSScript.RoslynEvaluator
                                    .LoadCode(@"using Tests;
                                                public class Script : ICalc
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
    public void LoadCodeTyped()
    {
        ICalc script = CSScript.RoslynEvaluator
                               .LoadCode<ICalc>(@"using System;
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
    public void LoadDelegateAction()
    {
        var Test = CSScript.RoslynEvaluator
                           .LoadDelegate<Action<InputData>>(
                                        @"using Tests;
                                              void Test(InputData data)
                                              {
                                                  data.Index = 7;
                                              }");

        var data = new InputData();

        Test(data);

        Assert.Equal(7, data.Index);
    }

    [Fact]
    public void LoadDelegateFunc()
    {
        var Product = CSScript.RoslynEvaluator
                              .LoadDelegate<Func<int, int, int>>(
                                         @"int Product(int a, int b)
                                               {
                                                   return a * b;
                                               }");

        int result = Product(3, 2);
        Assert.Equal(6, result);
    }

    [Fact]
    public void LoadMethod()
    {
        dynamic script = CSScript.RoslynEvaluator
                                 .LoadMethod(@"int Product(int a, int b)
                                               {
                                                   return a * b;
                                               }");

        int result = script.Product(3, 2);

        Assert.Equal(6, result);
    }

    [Fact]
    public void LoadMethodTyped()
    {
        ICalc script = CSScript.RoslynEvaluator
                               .LoadMethod<ICalc>(@"using System;

                                                    public int Sum(int a, int b)
                                                    {
                                                        return sumImpl(a,b);
                                                    }

                                                    int sumImpl(int a, int b)
                                                    {
                                                        return a+b;
                                                    }");

        int result = script.Sum(1, 2);

        Assert.Equal(3, result);
    }
}