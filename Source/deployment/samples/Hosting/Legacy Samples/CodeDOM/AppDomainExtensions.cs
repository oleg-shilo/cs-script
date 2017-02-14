using System;
using System.Linq;
using System.Reflection;
using CSScriptLibrary;

//NOTE: this script must be run under Visual Studio
static class Host
{
    static public void Main(string[] args)
    {
        string code = @"using System;

                         public class Script
                         {
                             public static void Hello(string greeting)
 	                         {
 	                             Console.WriteLine(greeting);
 	                         }

                             public static void HelloUp(string greeting)
 	                         {
 	                             Console.WriteLine(greeting.ToUpper());
 	                         }
                         }";

        var hello1 = CSScript.LoadCode(code).GetStaticMethod();
        hello1("test local");

        //Note you cannot pass "code"  into Job1/Job2 as this will trigger auto-generation of "DisplayClass#" class by CLR as part of the anonymous method implementation.
        //And the problem is that "DisplayClass#" is not serializable not inherited from MarshalByRefObject. 
        //Thus if some data needs to be passed to the Action to be executed (e.g. Job1) then it needs to be wrapped up into Remoting compatible container. You can use CS-Script class for this.

        var codeRef = new Ref<string>(code);
        var contextRef = new Ref<string[]>(new[] { code, "HelloUp" });

        AppDomain.CurrentDomain
                 .Clone()
                 .Execute(arg => Job1(arg.Value), codeRef) //arg is the codeRef
                 .Execute(arg => Job2(arg.Value), contextRef) //arg is the contextRef
                 .Unload();
    }

    static void Job1(string code)
    {
        var hello = CSScript.LoadMethod(code, null, true).GetStaticMethod(); //will return the first static method found
        hello("test remote");
    }

    static void Job2(string[] context)
    {
        string code = context[0];
        string methodName = context[1];
        var hello = CSScript.LoadMethod(code, null, true).GetStaticMethodWithArgs("*." + methodName, typeof(string));
        hello("test remote");
    }
}