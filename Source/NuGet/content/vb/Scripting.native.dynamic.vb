Option Infer On
Option Strict Off

Imports System.IO
Imports csscript
Imports CSScriptClass = CSScriptLibrary.CSScript

Namespace Global.CSScriptNativeApi

    Partial Public Class HostApp

        Partial Public Class CodeDomSamples

            Public Shared Sub LoadMethod_Instance()
                ' 1- LoadMethod wraps method into a class definition, compiles it and returns loaded assembly
                ' 2 - CreateObject creates instance of a first class in the assembly  
                Dim myCode As String = "int Sqr(int data) {
                                          return data * data;
                                        }"
                Dim myScript = CSScriptClass.LoadMethod(myCode).CreateObject("*")
                Dim myResult = myScript.Sqr(7)
            End Sub

            Public Shared Sub LoadCode()
                ' LoadCode compiles code and returns instance of a first class 
                ' in the compiled assembly  
                Dim myCode As String = "using System;

                                    public class Script {

                                        public int Sum(int a, int b) {
                                            return a + b;
                                        }

                                    }"
                Dim myScript = CSScriptClass.LoadCode(myCode).CreateObject("*")
                Dim myResult As Integer = myScript.Sum(1, 2)
            End Sub

            Public Shared Sub LoadCodeWithConfig()
                ' LoadCode compiles code and returns instance of a first class 
                ' in the compiled assembly  

                Dim myCode As String = "using System;

                                    public class Script {

                                        public int Sum(int a, int b) {
                                            return a + b;
                                        }

                                    }"
                Dim myFile As String = Path.GetTempFileName()
                Try
                    File.WriteAllText(myFile, myCode)
                    Dim settings As New Settings()
                    'Dim settings As Settings = Nothing 'set to Nothing to fall back to defaults 

                    Dim myScript = CSScriptClass.LoadWithConfig(myFile, Nothing, False, settings, "/define:TEST").CreateObject("*")
                    Dim myResult As Integer = myScript.Sum(1, 2)
                Finally
                    If File.Exists(myFile) Then
                        File.Delete(myFile)
                    End If
                End Try
            End Sub

            Public Shared Sub DebugTest()
                'pops up an assertion dialog 
                Dim myCode As String = "using System;
                                        using System.Diagnostics;
                
                                        public class Script {
                
                                          public int Sum(int a, int b) {
                                            Debug.Assert(false, ""Testing CS-Script debugging..."");
                                            return a + b;
                                          }
                
                                        }"
                Dim myScript = CSScriptClass.LoadCode(myCode, Nothing, debugBuild:=True).CreateObject("*")
                Dim result As Integer = myScript.Sum(1, 2)
            End Sub

        End Class

    End Class

End Namespace
