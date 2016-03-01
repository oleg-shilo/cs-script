//css_dbg /t:library;
using System;
using System.IO;
using System.Windows.Forms;
using System.CodeDom.Compiler;
using Microsoft.JScript;
using Microsoft.CSharp;
using System.Diagnostics;
using System.Collections.Generic;

//css_import ccscompiler;
//css_import cppcompiler;
//css_import xamlcompiler;

public class CSSCodeProvider
{
    static public ICodeCompiler CreateCompiler(string sourceFile)
    {
        //Debug.Assert(false);
        var compilerVersion35 = new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } };

        if (Path.GetExtension(sourceFile).ToLower() == ".vb")
            return (new Microsoft.VisualBasic.VBCodeProvider(compilerVersion35)).CreateCompiler();
        else if (Path.GetExtension(sourceFile).ToLower() == ".js")
            return (new Microsoft.JScript.JScriptCodeProvider()).CreateCompiler();
        else if (Path.GetExtension(sourceFile).ToLower() == ".ccs")
            return new CSScriptCompilers.CSharpCompiler("v4.0");
        else if (Path.GetExtension(sourceFile).ToLower() == ".cpp")
            return new CSScriptCompilers.CPPCompiler();
        else
            return new CSScriptCompilers.CSCompiler("v4.0");
    }

    static public ICodeCompiler CreateCompilerVersion(string sourceFile, string version)
    {
        //Debug.Assert(false);

        var compilerCreateOptions = new Dictionary<string, string>() { { "CompilerVersion", version } };
        
        if (Path.GetExtension(sourceFile).ToLower() == ".vb")
            return (new Microsoft.VisualBasic.VBCodeProvider(compilerCreateOptions)).CreateCompiler();
        else if (Path.GetExtension(sourceFile).ToLower() == ".js")
            return (new Microsoft.JScript.JScriptCodeProvider()).CreateCompiler();
        else if (Path.GetExtension(sourceFile).ToLower() == ".ccs")
            return new CSScriptCompilers.CSharpCompiler(compilerCreateOptions["CompilerVersion"]);
        else if (Path.GetExtension(sourceFile).ToLower() == ".cpp")
            return new CSScriptCompilers.CPPCompiler();
        else
            return new CSScriptCompilers.CSCompiler(compilerCreateOptions["CompilerVersion"]);
    }
}
