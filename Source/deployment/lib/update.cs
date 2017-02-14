using System;
using System.Net;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;

//css_import credentials;
//css_import webscript.cs;

namespace Scripting
{
    class UpdateScript
    {
        [STAThread]
        static public void Main(string[] args)
        {
            if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
            {
                Console.WriteLine("Usage: cscscript update ...\n"+
					"Checks for available update on CS-Script home page.\n");
            }
            else
            {
                bool useProxyAuthentication = false;
                while (true)
                {
                    try
                    {
                        string user = null, pw = null;
                        if (useProxyAuthentication)
                        {
                            if (!AuthenticationForm.GetCredentials(ref user, ref pw, "Proxy Authentication"))
                            {
                                return;
                            }
                        }

                        CheckForUpdate(user, pw);
                        return;
                    }
                    catch (Exception e)
                    {
                        if (e is System.Net.WebException && e.Message == "The remote server returned an error: (407) Proxy Authentication Required.")
                        {
                            if (useProxyAuthentication)
                                Console.WriteLine(e.Message);

                            useProxyAuthentication = true;
                            continue;
                        }

                        Console.WriteLine(e.Message);
                        return;
                    }
                }
            }
        }

        static void CheckForUpdate(string proxyUser, string proxyPw)
        {
            //format: Current Version 1.0.10.30039
			string latestVersionStr = GetHTML("http://www.csscript.net/version.txt", proxyUser, proxyPw).Substring("Current Version ".Length);
            string installedVersionStr = GetInstalledScriptVersion();

            string[] latestVer = latestVersionStr.Split(".".ToCharArray());
            string[] installedVer = installedVersionStr.Split(".".ToCharArray());

            bool updateRequired = false;
            if (int.Parse(latestVer[0]) > int.Parse(installedVer[0]))
                updateRequired = true;
            else if (int.Parse(latestVer[0]) == int.Parse(installedVer[0]) && int.Parse(latestVer[1]) > int.Parse(installedVer[1]))
                updateRequired = true;
            else if (int.Parse(latestVer[0]) == int.Parse(installedVer[0]) && int.Parse(latestVer[1]) == int.Parse(installedVer[1]) && int.Parse(latestVer[2]) > int.Parse(installedVer[2]))
                updateRequired = true;
            else if (int.Parse(latestVer[0]) == int.Parse(installedVer[0]) && int.Parse(latestVer[1]) == int.Parse(installedVer[1]) && int.Parse(latestVer[2]) > int.Parse(installedVer[2]) && int.Parse(latestVer[3]) > int.Parse(installedVer[3]))
                updateRequired = true;

            if (Environment.GetEnvironmentVariable("CSS_UPDATE_TEST") != null)
                updateRequired = true;

            if (updateRequired)
            {
                /*
				string files = "";

				if (IsFileLocked(comShellEtxDLL32))
					files += "\r\n    " + comShellEtxDLL32;
				if (IsFileLocked(comShellEtxDLL64))
					files += "\r\n    " + comShellEtxDLL64;	
				
				if (files != "")
				{
					MessageBox.Show("Newer version is available\n" +
									"  Installed version " + installedVersionStr + "\n" +
									"  Available version: " + latestVersionStr + "\n\n" +
									"However you cannot use automatic update as some files are locked:" + files +
									"\n\nIn order to enable automatic update please disable Advanced Shell Extensions and reboot/relogin.", "CS-Script");
					return;
				}
				*/

				if (DialogResult.Yes == MessageBox.Show("Newer version is available\n" +
														"  Installed version " + installedVersionStr + "\n" +
														"  Available version: " + latestVersionStr + "\n\n" +
														"You will be redirected to the CodePlex repository where you will be able to download the latest version of CS-Script.\n\n"+
                                                        "You will also find there instructions for installing CS-Script with Chocolatey (preferred method).", "CS-Script Update",
														MessageBoxButtons.OK, MessageBoxIcon.Information))
                {
                }
                
                // if (DialogResult.Yes == MessageBox.Show("Newer version is available\n" +
														// "  Installed version " + installedVersionStr + "\n" +
														// "  Available version: " + latestVersionStr + "\n\n" +
														// "Do you want to proceed with upgrading?", "CS-Script Update",
														// MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                // {
                    // string updateScriptUrl = "http://www.csscript.net/dynamicupdate.cs.txt";

                    // if (Environment.GetEnvironmentVariable("CSS_UPDATE_TEST") != null)
                        // updateScriptUrl = "http://www.csscript.net/Update/currupdate.cs.txt";

                    // string updateScript = ScriptSecurity.WebScript.GetScript(updateScriptUrl, null, null);

                    // File.Move(updateScript, Path.ChangeExtension(updateScript, ".cs"));
                    // updateScript = Path.ChangeExtension(updateScript, ".cs");

                    // if (updateScript != null)
                    // {
                        // ProcessStartInfo pInfo = new ProcessStartInfo();
                        // pInfo.UseShellExecute = false;
                        // if (GlobalProxySelection.Select.Credentials != null)
                        // {
                            // try
                            // {
                                // pInfo.EnvironmentVariables.Add("CSS_UPDATE_PROXY_USER", GlobalProxySelection.Select.Credentials.GetCredential(null, "").UserName);
                                // pInfo.EnvironmentVariables.Add("CSS_UPDATE_PROXY_PW", GlobalProxySelection.Select.Credentials.GetCredential(null, "").Password);
                            // }
                            // catch { }
                        // }

                        // pInfo.FileName = @"csws.exe";
                        // pInfo.Arguments = "\"" + updateScript + "\"";
                        // Process.Start(pInfo);
                    // }
                    // else
                    // {
                        // Process.Start(@"http://www.csscript.net/CurrentRelease.html");
                    // }
                // }
            }
            else
            {
                MessageBox.Show("Your CS-Script version is current.\nNo update is required.", "CS-Script Update");
            }
        }

