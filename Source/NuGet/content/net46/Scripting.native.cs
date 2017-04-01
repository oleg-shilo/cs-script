using CSScriptLibrary;
using System;
using System.Linq;
using System.IO;
using csscript;
using System.CodeDom.Compiler;

// Read in more details about all aspects of CS-Script hosting in applications 
// here: http://www.csscript.net/help/Script_hosting_guideline_.html
//   
// This file contains samples for the script hosting scenarios relying on CS-Script Native interface (API).
// This API is a compiler specific interface, which relies solely on CodeDom compiler. In most of the cases 
// CS-Script Native model is the most flexible and natural choice 
//
// Apart from Native API CS-Script offers alternative hosting model: CS-Script Evaluator, which provides
// a unified generic interface allowing dynamic switch the underlying compiling services (Mono, Roslyn, CodeDom) 
// without the need for changing the hosting code. 
//   
// The Native interface is the original API that was designed to take maximum advantage of the dynamic C# code 
// execution with CodeDom. The original implementation of this API was developed even before any compiler-as-service 
// solution became available. Being based solely on CodeDOM the API doesn't utilize neither Mono nor Roslyn 
// scripting solutions. Despite that CS-Script Native is the most mature, powerful and flexible API available with CS-Script.
// 
// Native interface allows some unique features that are not available with CS-Script Evaluator:
//  - Debugging scripts
//  - Script caching
//  - Script unloading

namespace CSScriptNativeApi
{
    public class HostApp
    {
        public static void Test()
        {
            var host = new HostApp();
            host.Log("Testing compiling services CS-Script Native API");
            Console.WriteLine("---------------------------------------------");

            CodeDomSamples.LoadMethod_Instance();
            CodeDomSamples.LoadMethod_Static();
            CodeDomSamples.LoadDelegate();
            CodeDomSamples.CreateAction();
            CodeDomSamples.CreateFunc();
            CodeDomSamples.LoadCode();
            CodeDomSamples.LoadCode_WithInterface(host);
            CodeDomSamples.LoadCode_WithDuckTypedInterface(host);
            CodeDomSamples.ExecuteAndUnload();
            //CodeDomSamples.DebugTest(); //uncomment if want to fire an assertion during the script execution
        }

