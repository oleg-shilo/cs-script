using System.Collections;

public class AutoMethodPrecompiler 
{
    static public bool Compile(ref string code, string scriptFile, bool IsPrimaryScript, Hashtable context)
    {
        code = code.Replace("print (", "Console.WriteLine(")
		           .Replace("print(", "Console.WriteLine(")
                   .Replace("strings {", "new string[]{")
                   .Replace("strings{", "new string[]{")
                   .Replace("foreach (", "foreach (var ")
                   .Replace("foreach(", "foreach (var ")
                   .Replace("msgbox (", "MessageBox.Show(")
                   .Replace("msgbox(", "MessageBox.Show(");

        return true;
    }
}