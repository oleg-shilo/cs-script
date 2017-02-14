using System;
using System.Reflection;
using System.Windows.Forms;

class Script
{
	static public void Main(string[] args)
	{
		if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
		{
			Console.WriteLine("Usage: cscscript ver \nPrints version of OS, CLR and CS-Script runtime.\n");
			return;
		}

		Console.WriteLine("OS version - " + Environment.OSVersion.ToString());
		Console.WriteLine("Target CLR version - "+ Environment.Version.ToString());
		Console.WriteLine("CS-Script version - " + Assembly.GetCallingAssembly().GetName().Version.ToString());
	}
}

