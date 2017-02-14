using System;
using System.IO;
using CSScriptLibrary;

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
        Console.WriteLine("Host: opening document (" + file + ")...");
    }

    public void SaveDocument(string file)
    {
        Console.WriteLine("Host: saving document (" + file + ")...");
    }

    public string Name
    {
        get { return name; }
    }

    void Start()
    {
        //ExecuteScript(Path.GetFullPath("script.cs"));
        ExecuteAndUnloadScript(Path.GetFullPath("script.cs"));
    }

    void ExecuteScript(string script)
    {
        AsmHelper helper = new AsmHelper(CSScript.Load(script, null, true));
        helper.Invoke("*.Execute", this);
    }

    void ExecuteAndUnloadScript(string script)
    {
        using (AsmHelper helper = new AsmHelper(CSScript.Compile(script, null, true), null, true))
        {
            helper.Invoke("*.Execute", this);
        }
    }

    public static Host instance;

    static void Main()
    {
        Host host = new Host();
        host.Start();
    }
}