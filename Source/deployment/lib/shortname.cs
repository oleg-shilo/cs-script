using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

class Script
{
	[DllImport("kernel32.dll")]
	static extern uint GetShortPathName(string lpszLongPath, [Out] StringBuilder lpszShortPath, uint cchBuffer);

	const string usage =	"Usage: cscscript shortName [path]...\nConvert and prints 8.3 version of the path.\n"+
							"<path> - Path to be converted. If not specified the current directory will be used.\n";

	static public void Main(string[] args)
	{
		if (args.Length > 1 || (args.Length == 1 && (args[0].ToLower() == "-?" || args[0].ToLower() == "/?")))
			Console.WriteLine(usage);
		else
			try
			{
				string path = (args.Length == 0) ? Environment.CurrentDirectory : Path.GetFullPath(args[0]);

				StringBuilder shortNameBuffer = new StringBuilder(556);
				GetShortPathName(path, shortNameBuffer, (uint)shortNameBuffer.Capacity);
				Console.WriteLine("Long name: "+path);
				Console.WriteLine("Short name: "+shortNameBuffer.ToString());
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
	}
}

