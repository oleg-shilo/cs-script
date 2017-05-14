using System;
using System.Reflection;
using System.Threading.Tasks;
using CSScriptLibrary;
using Tests;
using Xunit;
using System.IO;

public class EvalAsyncExtensions
{
    public EvalAsyncExtensions()
    {
        CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
    }

    [Fact]
    public async void LoadDelegateAsync()
    {
        var product = await CSScript.Evaluator
                                .LoadDelegateAsync<Func<int, int, int>>(
                                      @"int Product(int a, int b)
                                            {
                                                return a * b;
                                            }");

        Assert.Equal(8, product(4, 2));
    }

    [Fact]
    public async void LoadMethodAsync()
    {
        dynamic script = await CSScript.Evaluator
                                   .LoadMethodAsync(@"public int Sum(int a, int b)
                                            {
                                                return a + b;
                                            }
                                            public int Div(int a, int b)
                                            {
                                                return a/b;
                                            }");
        Assert.Equal(5, script.Div(15, 3));
    }

    [Fact]
    public async void LoadCodeAsync()
    {
        ICalc calc = await CSScript.Evaluator
                                       .LoadCodeAsync<ICalc>(
                                                  @"using System;
                                                    public class Script
                                                    {
                                                        public int Sum(int a, int b)
                                                        {
                                                            return a+b;
                                                        }
                                                    }");
        Assert.Equal(3, calc.Sum(1, 2));
    }

    [Fact]
    public async void LoadCodeAsync2()
    {
        dynamic calc = await CSScript.Evaluator
                                        .LoadCodeAsync(
                                                 @"using System;
                                                   public class Script
                                                   {
                                                       public int Sum(int a, int b)
                                                       {
                                                           return a+b;
                                                       }
                                                   }");

        Assert.Equal(3, calc.Sum(1, 2));
    }

    [Fact]
    public async void CreateUnsafeDelegateAsync()
    {
        var test = await CSScript.CodeDomEvaluator
                                 .CreateDelegateAsync(
                                          @"//css_co /unsafe
                                            unsafe float UnsafeFunction(IntPtr data)
                                            {
                                                return (float)data + 5f;
                                            }");
        var data = (IntPtr)10;
        var result = test(data);
        Assert.Equal(15f, result);
    }

    [Fact]
    public async void CreateUnsafeDelegateAsync2()
    {
        MonoEvaluator.CreateCompilerSettings = () => new Mono.CSharp.CompilerSettings { Unsafe = true };

        var test = await CSScript.MonoEvaluator
                                 .CreateDelegateAsync(
                                          @"unsafe float UnsafeFunction(IntPtr data)
                                            {
                                                return (float)data + 5f;
                                            }");
        var data = (IntPtr)10;
        var result = test(data);
        Assert.Equal(15f, result);
    }

    [Fact]
    public async void CreateUnsafeDelegateAsync2b()
    {
        //MonoEvaluator.CreateCompilerSettings = () => new Mono.CSharp.CompilerSettings { Unsafe = true };
        var eval = CSScript.MonoEvaluator;
        eval.CompilerSettings.Unsafe = true;
        var test = await eval.CreateDelegateAsync(
                                          @"unsafe float UnsafeFunction(IntPtr data)
                                            {
                                                return (float)data + 5f;
                                            }");
        var data = (IntPtr)10;
        var result = test(data);
        Assert.Equal(15f, result);
    }

    [Fact]
    public async void CreateUnsafeDelegateAsync3()
    {
        var test = await CSScript.RoslynEvaluator
                                 .CreateDelegateAsync(
                                          @"unsafe float UnsafeFunction(IntPtr data)
                                            {
                                                return (float)data + 5f;
                                            }");
        var data = (IntPtr)10;
        var result = test(data);
        Assert.Equal(15f, result);
    }

    [Fact]
    public async void CreateDelegateAsync()
    {
        string message = "success";
        Environment.SetEnvironmentVariable("CreateDelegateAsync_test", "empty");

        var set = await CSScript.Evaluator
                                .CreateDelegateAsync(
                                          @"void Log(string message)
                                            {
                                                System.Environment.SetEnvironmentVariable(""CreateDelegateAsync_test"", message);
                                            }");
        set(message);

        Assert.Equal(message, Environment.GetEnvironmentVariable("CreateDelegateAsync_test"));
    }

    [Fact]
    public async void CreateDelegateAsyncTyped()
    {
        var product = await CSScript.Evaluator
                                       .CreateDelegateAsync<int>(
                                          @"int Product(int a, int b)
                                            {
                                                return a * b;
                                            }");
        Assert.Equal(45, product(15, 3));
    }

    [Fact]
    public async void CompileCodeAsync()
    {
        Assembly script = await CSScript.Evaluator
                                        .CompileCodeAsync(@"using System;
                                                            public class Script
                                                            {
                                                                public int Sum(int a, int b)
                                                                {
                                                                    return a+b;
                                                                }
                                                            }");
        dynamic calc = script.CreateObject("*");
        Assert.Equal(18, calc.Sum(15, 3));
    }

    [Fact]
    public async void CompileMethodAsync()
    {
        Assembly script = await CSScript.Evaluator
                                        .CompileMethodAsync(
                                                         @"int Sum(int a, int b)
                                                           {
                                                               return a+b;
                                                           }");
        dynamic calc = script.CreateObject("*");
        Assert.Equal(18, calc.Sum(15, 3));
    }
}