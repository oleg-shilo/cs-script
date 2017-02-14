using System;
using System.IO;
using System.Reflection;
using CSScriptLibrary;

public class Host
{
    void Start()
    {
        ExecuteScript(Path.GetFullPath("script.cs"));
        ExecuteAndUnloadScript(Path.GetFullPath("script.cs"));
    }
    void ExecuteScript(string script)
    {
        AsmHelper helper = new AsmHelper(CSScript.Load(script, Path.GetFullPath("ExternalAsm.dll")));
        helper.Invoke("*.Execute");
    }
	void ExecuteAndUnloadScript(string script)
    {
        using (AsmHelper helper = new AsmHelper(CSScript.Compile(script, Path.GetFullPath("ExternalAsm.dll")), null, true))
        {
            helper.Invoke("*.Execute");
        }
    }

    static void Main()
    {
        Host host = new Host();
        host.Start();
    }
}
