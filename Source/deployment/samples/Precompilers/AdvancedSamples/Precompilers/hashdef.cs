using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text;

public class HashDefPrecompiler 
{
    static public bool Compile(ref string code, string scriptFile, bool IsPrimaryScript, Hashtable context)
    {
        var hashDefs = new Dictionary<string, string>();

        var content = new StringBuilder();

        string line;
        using (var sr = new StringReader(code))
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Trim().StartsWith("#define ")) //#define <pattern> <replacement> 
                {
                    string[] tokens = line.Split(" ".ToCharArray(), 3, StringSplitOptions.RemoveEmptyEntries);
                    hashDefs.Add(tokens[1], tokens[2]);

                    content.AppendLine("//" + line);
                }
                else
                    content.AppendLine(line);
            }
        
        code = content.ToString();
        foreach(string key in hashDefs.Keys) 
            code = code.Replace(key, hashDefs[key]);

        return true;
    }
}