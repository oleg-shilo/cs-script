Option Infer On
Option Strict On

Imports System
Imports System.Linq
Imports System.IO
Imports System.CodeDom.Compiler
Imports csscript
Imports CSScriptLibrary
Imports CSScriptClass = CSScriptLibrary.CSScript

' Read in more details about all aspects of CS-Script hosting in applications 
' here: http://www.csscript.net/help/Script_hosting_guideline_.html
'   
' This file contains samples for the script hosting scenarios relying on CS-Script Native interface (API).
' This API is a compiler specific interface, which relies solely on CodeDom compiler. In most of the cases 
' CS-Script Native model is the most flexible and natural choice 
'
' Apart from Native API CS-Script offers alternative hosting model: CS-Script Evaluator, which provides
' a unified generic interface allowing dynamic switch the underlying compiling services (Mono, Roslyn, CodeDom) 
' without the need for changing the hosting code. 
'   
' The Native interface is the original API that was designed to take maximum advantage of the dynamic C# code 
' execution with CodeDom. The original implementation of this API was developed even before any compiler-as-service 
' solution became available. Being based solely on CodeDOM the API doesn't utilize neither Mono nor Roslyn 
' scripting solutions. Despite that CS-Script Native is the most mature, powerful and flexible API available with CS-Script.
' 
' Native interface allows some unique features that are not available with CS-Script Evaluator:
'  - Debugging scripts
'  - Script caching
'  - Script unloading

