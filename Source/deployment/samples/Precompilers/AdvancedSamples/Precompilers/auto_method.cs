using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;


public class AutoMethodPrecompiler
{
    static public bool Compile(ref string code, string scriptFile, bool IsPrimaryScript, Hashtable context)
    {
		var newReferences = (List<string>)context["NewReferences"];
	
        StringBuilder codePart = new StringBuilder(4096);
        StringBuilder usingPart = new StringBuilder(4096);

        bool usingsProcessed = false;

        string line;
        using (StringReader sr = new StringReader(code))
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("//") || line.Trim() == "")      //not comment nor empty line
                    continue;

                if (!line.TrimStart().StartsWith("using "))          //not using...; statement
                    usingsProcessed = true;

                if (!usingsProcessed)
                    usingPart.AppendLine(line);
                else
                    codePart.AppendLine(line);
            }

        newReferences.Add("System.Data.dll");
		newReferences.Add("System.Xml.dll");
		newReferences.Add("System.Windows.Forms.dll");

        string codeTemplate = @"
//Auto-generated (precompiled) from {SCRIPT} file
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Data;

{USING}

public class ScriptClass
{
    static public int Main(string[] args)
    {
        {CODE}
        return 0;
    }
}
";
        code = codeTemplate.Replace("{SCRIPT}", scriptFile)
                           .Replace("{USING}", usingPart.ToString())
                           .Replace("{CODE}", codePart.ToString());

        return true;
    }
}