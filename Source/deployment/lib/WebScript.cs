using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Net;

namespace ScriptSecurity
{
	class WebScript
	{
		[STAThread]
		static public void Main(string[] args)
		{
			if (args.Length == 0 || (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
			{
				Console.WriteLine("Usage: cscscript WebScript\n" +
								  "This script contains genaral purpose script security routine(s).\n" +
								  "For example: downloading and validating the scripts from WEB sources.\n");
			}
		}
		static public string GetScript(string url, string proxyUser, string proxyPw)
		{
			string updateScript = Path.GetTempFileName();

			try
			{
				using (StreamWriter sw = new StreamWriter(updateScript))
				{
					sw.Write(GetHTML(url, proxyUser, proxyPw));
				}
			}
			catch
			{
				updateScript = null;
			}

			if (updateScript != null)
			{
				if (!IsPermitedScript(updateScript, url))
				{
					File.Delete(updateScript);
					updateScript = null;
				}
			}
			
			return updateScript;
		}
		static string GetHTML(string url, string proxyUser, string proxyPw)
		{
			StringBuilder sb = new StringBuilder();
			byte[] buf = new byte[8192];

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			if (proxyUser != null)
			{
				GlobalProxySelection.Select.Credentials = new NetworkCredential(proxyUser, proxyPw);
			}
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			Stream resStream = response.GetResponseStream();

			string tempString = null;
			int count = 0;

			while (0 < (count = resStream.Read(buf, 0, buf.Length)))
			{
				tempString = Encoding.ASCII.GetString(buf, 0, count);
				sb.Append(tempString);
			}
			return sb.ToString();
		}
		static bool IsPermitedScript(string file, string url)
		{
			string message = "You are about to execute the script downloaded from \n" +
							 "  "+url+"\n\n" +   
							 "  You may cancel the script execution by pressing the Cancel button.\n" +
							 "  Otherwise press No to review the script or Yes to execute it immediately.";
			DialogResult response;
			while (DialogResult.No == (response = MessageBox.Show(message, "CS-Script Securiy Alert", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning)))
			{
				Process myProcess = new Process();
				myProcess.StartInfo.FileName = "notepad.exe";
				myProcess.StartInfo.Arguments = file;
				myProcess.Start();
				myProcess.WaitForExit();
			}

			return response == DialogResult.Yes;
		}
	}

}