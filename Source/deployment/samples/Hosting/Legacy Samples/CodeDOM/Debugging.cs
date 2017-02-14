using System;
using System.Diagnostics;
using CSScriptLibrary;

public class Host
{
    static public void Log(string text)
    {
        Console.WriteLine(text);
    }

    static void Main()
    {
        var Sum = new AsmHelper(CSScript.LoadMethod(
            @"public static int Sum(int a, int b)
              {
                  try
                  {
                        System.Diagnostics.Debugger.Break();
                    
                        Host.Log(""Calculating sum..."");

                        //throw new Exception(""Test exception""); //uncomment to test the StackTrace output

                        return a + b;
                   }
                   catch (Exception e)
                   {
                        // Create a StackTrace that captures filename, line number and column information.
                        var st = new System.Diagnostics.StackTrace(true);
                        string stackIndent = """";
                        for(int i =0; i< st.FrameCount; i++ )
                        {
                            // Note that at this level, there are four 
                            // stack frames, one for each method invocation.
                            var sf = st.GetFrame(i);
                            Console.WriteLine();
                            Console.WriteLine(stackIndent + "" Method: {0}"", sf.GetMethod() );
                            Console.WriteLine(  stackIndent + "" File: {0}"", sf.GetFileName());
                            Console.WriteLine(  stackIndent + "" Line Number: {0}"", sf.GetFileLineNumber());
                            stackIndent += ""  "";
                        }

                     throw ;
                    }
              }", null, true))
              .GetStaticMethod();

        try
        {
            int result = (int)Sum(1, 2);
        }
        catch (Exception)
        {
        }
    }
}

