Option Infer On
Option Strict Off

Imports System
Imports CSScriptLibrary
Imports CSScriptClass = CSScriptLibrary.CSScript

Namespace Global.CSScriptEvaluatorApi

    Partial Public Class HostApp

        Partial Private Class EvaluatorSamples

            Public Sub CompileMethod_Instance()
                ' 1- CompileMethod wraps method into a class definition and returns compiled assembly
                ' 2 - CreateObject creates instance of a first class in the assembly

                Dim myCode As String = "int Sqr(int data) {
                                          return data * data;
                                        }"
                Dim myScript = CSScriptClass.Evaluator.CompileMethod(myCode).CreateObject("*")
                Dim myResult As Int32 = myScript.Sqr(7)
            End Sub

            Public Sub LoadCode()
                ' LoadCode compiles code and returns instance of a first class
                ' in the compiled assembly
                Dim myCode As String = "using System;
                
                                        public class Script {
                                          
                                          public int Sum(int a, int b) {
                                            return a + b;
                                          }
                                        
                                        }"
                Dim myScript = CSScriptClass.Evaluator.LoadCode(myCode)
                Dim myResult As Int32 = myScript.Sum(1, 2)
            End Sub

            Public Sub LoadMethod()
                ' LoadMethod compiles code and returns instance of a first class
                ' in the compiled assembly.
                ' LoadMethod is essentially the same as LoadCode. It just deals not with the 
                ' whole class definition but a single method(s) only. And the rest of the class definition is 
                ' added automatically by CS-Script. 
                ' 'public' is optional as it will be injected if the code doesn't start with it.
                Dim myCode As String = "using System;
                
                                        public int Sum(int a, int b) {
                                          return a + b;
                                        }"
                Dim myScript = CSScriptClass.Evaluator.LoadMethod(myCode)
                Dim myResult As Int32 = myScript.Sum(1, 2)
            End Sub

            Public Sub PerformanceTest(Optional count As Integer = -1)
                Dim myCode As String = "int Sqr(int a) {
                                          return a * a;
                                        }"
                'this unique extra code comment ensures the code to be compiled cannot be cached
                If count <> -1 Then myCode &= " //" & count
                Dim myScript = CSScriptClass.Evaluator.CompileMethod(myCode).CreateObject("*")
                Dim myResult As Int32 = myScript.Sqr(3)
            End Sub

            Public Sub DebugTest()
                'pops up an assertion dialog 
                CSScriptClass.EvaluatorConfig.DebugBuild = True
                CSScriptClass.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom

                Dim myCode As String = "using System;
                                        using System.Diagnostics;
                
                                        public class Script {
                
                                          public int Sum(int a, int b) {
                                            Debug.Assert(false, ""Testing CS-Script debugging..."");
                                            return a + b;
                                          }
                
                                        }"
                Dim myScript = CSScriptClass.Evaluator.LoadCode(myCode)
                Dim myResult = myScript.Sum(3, 4)
            End Sub

        End Class

    End Class

End Namespace
