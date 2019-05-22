Option Infer On
Option Strict On

Imports System
Imports System.Diagnostics
Imports CSScriptLibrary
Imports CSScriptClass = CSScriptLibrary.CSScript


' Read in more details about all aspects of CS-Script hosting in applications 
' here: http://www.csscript.net/help/Script_hosting_guideline_.html
'
' This file contains samples for the script hosting scenarios relying on CS-Script Evaluator interface (API).
' This API is a unified generic interface allowing dynamic switch of the underlying compiling services (Mono, Roslyn, CodeDom)
' without the need for changing the hosting code. 
'
' Apart from Evaluator (compiler agnostic) API CS-Script offers alternative hosting model: CS-Script Native, 
' which relies solely on CodeDom compiler. CS-Script Native offers some features that are not available with CS-Script Evaluator 
' (e.g. script unloading). 
' 
' The choice of the underlying compiling engine (e.g. Mono vs CodeDom) when using CS-Script Evaluator is always dictated by the 
' specifics of the hosting scenario. Thanks to in-process compiler hosting, Mono and Roslyn demonstrate much better compiling 
' performance comparing to CodeDom engine. However they don't allow  script debugging and caching easily supported with CodeDom. 
' Mono and particularly Roslyn also leas create more memory pressure due to the higher volume of the temp assemblies loaded into 
' the hosting AppDomain. Roslyn (at least CSharp.Scripting-v1.2.0.0) also has very high initial loading overhead up to 4 seconds.
'
' One of the possible approaches would be to use EvaluatorEngine.CodeDom during the active development and later on switch to Mono/Roslyn.

