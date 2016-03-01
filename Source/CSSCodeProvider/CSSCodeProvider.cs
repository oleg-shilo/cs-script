//css_dbg /t:library;
using System;
using System.IO;
using System.Windows.Forms;
using System.CodeDom.Compiler;
using Microsoft.JScript;
using Microsoft.CSharp;

//css_import ccscompiler;
//css_import cppcompiler;
//css_import xamlcompiler;

public class CSSCodeProvider
{
	static public ICodeCompiler CreateCompiler(string sourceFile)
	{
		if (Path.GetExtension(sourceFile).ToLower() == ".vb")
			return (new Microsoft.VisualBasic.VBCodeProvider()).CreateCompiler();
		else if (Path.GetExtension(sourceFile).ToLower() == ".js")
			return (new Microsoft.JScript.JScriptCodeProvider()).CreateCompiler();
		else if (Path.GetExtension(sourceFile).ToLower() == ".ccs")
			return new CSScriptCompilers.CSharpCompiler();
		else if (Path.GetExtension(sourceFile).ToLower() == ".cpp")
			return new CSScriptCompilers.CPPCompiler();
		else
			return new CSScriptCompilers.CSCompiler();
	}
}
