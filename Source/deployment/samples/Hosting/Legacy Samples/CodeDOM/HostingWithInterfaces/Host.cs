using System;
using CSScriptLibrary;

public interface IHost
{
    string Name { get; }
    void Who();
}

public interface IScript
{
    IHost Parent { set; }
    void Execute(string context);
}

class Host : IHost
{
    string name = "Host";
    void IHost.Who()
    {
        Console.WriteLine("I am " + name);
    }
    
    string IHost.Name
    {
        get
        {
            return name;
        }
    }

    void Start()
    {
        //Because the script object is accessed through an interface it is possible to use
        //it as any other non-scripted type (class): set properties (not only invoke methods), intellisence, compiler checking  
        IScript script = (IScript)CSScript.Load("script.cs")
                                          .CreateObject("Script");

        script.Parent = this; 
        script.Execute("calling from Host.Start()");
    }
    
    static void Main()
    {
        new Host().Start();
    }
}