Namespace Global.CSScriptEvaluatorApi

    Public Class HostApp

        Public Shared Sub Test()
            ' Just in case clear AlternativeCompiler so it is not set to Roslyn or anything else by 
            ' the CS-Script installed (if any) on the host OS
            CSScriptClass.GlobalSettings.UseAlternativeCompiler = Nothing

            Dim samples = New EvaluatorSamples()

            Console.WriteLine("Testing compiling services")
            Console.WriteLine("---------------------------------------------")

            CSScriptClass.EvaluatorConfig.Engine = EvaluatorEngine.Mono
            Console.WriteLine(CSScriptClass.Evaluator.[GetType]().Name + "...")
            samples.RunAll()

            Console.WriteLine("---------------------------------------------")

            CSScriptClass.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn
            Console.WriteLine(CSScriptClass.Evaluator.[GetType]().Name + "...")
            samples.RunAll()

            Console.WriteLine("---------------------------------------------")

            CSScriptClass.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom
            Console.WriteLine(CSScriptClass.Evaluator.[GetType]().Name + "...")

            samples.RunAll()

            'samples.DebugTest(); //uncomment if want to fire an assertion during the script execution

            'Profile(); //uncomment if want to test performance of the engines
        End Sub

        Private Class EvaluatorSamples

            Public Sub RunAll()
                Dim run As Action(Of Action, String) = Sub(action, name)
                                                           action.Invoke()
                                                           Console.WriteLine(name & " - OK")
                                                       End Sub

                run(AddressOf CompileMethod_Instance, NameOf(CompileMethod_Instance))
                run(AddressOf CompileMethod_Static, NameOf(CompileMethod_Static))
                run(AddressOf CreateDelegate, NameOf(CreateDelegate))
                run(AddressOf LoadDelegate, NameOf(LoadDelegate))
                run(AddressOf LoadCode, NameOf(LoadCode))
                run(AddressOf LoadMethod, NameOf(LoadMethod))
                run(AddressOf LoadMethodWithInterface, NameOf(LoadMethodWithInterface))
                run(AddressOf LoadCode_WithInterface, NameOf(LoadCode_WithInterface))
                run(AddressOf LoadCode_WithDuckTypedInterface, NameOf(LoadCode_WithDuckTypedInterface))
            End Sub

            Public Sub CompileMethod_Static()
                ' 1 - CompileMethod wraps method into a class definition and returns compiled assembly
                ' 2 - GetStaticMethod returns duck-typed delegate that accepts 'params object[]' arguments
                ' Note: GetStaticMethodWithArgs can be replaced with a more convenient/shorter version
                ' that takes the object instead of the Type and then queries objects type internally:
                '  "GetStaticMethod("*.Test", data)"

                Dim myCode As String = "using CSScriptEvaluatorApi;

                                        static void Test(InputData data) {
                                            data.Index = GetIndex();
                                        }

                                        static int GetIndex() {
                                            return Environment.TickCount;
                                        }"
                Dim test = CSScriptClass.Evaluator.CompileMethod(myCode).GetStaticMethodWithArgs("*.Test", GetType(InputData))
                Dim data = New InputData()
                test(data)
            End Sub

            Public Sub CreateDelegate()
                ' Wraps method into a class definition, compiles it and loads the compiled assembly.
                ' It returns duck-typed delegate. A delegate with 'params object[]' arguments and
                ' without any specific return type.

                Dim myCode As String = "int Sqr(int a) {
                                            return a * a;
                                        }"
                Dim sqr = CSScriptClass.Evaluator.CreateDelegate(myCode)
                Dim r = sqr(3)
            End Sub

            Public Sub LoadDelegate()
                ' Wraps method into a class definition, loads the compiled assembly
                ' and returns the method delegate for the method, which matches the delegate specified
                ' as the type parameter of LoadDelegate

                Dim myCode As String = "int Product(int a, int b) {
                                            return a * b;
                                        }"
                Dim product = CSScriptClass.Evaluator.LoadDelegate(Of Func(Of Integer, Integer, Integer))(myCode)
                Dim result As Integer = product(3, 2)
            End Sub

            Public Sub LoadMethodWithInterface()
                ' LoadMethod compiles code and returns instance of a first class
                ' in the compiled assembly.
                ' LoadMethod is essentially the same as LoadCode. It just deals not with the 
                ' whole class definition but a single method(s) only. And the rest of the class definition is 
                ' added automatically by CS-Script. The auto-generated class declaration also indicates 
                ' that the class implements ICalc interface. Meaning that it will trigger compile error
                ' if the set of methods in the script code doesn't implement all interface members.

                'This use-case uses Interface Alignment and this requires all assemblies involved to have 
                'non-empty Assembly.Location 
                CSScriptClass.GlobalSettings.InMemoryAssembly = False

                Dim myCode As String = "int Sum(int a, int b) {
                                            return a + b;
                                        }"
                Dim script As ICalc = CSScriptClass.Evaluator.LoadMethod(Of ICalc)(myCode)
                Dim result As Integer = script.Sum(1, 2)
            End Sub

            Public Sub LoadCode_WithInterface()
                ' 1 - LoadCode compiles code and returns instance of a first class in the compiled assembly
                ' 2 - The script class implements host app interface so the returned object can be type casted into it

                Dim myCode As String = "using System;
                                        
                                        public class Script : CSScriptEvaluatorApi.ICalc {
 
                                            public int Sum(int a, int b) {
                                                return a + b;
                                            }

                                        }"
                Dim script = DirectCast(CSScriptClass.Evaluator.LoadCode(myCode), ICalc)
                Dim result As Integer = script.Sum(1, 2)
            End Sub

            Public Sub LoadCode_WithDuckTypedInterface()
                ' 1 - LoadCode compiles code and returns instance of a first class in the compiled assembly
                ' 2- The script class doesn't implement host app interface but it can still be aligned to
                ' one as long at it implements the  interface members

                'This use-case uses Interface Alignment and this requires all assemblies involved to have 
                'non-empty Assembly.Location 
                CSScriptClass.GlobalSettings.InMemoryAssembly = False

                Dim myCode As String = "using System;

                                        public class Script {

                                            public int Sum(int a, int b) {
                                                return a + b;
                                            }

                                        }"
                Dim script As ICalc = CSScriptClass.MonoEvaluator.LoadCode(Of ICalc)(myCode)
                Dim result As Integer = script.Sum(1, 2)
            End Sub

        End Class

        Public Shared Sub Profile()
            Dim sw = New Stopwatch()
            Dim samples = New EvaluatorSamples()
            Dim count = 20
            Dim inxed = 0
            Dim preventCaching As Boolean = False

            Dim run As Action = Sub()
                                    sw.Restart()
                                    For i As Integer = 0 To count - 1
                                        If preventCaching Then
                                            samples.PerformanceTest(System.Math.Max(System.Threading.Interlocked.Increment(inxed), inxed - 1))
                                        Else
                                            samples.PerformanceTest()
                                        End If
                                    Next

                                    Console.WriteLine(CSScriptClass.Evaluator.[GetType]().Name + ": " & Convert.ToString(sw.ElapsedMilliseconds))

                                End Sub

            Dim runAll As Action = Sub()
                                       Console.WriteLine()
                                       Console.WriteLine("---------------------------------------------")
                                       Console.WriteLine($"Caching enabled: { Not preventCaching}")
                                       Console.WriteLine()

                                       CSScriptClass.EvaluatorConfig.Engine = EvaluatorEngine.Mono
                                       run()

                                       CSScriptClass.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom
                                       run()

                                       CSScriptClass.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn
                                       run()

                                   End Sub

            RoslynEvaluator.LoadCompilers()
            'Roslyn is extremely heavy so exclude startup time from profiling
            Console.WriteLine("Testing performance")

            preventCaching = True
            runAll()

            preventCaching = False
            runAll()
        End Sub

    End Class

    Public Interface ICalc

        Function Sum(a As Integer, b As Integer) As Integer

    End Interface

    Public Class InputData

        Public Index As Integer = 0

    End Class

End Namespace
