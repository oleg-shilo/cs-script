using System;
using System.Collections;
using System.Text;
using System.IO;

public class AutoclassPrecompiler
{
    static public bool Compile(ref string content, string scriptFile, bool IsPrimaryScript, Hashtable context)
    {
        if (!IsPrimaryScript)
            return false;

        StringBuilder code = new StringBuilder(4096);
        code.AppendLine("//Auto-generated file");
        code.AppendLine("using System;");
        code.AppendLine("using System.IO;");

        bool headerProcessed = false;

        string line;
        using (StringReader sr = new StringReader(content))
            while ((line = sr.ReadLine()) != null)
            {
                if (!headerProcessed && !line.TrimStart().StartsWith("using ")) //not using...; statement of the file header
                    if (!line.StartsWith("//") && line.Trim() != "") //not comments or empty line
                    {
                        headerProcessed = true;

                        code.AppendLine("   public class ScriptClass");
                        code.AppendLine("   {");
                        code.AppendLine("       static public int Main(string[] args)");
                        code.AppendLine("       {");
                    }

                code.Append(line);
                code.Append(Environment.NewLine);
            }

        code.AppendLine("       return 0;");
        code.AppendLine("   }");
        code.AppendLine("}");

        content = code.ToString();
        return true;
    }
}