Namespace Global.CSScriptNativeApi

    Public Class HostApp

        Public Shared Sub Test()
            Dim host As New HostApp()
            host.Log("Testing compiling services CS-Script Native API")
            Console.WriteLine("---------------------------------------------")

            CodeDomSamples.LoadMethod_Instance()
            CodeDomSamples.LoadMethod_Static()
            CodeDomSamples.LoadDelegate()
            CodeDomSamples.CreateAction()
            CodeDomSamples.CreateFunc()
            CodeDomSamples.LoadCode()
            CodeDomSamples.LoadCode_WithInterface(host)
            CodeDomSamples.LoadCode_WithDuckTypedInterface(host)
            CodeDomSamples.ExecuteAndUnload()
            'CodeDomSamples.DebugTest(); //uncomment if want to fire an assertion during the script execution
        End Sub

        Public Class CodeDomSamples

            Public Shared Sub LoadMethod_Static()
                ' 1 - LoadMethod wraps method into a class definition, compiles it and returns loaded assembly
                ' 2 - GetStaticMethod returns first found static method as a duck-typed delegate that 
                '     accepts 'params object[]' arguments 
                '
                ' Note: you can use GetStaticMethodWithArgs for higher precision method search: GetStaticMethodWithArgs("*.SayHello", typeof(string)); 
                Dim myCode As String = "static void SayHello(string greeting) {
                                            Console.WriteLine(greeting);
                                        }"
                Dim sayHello = CSScriptClass.LoadMethod(myCode).GetStaticMethod()
                sayHello("Hello World!")
            End Sub

            Public Shared Sub LoadDelegate()
                ' LoadDelegate wraps method into a class definition, compiles it and loads the compiled assembly.
                ' It returns the method delegate for the method, which matches the delegate specified 
                ' as the type parameter of LoadDelegate

                ' The 'using System;' is optional; it demonstrates how to specify 'using' in the method-only syntax

                Dim myCode As String = "void SayHello(string greeting) {
                                            Console.WriteLine(greeting);
                                        }"
                Dim sayHello = CSScriptClass.LoadDelegate(Of Action(Of String))(myCode)
                sayHello("Hello World!")
            End Sub

            Public Shared Sub CreateAction()
                ' Wraps method into a class definition, compiles it and loads the compiled assembly.
                ' It returns duck-typed delegate. A delegate with 'params object[]' arguments and 
                ' without any specific return type. 

                Dim myCode As String = "void SayHello(string greeting) {
                                            Console.WriteLine(greeting);
                                        }"
                Dim sayHello = CSScriptClass.CreateAction(myCode)
                sayHello("Hello World!")
            End Sub

            Public Shared Sub CreateFunc()
                ' Wraps method into a class definition, compiles it and loads the compiled assembly.
                ' It returns duck-typed delegate. A delegate with 'params object[]' arguments and 
                ' int as a return type. 

                Dim myCode As String = "int Sqr(int a) {
                                            return a * a;
                                        }"
                Dim square = CSScriptClass.CreateFunc(Of Integer)(myCode)
                Dim myResult As Integer = square(3)
            End Sub


            Public Shared Sub LoadCode_WithInterface(host As HostApp)
                ' 1 - LoadCode compiles code and returns instance of a first class in the compiled assembly. 
                ' 2 - The script class implements host app interface so the returned object can be type casted into it.
                ' 3 - In this sample host object is passed into script routine.
                Dim myCode As String = "using CSScriptNativeApi;

                                            public class Script : ICalc { 

                                            public int Sum(int a, int b) {
                                                if (Host != null) Host.Log(""Sum is invoked"");
                                                return a + b;
                                            }
                                                      
                                            public HostApp Host { get; set; }

                                        }"
                Dim calc = DirectCast(CSScriptClass.LoadCode(myCode).CreateObject("*"), ICalc)
                calc.Host = host
                Dim result As Integer = calc.Sum(1, 2)
            End Sub

            Public Shared Sub LoadCode_WithDuckTypedInterface(host As HostApp)
                ' 1 - LoadCode compiles code and returns instance of a first class in the compiled assembly 
                ' 2- The script class doesn't implement host app interface but it can still be aligned to 
                ' one as long at it implements the  interface members
                ' 3 - In this sample host object is passed into script routine.

                'This use-case uses Interface Alignment and this requires all assemblies involved to have 
                'non-empty Assembly.Location 
                CSScriptClass.GlobalSettings.InMemoryAssembly = False
                Dim myCode As String = "using CSScriptNativeApi;

                                        public class Script { 

                                          public int Sum(int a, int b) {
                                            if (Host != null) Host.Log(""Sum is invoked"");
                                            return a + b;
                                          }
                                                 
                                          public HostApp Host { get; set; }
                                        
                                        }"


                Dim calc As ICalc = CSScriptLibraryExtensionMethods.AlignToInterface(Of ICalc)(CSScriptClass.LoadCode(myCode).CreateObject("*"))
                calc.Host = host
                Dim result As Integer = calc.Sum(1, 2)
            End Sub

            Public Shared Sub ExecuteAndUnload()
                ' The script will be loaded into a temporary AppDomain and unloaded after the execution.

                ' Note: remote execution is a subject of some restrictions associated with the nature of the 
                ' CLR cross-AppDomain interaction model: 
                ' * the script class must be serializable or derived from MarshalByRefObject.
                '
                ' * any object (call arguments, return objects) that crosses ApPDomain boundaries
                '   must be serializable or derived from MarshalByRefObject.
                '
                ' * long living script class instances may get disposed in remote domain even if they are 
                '   being referenced in the current AppDomain. You need to use the usual .NET techniques
                '   to prevent that. See LifetimeManagement.cs sample for details.  

                'This use-case uses Interface Alignment and this requires all assemblies involved to have 
                'non-empty Assembly.Location 
                CSScriptClass.GlobalSettings.InMemoryAssembly = False

                Dim myCode As String = "using System;

                                        public class Script : MarshalByRefObject {

                                            public void Hello(string greeting) {
                                                Console.WriteLine(greeting);
                                            }

                                        }"
                'Note: usage of helper.CreateAndAlignToInterface<IScript>("Script") is also acceptable
                Using helper = New AsmHelper(CSScriptClass.CompileCode(myCode), Nothing, deleteOnExit:=True)
                    Dim script As IScript = helper.CreateAndAlignToInterface(Of IScript)("*")
                    script.Hello("Hi there...")
                End Using
                'from this point AsmHelper is disposed and the temp AppDomain is unloaded
            End Sub

        End Class

        Public Sub Log(message As String)
            Console.WriteLine(message)
        End Sub
    End Class

    Public Interface IScript
        Sub Hello(greeting As String)
    End Interface

    Public Interface ICalc
        Property Host() As HostApp

        Function Sum(a As Integer, b As Integer) As Integer
    End Interface

    Public Class Samples

        Public Shared Sub CompilingHistory()
            Dim script As String = Path.GetTempFileName()
            Dim scriptAsm As String = script & ".dll"
            CSScriptClass.KeepCompilingHistory = True

            Dim myCode As String = "using System;
                                    using System.Windows.Forms;

                                    public class Script {

                                        public int Sum(int a, int b) {
                                            return a + b;
                                        }

                                    }"
            Try
                File.WriteAllText(script, myCode)
                CSScriptClass.CompileFile(script, scriptAsm, False, Nothing)

                Dim info As CompilingInfo = CSScriptClass.CompilingHistory.Values.FirstOrDefault(Function(item) item.ScriptFile = script)
                If info IsNot Nothing Then
                    Console.WriteLine("Script: " + info.ScriptFile)

                    Console.WriteLine("Referenced assemblies:")
                    For Each asm As String In info.Input.ReferencedAssemblies
                        Console.WriteLine(asm)
                    Next

                    If info.Result.Errors.HasErrors Then
                        For Each err As CompilerError In info.Result.Errors
                            If Not err.IsWarning Then
                                Console.WriteLine("Error: " + err.ErrorText)
                            End If
                        Next
                    End If
                End If


                CSScriptClass.CompilingHistory.Clear()
            Finally
                CSScriptClass.KeepCompilingHistory = False
            End Try
        End Sub

    End Class

End Namespace
