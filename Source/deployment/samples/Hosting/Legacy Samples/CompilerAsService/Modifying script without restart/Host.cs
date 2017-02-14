//css_ref plugin.interface.dll;
//css_nuget cs-script;
using MyCompany;
using System; 
using System.Collections.Generic;
using System.IO;
using CSScriptLibrary;
using System.Linq;
using System.Text;  
using System.Threading.Tasks;

//If you want to run this sample with EvaluatorEngine.CodeDom you will need to use 

namespace ConsoleApplication31
{
    class Program 
    {
        static void Main()
        {
            SimpleHost.Test();
            //ComplexHost.Test();
        }
    }

    class SimpleHost : IHost
    {
        public static void Test()
        {
            new SimpleHost().Run();
        }

        void Run()
        {
            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn;

            var scripts = @"pluginA.cs;pluginB.cs".Split(';');

            Console.WriteLine("Enter index of the plugin to invoke to 'e' to exit: ");
            for (int i = 0; i < scripts.Length; i++)
                Log("  " + i + " - " + Path.GetFileNameWithoutExtension(scripts[i]));

            while (true)
            {
                var input = Console.ReadLine();
                if (input == "e")
                    return;

                int index = -1;
                if (int.TryParse(input, out index) && index < scripts.Length)
                {
                    var script = scripts[index];

                    var plugin = (IPlugin) CSScript.Evaluator
                                                   .LoadFile(script);
                    plugin.Init(this);
                    plugin.Log("test");
                }
                else
                    Log("error: Invalid input");
            }

            Log("Bye...");
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

    class ComplexHost : MarshalByRefObject, IHost
    {
        public static void Test()
        {
            new ComplexHost().Run();
        }

        void Run()
        {
            //CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;

            var scripts = @"..\..\pluginA.cs;..\..\pluginB.cs".Split(';');

            Console.WriteLine("Enter index of the plugin to invoke to 'e' to exit: ");
            for (int i = 0; i < scripts.Length; i++)
                Log("  " + i + " - " + Path.GetFileNameWithoutExtension(scripts[i]));

            while (true)
            {
                var input = Console.ReadLine();
                if (input == "e")
                    return;

                int index = -1;
                if (int.TryParse(input, out index) && index < scripts.Length)
                {
                    var script = scripts[index];

                    var plugin = CSScript.Evaluator
                                         .LoadFileRemotely<IPlugin>(script);

                    plugin.Init(this);
                    plugin.Log("test");
                    script.UnloadOwnerDomain();
                }
                else
                    Log("error: Invalid input");
            }

            Log("Bye...");
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
