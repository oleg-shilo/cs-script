using System;
using System.IO;
using System.Reflection;
using CSScriptLibrary;
using csscript;
public class Host : MarshalByRefObject
{
    string name = "Host";

    public void CreateDocument()
    {
        Console.WriteLine("Host: creating document...");
    }
    public void CloseDocument()
    {
        Console.WriteLine("Host: closing document...");
    }
    public void OpenDocument(string file)
    {
        Console.WriteLine("Host: opening documant (" + file + ")...");
    }
    public void SaveDocument(string file)
    {
        Console.WriteLine("Host: saving documant (" + file + ")...");
    }
    public string Name
    {
        get { return name; }
    }
    void Start()
    {
        ExecuteScript(Path.GetFullPath("script.cs"));
        //ExecuteAndUnloadScript(Path.GetFullPath("script.cs"));
    }
    void ExecuteScript(string script)
    {
        CSScriptLibrary.AsmHelper helper = new CSScriptLibrary.AsmHelper(CSScriptLibrary.CSScript.Load(script, null, true));
        helper.Invoke("*.Execute", this);
    }
    void ExecuteAndUnloadScript(string script)
    {
        using (CSScriptLibrary.AsmHelper helper = new CSScriptLibrary.AsmHelper(CSScriptLibrary.CSScript.Compile(Path.GetFullPath(script), null, true), null, true))
        {
            helper.Invoke("*.Execute", this);
        }
    }

    public static Host instance;
    static void Main()
    {
		CSScript.GlobalSettings = new Settings(); //create default settings instance instead of using the one initialized from CS-Script installation (if present)
	
        Host host = new Host();
        host.Start();
    }
}
