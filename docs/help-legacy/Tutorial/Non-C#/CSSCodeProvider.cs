using System;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.JScript;

public class CSSCodeProvider
{
	static public ICodeCompiler CreateCompiler(string sourceFile)
	{
		string errMsg = "Alternative compiler for file extension "+Path.GetExtension(sourceFile)+" cannot be created.";
		try
		{
			if (Path.GetExtension(sourceFile).ToLower() == ".cs")
				return (new Microsoft.CSharp.CSharpCodeProvider()).CreateCompiler();
			else if (Path.GetExtension(sourceFile).ToLower() == ".vb")	
				return (new Microsoft.VisualBasic.VBCodeProvider()).CreateCompiler();
			else if (Path.GetExtension(sourceFile).ToLower() == ".js")	
				return (new Microsoft.JScript.JScriptCodeProvider()).CreateCompiler();
		}
		catch (Exception ex)
		{
			throw new Exception(errMsg, ex);
		}
		throw new Exception(errMsg);		
	}
}

