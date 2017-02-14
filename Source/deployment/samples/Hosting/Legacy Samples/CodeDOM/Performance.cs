//css_ref Microsoft.CSharp;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using CSScriptLibrary;

public interface ICalculator
{
    int Add(int a, int b);
}

public interface IAnotherCalculator
{
    int Add(int a, int b);
}

public class Host
{
    static void Main()
    {
        Assembly assembly = CSScript.LoadCode(
            @"using System;
              public class Calculator : ICalculator
              {
                  public int Add(int a, int b)
                  {
                      return a + b;
                  }

                  public string Join(string a, string b)
                  {
                      return a + b;
                  }
              }");

        AsmHelper calc = new AsmHelper(assembly);
        object instance = calc.CreateObject("Calculator"); //calc.CreateObject("*") can be used too if assembly has only one class defined
        FastInvokeDelegate methodInvoker = calc.GetMethodInvoker("Calculator.Add", 0, 0);

        int numOfLoops = 1000000;

        TestReflection(numOfLoops, calc, instance);
        TestFastInvoking(numOfLoops, calc, instance);
        TestDelegates(numOfLoops, methodInvoker, instance);
        TestInterface(numOfLoops, instance);
        TestInterfaceAlignment(numOfLoops, instance);
        TestDynamic(numOfLoops, instance);
        TestCompiledCode(numOfLoops);
        TestCompiledDelegate(numOfLoops);

        //TestMethodDelegates();
    }

    static void TestReflection(int numOfLoops, AsmHelper script, object instance)
    {
        //Starting from version v2.2 pure Reflection calls (script.CachingEnabled = false) 
        //are ~2 times faster simple because of internal optimization in AsmHelper. 
        script.CachingEnabled = false;

        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < numOfLoops; i++)
            script.InvokeInst(instance, "Calculator.Add", 1, 2);

        sw.Stop();
        Console.WriteLine("Reflection: " + sw.ElapsedMilliseconds);
    }

    static void TestFastInvoking(int numOfLoops, AsmHelper script, object instance)
    {
        //Starting from version v2.2 pure Reflection calls are no longer the only available 
        //option for invoking scrips methods. AsmHelper can cache dynamically emitted method 
        //invokers and use them internally when AsmHelper's Invoke()/InvokeInst() called. 
        //
        //Thus Invoke()/InvokeInst() are more than 100 times faster in v2.2 than in v2.1 when 
        //AsmHelper caching is enabled (script.CachingEnabled = true).

        script.CachingEnabled = true; //it is true by default

        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < numOfLoops; i++)
            script.InvokeInst(instance, "Calculator.Add", 1, 2);

        sw.Stop();
        Console.WriteLine("Fast invoking: " + sw.ElapsedMilliseconds);
    }


    static void TestDelegates(int numOfLoops, FastInvokeDelegate fastInvoker, object instance)
    {
        //Starting from version v2.2 AsmHelper can return dynamically emitted method 
        //invoker (delegate) which can be used by the host application to invoke script methods without AsmHelper.
        //
        //This option allows script methods execution more than 250 times faster in than in pure reflection calls 
        //available in AsmHelper v2.1. The generated FastInvokeDelegate is almost as fast as direct calls for statically compiled types.


        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < numOfLoops; i++)
            fastInvoker(instance, 1, 2);

        sw.Stop();
        Console.WriteLine("Delegate: " + sw.ElapsedMilliseconds);
    }

    static void TestInterface(int numOfLoops, object instance)
    {
        //Using interfaces represents the best possible Invocation option with respect to performance (and type safety).
        //When dynamic type (from the script) is typecasted to the interface it is no longer "treated" as a dynamic type.
        //You can use compile time type checking and at runtime all method calls are "direct calls".
        //
        //This option is a clear winner (along with InterfaceAlignment) as it also allows usage of intellisense (at development stage) for the script types.

        ICalculator iCalc = (ICalculator)instance;

        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < numOfLoops; i++)
            iCalc.Add(1, 2);

        sw.Stop();
        Console.WriteLine("Interface: " + sw.ElapsedMilliseconds);
    }
    

    static void TestDynamic(int numOfLoops, object instance)
    {
        //Using C# 4.0 gives you the smallest coding overhead and great readability. Though even if it is faster than Reflection it still slower than Interfaces. And also 
        //it is less typesasfe than InterfaceAlignment, which allows validating the whole script class at once.
        //

        dynamic Calc = instance;

        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < numOfLoops; i++)
            Calc.Add(1, 2);

        sw.Stop();
        Console.WriteLine("Dynamic: " + sw.ElapsedMilliseconds);
    }
    static void TestInterfaceAlignment(int numOfLoops, object instance)
    {
        //Using interfaces represents the best possible Invocation option with respect to performance (and type safety).
        //When dynamic type (from the script) is typecasted to the interface it is no longer "treated" as a dynamic type.
        //You can use compiletime type checking and at runtime all method calls are "direct calls".
        //
        //This option is a clear winner as it also allows usage of intellisense (at development stage) for the script types.
        //
        //Note the Calculator instance does not actually inmplement IPrivateCalculator but it still can be aligned with this 
        //interface as it has int Add(int a, int b) method.

        IAnotherCalculator iCalc = instance.AlignToInterface<IAnotherCalculator>();

        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < numOfLoops; i++)
            iCalc.Add(1, 2);

        sw.Stop();
        Console.WriteLine("Interface (Aligned): " + sw.ElapsedMilliseconds);
    }

    static int Add(int a, int b)
    {
        return a + b;
    }

    static void TestCompiledCode(int numOfLoops)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < numOfLoops; i++)
            Add(1, 2);

        sw.Stop();
        Console.WriteLine("Compiled code: " + sw.ElapsedMilliseconds);
    }

    static void TestCompiledDelegate(int numOfLoops)
    {
        Func<int, int, int> add = (a, b) => a + b;

        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < numOfLoops; i++)
            add(1, 2);

        sw.Stop();
        Console.WriteLine("Compiled delegate: " + sw.ElapsedMilliseconds);
    }

    static void TestMethodDelegates()
    {
        Assembly assembly = CSScript.LoadCode(
            @"using System;
              public class Calculator
              {
                  static public void PrintSum(int a, int b)
                  {
                      Console.WriteLine(a + b);
                  }
                  public int Multiply(int a, int b)
                  {
                      return (a * b);
                  }	
              }");

        AsmHelper calc = new AsmHelper(assembly);

        //using static method delegate
        var PrintSum = calc.GetStaticMethod("Calculator.PrintSum", 0, 0);
        PrintSum(1, 2);

        //using instance method delegate
        var obj = calc.CreateObject("Calculator");
        var Multiply = calc.GetMethod(obj, "Multiply", 0, 0);
        Console.WriteLine(Multiply(3, 5));

        //using general method delegate; can invoke both static and instance methods 
        var methodInvoker = calc.GetMethodInvoker("Calculator.PrintSum", 0, 0);
        methodInvoker(null, 5, 12);
    }
}

