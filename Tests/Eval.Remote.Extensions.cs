using System;
using System.Linq;
using System.Collections.Generic;
using CSScriptLibrary;
using System.Threading.Tasks;
using Tests;
using Xunit;

public class EvalRemoteExtensions
{
    [Fact]
    public async void RemoteAsync()
    {
        var sum = await Task.Run(() =>
                             CSScript.Evaluator
                                     .CreateDelegateRemotely<int>(@"int Sum(int a, int b)
                                                                    {
                                                                        return a+b;
                                                                    }"));
        Assert.Equal(18, sum(15, 3));

        sum.UnloadOwnerDomain();
    }

    [Fact]
    public void CreateDelegateRemotelyTypedy()
    {
        var sum = CSScript.Evaluator
                          .CreateDelegateRemotely<int>(@"int Sum(int a, int b)
                                                         {
                                                             return a+b;
                                                         }");
        Assert.Equal(18, sum(15, 3));

        sum.UnloadOwnerDomain();
    }

    [Fact]
    public void CreateDelegateRemotely()
    {
        var sum = CSScript.Evaluator
                          .CreateDelegateRemotely(@"int Sum(int a, int b)
                                                    {
                                                        return a+b;
                                                    }");
        Assert.Equal(18, (int)sum(15, 3));

        sum.UnloadOwnerDomain();
    }

    [Fact]
    public void CreateDelegateRemotelyRoslyn()
    {
        // Roslyn assemblies have unorthodox dependency model, which needs an explicit AppDomain assembly
        // resolving if the Roslyn assemblies are not in the entry-assembly folder.
        //
        // MSTest (or xUnit) loads the test assembly from its build directory but places and loads the
        // dependencies into individual temporary folders (one asm per folder). What a "brilliant" idea!!!
        //
        // MSText does explicit asm resolving, what helps for MonoEval. However Roslyn resolving
        // has to be based on name-only algorithm (a version checking) as it has wired dependencies on
        // portable asms. Thus we need to set up special asm probing (extra argument) for Roslyn case.

        var sum = CSScript.RoslynEvaluator
                          .CreateDelegateRemotely(@"int Sum(int a, int b)
                                                    {
                                                        return a+b;
                                                    }",
                                                    @"..\..\..\Roslyn.Scripting");
        Assert.Equal(18, (int)sum(15, 3));

        sum.UnloadOwnerDomain();
    }

    [Fact]
    public void CreateDelegateRemotelyMono()
    {
        var sum = CSScript.MonoEvaluator
                          .CreateDelegateRemotely(@"int Sum(int a, int b)
                                                    {
                                                        return a+b;
                                                    }");
        Assert.Equal(18, (int)sum(15, 3));

        sum.UnloadOwnerDomain();
    }

    [Fact]
    public void CreateDelegateRemotelyDebug()
    {
        var eval = CSScript.CodeDomEvaluator;
        eval.DebugBuild = true;
        var sum = eval.CreateDelegateRemotely(@"int Sum(int a, int b)
                                                {
                                                    //System.Diagnostics.Debug.Assert(false);
                                                    return a+b;
                                                }");
        Assert.Equal(18, (int)sum(15, 3));

        sum.UnloadOwnerDomain();
    }

    [Fact]
    public void CreateDelegateRemotelyCodeDom()
    {
        var sum = CSScript.CodeDomEvaluator
                          .CreateDelegateRemotely(@"int Sum(int a, int b)
                                                    {
                                                        return a+b;
                                                    }");
        Assert.Equal(18, (int)sum(15, 3));

        sum.UnloadOwnerDomain();
    }

    [Fact]
    public void LoadCodeRemotelyCasted()
    {
        var script = CSScript.Evaluator
                             .LoadCodeRemotely<ICalc>(@"using System;
                                                        public class Calc : MarshalByRefObject, Tests.ICalc
                                                        {
                                                            public int Sum(int a, int b)
                                                            {
                                                                return a+b;
                                                            }
                                                        }", @"..\..\..\Roslyn.Scripting");
        Assert.Equal(18, script.Sum(15, 3));

        script.UnloadOwnerDomain();
    }

    [Fact]
    public void LoadCodeRemotelyTyped()
    {
        //Mono and Roslyn file-less asms cannot be used to build duck-typed proxies
        var script = CSScript.Evaluator
                             .LoadCodeRemotely<ICalc>(@"using System;
                                                        public class Calc : MarshalByRefObject
                                                        {
                                                            public int Sum(int a, int b)
                                                            {
                                                                return a+b;
                                                            }
                                                        }", @"..\..\..\Roslyn.Scripting");
        Assert.Equal(18, script.Sum(15, 3));

        script.UnloadOwnerDomain();
    }

    [Fact]
    public void LoadMethodRemotely()
    {
        //Mono and Roslyn file-less asmss cannot be used to build duck-typed proxies
        var script = CSScript.CodeDomEvaluator
                             .LoadMethodRemotely<ICalc>(@"int Sum(int a, int b)
                                                          {
                                                              return a+b;
                                                          }");
        Assert.Equal(18, script.Sum(15, 3));

        script.UnloadOwnerDomain();
    }
}
