using System;
using System.IO;
using CSScriptLibrary;

public class Host 
{
    static void Main()
    {
        CallStaticMethodOfFreeStandingClasslessCode();

        CallStaticMethodOfDefaultClassInClasslessScript();
        CallStaticMethodOfTheOnlyClassAvailableInClasslessScript();
        CallStaticMethodOfSpecifiedClassWithNamespaceInClasslessScript();
        CallStaticMethodOfSpecifiedClassWithoutNamespaceInClasslessScript();
    }

    static void CallStaticMethodOfFreeStandingClasslessCode()
    {
        //There is no script. 
        //The code consist of the only method definition. 
        //Autoclass is injected by CS-Script engine and user has no control 
        //on class name, which is always Scripting.DynamicClass

        var assembly = CSScript.LoadMethod(
        @"public static void SayHello(string greeting)
          {
              Console.WriteLine(greeting);
          }");

        AsmHelper script = new AsmHelper(assembly);
        script.Invoke("*.SayHello", "Hello World!");
        //or
        var SayHello = script.GetStaticMethod();
        SayHello("Hello World!");
    }

    static void CallStaticMethodOfDefaultClassInClasslessScript()
    {
        //There is a script file to execute. 
        //The script consist of the only method definition. 
        //Autoclass is injected by CS-Script engine and user has no control 
        //on class name, which is always Scripting.DynamicClass

        CSScript.GlobalSettings.UseAlternativeCompiler =
            Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\CSSCodeProvider.dll");

        var helper = new AsmHelper(CSScript.Load("script1.ccs", null, true));
        helper.Invoke("auto_script1.Script.SayHello", "Hello World!");
    }

    static void CallStaticMethodOfTheOnlyClassAvailableInClasslessScript()
    {
        CSScript.GlobalSettings.UseAlternativeCompiler =
            Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\CSSCodeProvider.dll");

        var helper = new AsmHelper(CSScript.Load("script1.ccs", null, true));
        helper.Invoke("*.SayHello", "Hello World!");
    }

    static void CallStaticMethodOfSpecifiedClassWithNamespaceInClasslessScript()
    {
        CSScript.GlobalSettings.UseAlternativeCompiler =
            Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\CSSCodeProvider.dll");

        var helper = new AsmHelper(CSScript.Load("script2.ccs", null, true));
        helper.Invoke("MyNamespace.MyClass.SayHello", "Hello World!");
    }

    static void CallStaticMethodOfSpecifiedClassWithoutNamespaceInClasslessScript()
    {
        CSScript.GlobalSettings.UseAlternativeCompiler =
            Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\CSSCodeProvider.dll");

        var helper = new AsmHelper(CSScript.Load("script3.ccs", null, true));
        helper.Invoke("MyClass.SayHello", "Hello World!");
    }


}
