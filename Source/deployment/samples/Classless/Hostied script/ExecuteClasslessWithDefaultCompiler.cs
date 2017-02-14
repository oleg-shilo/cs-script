using System;
using System.IO;
using CSScriptLibrary;

public class Host 
{
    static void Main()
    {
        //There is no script file but a free standing C# code. 
        //The required class definition infrastructure is injected by CS-Script engine 
        //and user has no control on class name, which is always Scripting.DynamicClass.
        //No static Main is generated as the code will always be executed from the host 
        //application thus there is no need for "well known" entry point.

        Run1();
        Run2();
        Run3();
        Run4();
    }

    static void Run1()
    {
        var code =
            @"public static void Hello(string greeting)
              {
                  SayHello(greeting);
              }
              static void SayHello(string greeting)
              {
                  Console.WriteLine(greeting);
              }";

        var script = new AsmHelper(CSScript.LoadMethod(code)); // /call first Hello method found with Reflection based MethodInvoker
        script.Invoke("*.Hello", "Hello World!");
    }

    static void Run2()
    {
        var code =
            @"public static void Hello(string greeting)
              {
                  SayHello(greeting);
              }
              static void SayHello(string greeting)
              {
                  Console.WriteLine(greeting);
              }";

        var script = new AsmHelper(CSScript.LoadMethod(code));       

        var SayHello = script.GetStaticMethod("*.Hello", ""); //call first Hello method found with FastMethodInvoker
        SayHello("Hello World!");
    }

    static void Run3()
    {
        var code =
            @"public static void Hello(string greeting)
              {
                  SayHello(greeting);
              }
              static void SayHello(string greeting)
              {
                  Console.WriteLine(greeting);
              }";

        var script = new AsmHelper(CSScript.LoadMethod(code));

        var SayHello = script.GetStaticMethod("Scripting.DynamicClass.Hello", ""); //fully qualified Hello method with  with FastMethodInvoker
        SayHello("Hello World!");
    }

    static void Run4()
    {
        var code =
            @"public static void Hello(string greeting)
              {
                  Console.WriteLine(greeting);
              }";

        var script = new AsmHelper(CSScript.LoadMethod(code));

        var SayHello1 = script.GetStaticMethod("*.*"); //first method found with FastMethodInvoker
        SayHello1("Hello World!");
    
        //or 

        var SayHello2 = script.GetStaticMethod(); 
        SayHello2("Hello World!");
    }
}
