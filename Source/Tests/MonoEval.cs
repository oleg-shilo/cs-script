using csscript;
using CSScriptLibrary;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tests;
using Xunit;

public class MonoEval : TestBase
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

    [DebugBuildFactAttribute]
    public void CompileCode()
    {
        Assembly script = CSScript.MonoEvaluator.CompileCode(classCode);
        Assert.NotNull(script);
    }

    [DebugBuildFactAttribute]
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

        Assert.Equal("(1,12): error CS1525: Unexpected symbol `class'" + Environment.NewLine, ex.Message);
    }

    [DebugBuildFactAttribute]
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

    [DebugBuildFactAttribute]
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

    [DebugBuildFactAttribute]
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

    [DebugBuildFactAttribute]
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

    [DebugBuildFactAttribute]
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

    [DebugBuildFactAttribute]
    public void LoadCode2()
    {
        dynamic script = CSScript.MonoEvaluator
                                 .With(eval => eval.DisableReferencingFromCode = true)
                                 .LoadCode(@"
                    using System;

                    public class Script
                    {
                        public int Run()
                        {
                            Console.WriteLine(""Hello from Script.Run()"");
                            int x = (int)System.Net.HttpStatusCode.OK;
                            return x;
                        }
                    }");

        int result = script.Run();

        Assert.Equal(200, result);
    }

    [DebugBuildFactAttribute]
    public void LoadCode3()
    {
        dynamic script = CSScript.MonoEvaluator
                                 .LoadCode(@"
                    using System;
                    using System.Runtime.InteropServices;

                    public class Script
                    {
                        public TestStruct Run()
                        {
                            return new TestStruct();
                        }
                    }

                    [StructLayout(LayoutKind.Explicit, Size = 64)]
                    public struct TestStruct
                    {
                        [FieldOffset(0)]
                        public int Index;
                    }");

        var result = script.Run();

        Assert.NotNull(result);
    }

    [DebugBuildFactAttribute]
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

    [DebugBuildFactAttribute]
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

    [DebugBuildFactAttribute]
    public void LoadCodeTyped()
    {
        //some of the accumulated ref assemblies are causing Mono
        //to throw "error CS0433: The imported type `System.MarshalByRefObject' is defined multiple times" ((result of batch UnitTesting))
        //so use fresh copy (clone) of evaluator
        ICalc script = CSScript.MonoEvaluator
                               .Clone(copyRefAssemblies: false)
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

    [DebugBuildFactAttribute]
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

    // [Fact]
    // public void ConcurrentCloning()
    // {
    //     // To ensure that "lock (As.Blocking)" does not hide concurrency bugs

    //     int done = 0;
    //     string error = null;

    //     void Test(Action action)
    //     {
    //         try
    //         {
    //             for (int i = 0; i < 20; i++)
    //                 action();
    //             done++;
    //         }
    //         catch (Exception er)
    //         {
    //             error = er.Message;
    //         }
    //     }

    //     Task.Run(() => Test(CloneImpl));
    //     Task.Run(() => Test(CloneImpl2));

    //     while (done < 2 && error == null)
    //         Thread.Sleep(1000);

    //     Assert.Null(error);
    // }

    [DebugBuildFactAttribute]
    public void Clone()
    {
        lock (As.BlockingTest)
        {
            CloneImpl();
        }
    }

    [DebugBuildFactAttribute]
    public void Clone2()
    {
        lock (As.BlockingTest)
        {
            CloneImpl2();
        }
    }

    public void CloneImpl()
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

        var result = sum(7, sub(4, 2));

        Assert.Equal(9, result);
    }

    public void CloneImpl2()
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

    [DebugBuildFactAttribute]
    public void LoadDelegateFunc()
    {
        lock (As.BlockingTest)
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
    }

    [DebugBuildFactAttribute]
    public void LoadMethod()
    {
        lock (As.BlockingTest)
        {
            dynamic script = CSScript.MonoEvaluator
                                 .LoadMethod(@"int Product(int a, int b)
                                               {
                                                   return a * b;
                                               }");

            int result = script.Product(3, 2);

            Assert.Equal(6, result);
        }
    }

    [DebugBuildFactAttribute]
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