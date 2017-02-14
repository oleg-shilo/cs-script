//css_dbg /t:exe, /args:"C:\cs-script\Samples\Macros\script.cs";  
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

    CompileResult USING(string[] args, Context cx)
    {
        string code = "using " + args[0] + ";";
        return new CompileResult(code, InsertionPosition.ModuleStart);
    }

    CompileResult NEW(string[] args, Context cx)
    {
        string code = "    public static  " + cx.autoTypeName + " New() { return new " + cx.autoTypeName + "();}";
        return new CompileResult(code, InsertionPosition.ClassBody);
    }

    CompileResult VALUE(string[] args, Context cx)
    {
        string code = "    " + args[0] + ",";
        return new CompileResult(code, InsertionPosition.ClassBody);
    }

    public override CompileResult OnAfterSection(Context ct)
    {
        if (ct.autoTypeDeclaration == "css_extend_str_enum")
        {
            string code =
                "static public class " + ct.autoTypeName + "Extensions\r\n" +
                "{\r\n" +
                "    static System.Collections.Generic.Dictionary<" + ct.autoTypeName + ", string> map = new System.Collections.Generic.Dictionary<" + ct.autoTypeName + ",string>();\r\n" +
                "    static " + ct.autoTypeName + "Extensions()\r\n" +
                "    {\r\n";

            foreach (string[] args in ct.macros[ct.autoTypeName])
                code += "        map[" + ct.autoTypeName + "." + args[0] + "] = " + args[1] + ";\r\n";

            code += 
                "    }\r\n" +
                "    static public string ToStringEx(this " + ct.autoTypeName + " obj)\r\n" +
                "    {\r\n" +
                "        return map[obj];\r\n" +
                "    }\r\n" +
                "}\r\n";
            return new CompileResult(code, InsertionPosition.ClassBody);
        }
        else
            return null;
    }
}
