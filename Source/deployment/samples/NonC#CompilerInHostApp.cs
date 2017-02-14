//css_ref Microsoft.CSharp
//css_ref System.Core
using System;
using System.Windows.Forms;
using System.CodeDom.Compiler;
using System.Reflection;
using CSScriptLibrary;

public class Host
{
    [STAThread]
    static public void Main(string[] args)
    {
        //indicate that the CS-Script engine should use custom (VB.NET) compiler
        CSScript.GlobalSettings.UseAlternativeCompiler = Assembly.GetExecutingAssembly().Location;
        
        string[] refAssemblies = new[] { typeof(Form).Assembly.Location};
        
        string code = @"Imports System
                        Imports System.Windows.Forms

                        Public Class Script
                            Public Sub Main()
                                Console.WriteLine(""Hello World!"")
                                MessageBox.Show(""Hello World!"")
                            End Sub
                        End Class";

        dynamic script = CSScript.LoadCode(code, refAssemblies)
                                 .CreateObject("Script");
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