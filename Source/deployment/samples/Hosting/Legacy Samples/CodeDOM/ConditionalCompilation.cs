using System.IO;
using CSScriptLibrary;

public class Host
{
     static string code = @"using System;
    					    
                            public class Script
    					    {
    						    public static void Print(string greeting)
    						    {
                                #if USE_TIMESTAMP 
                                    Console.Write(DateTime.Now.ToString() + "":"");
                                #endif 						  	  
                                    Console.WriteLine(greeting);
    						    }
    					    }";
    
    static void Main()
    {
        //NOTE: because "Print" is the only static method in the script we can use GetStaticMethod() 
        //otherwise you would need to use GetStaticMethodWithArgs("*.Print", typeof(string)) or any
        //appropriate GetStaticMethod() overloads

        //executing code without USE_TIMESTAMP defined
        var Print = CSScript.LoadCode(code)
                            .GetStaticMethod();

        Print("Hello World!");

        //executing code with USE_TIMESTAMP defined
        Print = CSScript.LoadCode("//css_co /define:USE_TIMESTAMP;\n" + code)
                        .GetStaticMethod();

        Print("Hello World!");

        //executing script file with USE_TIMESTAMP defined in the master script
        string scriptFile = scriptFile = Path.GetTempFileName();
        File.WriteAllText(scriptFile, code);
        string masterScriptCode = string.Format(@"//css_co /define:USE_TIMESTAMP;
                                                  //css_inc {0};", scriptFile);
        Print = CSScript.LoadCode(masterScriptCode)
                        .GetStaticMethod();

        Print("Hello World!");

        File.Delete(scriptFile);
    }
}

