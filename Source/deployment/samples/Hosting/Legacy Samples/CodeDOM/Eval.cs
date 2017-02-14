using System;
using System.Linq;
using CSScriptLibrary;
using System.Diagnostics;

public class Host : MarshalByRefObject
{
    static public void Log(string message, params object[] args)
    {
        Console.WriteLine(string.Format(message, args));
    }

    private static void Main()
    {
        TraceNewAssemblies(UnloadableEval);
        TraceNewAssemblies(ReusableEval);
        PerformanceTest();
    }

    static void TraceNewAssemblies(Action action)
    {
        var loadedAssemblies = from a in AppDomain.CurrentDomain.GetAssemblies()
                               select a.FullName;

        action();

        var newAssemblies = from a in AppDomain.CurrentDomain.GetAssemblies()
                            where !loadedAssemblies.Contains(a.FullName)
                            select a.FullName;

        Console.WriteLine("\nNew assemblies loaded: ");
        foreach (var name in newAssemblies)
            Console.WriteLine("   " + name);
        Console.WriteLine();
    }

    private static void ReusableEval()
    {
        //Note BuildEval returns reusable delegate that can be invoked as any times as required. The delegate is loaded in the current AppDomain and 
        //it is extremely fast and demonstrates practically the same performance as the compiled code. 
        //Though the disposal of this delegate is a responsibility of the caller.
        var Sum = CSScript.BuildEval(@"func(int a, int b) {
                                           Host.Log(""Calculating sum: {0} + {1}"", a, b);
                                           return a + b;
                                       }");

        var result = Sum(1, 2);
        result = Sum(2, 2);
        result = Sum(3, 2);
    }

    private static void UnloadableEval()
    {
        //Note Eval does not return any reusable delegate but rather "evaluates" the routine definition and executes it immediately 
        //with the input parameters specified. 
        //The important aspect of Eval is that the actual routine execution happens in a separate AppDomain, which is unloaded 
        //after the execution. Thus there is no potential memory leaks.

        var result = CSScript.Eval(1, 2,
                                    @"func(int a, int b) {
                                        Host.Log(""Calculating sum: {0} + {1}"", a, b);
                                        return a + b;
                                    }");

        CSScript.Eval(result,
                      "func(object x) { Host.Log(\"Result: {0}\", x); }");

        CSScript.Eval("func() { Host.Log(\"Done...\"); }");
    }

    static int sum(int a, int b) 
    {
        return a + b;
    }

    private static void PerformanceTest()
    {
        var Sum = CSScript.BuildEval(@"func(int a, int b) {
                                           return a + b;
                                       }");
        ProfileAction("'BuildEval'", 1000000, () =>
            {
                var result = Sum(1, 2);
            });
        
        ProfileAction("'Compiled'", 1000000, () =>
            {
                var result = sum(1, 2);
            });

        //Eval is expected to be very slow
        ProfileAction("'Eval'", 20, () =>
            {
                var result = CSScript.Eval(1, 2,
                                         @"func(int a, int b) {
                                               return a + b;
                                           }");
            });
    }

    static void ProfileAction(string message, int cycles, Action action)
    {
        var sw = new Stopwatch();
        sw.Start();
        for (int i = 0; i < cycles; i++)
            action();
        sw.Stop();
        Console.WriteLine("\n{0} execution time for {1} cycles: {2} msec\n", message, cycles, sw.ElapsedMilliseconds);
    }
}