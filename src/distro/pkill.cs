//css_args /nl
using System;
using System.Diagnostics;

class Script
{
	const string usage = "Usage: nkill.cs [process name #0] [process name #1] ...\nTerminates processes\n";

	static public void Main(string[] args)
	{
		if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(usage);
		}
		else
		{
			foreach(string prName in args)
			{
				try
				{
					Process [] processByName = Process.GetProcessesByName(prName);
					foreach(Process pr in processByName)
						pr.Kill();
				}
				catch
				{
					Console.WriteLine("Cannot terminate " + prName + "\n");
				}
			}
		}
	}
}

