Option Infer On
Option Strict Off

Imports System
Imports System.IO
Imports System.Reflection
Imports CSScriptLibrary
Imports CSScriptClass = CSScriptLibrary.CSScript

Namespace Global.CSScriptEvaluatorExtensions

    Partial Public Class HostApp

        Partial Private Class AsyncSamples

            Public Async Sub LoadMethodAsync()
                Dim myCode As New StringWriter()
                myCode.WriteLine("public int Sum(int a, int b) {")
                myCode.WriteLine("  return a + b;")
                myCode.WriteLine("}")
                myCode.WriteLine()
                myCode.WriteLine("public int Div(int a, int b) {")
                myCode.WriteLine("  return a / b;")
                myCode.WriteLine("}")
                Dim myScript = Await CSScriptClass.Evaluator.LoadMethodAsync(myCode.ToString())
                Console.WriteLine("   End of {0}: {1}", NameOf(LoadMethodAsync), myScript.Div(15, 3))
            End Sub

            Public Async Sub CompileCodeAsync()
                Dim myCode As New StringWriter()
                myCode.WriteLine("using System;")
                myCode.WriteLine()
                myCode.WriteLine("public class Script {")
                myCode.WriteLine()
                myCode.WriteLine("  public int Sum(int a, int b) {")
                myCode.WriteLine("    return a + b;")
                myCode.WriteLine("  }")
                myCode.WriteLine()
                myCode.WriteLine("}")
                Dim myScript As Assembly = Await CSScriptClass.Evaluator.CompileCodeAsync(myCode.ToString())
                Dim myCalculator = myScript.CreateObject("*")
                Console.WriteLine("   End of {0}: {1}", NameOf(CompileCodeAsync), myCalculator.Sum(15, 3))
            End Sub

        End Class

    End Class

End Namespace
