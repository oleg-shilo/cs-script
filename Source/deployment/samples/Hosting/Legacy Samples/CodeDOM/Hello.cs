//css_dir %csscript_dir%\Lib
//css_ref Microsoft.CSharp;
//css_ref System.Core;
using System;
using System.Reflection;
using CSScriptLibrary;
using System.IO;
using System.Diagnostics;

public interface IScript
{
    void Say(string greeting);
}

public class Host
{
    void Test()
    {
    }

    static void Main()
    {
        string code =
            @"using System;
			  public class Script
			  {
				  public static void SayHello(string greeting)
				  {
					  Console.WriteLine(""Static:   "" + greeting);
				  }
                  public void Say(string greeting)
				  {
					  Console.WriteLine(""Instance: "" + greeting);
				  }
			  }";

        var script = new AsmHelper(CSScript.LoadCode(code, null, true));

        //call static method
        script.Invoke("*.SayHello", "Hello World! (invoke)");

        //call static method via emitted FastMethodInvoker
        var SayHello = script.GetStaticMethod("*.SayHello", "");
        SayHello("Hello World! (emitted method)");

        object obj = script.CreateObject("Script");

        //call instance method
        script.InvokeInst(obj, "*.Say", "Hello World! (invoke)");

        //call instance method via emitted FastMethodInvoker
        var Say = script.GetMethod(obj, "*.Say", "");
        Say("Hello World! (emitted method)");

        //call using C# 4.0 Dynamics
        dynamic script1 = CSScript.LoadCode(code)
                                  .CreateObject("*");

        script1.Say("Hello World! (dynamic object)");

        //call using CS-Scrit Interface Alignment
        IScript script2 = obj.AlignToInterface<IScript>();
        script2.Say("Hello World! (aligned interface)");
    }
}