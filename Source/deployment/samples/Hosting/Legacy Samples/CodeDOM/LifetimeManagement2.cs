//css_args -inmem:0
using System.Runtime.Remoting.Lifetime;
using System;
using System.Threading;
using CSScriptLibrary;

// This hosting sample can only work from the file-full host assemblies. Thus the host must be either 
// an application or a script executed with -inmem:0 switch

public interface IScript
{
    void Hello(string greeting);
}

class Host
{
    const string code = @"using System;

                         public class Script : MarshalByRefObjectWithInfiniteLifetime
                         {
                             public void Hello(string greeting)
 	                         {
 	                             Console.WriteLine(greeting);
 	                         }
                         }";

    static void Main()
    {
        using (var helper = new AsmHelper(CSScript.CompileCode(code), null, false))
        {
            IScript script = helper.CreateAndAlignToInterface<IScript>("*");

            try
            {
                script.Hello("Hi there...");
                Thread.Sleep(1000 * 60 * 7); // 7 minutes
                script.Hello("Hi there again...");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        Console.ReadLine();
    }
}