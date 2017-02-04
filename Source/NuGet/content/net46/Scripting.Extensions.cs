using CSScriptLibrary;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Threading.Tasks;

// Read in more details about all aspects of CS-Script hosting in applications 
// here: http://www.csscript.net/help/Script_hosting_guideline_.html
//
// This file contains samples for the script hosting scenarios requiring asynchronous script execution as well as unloading the 
// scripts being executed.
//  AsyncSamples
//    Samples demonstrate the use of Async and Await mechanism available in C# 5 and higher. Note that the async method extensions 
//    cover the complete set of CSScript.Evaluator methods. 
//
//  UnloadingSamples 
//    Samples demonstrate the use of temporary AppDoamain for loading and executing dynamic C# code (script). It is the 
//    only mechanism available for unloading dynamically loaded assemblies. This is a well known CLR design limitation that leads to 
//    memory leaks if the assembly/script loaded in the caller AppDomain. The problem affects all C# script engines (e.g. Roslyn, CodeDom)
//    and it cannot be solved by the engine itself thus CS-Script provides a work around in form of the MethodExtensions for the 
//    CSScript.Evaluator methods that are compatible with the unloading mechanism.
//
//    Nevertheless you should try to avoid using remote AppDoamain unless you have to. It is very heavy and also imposes the serialization
//    constrains.  
//
// All samples rely on the compiler agnostic CSScript.Evaluator API. 
namespace CSScriptEvaluatorExtensions
{
    public class HostApp
    {
        public static void Test()
        {
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("Testing asynchronous API");
            Console.WriteLine("---------------------------------------------");
            new AsyncSamples().RunAll();
            Thread.Sleep(2000);
            Console.WriteLine("\nPress 'Enter' to run uloading samples...");
            Console.ReadLine();
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("Testing unloading API");
            Console.WriteLine("---------------------------------------------");
            new UnloadingSamples().RunAll();
        }

        class AsyncSamples
        {
            public void RunAll()
            {
                Action<Action, string> run = (action, name) => { action(); Console.WriteLine(name); };

                run(LoadDelegateAsync, "Start of " + nameof(LoadDelegateAsync));
                run(LoadMethodAsync, "Start of " + nameof(LoadMethodAsync));
                run(LoadCodeAsync, "Start of " + nameof(LoadCodeAsync));
                run(CreateDelegateAsync, "Start of " + nameof(CreateDelegateAsync));
                run(CompileCodeAsync, "Start of " + nameof(CompileCodeAsync));
                run(RemoteAsynch, "Start of " + nameof(RemoteAsynch));
            }

            async void LoadDelegateAsync()
            {
                var product = await CSScript.Evaluator
                                            .LoadDelegateAsync<Func<int, int, int>>(
                                                  @"int Product(int a, int b)
                                                    {
                                                        return a * b;
                                                    }");

                Console.WriteLine("   End of {0}: {1}", nameof(LoadDelegateAsync), product(4, 2));
            }

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

                Console.WriteLine("   End of {0}: {1}", nameof(LoadMethodAsync), script.Div(15, 3));
            }

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

                Console.WriteLine("   End of {0}: {1}", nameof(LoadCodeAsync), calc.Sum(1, 2));
            }

            public async void CreateDelegateAsync()
            {
                var product = await CSScript.Evaluator
                                            .CreateDelegateAsync<int>(
                                                  @"int Product(int a, int b)
                                                    {
                                                        return a * b;
                                                    }");

                Console.WriteLine("   End of {0}: {1}", nameof(CreateDelegateAsync), product(15, 3));
            }

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

                Console.WriteLine("   End of {0}: {1}", nameof(CompileCodeAsync), calc.Sum(15, 3));
            }

            public async void RemoteAsynch()
            {
                var sum = await Task.Run(() =>
                                     CSScript.Evaluator
                                             .CreateDelegateRemotely<int>(
                                                                   @"int Sum(int a, int b)
                                                                     {
                                                                         return a+b;
                                                                     }")
                                                                          );
                Console.WriteLine("   End of {0}: {1}", nameof(RemoteAsynch), sum(1, 2));

                sum.UnloadOwnerDomain();
            }
        }

