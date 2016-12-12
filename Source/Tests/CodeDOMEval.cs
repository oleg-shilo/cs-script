using csscript;
using CSScriptLibrary;
using System;
using System.Diagnostics;
using System.Reflection;
using Tests;
using Xunit;

public class CodeDomEval
{
    static string classCode = @"public class ScriptedClass
                                {
                                    public string HelloWorld {get;set;}
                                    public ScriptedClass()
                                    {
                                        HelloWorld = ""Hello CodeDom!"";
                                    }

                                    public string Test()
                                    {
                                        //System.Diagnostics.Debugger.Break();
                                        HelloWorld = ""Just testing..."";
                                        #if DEBUG
                                        return ""Debug testing"";
                                        #else
                                        return ""Release testing"";
                                        #endif
                                    }
                                }";

    public CodeDomEval()
    {
        CSScript.CacheEnabled = false;
    }

    [Fact]
    public void CompileCode()
    {
        Assembly script = CSScript.CodeDomEvaluator.CompileCode(classCode);
        Assert.NotNull(script);
    }

    [Fact]
    public void CompileDebugCode()
    {
        //CSScript.EvaluatorConfig.DebugBuild = true; //or set DebugBuild globally

        var eval = CSScript.CodeDomEvaluator;
        var eval2 = CSScript.CodeDomEvaluator;
        eval2.DebugBuild = true;

        dynamic script = eval.LoadCode(classCode);
        dynamic script2 = eval2.LoadCode(classCode);

        var result = script.Test();  
        var result2 = script2.Test();  

        Assert.Equal("Release testing", result);
        Assert.Equal("Debug testing", result2);
    }

    [Fact]
    public void CompileCode_Error()
    {
        var ex = Assert.Throws<CompilerException>(() =>
            CSScript.CodeDomEvaluator.CompileCode(classCode.Replace("public", "error_word")));

        Assert.Contains("error CS0116: A namespace cannot directly contain", ex.Message);
    }

    [Fact]
    public void CompileMethodInstance()
    {
        dynamic script = CSScript.CodeDomEvaluator
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
        var script = CSScript.CodeDomEvaluator
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
        var Sqr = CSScript.CodeDomEvaluator
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
        var Sqr = CSScript.CodeDomEvaluator
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
        //CSScript.EvaluatorConfig.RefernceDomainAsemblies = false; 
        dynamic script = CSScript.CodeDomEvaluator
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
        dynamic script = CSScript.CodeDomEvaluator
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
        var script = (ICalc)CSScript.CodeDomEvaluator
                                    .LoadCode(@"using System;
                                                using Tests;
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
        //This use-case uses Interface Alignment and this requires all assemblies involved to have non-empty Assembly.Location 
        CSScript.GlobalSettings.InMemoryAssembly = false;

        ICalc script = CSScript.CodeDomEvaluator
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
        var Test = CSScript.CodeDomEvaluator
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
    public void Clone()
    {
        var eval1 = CSScript.CodeDomEvaluator.Clone();
        var eval2 = CSScript.CodeDomEvaluator.Clone();

        var sub = eval1.LoadDelegate<Func<int, int, int>>(
                                   @"int Sub(int a, int b) {
                                         return a - b;
                                     }");

        var sum = eval2.LoadDelegate<Func<int, int, int>>(
                                   @"int Sub(int a, int b) {
                                         return a + b;
                                     }");

        var result = sum(7, sub(4, 2));

        Assert.Equal(9, result);
    }

    [Fact]
    public void Clone2()
    {
        var eval1 = CSScript.CodeDomEvaluator.Clone();
        var eval2 = CSScript.CodeDomEvaluator.Clone();

        var sub = eval1.LoadDelegate<Func<int, int, int>>(
                                   @"int Sub(int a, int b) {
                                         return a - b;
                                     }");

        var sum = eval2.LoadDelegate<Func<int, int, int>>(
                                   @"int Sub(int a, int b) {
                                         return a + b;
                                     }");

        var result = sum(7, sub(4, 2));

        Assert.Equal(9, result);
    }

    [Fact]
    public void LoadDelegateFunc()
    {
        var Product = CSScript.CodeDomEvaluator
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
        dynamic script = CSScript.CodeDomEvaluator
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
        //This use-case uses Interface Alignment and this requires all assemblies involved to have non-empty Assembly.Location 
        CSScript.GlobalSettings.InMemoryAssembly = false;
        ICalc script = CSScript.CodeDomEvaluator 
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