using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

public partial class  AutoMethodPrecompiler
{ 
}
public partial class AutoMethodPrecompiler 
{
    public static bool Compile(ref string content, string scriptFile, bool IsPrimaryScript, Hashtable context)
    {
        if (!IsPrimaryScript)
            return false;

        var references = (context["NewReferences"] as List<string>);
        StringBuilder code = new StringBuilder(4096);

        code.AppendLine("//Auto-generated file"); 
        
        code.AppendLine("using System;");
        code.AppendLine("using System.Windows.Forms;");

        references.Add("System.Windows.Forms.dll");
        
        bool headerProcessed = false;
        string line;
        using (StringReader sr = new StringReader(content))
            while ((line = sr.ReadLine()) != null)
            {
                if (!headerProcessed && !line.TrimStart().StartsWith("using ")) //not using...; statement of the file header
                    if (!line.StartsWith("//") && line.Trim() != "") //not comments or empty line
                    {
                        headerProcessed = true;
                            
                        code.AppendLine("   public partial class ScriptClass" + Environment.NewLine);
                        code.AppendLine("   {" + Environment.NewLine);
                        code.AppendLine("    public ");
                    }
                        
                code.Append(line);
                code.Append(Environment.NewLine);
            }

        code.AppendLine("   }");

        content = code.ToString();

        return true;
    }
}