        class UnloadingSamples
        {
            public void RunAll()
            {
                CreateDelegateRemotely();
                LoadMethodRemotely();
                LoadCodeRemotely();
                LoadCodeRemotelyWithInterface();
            }

            public void CreateDelegateRemotely()
            {
var sum = CSScript.Evaluator
                    .CreateDelegateRemotely<int>(@"int Sum(int a, int b)
                                                    {
                                                        return a+b;
                                                    }");

Console.WriteLine("{0}: {1}", nameof(CreateDelegateRemotely), sum(15, 3));

sum.UnloadOwnerDomain();
            }

            public void LoadCodeRemotely()
            {
                // Class Calc doesn't implement ICals interface. Thus the compiled object cannot be typecasted into 
                // the interface and Evaluator will emit duck-typed assembly instead. 
                // But Mono and Roslyn build file-less assemblies, meaning that they cannot be used to build 
                // duck-typed proxies and CodeDomEvaluator needs to be used explicitly.
                // Note class Calc also inherits from MarshalByRefObject. This is required for all object that 
                // are passed between AppDomain: they must inherit from MarshalByRefObject or be serializable.
                var script = CSScript.CodeDomEvaluator
                                     .LoadCodeRemotely<ICalc>(
                                                      @"using System;
                                                        public class Calc : MarshalByRefObject
                                                        { 
                                                            object t;
                                                            public int Sum(int a, int b)
                                                            {
                                                                t = new Test();
                                                                return a+b;
                                                            }
                                                        }    
                                                        
                                                        class Test
                                                        {
                                                            ~Test()
                                                            {
                                                                Console.WriteLine(""Domain is unloaded: ~Test()"");
                                                            }
                                                        }                                         
                                                        ");

                Console.WriteLine("{0}: {1}", nameof(LoadCodeRemotely), script.Sum(15, 3));

                script.UnloadOwnerDomain();
            }

            public void LoadCodeRemotelyWithInterface()
            {
                // Note class Calc also inherits from MarshalByRefObject. This is required for all object that 
                // are passed between AppDomain: they must inherit from MarshalByRefObject or be serializable.
                var script = CSScript.Evaluator
                                     .LoadCodeRemotely<ICalc>(
                                                      @"using System;
                                                        public class Calc : MarshalByRefObject, CSScriptEvaluatorExtensions.ICalc
                                                        { 
                                                            public int Sum(int a, int b)
                                                            {
                                                                return a+b;
                                                            }
                                                        }    
                                                        ");

                Console.WriteLine("{0}: {1}", nameof(LoadCodeRemotelyWithInterface), script.Sum(15, 3));

                script.UnloadOwnerDomain();
            }

            public void LoadMethodRemotely()
            {
                // LoadMethodRemotely is essentially the same as LoadCodeRemotely. It just deals not with the 
                // whole class definition but a single method(s) only. And the rest of the class definition is 
                // added automatically by CS-Script. The auto-generated class declaration also indicates 
                // that the class implements ICalc interface. Meaning that it will trigger compile error
                // if the set of methods in the script code doesn't implement all interface members.
var script = CSScript.Evaluator
                        .LoadMethodRemotely<IFullCalc>(
                                        @"public int Sum(int a, int b)
                                            {
                                                return a+b;
                                            }
                                            public int Sub(int a, int b)
                                            {
                                                return a-b;
                                            }");

Console.WriteLine("{0}: {1}", nameof(LoadMethodRemotely), script.Sum(15, 3));

script.UnloadOwnerDomain();
            }

            MethodDelegate sum;
            ClientSponsor sumSponsor;

            public void KeepRemoteObjectAlive()
            {
                sum = CSScript.Evaluator
                              .CreateDelegateRemotely(@"int Sum(int a, int b)
                                                        {
                                                            return a+b;
                                                        }");

                //Normally remote objects are disposed if they are not accessed withing a default timeout period.
                //It is not even enough to keep transparent proxies or their wrappers (e.g. 'sum') referenced. 
                //To prevent GC collection in the remote domain use .NET ClientSponsor mechanism as below.
                sumSponsor = sum.ExtendLifeFromMinutes(30);
            }
        }
    }

    public interface ICalc
    {
        int Sum(int a, int b);
    }

    public interface IFullCalc
    {
        int Sum(int a, int b);
        int Sub(int a, int b);
    }
}