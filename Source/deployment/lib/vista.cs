using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Principal;

internal class VistaCSS
{
	static void Main()
	{
		Console.WriteLine("Usage: This script is to be imported/included by other scripts. To resturt the script execution as an elevated process.\n"+
		"Example: \n"+
		"static public void Main(string[] args)\n"+
	    "{\n"+
	     "   if (VistaCSS.RestartElevated())\n"+
	     "      return;\n");
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

