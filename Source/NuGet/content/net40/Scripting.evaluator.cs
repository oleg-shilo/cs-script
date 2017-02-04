using CSScriptLibrary;
using System;
using System.Diagnostics;

// Read in more details about all aspects of CS-Script hosting in applications 
// here: http://www.csscript.net/help/Script_hosting_guideline_.html
//
// This file contains samples for the script hosting scenarios relying on CS-Script Evaluator interface (API).
// This API is a unified generic interface allowing dynamic switch of the underlying compiling services (Mono, CodeDom)
// without the need for changing the hosting code. 
//
// Apart from Evaluator (compiler agnostic) API CS-Script offers alternative hosting model: CS-Script Native, 
// which relies solely on CodeDom compiler. CS-Script Native offers some features that are not available with CS-Script Evaluator 
// (e.g. script unloading). 
// 
// The choice of the underlying compiling engine (e.g. Mono vs CodeDom) when using CS-Script Evaluator is always dictated by the 
// specifics of the hosting scenario. Thanks to in-process compiler hosting, Mono demonstrates much better compiling 
// performance comparing to CodeDom engine. However they don't allow  script debugging and caching easily supported with CodeDom. 
// Mono also creates more memory pressure due to the higher volume of the temp assemblies loaded into 
// the hosting AppDomain. 
//
// One of the possible approaches would be to use EvaluatorEngine.CodeDom during the active development and later on switch to Mono.

namespace CSScriptEvaluatorApi
{
    public class HostApp
    {
        public static void Test()
        {
            var samples = new EvaluatorSamples();

            Console.WriteLine("Testing compiling services");
            Console.WriteLine("---------------------------------------------");

            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
            Console.WriteLine(CSScript.Evaluator.GetType().Name + "...");
            samples.RunAll();

            Console.WriteLine("---------------------------------------------");

            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;
            Console.WriteLine(CSScript.Evaluator.GetType().Name + "...");

            samples.RunAll();

            //samples.DebugTest(); //uncomment if want to fire an assertion during the script execution

            //Profile(); //uncomment if want to test performance of the engines
        }

        class EvaluatorSamples
        {
            public void RunAll()
            {
                Action<Action, string> run = (action, name) => { action(); Console.WriteLine(name + " - OK"); };

                run(CompileMethod_Instance, nameof(CompileMethod_Instance));
                run(CompileMethod_Static, nameof(CompileMethod_Static));
                run(CreateDelegate, nameof(CreateDelegate));
                run(LoadDelegate, nameof(LoadDelegate));
                run(LoadCode, nameof(LoadCode));
                run(LoadCode_WithInterface, nameof(LoadCode_WithInterface));
                run(LoadCode_WithDuckTypedInterface, nameof(LoadCode_WithDuckTypedInterface));
            }