        public class CodeDomSamples
        {
            public static void LoadMethod_Instance()
            {
                // 1- LoadMethod wraps method into a class definition, compiles it and returns loaded assembly
                // 2 - CreateObject creates instance of a first class in the assembly  

                dynamic script = CSScript.LoadMethod(@"int Sqr(int data)
                                                       {
                                                           return data * data;
                                                       }")
                                         .CreateObject("*");

                var result = script.Sqr(7);
            }

            public static void LoadMethod_Static()
            {
                // 1 - LoadMethod wraps method into a class definition, compiles it and returns loaded assembly
                // 2 - GetStaticMethod returns first found static method as a duck-typed delegate that 
                //     accepts 'params object[]' arguments 
                //
                // Note: you can use GetStaticMethodWithArgs for higher precision method search: GetStaticMethodWithArgs("*.SayHello", typeof(string)); 
                var sayHello = CSScript.LoadMethod(@"static void SayHello(string greeting)
                                                     {
                                                         Console.WriteLine(greeting);
                                                     }")
                                       .GetStaticMethod();

                sayHello("Hello World!");
            }

            public static void LoadDelegate()
            {
                // LoadDelegate wraps method into a class definition, compiles it and loads the compiled assembly.
                // It returns the method delegate for the method, which matches the delegate specified 
                // as the type parameter of LoadDelegate

                // The 'using System;' is optional; it demonstrates how to specify 'using' in the method-only syntax

                var sayHello = CSScript.LoadDelegate<Action<string>>(
                                                   @"void SayHello(string greeting)
                                                     {
                                                         Console.WriteLine(greeting);
                                                     }");

                sayHello("Hello World!");
            }

            public static void CreateAction()
            {
                // Wraps method into a class definition, compiles it and loads the compiled assembly.
                // It returns duck-typed delegate. A delegate with 'params object[]' arguments and 
                // without any specific return type. 

                var sayHello = CSScript.CreateAction(@"void SayHello(string greeting)
                                                       {
                                                           Console.WriteLine(greeting);
                                                       }");

                sayHello("Hello World!");
            }

            public static void CreateFunc()
            {
                // Wraps method into a class definition, compiles it and loads the compiled assembly.
                // It returns duck-typed delegate. A delegate with 'params object[]' arguments and 
                // int as a return type. 

                var Sqr = CSScript.CreateFunc<int>(@"int Sqr(int a)
                                                     {
                                                         return a * a;
                                                     }");
                int r = Sqr(3);
            }

            public static void LoadCode()
            {
                // LoadCode compiles code and returns instance of a first class 
                // in the compiled assembly  

                dynamic script = CSScript.LoadCode(@"using System;
                                                     public class Script
                                                     {
                                                         public int Sum(int a, int b)
                                                         {
                                                             return a+b;
                                                         }
                                                     }")
                                         .CreateObject("*");

                int result = script.Sum(1, 2);
            }

            public static void LoadCodeWithConfig()
            {
                // LoadCode compiles code and returns instance of a first class 
                // in the compiled assembly  

                string file = Path.GetTempFileName();
                try
                {
                    File.WriteAllText(file, @"using System;
                                              public class Script
                                              {
                                                  public int Sum(int a, int b)
                                                  {
                                                      return a+b;
                                                  }
                                              }");

                    var settings = new Settings();
                    //settings = null; // set to null to foll back to defaults 

                    dynamic script = CSScript.LoadWithConfig(file, null, false, settings, "/define:TEST")
                                             .CreateObject("*");

                    int result = script.Sum(1, 2);
                }
                finally
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }

            public static void LoadCode_WithInterface(HostApp host)
            {
                // 1 - LoadCode compiles code and returns instance of a first class in the compiled assembly. 
                // 2 - The script class implements host app interface so the returned object can be type casted into it.
                // 3 - In this sample host object is passed into script routine.

                var calc = (ICalc) CSScript.LoadCode(@"using CSScriptNativeApi;
                                                      public class Script : ICalc
                                                      { 
                                                          public int Sum(int a, int b)
                                                          {
                                                              if(Host != null) 
                                                                  Host.Log(""Sum is invoked"");
                                                              return a + b;
                                                          }
                                                      
                                                          public HostApp Host { get; set; }
                                                      }")
                                          .CreateObject("*");
                calc.Host = host;
                int result = calc.Sum(1, 2);
            }

            public static void LoadCode_WithDuckTypedInterface(HostApp host)
            {
                // 1 - LoadCode compiles code and returns instance of a first class in the compiled assembly 
                // 2- The script class doesn't implement host app interface but it can still be aligned to 
                // one as long at it implements the  interface members
                // 3 - In this sample host object is passed into script routine.

                //This use-case uses Interface Alignment and this requires all assemblies involved to have 
                //non-empty Assembly.Location 
                CSScript.GlobalSettings.InMemoryAssembly = false;

                ICalc calc = CSScript.LoadCode(@"using CSScriptNativeApi;
                                                 public class Script  
                                                 { 
                                                     public int Sum(int a, int b)
                                                     {
                                                         if(Host != null) 
                                                             Host.Log(""Sum is invoked"");
                                                         return a + b;
                                                     }
                                                 
                                                     public HostApp Host { get; set; }
                                                 }")
                                     .CreateObject("*")
                                     .AlignToInterface<ICalc>();
                calc.Host = host;
                int result = calc.Sum(1, 2);
            }

            public static void ExecuteAndUnload()
            {
                // The script will be loaded into a temporary AppDomain and unloaded after the execution.

                // Note: remote execution is a subject of some restrictions associated with the nature of the 
                // CLR cross-AppDomain interaction model: 
                // * the script class must be serializable or derived from MarshalByRefObject.
                //
                // * any object (call arguments, return objects) that crosses ApPDomain boundaries
                //   must be serializable or derived from MarshalByRefObject.
                //
                // * long living script class instances may get disposed in remote domain even if they are 
                //   being referenced in the current AppDomain. You need to use the usual .NET techniques
                //   to prevent that. See LifetimeManagement.cs sample for details.  

                //This use-case uses Interface Alignment and this requires all assemblies involved to have 
                //non-empty Assembly.Location 
                CSScript.GlobalSettings.InMemoryAssembly = false;

                var code = @"using System;
                             public class Script : MarshalByRefObject
                             {
                                 public void Hello(string greeting)
                                 {
                                     Console.WriteLine(greeting);
                                 }
                             }";

                //Note: usage of helper.CreateAndAlignToInterface<IScript>("Script") is also acceptable
                using (var helper = new AsmHelper(CSScript.CompileCode(code), null, deleteOnExit: true))
                {
                    IScript script = helper.CreateAndAlignToInterface<IScript>("*");
                    script.Hello("Hi there...");
                }
                //from this point AsmHelper is disposed and the temp AppDomain is unloaded
            }

            public static void DebugTest()
            {
                //pops up an assertion dialog 
                dynamic script = CSScript.LoadCode(@"using System;
                                                     using System.Diagnostics;
                                                     public class Script
                                                     {
                                                         public int Sum(int a, int b)
                                                         {
                                                             Debug.Assert(false,""Testing CS-Script debugging..."");
                                                             return a+b;
                                                         }
                                                     }", null, debugBuild: true).CreateObject("*");

                int result = script.Sum(1, 2);
            }
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

    public interface IScript
    {
        void Hello(string greeting);
    }

    public interface ICalc
    {
        HostApp Host { get; set; }

        int Sum(int a, int b);
    }

    public class Samples
    {
        static public void CompilingHistory()
        {
            string script = Path.GetTempFileName();
            string scriptAsm = script + ".dll";
            CSScript.KeepCompilingHistory = true;

            try
            {
                File.WriteAllText(script, @"using System;
                                            using System.Windows.Forms;
                                            public class Script
                                            {
                                                public int Sum(int a, int b)
                                                {
                                                    return a+b;
                                                }
                                            }");



                CSScript.CompileFile(script, scriptAsm, false, null);

                CompilingInfo info = CSScript.CompilingHistory
                                             .Values
                                             .FirstOrDefault(item => item.ScriptFile == script);
                if (info != null)
                {
                    Console.WriteLine("Script: " + info.ScriptFile);

                    Console.WriteLine("Referenced assemblies:");
                    foreach (string asm in info.Input.ReferencedAssemblies)
                        Console.WriteLine(asm);

                    if (info.Result.Errors.HasErrors)
                    {
                        foreach (CompilerError err in info.Result.Errors)
                            if (!err.IsWarning)
                                Console.WriteLine("Error: " + err.ErrorText);
                    }
                }

                CSScript.CompilingHistory.Clear();

            }
            finally
            {
                CSScript.KeepCompilingHistory = false;
            }
        }
    }
}