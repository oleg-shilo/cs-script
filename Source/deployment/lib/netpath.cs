using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

class Script
{
	[DllImport("Kernel32.dll")]
	public static extern int SetEnvironmentVariable(string name, string value);   

	static public void Main(string[] args)
	{
		if (args.Length == 1 && (args[0].ToLower() == "-?" || args[0].ToLower() == "/?"))
			Console.WriteLine("Usage: cscscript netpath ...\nSets %WINDOWS%\\Microsoft.NET\\Framework\\<current version> to the system PATH for the current process.\n");
		else
		{
			string[] version = Environment.Version.ToString().Split(".".ToCharArray());
			string sdkDir = "";
			
			if (version[0] == "1")
				sdkDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio .NET 2003\\SDK");
			else if (version[0] == "2")
				sdkDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio 8\\SDK");
			sdkDir = Path.Combine(sdkDir, "v"+version[0]+"."+version[1]+@"\bin");

			if (Directory.Exists(sdkDir))
				SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Path.GetDirectoryName(typeof(string).Assembly.Location) + ";" + sdkDir);
			else
				SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Path.GetDirectoryName(typeof(string).Assembly.Location));
		}
	}
}