            public void CompileMethod_Instance()
            {
                // 1- CompileMethod wraps method into a class definition and returns compiled assembly
                // 2 - CreateObject creates instance of a first class in the assembly

                dynamic script = CSScript.Evaluator
                                         .CompileMethod(@"int Sqr(int data)
                                                          {
                                                              return data * data;
                                                          }")
                                         .CreateObject("*");

                var result = script.Sqr(7);
            }

            public void CompileMethod_Static()
            {
                // 1 - CompileMethod wraps method into a class definition and returns compiled assembly
                // 2 - GetStaticMethod returns duck-typed delegate that accepts 'params object[]' arguments
                // Note: GetStaticMethodWithArgs can be replaced with a more convenient/shorter version
                // that takes the object instead of the Type and then queries objects type internally:
                //  "GetStaticMethod("*.Test", data)"

                var test = CSScript.Evaluator
                                   .CompileMethod(@"using CSScriptEvaluatorApi;
                                                    static void Test(InputData data)
                                                    {
                                                        data.Index = GetIndex();
                                                    }
                                                    static int GetIndex()
                                                    {
                                                        return Environment.TickCount;
                                                    }")
                                   .GetStaticMethodWithArgs("*.Test", typeof(InputData));

                var data = new InputData();
                test(data);
            }

            public void CreateDelegate()
            {
                // Wraps method into a class definition, compiles it and loads the compiled assembly.
                // It returns duck-typed delegate. A delegate with 'params object[]' arguments and
                // without any specific return type.

                var sqr = CSScript.Evaluator
                                  .CreateDelegate(@"int Sqr(int a)
                                                    {
                                                        return a * a;
                                                    }");

                var r = sqr(3);
            }

            public void LoadDelegate()
            {
                // Wraps method into a class definition, loads the compiled assembly
                // and returns the method delegate for the method, which matches the delegate specified
                // as the type parameter of LoadDelegate

                var product = CSScript.Evaluator
                                      .LoadDelegate<Func<int, int, int>>(
                                                  @"int Product(int a, int b)
                                                    {
                                                        return a * b;
                                                    }");

                int result = product(3, 2);
            }

            public void LoadCode()
            {
                // LoadCode compiles code and returns instance of a first class
                // in the compiled assembly

                dynamic script = CSScript.Evaluator
                                         .LoadCode(@"using System;
                                                     public class Script
                                                     {
                                                         public int Sum(int a, int b)
                                                         {
                                                             return a+b;
                                                         }
                                                     }");

                int result = script.Sum(1, 2);
            }

            public void LoadCode_WithInterface()
            {
                // 1 - LoadCode compiles code and returns instance of a first class in the compiled assembly
                // 2 - The script class implements host app interface so the returned object can be type casted into it

                var script = (ICalc)CSScript.Evaluator
                                            .LoadCode(@"using System;
                                                        public class Script : CSScriptEvaluatorApi.ICalc
                                                        {
                                                            public int Sum(int a, int b)
                                                            {
                                                                return a+b;
                                                            }
                                                        }");

                int result = script.Sum(1, 2);
            }

            public void LoadCode_WithDuckTypedInterface()
            {
                // 1 - LoadCode compiles code and returns instance of a first class in the compiled assembly
                // 2- The script class doesn't implement host app interface but it can still be aligned to
                // one as long at it implements the  interface members

                //This use-case uses Interface Alignment and this requires all assemblies involved to have 
                //non-empty Assembly.Location 
                CSScript.GlobalSettings.InMemoryAssembly = false;

                ICalc script = CSScript.MonoEvaluator
                                       .LoadCode<ICalc>(@"using System;
                                                          public class Script
                                                          {
                                                              public int Sum(int a, int b)
                                                              {
                                                                  return a+b;
                                                              }
                                                          }");

                int result = script.Sum(1, 2);
            }

            public void PerformanceTest(int count = -1)
            {
                var code = @"int Sqr(int a)
                            {
                                return a * a;
                            }";

                if (count != -1)
                    code += "//" + count; //this unique extra code comment ensures the code to be compiled cannot be cached

                dynamic script = CSScript.Evaluator
                                         .CompileMethod(code)
                                         .CreateObject("*");

                var r = script.Sqr(3);
            }

            public void DebugTest()
            {
                //pops up an assertion dialog 

                CSScript.EvaluatorConfig.DebugBuild = true;
                CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;

                dynamic script = CSScript.Evaluator
                                         .LoadCode(@"using System;
                                                     using System.Diagnostics;
                                                     public class Script
                                                     {
                                                         public int Sum(int a, int b)
                                                         {
                                                             Debug.Assert(false,""Testing CS-Script debugging..."");
                                                             return a+b;
                                                         }
                                                     }");

                var r = script.Sum(3, 4);
            }
        }

        public static void Profile()
        {
            var sw = new Stopwatch();
            var samples = new EvaluatorSamples();
            var count = 20;
            var inxed = 0;
            bool preventCaching = false;

            Action run = () =>
            {
                sw.Restart();
                for (int i = 0; i < count; i++)
                    if (preventCaching)
                        samples.PerformanceTest(inxed++);
                    else
                        samples.PerformanceTest();

                Console.WriteLine(CSScript.Evaluator.GetType().Name + ": " + sw.ElapsedMilliseconds);
            };

            Action runAll = () =>
            {
                Console.WriteLine("\n---------------------------------------------");
                Console.WriteLine($"Caching enabled: {!preventCaching}\n");

                CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
                run();

                CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;
                run();
            };

            Console.WriteLine("Testing performance");

            preventCaching = true;
            runAll();

            preventCaching = false;
            runAll();
        }
    }

    public interface ICalc
    {
        int Sum(int a, int b);
    }

    public class InputData
    {
        public int Index = 0;
    }
}