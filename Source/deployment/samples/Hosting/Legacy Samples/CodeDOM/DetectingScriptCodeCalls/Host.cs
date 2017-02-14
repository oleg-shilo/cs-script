using System;
using System.IO;
using System.Reflection;
using CSScriptLibrary;
using System.Diagnostics;

public class Host : MarshalByRefObject
{
    string name = "Host";

    public void CreateDocument()
    {
        if (Assembly.GetCallingAssembly().IsScriptAssembly())
            Console.WriteLine("Host: creating document (requested from the script)...");
        else
            Console.WriteLine("Host: creating document (requested from the host)...");
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
        CreateDocument();
        
        var script = new AsmHelper(CSScript.Load("script.cs", null, true));
        script.Invoke("*.Execute", this);
    }


    public static Host instance;
    static void Main()
    {
        Host host = new Host();
        host.Start();
    }
}
