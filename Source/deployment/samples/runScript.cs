using System;
using System.IO; 
using System.Data; 
using System.Data.SqlClient;
using System.Xml;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;


class Script
{
	const string usage = "Usage: cscscript runScript scriptFile [argiment0] [argiment1]...[argimentN] ...\nRuns script in background.\n";

	static public void Main(string[] args)
	{
		try 
		{
			if (args.Length == 0 || 
				(args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
			{
				Console.WriteLine(usage);
			}
			else
			{
				Console.WriteLine("Script '{0}' is started", args[0]);
				
				string scriptFileCmd = "";
				for (int i = 0; i < args.Length; i++)
				{	
					if (i == 0)
						scriptFileCmd += " \"" + (Path.IsPathRooted(args[i]) ? args[i] : Path.Combine(Environment.CurrentDirectory, args[i])) + "\"";
					else
						scriptFileCmd += " " + args[i];
				}

				RunScript(scriptFileCmd);
				Console.WriteLine("Script is completed");
			}
			
		} 
		catch (Exception e) 
		{
			Console.WriteLine("Analysising was terminated because of error.\n{0}", e.ToString());
		}
	}

	static void RunScript(string scriptFileCmd)
	{
		Process myProcess = new Process();
		myProcess.StartInfo.FileName = "cscs.exe";
		myProcess.StartInfo.Arguments = "/nl " + scriptFileCmd;
		myProcess.StartInfo.UseShellExecute = false;
		myProcess.StartInfo.RedirectStandardOutput = true;
		myProcess.StartInfo.CreateNoWindow = true;
		myProcess.Start();
		
		string line = null;
		while (null != (line = myProcess.StandardOutput.ReadLine()))
		{
			Console.WriteLine(line);
		}
		myProcess.WaitForExit();
	}
}

