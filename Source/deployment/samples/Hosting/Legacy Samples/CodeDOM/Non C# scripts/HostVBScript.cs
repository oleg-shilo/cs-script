using System.CodeDom.Compiler;
using System.Reflection;
using CSScriptLibrary;

public class Host
{
    static void Main()
    {
        CSScript.GlobalSettings.UseAlternativeCompiler = Assembly.GetExecutingAssembly().Location; //indicate that the CS-Script engine should use custom (VB.NET) compiler

        string code = @"Imports System
                        Imports System.Windows.Forms

                        Public Class Script
                            Public Sub Main()
                                MessageBox.Show(""Hello World! (VB)"")
                            End Sub
                        End Class";
            

        dynamic script = CSScript.LoadCode(code) 
                                 .CreateObject("Script");       //Note: you cannot use '.CreateObject("*")' as VB compiler emits additional classes (MyApplication, MyComputer and MyProject) and Script is no longer a single class of the assembly

        script.Main();
    }
}


public class CSSCodeProvider
{
    static public ICodeCompiler CreateCompiler(string sourceFile) //sourceFile value is irrelevant in this case
    {
        return new Microsoft.VisualBasic.VBCodeProvider().CreateCompiler();
    }
}