        public static bool IsFileLocked(string file)
        {
            if (File.Exists(file))
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(file, true))
                    {
                    }
                }
                catch (Exception ex)
                {
                    return true;
                }
            }

            return false;
        }

        public static string comShellEtxDLL32
        {
            get
            {
                if (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") != null)
                    return Path.Combine(Environment.GetEnvironmentVariable("CSSCRIPT_DIR"), @"Lib\ShellExtensions\CS-Script\ShellExt.cs.{25D84CB0-7345-11D3-A4A1-0080C8ECFED4}.dll");
                else
                    return "";
            }
        }

        public static string comShellEtxDLL64
        {
            get
            {
                if (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") != null)
                    return Path.Combine(Environment.GetEnvironmentVariable("CSSCRIPT_DIR"), @"Lib\ShellExtensions\CS-Script\ShellExt64.cs.{25D84CB0-7345-11D3-A4A1-0080C8ECFED4}.dll");
                else
                    return "";
            }
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
        
		static string GetInstalledScriptVersion_old()
		{
			Process myProcess = new Process();
			myProcess.StartInfo.FileName = "cscs.exe";
			myProcess.StartInfo.UseShellExecute = false;
			myProcess.StartInfo.RedirectStandardOutput = true;
			myProcess.StartInfo.CreateNoWindow = true;
			myProcess.Start();

			string line = myProcess.StandardOutput.ReadLine();
			return line.Substring(line.LastIndexOf("Version") + "Version".Length);
		}
		static string GetInstalledScriptVersion()
		{
			string homeDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");

			if (homeDir != null && File.Exists(Path.Combine(homeDir, "cscs.exe")))
			{
				Assembly asm = Assembly.LoadFrom(Path.Combine(homeDir, "cscs.exe")); //script engine
				foreach (string info in asm.FullName.Split(','))
				{
					string ver = info.Trim().ToLower();
					if (ver.StartsWith("version="))
						return ver.Substring("version=".Length);
				}
			}
			return null;
		}
	}
}