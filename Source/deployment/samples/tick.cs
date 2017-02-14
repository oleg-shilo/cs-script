using System;
using System.Windows.Forms;
using System.Threading;

namespace  CSScript
{
	class Ticker
	{
		const string usage = "Usage: cscscript tick [seconds] ...\nCounts specified number of seconds (default is 5 sec). This script is used in the importTickScript sample. \n";

		static public void Main (string[] args)
		{
			if ((args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
			{
				Console.WriteLine(usage);
			}
			else
			{
				int count = 5;
				if (args.Length != 0)
				{
					try
					{
						count = Convert.ToInt32(args[0]);
					}
					catch 
					{
						Console.WriteLine("Error: Invalid parameter");
					}
				}

				for (int i = 0; i < count; i++)
				{
					Console.WriteLine("tick");
					Thread.Sleep(1000);
				}
			}
		}
	}
}