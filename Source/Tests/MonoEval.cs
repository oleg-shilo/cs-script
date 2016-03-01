using csscript;
using CSScriptLibrary;
using System;
using System.Reflection;
using Tests;
using Xunit;

public class MonoEval
{
    static string classCode = @"public class ScriptedClass
                                    {
                                        public string HelloWorld {get;set;}
                                        public ScriptedClass()
                                        {
                                            HelloWorld = ""Hello Roslyn!"";
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

    [Fact]
    public void CompileCode()
    {
        Assembly script = CSScript.MonoEvaluator.CompileCode(classCode);
        Assert.NotNull(script);
    }

    [Fact]
    public void CompileDebugCode()
    {
        //CSScript.EvaluatorConfig.DebugBuild = true; //or set DebugBuild globally

        var eval = CSScript.MonoEvaluator;
        var eval2 = CSScript.MonoEvaluator;
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
                     CSScript.MonoEvaluator.CompileCode(classCode.Replace("public", "error_word")));

        Assert.Equal("(1,12): error CS1525: Unexpected symbol `class'\r\n", ex.Message);
    }

    [Fact]
    public void CompileMethodInstance()
    {
        dynamic script = CSScript.MonoEvaluator
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
        var script = CSScript.MonoEvaluator
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
        var Sqr = CSScript.MonoEvaluator
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
        var Sqr = CSScript.MonoEvaluator
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
        dynamic script = CSScript.MonoEvaluator
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
        dynamic script = CSScript.MonoEvaluator
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
        var script = (ICalc)CSScript.MonoEvaluator
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
        //some of the accumulated ref assemblies are causing Mono 
        //to throw "error CS0433: The imported type `System.MarshalByRefObject' is defined multiple times" ((result of batch UnitTesting))
        //so use fresh copy (clone) of evaluator
        ICalc script = CSScript.MonoEvaluator
                               .Clone(copyRefAssemblies:false)
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
        var Test = CSScript.MonoEvaluator
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
        var eval1 = CSScript.Evaluator.Clone();
        var eval2 = CSScript.Evaluator.Clone();
        
        var sub = eval1.LoadDelegate<Func<int, int, int>>(
                                   @"int Sub(int a, int b) {
                                         return a - b;
                                     }");

        var sum = eval2.LoadDelegate<Func<int, int, int>>(
                                   @"int Sub(int a, int b) {
                                         return a + b;
                                     }");
                                     
        var result = sum(7, sub(4,2));

        Assert.Equal(9, result);
    }

    [Fact]
    public void Clone2()
    {
        //
       // CSScript.DefaultEvaluatorEngine

        var eval1 = CSScript.Evaluator.Clone();
        var eval2 = CSScript.Evaluator.Clone();

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
        var Product = CSScript.MonoEvaluator
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
        dynamic script = CSScript.MonoEvaluator
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
        //ICalc script = CSScript.MonoEvaluator //as alternative syntax
        ICalc script = new MonoEvaluator()
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