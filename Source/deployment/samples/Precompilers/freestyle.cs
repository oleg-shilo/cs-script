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

        StringBuilder code = new StringBuilder();
        code.AppendLine("//CS-Script auto-generated file: freestyle");
        code.AppendLine("using System;");
        code.AppendLine("using System.IO;");

        bool headerProcessed = false;

        string line;
        int lineIndex = 0;

        using (StringReader sr = new StringReader(content))
            while ((line = sr.ReadLine()) != null)
            {
                //not using...; statement of the file header
                if (!headerProcessed && !line.TrimStart().StartsWith("using "))
                {
                    if (!line.StartsWith("//") && line.Trim() != "") //not a comment/empty line
                    {
                        headerProcessed = true;

                        code.AppendLine("public class ScriptClass ");
                        code.AppendLine("{");
                        code.AppendLine("    static public int Main(string[] args) { return main_impl(args); }");
                        code.AppendLine("#line " + (lineIndex - 1) + " \"" + scriptFile + "\""); // must be outside of the main_impl
                        code.AppendLine("    static public int main_impl(string[] args)");
                        code.AppendLine("    {");
                    }
                }

                code.Append(line);
                code.Append(Environment.NewLine);
                lineIndex++;
            }

        code.AppendLine("       return 0;");
        code.AppendLine("   }");
        code.AppendLine("}");

        content = code.ToString();
        return true;
    }
}