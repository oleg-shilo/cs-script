using csscript;
using CSScriptLibrary;
using System;
using System.Reflection;
using Tests;
using Xunit;

public class CodeDom
{
    static string classCode = @"public class ScriptedClass
                                    {
                                        public string HelloWorld {get;set;}
                                        public ScriptedClass()
                                        {
                                            //{$comment}
                                            HelloWorld = ""Hello Roslyn!"";
                                        }
                                    }";


    /// <summary>
    /// Gets the unique class code so caching is disabled.
    /// </summary>
    static string uniqueClassCode
    {
        get { return classCode.Replace("{$comment}", Guid.NewGuid().ToString()); }
    }

    [Fact]
    public void CompileCode()
    {
        string scriptAsm = CSScript.CompileCode(uniqueClassCode);
        Assert.True(scriptAsm.EndsWith(".compiled"));
    }

    [Fact]
    public void CompileCode_Error()
    {
        var ex = Assert.Throws<CompilerException>(() =>
                     CSScript.CompileCode(classCode.Replace("public", "error_word")));
        Assert.Contains("error CS0116: A namespace cannot directly contain", ex.Message);
    }

    [Fact]
    public void LoadMethodInstance()
    {
        dynamic script = CSScript.LoadMethod(@"int Sqr(int data)
                                               {
                                                     return data * data;
                                               }")
                                 .CreateObject("*");

        var result = script.Sqr(7);

        Assert.Equal(49, result);
    }

    [Fact]
    public void LoadMethodStatic()
    {
        var Test = CSScript.LoadMethod(@"using Tests;
                                         static void Test(InputData data)
                                         {
                                             data.Index = 7;
                                         }")
                           .GetStaticMethodWithArgs("*.Test", typeof(InputData));

        var data = new InputData();
        Test(data);
        Assert.Equal(7, data.Index);
    }

    [Fact]
    public void CreateAction()
    {
        var test = CSScript.CreateAction(@"using Tests;
                                           void Test(InputData data)
                                           {
                                               data.Index = 7;
                                           }");

        var data = new InputData();
        test(data);
        Assert.Equal(7, data.Index);
    }

    [Fact]
    public void CreateFunc()
    {
        var Sqr = CSScript.CreateFunc<int>(@"int Sqr(int a)
                                             {
                                                 return a * a;
                                             }");
        int r = Sqr(3);

        Assert.Equal(9, r);
    }

    [Fact]
    public void LoadCode()
    {
        Assembly asm = CSScript.LoadCode(@"using System;
                                              public class Script
                                              {
                                                  public int Sum(int a, int b)
                                                  {
                                                      return a+b;
                                                  }
                                              }");
        Assert.NotNull(asm);

        dynamic script = asm.CreateObject("*");
        int result = script.Sum(1, 2);

        Assert.Equal(3, result);
    }

    [Fact]
    public void LoadCodeWithInterface()
    {
        var script = (ICalc)CSScript.LoadCode(@"using System;
                                                using Tests;
                                                public class Script : ICalc
                                                {
                                                    public int Sum(int a, int b)
                                                    {
                                                        return a+b;
                                                    }
                                                }")
                                    .CreateObject("*");

        int result = script.Sum(1, 2);

        Assert.Equal(3, result);
    }

    [Fact]
    public void LoadCodeAndAlignToInterface()
    {
        CSScript.GlobalSettings.InMemoryAssembly = false;
        var script = CSScript.LoadCode(@"public class Script
                                                {
                                                    public int Sum(int a, int b)
                                                    {
                                                        return a+b;
                                                    }
                                                }")
                             .CreateObject("*")
                             .AlignToInterface<ICalc>();

        int result = script.Sum(1, 2);

        Assert.Equal(3, result);
    }

    [Fact]
    public void LoadDelegateAction()
    {
        CSScript.CleanupDynamicSources();
        var Test = CSScript.LoadDelegate<Action<InputData>>(
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
        var Product = CSScript.LoadDelegate<Func<int, int, int>>(
                                         @"int Product(int a, int b)
                                           {
                                                return a * b;
                                           }");

        int result = Product(3, 2);
        Assert.Equal(6, result);
    }


    [Fact]
    public void LoadMethodAndAlignToInterface()
    {
        CSScript.GlobalSettings.InMemoryAssembly = false;
        var script = CSScript.LoadMethod(@"int Sum(int a, int b)
                                         {
                                             return a+b;
                                         }")
                             .CreateObject("*")
                             .AlignToInterface<ICalc>();

        int result = script.Sum(1, 2);

        Assert.Equal(3, result);
    }
}
