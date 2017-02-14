//css_ref ..\..\..\CSScriptLibrary.dll
//css_ref ..\..\..\Lib\Mono.CSharp.dll;
//css_ref Microsoft.CSharp;
//css_ref System.Core;
using System;
using System.Reflection;
using CSScriptLibrary;

public class Data
{
    public static int Count;
}

public class HostApp
{
    public static HostApp Instance;

    static string code =
          @"using System;
            public class Script
            {
                public int Sum(int a, int b)
                {
                    return a+b;
                }
            }";

    public string Name;

    public HostApp()
    {
        Instance = this;
    }

    static public void Main()
    {
        try
        {
            //It is a good idea to call CSScript.Evaluator.Reset() between tests.
            //Though resetting is omitted in these samples to improve the readability.  

            var host = new HostApp();

            host.Eval_Returning_Result();
            host.Eval_Void();
            host.Eval_Void_With_Pure_Mono();

            host.LoadCode();
            host.LoadCode_With_Interface_Type_Casting();
            host.LoadCode_With_Interface_Alignment();
            host.LoadCode_With_Host_Referencing();

            host.CompileCode_With_CSScript_CreateObject();
            host.CompileCode_With_Assembly_CreateInstance();

            host.LoadMethod();
            host.LoadMethod_With_InterfaceAlignment();
            host.LoadMethod_With_Host_Referencing();

            host.CreateDelegate();

            host.CodeDOM_Debug();
            host.Evaluator_Debug();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public interface ICalc
    {
        int Sum(int a, int b);
    }

    void LoadCode()
    {
        dynamic script = CSScript.Evaluator.LoadCode(code);

        int result = script.Sum(1, 2);
    }

    void LoadCode_With_Interface_Type_Casting()
    {
        //inherit Script from the ICalc and you do not have
        //to use dynamic and all script invokes are strongly typed now
        string codeToCompile = code.Replace("class Script", "class Script : HostApp.ICalc");

        var script = (HostApp.ICalc)CSScript.Evaluator.LoadCode(codeToCompile);
        int result = script.Sum(1, 2);
    }

    void LoadCode_With_Interface_Alignment()
    {
        //NOTE: the scrip class defined in 'code' does not inherit from ICalc
        ICalc calc = CSScript.Evaluator.LoadCode<ICalc>(code);
        int result = calc.Sum(1, 2);
    }

    void LoadCode_With_Host_Referencing()
    {
        //there is no need to call ReferenceAssemblyOf as all AppDomain assemblies are already referenced
        //during the CSScript.Evaluator initialization
        //CSScript.Evaluator.ReferenceAssemblyOf<HostApp>();

        dynamic script = CSScript.Evaluator.LoadCode(@"
                        using System;
                        public class Script
                        {
                            public void Print(HostApp host)
                            {
                                Console.WriteLine(host.GetType().Assembly);
                                host.Name = ""testHost"";
                                Console.WriteLine(host.Name);
                            }
                        }");

        script.Print(this);
    }

    void CompileCode_With_CSScript_CreateObject()
    {
        //reateObject is the CSScript extension method to the Assembly type
        //for creating the instances with the wild-card type name definitions
        dynamic script = CSScript.Evaluator.CompileCode(code)
                                           .CreateObject("*"); //CreateObject("Script") will also work

        int result = script.Sum(1, 2);
    }

    void CompileCode_With_Assembly_CreateInstance()
    {
        //CreateInstance is a canonical Assembly method
        dynamic script = CSScript.Evaluator.CompileCode(code)
                                           .CreateInstance("Script");

        int result = script.Sum(1, 2);
    }

    void LoadMethod()
    {
        //The method definition will be decorated with the class definition by CSScript
        //Note: method must be public in order to be exercised by the host but the
        //public access modifier for the method (first method only) can be injected
        //by CSScript if missing in the method code.

        dynamic script = CSScript.Evaluator.LoadMethod(@"int Product(int a, int b)
                                                         {
                                                             return a * b;
                                                         }");

        int result = script.Product(3, 2);
    }

    public interface ICalc2
    {
        int Sum(int a, int b);

        int Div(int a, int b);
    }

    void LoadMethod_With_InterfaceAlignment()
    {
        //Note: Method definition code must have at least all interface methods implemented

        ICalc2 script = CSScript.Evaluator.LoadMethod<ICalc2>(
                                                       @"public int Sum(int a, int b)
                                                         {
                                                             return a + b;
                                                         }
                                                         public int Div(int a, int b)
                                                         {
                                                             return a/b;
                                                         }");
        int result = script.Div(15, 3);
    }

    void LoadMethod_With_Host_Referencing()
    {
        //Note: Host and Script have full visibility of each other
        dynamic script = CSScript.Evaluator.LoadMethod(@"void Print(HostApp host)
                                                         {
                                                             Console.WriteLine(host.Name);
                                                         }");
        this.Name = "TestScript";
        script.Print(this);
    }

    void CreateDelegate()
    {
        //Note: Product is not a type but a method delegate
        var Product = CSScript.Evaluator.CreateDelegate(@"int Product(int a, int b)
                                                          {
                                                              return a * b;
                                                          }");

        int result = (int)Product(3, 2);
    }

    void Eval_Returning_Result()
    {
        //Note: the statement must be a fully formatted C# statement
        //with the semicolon at the end.
        //See Mono.CSharp.Evaluator documentation for the details.

        this.Name = (string)CSScript.Evaluator.Evaluate("\"Host\".ToUpper();");
    }

    void Eval_Void()
    {
        Data.Count++;

        CSScript.Evaluator.Run("System.Console.Write(\"The art\");");
        CSScript.Evaluator.Run("using System;"); //how we can skip "System." 
        CSScript.Evaluator.Run("Console.WriteLine(\" of Eval is dark and mysterious...\");");
        CSScript.Evaluator.Run("Console.WriteLine();");
        CSScript.Evaluator.Run("Console.WriteLine(\"Current directory: \" + Environment.CurrentDirectory);");
        CSScript.Evaluator.Run("Console.WriteLine(\"Data.Count = \" + Data.Count);");
    }

    bool IsMono
    {
        get
        {
            return Type.GetType("Mono.Runtime") != null;
        }
    }

    void Eval_Void_With_Pure_Mono()
    {
        Mono.CSharp.Evaluator evaluator = CSScript.MonoEvaluator.GetService();
        //Note: Calling evaluator.ReferenceAssembly will trigger the error as the ExecutingAssembly
        //(as one of all AddDomain static assemblies) is already referenced during 
        //CSScript.Evaluator initialization. 
        //However the call CSScript.Evaluator.ReferenceAssembly is safe as CSScript.Evaluator
        //keeps track of the assemblies and does not reference one again if it is already there. 
        //evaluator.ReferenceAssembly (Assembly.GetExecutingAssembly());

        this.Name = "ScriptTester";
        evaluator.Run("System.Console.WriteLine(\"Host Name = \" + HostApp.Instance.Name);");
    }

    void CodeDOM_Debug()
    {
        //Because .NET compiler always creates source file it is possible for the debugger to 
        //map the execution step to the source code statement. 
        //Both Assert and Break will work just fine
        var Test = CSScript.LoadMethod(
            @"using System.Diagnostics;
              void Test()
              {
                  Trace.WriteLine(""Trace.WriteLine Test..."");
                  Debug.WriteLine(""Debug.WriteLine Test..."");
                  //Debug.Assert(false);
                  //Debugger.Break();
              }", null, debugBuild: true)
                  .GetStaticMethod();
        Test();
    }

    void Evaluator_Debug()
    {
        //It is problematic to set breakpoint in the script compiled with Mono compiler as service.
        //This is because there is no source file created as opposite to the .NET CodeDOM compiler.
        //There were a few posts on the forums asking for the debugging solutions of the 
        //Mono.CSharp.Evaluator. None of them was answered but keep checking may be some Mono
        //guys will eventually post the answer:
        //http://www.thebirdietoldme.com/userActions/thread/Question.aspx?id=11816467
        //http://stackoverflow.com/questions/11816467/debugging-code-compiled-using-the-mono-csharp-evaluator

        //From the other hand compiling the "debug" code is possible even if debugging the source code is not.
        //The following example demonstrates how to enable Trace and Debug outputs.
        //Assert and Break are commented out as the source code mapping by Mono.CSharp is currently 
        //impossible.

        //If the full scale debugging is vital then it is recommended to do the temporary switch to CodeDOM 
        //compiler to do the debugging as it is demonstrated in the CodeDOM_Debug() sample in this file.

        CSScript.Evaluator.Configuration = BuildConfiguration.Debug;

        dynamic script = CSScript.Evaluator.LoadMethod(
                @"using System.Diagnostics;
                  void Test()
                  {
                      Trace.WriteLine(""Trace.WriteLine Test..."");
                      Debug.WriteLine(""Debug.WriteLine Test..."");
                      //Debug.Assert(false);
                      //Debugger.Break();
                  }");

        script.Test();
    }
}