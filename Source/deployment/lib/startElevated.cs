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
			Console.WriteLine("Usage: cscscript startElevated <script>\nStarts script as an elevated process.\n");
			return;
		}
		StartElevated(args);
	}
	internal static bool StartElevated(string[] arguments)
	{
		string args = "";
		for (int i = 0; i < arguments.Length; i++)
			args += "\"" + arguments[i] + "\" ";

		ProcessStartInfo startInfo = new ProcessStartInfo();
		startInfo.UseShellExecute = true;
		startInfo.WorkingDirectory = Environment.CurrentDirectory;
		startInfo.FileName = Application.ExecutablePath;
		startInfo.Arguments = args;

		if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
			startInfo.Verb = "runas";

		Process.Start(startInfo);
		return true;
	}
}

