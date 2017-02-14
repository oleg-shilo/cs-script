using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Principal;

internal class VistaCSS
{
	static void Main(string[] args)
	{
		if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
		{
			Console.WriteLine("Usage: This script is to be used as a pre-script in the primary script code to ensure elevated execution.\n"+
			"Example: //css_pre elevate();\n");
			return;
		}
		
		if (RestartElevated())
		{
			Process.GetCurrentProcess().Kill();
		}
	}
	internal static bool RestartElevated()
	{
		if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
		{
			string args = "";
			string[] arguments = Environment.GetCommandLineArgs();
			
			for (int i = 1; i < arguments.Length; i++)
				args += "\"" + arguments[i] + "\" ";

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.UseShellExecute = true;
			startInfo.WorkingDirectory = Environment.CurrentDirectory;
			startInfo.FileName = Application.ExecutablePath;
			startInfo.Arguments = args;
			startInfo.Verb = "runas";
			
			Process.Start(startInfo);
			return true; 
		}
		else
			return false;

	}
}

