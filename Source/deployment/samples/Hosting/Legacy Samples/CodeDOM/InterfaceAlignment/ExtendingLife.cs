using System;
using System.Threading;
using CSScriptLibrary;
using csscript;

public interface IScript
{
    void Hello(string greeting);
}

class Host
{
    const string code = @"using System;

                         public class Script : MarshalByRefObject
                         {
                             public void Hello(string greeting)
 	                         {
 	                             Console.WriteLine(greeting);
 	                         }
                         }";

    static void Main()
    {
        TestLifeExtension();
        
        Console.ReadLine();
    }

    static void TestLifeExtension()
    {
        using (var helper = new AsmHelper(CSScript.CompileCode(code), null, false))
        {
            IScript script = helper.CreateAndAlignToInterface<IScript>("*");

            int minutes = 1;

            helper.RemoteObject.ExtendLifeFromMinutes(minutes);
            (script as MarshalByRefObject).ExtendLifeFromMinutes(minutes);

            try
            {
                Thread.Sleep(1000 * 60 * 2); //call before 7 minutes expire
                script.Hello("Hi TestLifeExtension1...");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in TestLifeExtension1: " + e.Message);
            }
        }
    }
}