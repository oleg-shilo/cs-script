using System;
using System.IO;
using System.Reflection;
using CSScriptLibrary;

public class Host
{
    static void ExecuteAndUnloadScript(string script)
    {
        using (AsmHelper helper = new AsmHelper(CSScript.Compile(script), null, true))
        {
            helper.Invoke("*.Execute");
        }
    }

    static void Main()
    {
        ExecuteAndUnloadScript("script.cs");
        
        Console.WriteLine("\n>Modify script.cs file and press 'Enter'...");
        Console.ReadLine();
        
        ExecuteAndUnloadScript("script.cs");
    }
}
