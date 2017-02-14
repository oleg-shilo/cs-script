using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.Diagnostics;

class Script
{
	const string usage = "Usage: cscscript sample [sampleIndex]:[sampleName] [outputFile]\n\tCreates copy of a sample file.\n\n"+
	"Example: cscscript sample MailTo \n"+
	"         cscscript sample 3 \n";

	static public void Main(string[] args)
	{
		if ((args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(usage);
		}
		else
		{
			string sampleDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");
			if (sampleDir == null)
			{
				Console.WriteLine("Feature is not available.\nC# Script engine was not installed properly.");
				return;
			}
			
			ArrayList sampleFiles = null;
			GetSamples(Path.Combine(sampleDir, "Samples"), ref sampleFiles);
	
			if (args.Length == 0)
			{
				Console.WriteLine(usage);
				ShowSamples(sampleFiles);
			}
			else if (args[0] == "?")
			{
				Console.WriteLine(usage);
			}
			else 
			{
				if ((IsNumeric(args[0]) && Convert.ToInt32(args[0]) == 0) || 
					args[0].ToUpper() == "default")
				{
					CreateDefaultSample((args.Length > 1) ? args[1] : "default.cs");
				}
				else
				{
					string sampleFile = null;
					if (IsNumeric(args[0]))
					{
						sampleFile = (string)sampleFiles[Convert.ToInt32(args[0]) - 1];
					}
					else
					{
						string sampleName = args[0].EndsWith(".cs") ? args[0] : args[0] + ".cs";
						foreach (string file in sampleFiles)
						{
							if (file.ToLower().EndsWith(Path.ChangeExtension(sampleName.ToLower(), ".cs")))
							{
								sampleFile = file;
							}
						}
					}
				
					if (sampleFile != null)
					{
				
						FileInfo flInfo = new FileInfo(sampleFile);
						string outputFile = (args.Length > 1) ? args[1] : flInfo.Name;

						if (!sampleFile.EndsWith(".cs"))
						{
							sampleFile += ".cs";
						}
						if (!outputFile.EndsWith(".cs"))
						{
							outputFile += ".cs";
						}
						try
						{
							if (File.Exists(outputFile))
							{
								DialogResult res = MessageBox.Show(
									"The file "+Path.GetFileName(outputFile)+" already exist. Do you want to replace it?\n\n"+
									"Click 'Yes' to replace the file, 'No' to create a new copy of the "+Path.GetFileName(outputFile)+
									" or 'Cancel' to cancel the operation.", "CS-Script", MessageBoxButtons.YesNoCancel);
								if (res == DialogResult.Cancel)
									return;
								else if (res == DialogResult.No)
									outputFile = Path.Combine(Path.GetDirectoryName(outputFile), "Copy of "+Path.GetFileName(outputFile)); 
								else
									File.Delete(outputFile);
							}
							File.Copy(sampleFile, outputFile);
							Console.WriteLine("File {0} has been created.", outputFile);
						}
						catch(Exception e)
						{
							Console.WriteLine("File {0} cannot be created.\n{1}", outputFile, e.Message);
						}
					}
					else
					{
						Console.WriteLine("Error: invalid argument specified.\n");
					}
				}
			}
		}
	}
	
	static void CreateDefaultSample(string outputFile)
	{
		Process myProcess = new Process();
		myProcess.StartInfo.FileName = "cscs.exe";
		myProcess.StartInfo.Arguments = "/s";
		myProcess.StartInfo.UseShellExecute = false;
		myProcess.StartInfo.RedirectStandardOutput = true;
		myProcess.StartInfo.CreateNoWindow = true;
		myProcess.Start();
		
		StringBuilder builder = new StringBuilder();
		string line = null;
		while (null != (line = myProcess.StandardOutput.ReadLine()))
		{
			builder.Append(line);
			builder.Append("\r\n");
		}
		myProcess.WaitForExit();
				
		using (StreamWriter sw = new StreamWriter(outputFile)) 
		{
			sw.Write(builder.ToString());
		}
		Console.WriteLine("File {0} has been created.", outputFile);
	}

	static bool IsNumeric(string stringVal) 
	{
		try
		{
			Convert.ToInt32(stringVal);
			return true;
		}
		catch 
		{
			return false;
		}
	}
	static void ShowSamples(ArrayList sampleFiles) 
	{
		Console.WriteLine("Available samples:\n");
		Console.WriteLine("0\t- Default <dafault code sceleton>");
		int index = 1;
		string currentSampleDir = null;
		foreach (string sampleFile in sampleFiles)
		{
			FileInfo flInfo = new FileInfo(sampleFile);
			string dir = flInfo.Directory.FullName;
			if (currentSampleDir == null || currentSampleDir != dir)
			{
				Console.WriteLine("\n{0}", dir);
				currentSampleDir = dir;
			}
			Console.WriteLine("{0}\t- {1}", index, flInfo.Name);
			index++;
		}
	}

	static void GetSamples(string sampleDir, ref ArrayList sampleFiles) 
	{
		try 
		{
			if (sampleFiles == null)
			{
				sampleFiles = new ArrayList();
			}
			foreach(string fileName in Directory.GetFileSystemEntries(sampleDir, string.Format("*.cs")))
			{
				sampleFiles.Add(fileName);
			}
			
			foreach (string subDir in Directory.GetDirectories(sampleDir)) 
			{
				GetSamples(subDir, ref sampleFiles);
			}
		}
		catch (System.IO.IOException) 
		{
			System.Console.WriteLine("An I/O error occurs.");
		}
		catch (System.Security.SecurityException) 
		{
			System.Console.WriteLine("The caller does not have the " +
				"required permission.");
		}
	}
}
