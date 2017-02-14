using System;
using System.IO; 
using System.Text; 
using Microsoft.Win32;
using System.Diagnostics;

class Script
{
	const string usage = "Usage: cscscript vrify file...\nVerifies if script file can be compiled.\n"+
						 "</i> / </u> - command switch to install/uninstall shell extension\n";

	static public void Main(string[] args)
	{
		System.Diagnostics.Debug.Assert(false);
		if (args.Length == 0 || 
			(args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(usage);
		}
		else if (args.Length == 1 && (args[0].ToLower() == "/u" || args[0].ToLower() == "/i"))
		{
			Install(args[0].ToLower() == "/i");
		}
		else
		{
			string output = CompileScript(args[0]);
			
			Console.WriteLine(output);
						
			if (output == "")
				Console.WriteLine("\nScript compiled OK\n");

			Console.Write("\nPress Enter to continue...");
			Console.ReadLine();
		}
	}

	static string CompileScript(string scriptFile)
	{
		string retval = "";
		StringBuilder sb = new StringBuilder();

		Process myProcess = new Process();
		myProcess.StartInfo.FileName = "cscs.exe";
		myProcess.StartInfo.Arguments = "/nl /ca \"" + scriptFile + "\"";
		myProcess.StartInfo.UseShellExecute = false;
		myProcess.StartInfo.RedirectStandardOutput = true;
		myProcess.StartInfo.CreateNoWindow = true;
		myProcess.Start();
		
		string line = null;
		while (null != (line = myProcess.StandardOutput.ReadLine()))
		{
			sb.Append(line);
			sb.Append("\n");
		}
		myProcess.WaitForExit();
		
		retval = sb.ToString();

		string compiledFile = Path.ChangeExtension(scriptFile, ".csc");
		
		if (retval == "" && File.Exists(compiledFile))
			File.Delete(compiledFile);

		return retval;
	}

	static void Install(bool install)
	{
		string fileTypeName = ".cs";
		RegistryKey csFile = Registry.ClassesRoot.OpenSubKey(".cs");	

		if (csFile != null)
			fileTypeName = (string)csFile.GetValue("");

		if (install)
		{
			RegistryKey shell = Registry.ClassesRoot.CreateSubKey(fileTypeName+"\\shell\\Verify CSScript\\command");
			string regValue = "\"" + Path.Combine(Environment.GetEnvironmentVariable("CSSCRIPT_DIR"), "cscs.exe") + "\" \"" + Path.Combine(Environment.CurrentDirectory, "verify.cs") + "\" \"%1\"";
			shell.SetValue("", regValue);
			shell.Close();
		}
		else
		{
			try
			{
				Registry.ClassesRoot.DeleteSubKeyTree(fileTypeName+"\\shell\\Verify CSScript");
			}
			catch{}
		
		}
	}
}

