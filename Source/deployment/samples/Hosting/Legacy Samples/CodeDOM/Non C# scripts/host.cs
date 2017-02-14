using System;
using CSScriptLibrary;
using System.IO;

public class Host
{
    static void Main()
    {
        CSScript.GlobalSettings.UseAlternativeCompiler = Path.GetFullPath("CSSCodeProvider.dll"); //compiler that understands JS/VB.NET/C++ and classless-C#
        
        var script = new AsmHelper(CSScript.Load("Hello.js"));
        script.Invoke("*.Main");
    }
}

