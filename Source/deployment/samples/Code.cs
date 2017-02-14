using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections;

class Script
{

	const string usage = "Usage: cscscript code [/ns:[namsspaceN1;]...[namsspaceN;]][statement] ...\nExecutes C# statement with optional referenced namespaces.\n Example: cscscript code MessageBox.Show("+@"\""test\"""+") \n";

	static public void Main(string[] args)
	{
		if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(usage);
		}
		else
		{
			ArrayList namespaces = new ArrayList();
			namespaces.Add("System");
			namespaces.Add("System.Windows.Forms");
			namespaces.Add("System.Xml");
			namespaces.Add("System.IO");
			namespaces.Add("System.Diagnostics");
			namespaces.Add("System.Text");
			//add more default namespaces if required

			StringBuilder builder = new StringBuilder();
			foreach(string statement in args)
			{
				if (statement.StartsWith("/ns:"))
					namespaces.AddRange(statement.Substring("/ns:".Length).Split(";".ToCharArray()));
				else
					builder.Append(statement);
			}
			string scriptText = ComposeScript(builder.ToString(), (string[])namespaces.ToArray(typeof(string)));

			string fileName = Path.GetTempFileName();
			try
			{
				if (File.Exists(fileName))
					File.Delete(fileName);
				fileName = Path.ChangeExtension(fileName, ".cs");
				using (StreamWriter sw = new StreamWriter(fileName))
				{
					sw.Write(scriptText);
				}
				RunScript(fileName);
			}
			finally
			{
				if (File.Exists(fileName))
					File.Delete(fileName);
			}
		}
	}

	static string ComposeScript(string code, string[] namespaces)
	{
		StringBuilder msgBuilder = new StringBuilder();
		foreach(string item in namespaces)
		{
			if (item.Trim() != "")
				msgBuilder.Append(string.Format("using {0};\r\n", item));
		}
		msgBuilder.Append("\r\n");
		msgBuilder.Append("class Script\r\n");
		msgBuilder.Append("{\r\n");
		msgBuilder.Append("	static public void Main(string[] args)\r\n");
		msgBuilder.Append("	{\r\n");
		msgBuilder.Append(code + ";\r\n");
		msgBuilder.Append("	}\r\n");
		msgBuilder.Append("}\r\n");
		return msgBuilder.ToString();
	}

	static void RunScript(string scriptFileCmd)
	{
		Process myProcess = new Process();
		myProcess.StartInfo.FileName = "cscs.exe";
		myProcess.StartInfo.Arguments = "/nl \"" + scriptFileCmd + "\"";
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
