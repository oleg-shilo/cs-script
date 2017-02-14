//css_ref ..\..\..\CSScriptLibrary.dll
//css_ref ..\..\..\Lib\Mono.CSharp.dll;
//css_ref Microsoft.CSharp.dll;
//css_ref System.Core;

using CSScriptLibrary;
using Mono.CSharp;
using System;
using System.Diagnostics;

public class Host
{
    static int index = 0;
    static int index2 = 0;

    static public string GetCode(string className, string methodName = null)
    {
        string methodIndex = methodName ?? (index2++).ToString();
        return codeTemplate.Replace("Div", "Div" + methodIndex)
                           .Replace("[CLASS_NAME]", className);
    }

    static public string GetClass()
    {
        return "Script" + index++;
    }

    static string codeTemplate =
          @"using System;
            public class [CLASS_NAME]
            {
                public int Sum(int a, int b)
                {
                    return a+b;
                }

                public int Div(int a, int b)
                {
                    return a/b;
                }
            }";

    static Mono.CSharp.Evaluator evaluator = new Mono.CSharp.Evaluator(
           new CompilerContext(
               new CompilerSettings(),
               new ConsoleReportPrinter()));

    static void Main()
    {
        Console.WriteLine("Press 'Enter' to start...");
        Console.ReadLine();

        int numOfTests = 1500;

        TestMemory("CSScript.Evaluator - CreateDelegate", numOfTests, Hybrid_CreateDelegate_Test);
        TestMemory("CSScript.Evaluator - LoadMethod", numOfTests, Hybrid_LoadMethod_Test);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>");

        TestMemory("CSScript.Evaluator - Fixed script with Interface Alignment", numOfTests, Hybrid_InterfaceAlignment_Test);
        TestMemory("CSScript.Evaluator - Fixed script with Interface type casting", numOfTests, Hybrid_InterfaceCasting_Test);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>");

        TestMemory("Mono.CSharp - Fixed script", numOfTests, MonoCSharp_FixedScript_Test);
        TestMemory("CSScript.Evaluator - Fixed script", numOfTests, Hybrid_FixedScript_Test);
        TestMemory("CSScript.CodeDOM - Fixed script", numOfTests, CSScript_FixedScript_Test);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>");

        TestMemory("Mono.CSharp - Fixed class name", numOfTests, MonoCSharp_FixedClassName_Test);
        TestMemory("CSScript.Evaluator - Fixed class name", numOfTests, Hybrid_FixedClassName_Test);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>");

        TestMemory("Mono.CSharp - Changing script", numOfTests, MonoCSharp_Test);
        TestMemory("CSScript.Evaluator - Changing script", numOfTests, Hybrid_Test);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>");

        Console.WriteLine("Done");
        Console.ReadLine();
    }

    static void Hybrid_Test()
    {
        string className = GetClass();
        string code = GetCode(className);

        dynamic script = CSScript.Evaluator.LoadCode(code);

        int result = script.Sum(1, 2);
    }

    static void Hybrid_FixedClassName_Test()
    {
        string className = "Script";
        string code = GetCode(className);

        dynamic script = CSScript.Evaluator.LoadCode(code);

        int result = script.Sum(1, 2);
    }

    static void Hybrid_FixedScript_Test()
    {
        string className = "Script";
        string code = GetCode(className, "");

        dynamic script = CSScript.Evaluator.LoadCode(code);

        int result = script.Sum(1, 2);
    }

    public interface ICalc
    {
        int Sum(int a, int b);
    }

    static void Hybrid_InterfaceAlignment_Test()
    {
        string className = "Script";
        string code = GetCode(className, "");

        ICalc script = CSScript.Evaluator.LoadCode<ICalc>(code);

        int result = script.Sum(1, 2);
    }

    static void Hybrid_InterfaceCasting_Test()
    {
        string className = "Script : Host.ICalc";
        string code = GetCode(className, "");

        var script = (ICalc)CSScript.Evaluator.LoadCode(code);

        int result = script.Sum(1, 2);
    }
   
    static void Hybrid_LoadMethod_Test()
    {
        dynamic script = CSScript.Evaluator.LoadMethod(@"int Product(int a, int b)
                                                         { 
                                                             return a * b; 
                                                         }");

        int result = script.Product(3, 2);
    }

    static void Hybrid_CreateDelegate_Test()
    {
        var Product = CSScript.Evaluator.CreateDelegate(
                                                @"int Product(int a, int b)
                                                  { 
                                                      return a * b; 
                                                  }");

        int result = (int)Product(3, 2);
    }
   
    static void CSScript_Test()
    {
        //bad API choice performance and memory wise as the script is changing between runs.
        //no need even to profile 
        string className = GetClass();
        string code = GetCode(className);

        dynamic script = CSScript.LoadCode(code).CreateObject("*");

        int result = script.Sum(1, 2);
    }

    static void CSScript_FixedScript_Test()
    {
        string className = "Script";
        string code = GetCode(className, "");

        dynamic script = CSScript.LoadCode(code).CreateObject("*");

        int result = script.Sum(1, 2);
    }

    static void MonoCSharp_Test()
    {
        string className = GetClass();
        string code = GetCode(className);

        evaluator.Compile(code);

        dynamic script = evaluator.Evaluate("new " + className + "();");

        int result = script.Sum(1, 2);
    }

    static void MonoCSharp_FixedClassName_Test()
    {
        string className = "Script";
        string code = GetCode(className);

        evaluator.Compile(code);

        dynamic script = evaluator.Evaluate("new " + className + "();");

        int result = script.Sum(1, 2);
    }

    static void MonoCSharp_FixedScript_Test()
    {
        string className = "Script";
        string code = GetCode(className, "");

        evaluator.Compile(code);

        dynamic script = evaluator.Evaluate("new " + className + "();");

        int result = script.Sum(1, 2);
    }

    static void TestMemory(string message, int numOfLoops, Action action)
    {
        var sw = new Stopwatch();
        sw.Start();

        Console.WriteLine(message);

        int i = 0;

        for (; i < numOfLoops; i++)
        {
            action();

            if ((i % 10) == 0)
            {
                GC.Collect();
                Console.Write(string.Format("\r Cycle = {2:0000}: MemoryPrivateBytes = {0:00000} KB;  AppDomainAssemblies = {1}  \r", Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1), AppDomain.CurrentDomain.GetAssemblies().Length, i));
                if ((i % (numOfLoops/6)) == 0)
                    Console.WriteLine();
            }
        }

        sw.Stop();

        Console.WriteLine();
        Console.WriteLine(" Time: " + sw.ElapsedMilliseconds);
        Console.WriteLine("-------------------------------------------------------");
    }
}