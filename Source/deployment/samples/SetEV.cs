//css_pre com(WScript.Shell.*, swshell.dll);
using System;
using swshell;

namespace Scripting
{
    class Script
    {
		static string usage = "This script is an example of the setting the Environment variables permanently.\n";
		
        static public void Main(string[] args)
        {
			if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
			{
				Console.WriteLine(usage);
				return;
			}
			
            object envType = "SYSTEM";
            IWshEnvironment wshSysEnv = new WshShellClass().get_Environment(ref envType);
            wshSysEnv["TEST"] = "MyDirectory";
        }
    }
}