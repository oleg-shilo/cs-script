//css_args /nl
using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Security;


class Script
{
	const string usage = "Usage: cscscript runas <file> [arg0...argN]...\nStarts process as a specified (follow the prompt) user.\n"+
						 "Note: this script can be run only on .NET 2.0 or higher\n";

	[STAThread]
	static public void Main(string[] args)
	{
		try
		{
			if (args.Length == 0 || args[0].ToLower() == "-?" || args[0].ToLower() == "/?")
			{
				Console.WriteLine(usage);
			}
			else
			{
				ProcessStartInfo psi = new ProcessStartInfo(args[0]);

				Console.Write(@"User Name [domain\user]: ");
				string[] userParts = Console.ReadLine().Split('\\');
				if (userParts.Length == 2)
				{
					psi.Domain = userParts[0];
					psi.UserName = userParts[1];
				}
				else
					psi.UserName = userParts[0];

				psi.Password = ReadPassword();
				psi.UseShellExecute = false;
				for (int i = 1; i < args.Length; i++)
					psi.Arguments += (i == 1 ? "\"" : " \"") + args[i] + "\"";

				Process.Start(psi);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}
	public static SecureString ReadPassword()
	{
		Console.Write("Password: ");
		SecureString secPass = new SecureString();
		ConsoleKeyInfo key = Console.ReadKey(true);
		while (key.KeyChar != '\r')
		{
			secPass.AppendChar(key.KeyChar);
			key = Console.ReadKey(true);
		}
		return secPass;
	} 
}

