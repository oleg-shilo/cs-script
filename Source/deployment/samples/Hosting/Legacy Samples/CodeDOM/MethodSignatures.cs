using System;
using System.Reflection;
using CSScriptLibrary;

public class Host
{
    static void Main()
    {
        string code =
            @"using System;
			  public class Script
			  {
                  public static string Main()
				  {
                      return ""Main0"";  
				  }
                  public static string Main(string args)
				  {
                      return ""Main1"";  
				  }
                  public static string Main(string[] args)
				  {
                      return ""MainN"";  
				  }
                  public static string Main(string[] args, int count)
				  {
                      return ""MainN1"";  
				  }
			  }";


        string asmFile = CSScript.CompileCode(code);
        using (AsmHelper helper = new AsmHelper(asmFile, "", true))
        {
            string[] args = new string[0];

            Console.WriteLine(helper.Invoke("Script.Main"));                                    //Main0
            
            Console.WriteLine(helper.Invoke("Script.Main", "test arg"));                        //Main1

            Console.WriteLine(helper.Invoke("Script.Main", new object[] { args }));             //MainN
            Console.WriteLine(helper.Invoke("Script.Main", new object[] { new string[0] }));    //MainN

            Console.WriteLine(helper.Invoke("Script.Main", args, 7));                           //MainN1
        } 
    }
}

