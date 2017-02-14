//css_pre com(WScript.Shell.*, swshell.dll);
using System;
using System.IO;
using swshell;

class Script
{
	static public void Main(string[] args)
	{
		if ((args.Length == 0) ||
			(args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(	"Usage: cscscript CreateShortcut file [/c] ...\n"+
								"Creats execution shortcut to the script file.\n"+
								" file - script file path\n" +
								" /c   - console window\n");
		}
		else
		{
			string script = Path.GetFullPath(args[0]);
			string engine = @"csws.exe";
			string shortcut = Path.ChangeExtension(script, ".lnk");

			if (args.Length > 1 && args[1].ToLower() == "/c")
				engine = @"cscs.exe";

			IWshShortcut sc = (IWshShortcut)new WshShellClass().CreateShortcut(shortcut);
			sc.TargetPath = "\"" + engine + "\"";
			sc.Arguments = "\"" + script + "\"";
			sc.IconLocation = sc.TargetPath;
			sc.WorkingDirectory = Path.GetDirectoryName(script);
			sc.Description = "C# Script Shortcut";
			sc.Save();
		}
	}
}