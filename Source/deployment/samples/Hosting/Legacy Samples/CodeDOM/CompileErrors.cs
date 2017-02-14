using System;
using CSScriptLibrary;
using System.CodeDom.Compiler;
using System.Diagnostics;
using csscript;
using System.IO;

class Script
{
    static public void Main(string[] args)
    {
        TreatWarningAsError1();
        //TreatWarningAsError2();
    }

    static void CatchError()
    {
        string code = @"using System;
                        public class Calc
                        {
                            static public int Sum(int a, int b) 
                            {
                                return a + b                    
                            }
                        }";
        try
        {
            CSScript.LoadCode(code);
        }
        catch (CompilerException e)
        {
            ReportCompilerError(e);
        }
    }

    static void TreatWarningAsError1()
    {
        string code = @"//css_co /warnaserror+;
                        using System;
                        public class Calc
                        {
                            static public int Sum(int a, int b) 
                            {
                                #warning test warning
                                return a + b;                    
                            }
                        }";
        try
        {
            CSScript.LoadCode(code);
        }
        catch (CompilerException e)
        {
            ReportCompilerError(e);
        }
    }

    static void TreatWarningAsError2()
    {
        string code = @"using System;
                        public class Calc
                        {
                            static public int Sum(int a, int b) 
                            {
                                #warning test warning
                                return a + b;                    
                            }
                        }";
        try
        {
            string script = CSSEnvironment.SaveAsTempScript(code);
            string assembly = CSScript.CompileWithConfig(script, null, false, CSScript.GlobalSettings, "/warnaserror");
        }
        catch (CompilerException e)
        {
            ReportCompilerError(e);
        }
    }

    static void ReportCompilerError(CompilerException e)
    {
        CompilerErrorCollection errors = (CompilerErrorCollection)e.Data["Errors"];

        foreach (CompilerError err in errors)
        {
            Console.WriteLine("{0}({1},{2}): {3} {4}: {5}",
                                err.FileName,
                                err.Line,
                                err.Column,
                                err.IsWarning ? "warning" : "error",
                                err.ErrorNumber,
                                err.ErrorText);
        }
    }
}

