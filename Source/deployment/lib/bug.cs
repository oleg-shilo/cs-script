using System;
using System.Diagnostics;

public class Script
{
	[STAThread]
	static public void Main(string[] args)
	{
		if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
			Console.WriteLine("Usage: cswscript bug ...\nPrepares bug report e-mail and loades it into default e-mail client.\n");
		else
			Process.Start("mailto:csscript.support@gmail.com?subject=Bug report"); 
	}
}

