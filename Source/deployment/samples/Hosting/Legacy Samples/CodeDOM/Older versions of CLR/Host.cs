using System;
using CSScriptLibrary;
using csscript;

public class Host
{
    static public void Log(string text)
    {
        Console.WriteLine(text);
    }

    static void Main()
    {
        CSScript.GlobalSettings = new Settings(); //create default settings instance instead of using the one initialized from CS-Script installation (if present)

        var Sum = new AsmHelper(CSScript.LoadMethod(
           @"public static int Sum(int a, int b)
              {
                  Host.Log(""Calculating sum..."");
                  return a + b;
              }"))
             .GetStaticMethod();

        int result = (int)Sum(1, 2);

        Log("Result: " + result);
    }
}
