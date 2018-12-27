//css_inc precompile.part.cs;
using System;

partial class Precompiler
{
    CompileResult PROP(string[] args, Context cx)
    {
        string modifier = args[0];
        string type = args[1];
        string name = args[2];
        string initialVal = args[3];

        string code =
            "    " + modifier + " private " + type + " " + name.ToLower() + " = " + initialVal + ";\r\n" +
            "    " + modifier + " public " + type + " " + name + "\r\n" +
            "    {\r\n" +
            "    	get { return " + name.ToLower() + "; }\r\n" +
            "    	set { " + name.ToLower() + " = value; }\r\n" +
            "    }";

        return new CompileResult(code, InsertionPosition.ClassBody);
    }
}
