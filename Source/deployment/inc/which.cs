//css_args /nl
using System;
using System.IO; 
using System.Text; 
using Microsoft.Win32;
using System.Diagnostics;

class Script
{
	const string usage = "Usage: cscscript which file\nVerifies which copy of the executable file would be executed if invoked from command-prompt.\n"+
						 "file - name of the executable file\n";

	static public void Main(string[] args)
	{
		if (args.Length == 0 || 
			(args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(usage);
		}
		else 
		{
			string file = args[0].EndsWith(".exe") ? args[0] : args[0]+".exe";
			string filePath = file;

			if (File.Exists(file))
				Console.WriteLine(Path.GetFullPath(filePath));
			else			
				foreach(string dir in Environment.GetEnvironmentVariable("Path").Split(";".ToCharArray()))
				{
					if (File.Exists(filePath = Path.Combine(dir, file)))
					{
						Console.WriteLine(filePath);
						break;
					}
				}
		}
	}
}

