using System;
using System.EnterpriseServices;
using System.DirectoryServices;
using System.Windows.Forms;
using System.IO;
using CSScriptLibrary;

namespace Script
{
	class Script
	{
		const string usage =	"Usage: cscscript css2ws file <class | /r> ...\nConverts the C# script into WebService on the local WebServer (http:////localhost//).\n" +
								"file - C# script file.\n" +
								"class - name of the class implementing WebService in the C# script file.\n"+
								"/r - remove WebService previously created from the C# script file.\n\n"+
								"Note: The part of the conversion is the creation of the Virtual Directory with the same name as the name of the C# script file.\n";
		[STAThread]
		static public void Main(string[] args)
		{
			try
			{
				if (args.Length != 2 || args[0].ToLower() == "-?" || args[0].ToLower() == "/?")
					Console.WriteLine(usage);
				else
				{
					string script = (Path.GetExtension(args[0]) == string.Empty ? Path.GetFullPath(args[0] + ".cs") : Path.GetFullPath(args[0]));
					string className = args[1];
					string virtualDir = Path.GetFileNameWithoutExtension(script);
					string physicalDir = Path.Combine(Path.GetDirectoryName(script), virtualDir + "_WS");
					string binDir = Path.Combine(physicalDir, "bin");
					string asm = Path.ChangeExtension(script, ".dll");
					string binAsm = Path.Combine(binDir, Path.GetFileNameWithoutExtension(script) + ".dll");

					if (args[1].ToLower() == "/r")
					{
						Console.WriteLine("Deleting Virtual Directory...");
						DeleteVirtualDirectory(virtualDir, physicalDir);
						return;
					}
					Console.WriteLine("Creating Virtual Directory...");
					CreateVirtualDirectory(virtualDir, physicalDir);

					if (!Directory.Exists(binDir))
						Directory.CreateDirectory(binDir);

					Console.WriteLine("Compiling assembly...");
					CSScriptLibrary.CSScript.Execute(new PrintDelegate(Print), new string[] { "/nl", "/cd", script });

					if (!File.Exists(asm))
						throw new Exception("Cannot compile " + script + " into assembly.");

					Console.WriteLine("Copying files...");
					
					if (File.Exists(binAsm))
						File.Delete(binAsm);
					
					File.Move(asm, binAsm);

					using (StreamWriter sw = new StreamWriter(Path.Combine(physicalDir, virtualDir + ".asmx")))
						sw.WriteLine("<%@ WebService Language=\"c#\" Codebehind=\"..\\" + Path.GetFileName(script) + "\" Class=\"" + className + "\" %>");

					Console.WriteLine("\nThe script " + Path.GetFileName(script) + " has been converted to the WebService located in the " + physicalDir + " directory.\n\n" +

						"Use the following URL to test the service: \n" +
						"http://localhost/" + virtualDir + "/" + virtualDir + ".asmx\n\n" +

						"Use the following CS-Script directive in C# code to access the service:\n" +
						"//css_prescript wsdl(http://localhost/" + virtualDir + "/" + virtualDir + ".asmx?WSDL, " + virtualDir + "Service);\n" +
						"//css_imp " + virtualDir + "Service;");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		public static void CreateVirtualDirectory(string name, string path)
		{
			string err;
			new System.EnterpriseServices.Internal.IISVirtualRoot().Create("IIS://localhost/W3SVC/1/Root", path, name, out err);
			if (err != string.Empty)
				throw new Exception(err);
		}
		public static void DeleteVirtualDirectory(string name, string path)
		{
			string err;
			new System.EnterpriseServices.Internal.IISVirtualRoot().Delete("IIS://localhost/W3SVC/1/Root", path, name, out err);
			if (err != string.Empty)
				throw new Exception(err);
		}
		static void Print(string msg)
		{
			Console.Write(msg);
		}
	}
}
