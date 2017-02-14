using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

class Script
{
	static string usage = "Usage: cscscript help ...\nDisplays the CS-Script Help.\n";
	
	static public void Main(string[] args)
	{
		if (args.Length == 1 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
			Console.WriteLine(usage);
		else if ("CSSCRIPT_DIR" != null && System.IO.File.Exists(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Docs\Help\CSScript.chm")))
		{
			Process myProcess = new Process();
			myProcess.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Docs\Help\CSScript.chm");
			myProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(myProcess.StartInfo.FileName);
			myProcess.Start();
		}
		else
			MessageBox.Show("CS-Script Help is not installed.");
	}
}

