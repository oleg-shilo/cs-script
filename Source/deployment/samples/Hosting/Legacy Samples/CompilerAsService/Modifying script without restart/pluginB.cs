using System;
using MyCompany;

//Note: you cannot declare namespaces if you are going to use CSScript.Evaluator.Roslyn (which also has some other limitations). 
//other engines are OK.
//using MyCompany; {
public class PluginA : MarshalByRefObject, IPlugin
{
    IHost host;

    public void Init(IHost host)
    {
        this.host = host;
    }

    public void Log(string message)
    {
        host.Log("b> " + message);
    }
}
//}