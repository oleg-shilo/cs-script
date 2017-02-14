using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections;

class Script
{

	const string usage = "Usage: cscscript libhelp [/s] ...\nPrints usage info for all C# scripts (.cs files) in the cs-script\\lib directory\n"+
						 "/s - switch to print help for scripts from cs-script\\samples directory\n"	;
	static string[] exclude = new string[]
		{
			"CSSCodeProvider.cs",
			"ccscompiler.cs",
			"cppcompiler.cs",
			"xamlcompiler.cs",
			"GoogleSearchService.cs"
		};
	static public void Main(string[] args)
	{
		if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
		{
			Console.WriteLine(usage);
		}
		else
		{
			try
			{
				string dir = "lib";
				if (args.Length == 1 && args[0].ToLower() == "/s")
					dir = "samples";
				//Environment.CurrentDirectory = dir;	
				foreach (string file in Directory.GetFiles(Path.Combine(Environment.GetEnvironmentVariable("CSSCRIPT_DIR"), dir), "*.cs"))
				{
					if (null != Array.Find<string>(exclude, delegate(string obj) { return string.Compare(obj, Path.GetFileName(file), true) == 0; }))
						continue;

					Console.WriteLine("************************** ");
					Console.WriteLine("< " + Path.GetFileName(file) + " >");
					RunScript("\"" + file + "\" /?");
					System.Threading.Thread.Sleep(100);
				}
			}
			catch (Exception e)
			{
			}		
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
