using System;
using System.CodeDom.Compiler;
using System.Reflection;
using CSScriptLibrary;

public interface IScript
{
    void Main();
}

public class Host
{
    [STAThread]
    static public void Main(string[] args)
    {
        //indicate that the CS-Script engine should use custom (VB.NET) compiler
        CSScript.GlobalSettings.UseAlternativeCompiler = Assembly.GetExecutingAssembly().Location;

        string code = @"Imports System
                        Imports System.Windows.Forms

                        Public Class Script
                            Public Sub Main()
                                MessageBox.Show(""Hello World!"")
                            End Sub
                        End Class";

        IScript script = CSScript.LoadCode(code)
                                 .CreateObject("Script")
                                 .AlignToInterface<IScript>();

        script.Main();
    }

public class CSSCodeProvider
{
    public static ICodeCompiler CreateCompiler(string sourceFile)
    {
        return new Microsoft.VisualBasic.VBCodeProvider().CreateCompiler();
    }
}
}