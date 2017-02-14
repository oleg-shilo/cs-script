using System;
using System.IO;

class Script
{

	const string usage = "Usage: cscscript lib \n\tShows Script Library content.\n";

	static public void Main(string[] args)
	{
		if ((args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(usage);
		}
		else
		{
			string homeDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");
			if (homeDir == null)
				Console.WriteLine("Feature is not available.\nC# Script engine was not installed properly.");
			else
				foreach(string fileName in Directory.GetFiles(Path.Combine(homeDir, "lib"), string.Format("*.cs")))
					Console.WriteLine(fileName);
		}
	}
}