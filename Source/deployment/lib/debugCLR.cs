using System;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using System.Xml;
using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using CSScriptLibrary;


namespace CLRDebugger
{
	class Script
	{
		static string usage = "Usage: cscscript debugCLR [file]|[/i|/u] ...\nLoads C# script compiled executable into MS CLR Debugger (DbgCLR.exe).\n</i> / </u> - command switch to install/uninstall shell extension\n";

		static public void Main(string[] args)
		{
            if (CLRDE.GetAvailableIDE() == null)
            {
                MessageBox.Show("CLR Debugger cannot be found.");
                return;
            }

			if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
			{
				Console.WriteLine(usage);
			}
			else if (args[0].Trim().ToLower() == "/i")
			{
				CLRDE.InstallShellExtension();
			}
			else if (args[0].Trim().ToLower() == "/u")
			{
				CLRDE.UninstallShellExtension();
			}
			else
			{
				try
				{
					FileInfo info = new FileInfo(args[0]);
					string scriptFile = info.FullName;
					string outputFile = Path.Combine(Path.GetDirectoryName(scriptFile), Path.GetFileNameWithoutExtension(scriptFile) + ".exe");
					bool existed = File.Exists(outputFile);

					RunScript(" /e /dbg \"" + info.FullName + "\"");

					if (File.Exists(outputFile))
					{
						//open executable
						Environment.CurrentDirectory = Path.GetDirectoryName(scriptFile);

						Process myProcess = new Process();
						myProcess.StartInfo.FileName = CLRDE.GetIDEFile();
						myProcess.StartInfo.Arguments = "\"" + outputFile + "\" ";
						myProcess.Start();
						myProcess.WaitForExit();

						//do clean up
						if (!existed)
						{
							if (File.Exists(outputFile))
								File.Delete(outputFile);
							if (File.Exists(Path.GetFileNameWithoutExtension(outputFile) + ".pdb"))
								File.Delete(Path.GetFileNameWithoutExtension(outputFile) + ".pdb");
						}
					}
					else
					{
						if (File.Exists(Path.GetFileNameWithoutExtension(outputFile) + ".pdb"))
							File.Delete(Path.GetFileNameWithoutExtension(outputFile) + ".pdb");
						MessageBox.Show("Script cannot be compiled into excutable.");
					}

				}
				catch (Exception e)
				{
					MessageBox.Show("Specified file could not be opened\n" + e.Message);
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

		public class CLRDE
		{
			static public string GetIDEFile()
			{
                string retval = "<not defined>";

				try
				{
					RegistryKey DbgClr = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DbgClr");

					if (DbgClr == null)
						return retval;

					double ver = 0.0;
					string subKeyStr = "";
					foreach (string keyStr in DbgClr.GetSubKeyNames())
					{
						subKeyStr = keyStr;
						double currVer = Convert.ToDouble(keyStr, System.Globalization.CultureInfo.InvariantCulture);
						ver = Math.Max(currVer, ver);
					}

					if (ver != 0.0)
					{
						string debuggerDir = DbgClr.OpenSubKey(subKeyStr).GetValue("InstallDir").ToString();
						retval = Path.Combine(debuggerDir, "DbgCLR.exe");
					}
				}
				catch
				{ 
				}
				return retval;
			}
			static public string[] GetAvailableIDE()
			{
				if (GetIDEFile() != "<not defined>")
				{
					string scHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");

					return new string[] { 
						"Open (CLRDebug)",
						"\t- Open with MS CLR Debugger",
						"\"" + scHomeDir + "\\csws.exe\" /c \"" + scHomeDir + "\\lib\\debugCLR.cs\" \"%1\""};

				}
				return null;

			}

			static public void InstallShellExtension()
			{
				string fileTypeName = null;
				RegistryKey csFile = Registry.ClassesRoot.OpenSubKey(".cs");

				if (csFile != null)
				{
					fileTypeName = (string)csFile.GetValue("");
				}
				if (fileTypeName != null)
				{
					//Shell extensions
					Console.WriteLine("Create 'Open as script (CLR Debgger)' shell extension...");
					RegistryKey shell = Registry.ClassesRoot.CreateSubKey(fileTypeName + "\\shell\\Open as script(CLR Debgger)\\command");
					string scHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");
					if (scHomeDir != null)
					{
						string regValue = "\"" + Path.Combine(scHomeDir, "csws.exe") + "\"" +
							" /c " +
							"\"" + Path.Combine(scHomeDir, "lib\\DebugCLR.cs") + "\" " +
							"\"%1\"";


						shell.SetValue("", regValue);
						shell.Close();
					}
					else
						Console.WriteLine("\n" +
											"The Environment variable CSSCRIPT_DIR is not set.\n" +
											"This can be the result of running the script from the Total Commander or similar utility.\n" +
											"Please rerun install.bat from Windows Explorer or from the new instance of Total Commander.");

				}
			}
			static public string GetEnvironmentVariable(string name)
			{
				//It is important in the all "installation" scripts to have reliable GetEnvironmentVariable().
				//Under some circumstances freshly set environment variable CSSCRIPT_DIR cannot be obtained with
				//Environment.GetEnvironmentVariable(). For example when running under Total Commander or similar 
				//shell utility. Even SendMessageTimeout does not help in all cases. That is why GetEnvironmentVariable
				//is reimplemented here.
				object value = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment").GetValue(name);
				return value == null ? null : value.ToString();
			}
			static public void UninstallShellExtension()
			{
				string fileTypeName = null;
				RegistryKey csFile = Registry.ClassesRoot.OpenSubKey(".cs");

				if (csFile != null)
				{
					fileTypeName = (string)csFile.GetValue("");
				}
				if (fileTypeName != null)
				{
					try
					{
						if (Registry.ClassesRoot.OpenSubKey(fileTypeName + "\\shell\\Open as script(CLR Debgger)") != null)
						{
							Console.WriteLine("Remove 'Open as script (CLR Debgger)' shell extension...");
							Registry.ClassesRoot.DeleteSubKeyTree(fileTypeName + "\\shell\\Open as script(CLR Debgger)");
						}
					}
					catch { }
				}
			}
		}
	}
}