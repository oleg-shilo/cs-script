using System;
using CSScriptLibrary;

public class Host
{
    static void Main()
    {
       var Div = new AsmHelper(CSScript.LoadMethod(
            @"public static double Div(int a, int b)
              {  
                   return a / b;
			  }"
               , null, true)).GetStaticMethod();

        try
        {
            var result = Div(4, 0);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}

