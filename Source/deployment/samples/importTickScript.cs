using System;

//css_import tick, rename_namespace(CSScript, TickScript);

namespace CSScript
{
	class TickImporter
	{
		const string usage = "Usage: cscscript importTickScript ...\nImports and execues 'tick.cs' script.\n";

		static public void Main(string[] args)
		{
			if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
			{
				Console.WriteLine(usage);
			}
			else
			{
				TickScript.Ticker.i_Main(args);
			}
		}
	}
}