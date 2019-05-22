Option Infer On
Option Strict On

Imports System
Imports System.Runtime.Remoting.Lifetime
Imports System.Threading
Imports System.Threading.Tasks
Imports CSScriptLibrary
Imports CSScriptClass = CSScriptLibrary.CSScript

' Read in more details about all aspects of CS-Script hosting in applications 
' here: http://www.csscript.net/help/Script_hosting_guideline_.html
'
' This file contains samples for the script hosting scenarios requiring asynchronous script execution as well as unloading the 
' scripts being executed.
'  AsyncSamples
'    Samples demonstrate the use of Async and Await mechanism available in C# 5 and higher. Note that the async method extensions 
'    cover the complete set of CSScript.Evaluator methods. 
'
'  UnloadingSamples 
'    Samples demonstrate the use of temporary AppDoamain for loading and executing dynamic C# code (script). It is the 
'    only mechanism available for unloading dynamically loaded assemblies. This is a well known CLR design limitation that leads to 
'    memory leaks if the assembly/script loaded in the caller AppDomain. The problem affects all C# script engines (e.g. Roslyn, CodeDom)
'    and it cannot be solved by the engine itself thus CS-Script provides a work around in form of the MethodExtensions for the 
'    CSScript.Evaluator methods that are compatible with the unloading mechanism.
'
'    Nevertheless you should try to avoid using remote AppDoamain unless you have to. It is very heavy and also imposes the serialization
'    constrains.  
'
' All samples rely on the compiler agnostic CSScript.Evaluator API. 
Namespace Global.CSScriptEvaluatorExtensions

    Public Class HostApp

        Public Shared Sub Test()
            Console.WriteLine("---------------------------------------------")
            Console.WriteLine("Testing asynchronous API")
            Console.WriteLine("---------------------------------------------")
            Dim myAsyncSamples As New AsyncSamples()
            myAsyncSamples.RunAll()
            Thread.Sleep(2000)
            Console.WriteLine()
            Console.WriteLine("Press 'Enter' to run uloading samples...")
            Console.ReadLine()
            Console.WriteLine("---------------------------------------------")
            Console.WriteLine("Testing unloading API")
            Console.WriteLine("---------------------------------------------")
            Dim myUnloadingSamples As New UnloadingSamples()
            myUnloadingSamples.RunAll()
        End Sub

        Private Class AsyncSamples
            Public Sub RunAll()
                Dim run As Action(Of Action, String) = Sub(action, name)
                                                           action()
                                                           Console.WriteLine(name)

                                                       End Sub

                run(AddressOf LoadDelegateAsync, "Start of " & NameOf(LoadDelegateAsync))
                run(AddressOf LoadMethodAsync, "Start of " & NameOf(LoadMethodAsync))
                run(AddressOf LoadCodeAsync, "Start of " & NameOf(LoadCodeAsync))
                run(AddressOf CreateDelegateAsync, "Start of " & NameOf(CreateDelegateAsync))
                run(AddressOf CompileCodeAsync, "Start of " & NameOf(CompileCodeAsync))
                run(AddressOf RemoteAsync, "Start of " & NameOf(RemoteAsync))
            End Sub

            Private Async Sub LoadDelegateAsync()
                Dim myCode As String = "int Product(int a, int b) {
                                            return a * b;
                                        }"
                Dim product = Await CSScriptClass.Evaluator.LoadDelegateAsync(Of Func(Of Integer, Integer, Integer))(myCode)
                Console.WriteLine("   End of {0}: {1}", NameOf(LoadDelegateAsync), product(4, 2))
            End Sub

            Public Async Sub LoadCodeAsync()
                'This use-case uses Interface Alignment and this requires all assemblies involved to have 
                'non-empty Assembly.Location 
                CSScriptClass.GlobalSettings.InMemoryAssembly = False

                Dim myCode As String = "using System;

                                        public class Script {

                                            public int Sum(int a, int b) {
                                                return a + b;
                                            }

                                        }"
                Dim calc As ICalc = Await CSScriptClass.Evaluator.LoadCodeAsync(Of ICalc)(myCode)
                Console.WriteLine("   End of {0}: {1}", NameOf(LoadCodeAsync), calc.Sum(1, 2))
            End Sub

            Public Async Sub CreateDelegateAsync()
                Dim myCode As String = "int Product(int a, int b) {
                                            return a * b;
                                        }"
                Dim product = Await CSScriptClass.Evaluator.CreateDelegateAsync(Of Integer)(myCode)
                Console.WriteLine("   End of {0}: {1}", NameOf(CreateDelegateAsync), product(15, 3))
            End Sub

            Public Async Sub RemoteAsync()
                Dim myCode As String = "int Sum(int a, int b) {
                                            return a + b;
                                        }"
                Dim sum = Await Task.Run(Function() CSScriptClass.Evaluator.CreateDelegateRemotely(Of Integer)(myCode))
                Console.WriteLine("   End of {0}: {1}", NameOf(RemoteAsync), sum(1, 2))

                sum.UnloadOwnerDomain()
            End Sub
        End Class

        Private Class UnloadingSamples
            Public Sub RunAll()
                CreateDelegateRemotely()
                LoadMethodRemotely()
                LoadCodeRemotely()
                LoadCodeRemotelyWithInterface()
            End Sub

            Public Sub CreateDelegateRemotely()
                Dim myCode As String = "int Sum(int a, int b) {
                                            return a + b;
                                        }"
                Dim sum = CSScriptClass.Evaluator.CreateDelegateRemotely(Of Integer)(myCode)
                Console.WriteLine("{0}: {1}", NameOf(CreateDelegateRemotely), sum(15, 3))
                sum.UnloadOwnerDomain()
            End Sub

            Public Sub LoadCodeRemotely()
                ' Class Calc doesn't implement ICals interface. Thus the compiled object cannot be typecasted into 
                ' the interface and Evaluator will emit duck-typed assembly instead. 
                ' But Mono and Roslyn build file-less assemblies, meaning that they cannot be used to build 
                ' duck-typed proxies and CodeDomEvaluator needs to be used explicitly.
                ' Note class Calc also inherits from MarshalByRefObject. This is required for all object that 
                ' are passed between AppDomain: they must inherit from MarshalByRefObject or be serializable.

                'This use-case uses Interface Alignment and this requires all assemblies involved to have 
                'non-empty Assembly.Location 
                CSScriptClass.GlobalSettings.InMemoryAssembly = False

                Dim myCode As String = "using System;

                                        public class Calc : MarshalByRefObject { 

                                            private object t;

                                            public int Sum(int a, int b) {
                                                t = new Test();
                                                return a + b;
                                            }

                                        }    
                                                        
                                        class Test {
                                                            
                                            ~Test() {
                                                Console.WriteLine(""Domain is unloaded: ~Test()"");
                                            }

                                        }"
                Dim script = CSScriptClass.CodeDomEvaluator.LoadCodeRemotely(Of ICalc)(myCode)
                Console.WriteLine("{0}: {1}", NameOf(LoadCodeRemotely), script.Sum(15, 3))
                script.UnloadOwnerDomain()
            End Sub

            Public Sub LoadCodeRemotelyWithInterface()
                ' Note class Calc also inherits from MarshalByRefObject. This is required for all object that 
                ' are passed between AppDomain: they must inherit from MarshalByRefObject or be serializable.
                CSScriptClass.GlobalSettings.InMemoryAssembly = False
                Dim myCode As String = "using System;

                                        public class Calc : MarshalByRefObject, CSScriptEvaluatorExtensions.ICalc { 

                                            public int Sum(int a, int b) {
                                                return a + b;
                                            }

                                        }"
                Dim script = CSScriptClass.Evaluator.LoadCodeRemotely(Of ICalc)(myCode)

                Console.WriteLine("{0}: {1}", NameOf(LoadCodeRemotelyWithInterface), script.Sum(15, 3))

                script.UnloadOwnerDomain()
            End Sub

            Public Sub LoadMethodRemotely()
                ' LoadMethodRemotely is essentially the same as LoadCodeRemotely. It just deals not with the 
                ' whole class definition but a single method(s) only. And the rest of the class definition is 
                ' added automatically by CS-Script. The auto-generated class declaration also indicates 
                ' that the class implements ICalc interface. Meaning that it will trigger compile error
                ' if the set of methods in the script code doesn't implement all interface members.

                'This use-case uses Interface Alignment and this requires all assemblies involved to have 
                'non-empty Assembly.Location 
                CSScriptClass.GlobalSettings.InMemoryAssembly = False

                Dim myCode As String = "public int Sum(int a, int b) {
                                            return a + b;
                                        }

                                        public int Sub(int a, int b) {
                                            return a - b;
                                        }"
                Dim script = CSScriptClass.Evaluator.LoadMethodRemotely(Of IFullCalc)(myCode)
                Console.WriteLine("{0}: {1}", NameOf(LoadMethodRemotely), script.Sum(15, 3))
                script.UnloadOwnerDomain()
            End Sub

            Private _Sum As MethodDelegate
            Private _SumSponsor As ClientSponsor

            Public Sub KeepRemoteObjectAlive()
                Dim myCode As String = "int Sum(int a, int b) {
                                            return a + b;
                                        }"
                _Sum = CSScriptClass.Evaluator.CreateDelegateRemotely(myCode)

                'Normally remote objects are disposed if they are not accessed withing a default timeout period.
                'It is not even enough to keep transparent proxies or their wrappers (e.g. 'sum') referenced. 
                'To prevent GC collection in the remote domain use .NET ClientSponsor mechanism as below.
                _SumSponsor = _Sum.ExtendLifeFromMinutes(30)
            End Sub
        End Class
    End Class

    Public Interface ICalc

        Function Sum(a As Integer, b As Integer) As Integer

    End Interface

    Public Interface IFullCalc

        Function Sum(a As Integer, b As Integer) As Integer
        Function [Sub](a As Integer, b As Integer) As Integer

    End Interface

End Namespace
