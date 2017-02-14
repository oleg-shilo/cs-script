using System;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;
using CSScriptLibrary;
using csscript;

namespace CFBuildScript
{
    class CFBuildScript
    {
        const string usage = "Usage: cscscript cfbuild [/r] <file> | /r | /i | /u ...\nCompiles script file into executable for Pocket PC.\n" +
                        "/r - compile with resetting the Compact Framework assembly location.\n" +
                        "/i | /u - command switch to install/uninstall shell extension\n";

        static string configFile;

        [STAThread]
        static public void Main(string[] args)
        {
            configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Galos\C# Script engine\css_cf.dat");

            System.Diagnostics.Debug.Assert(false);
            
            try
            {
                if (args.Length == 0 || args[0].ToLower() == "-?" || args[0].ToLower() == "/?")
                    Console.WriteLine(usage);
                else if (args[0].ToLower() == "/u")
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(@"CsScript\shell\CF Build");
                    Console.WriteLine("Shell extension 'CF build' has been removed.");
                }
                else if (args[0].ToLower() == "/i")
                {
                    RegistryKey shell = Registry.ClassesRoot.CreateSubKey(@"CsScript\shell\CF Build\command");
                    shell.SetValue("", "\"" + Path.Combine(HomeDir, "cscs.exe") + "\" \"" + Path.Combine(HomeDir, "Lib\\cfbuild.cs") + "\" \"%1\"");
                    shell.Close();
                    Console.WriteLine("Shell extension 'CF build' has been created.");
                }
                else
                {
                    if (args[0].ToLower() == "/r")
                    {
                        GenerateConfigFile();
                        if (args[0].Length == 1)
                            return;
                    }

                    string scritFile = args.Length > 1 ? args[1] : args[0];
                    string targerFile = Path.ChangeExtension(scritFile, ".exe");

                    if (!File.Exists(configFile))
                        GenerateConfigFile();

                    ValidateCLRVersion();

                    if (File.Exists(targerFile))
                        File.Delete(targerFile);
                    RunScript("/ew \"/noconfig:" + configFile + "\" \"" + scritFile + "\"");
                    if (!File.Exists(targerFile))
                    {
                        Console.WriteLine("\nCannot compile " + scritFile);
                        Console.Write("\nPress Enter to continue...");
                        Console.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        static void ValidateCLRVersion()
        {
            string[] temp = Settings.Load(configFile).SearchDirs.Split(';');
            string CF_MSCorLibDir = temp[temp.Length-1];
            AssemblyName CF_MSCorLib = AssemblyName.GetAssemblyName(Path.Combine(CF_MSCorLibDir, "mscorlib.dll"));

            int compilerMajorVersion = int.Parse(Environment.Version.ToString().Split(".".ToCharArray())[0]);
            int targetMajorVersion = int.Parse(CF_MSCorLib.Version.ToString().Split(".".ToCharArray())[0]);
            if (targetMajorVersion < compilerMajorVersion)
            {
                string message = "There is a discrepancy between compiler and CF runtime versions:\n" +
                                 "  Comiler: " + Environment.Version.ToString() + "\n" +
                                 "  CF: " + CF_MSCorLib.Version.ToString() + "\n\n" +
                                 "You can fix the problem by changing the version of the compiler in\n" +
                                 "the CS-Script configuration console ('Target CLR version').\n" +
                                 "Otherwise press OK to continue with the current version of the compiler.";
                DialogResult response = MessageBox.Show(message, "CS-Script", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (response != DialogResult.OK)
                    throw new Exception("Operation canceled by user");
            }
        }
        static void GenerateConfigFile()
        {
            if (File.Exists(configFile))
                File.Delete(configFile);

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string message =
                "The location of the Compact Framework assemblies is unknown at this stage.\n\n" +
                "Please press OK to specify the assemblies folder.\n" +
                "You need to do this only once. If you need to change the selection later \n" +
                "just run cfbuild.cs with /r switch.\n\n" +
                "These are the usual locations:\n" +
                "  " + programFiles + @"\Microsoft.NET\SDK\CompactFramework\v1.0\WindowsCE" + "\n" +
                "  " + programFiles + @"\Microsoft.NET\SDK\CompactFramework\v2.0\WindowsCE" + "\n" +
                "  " + programFiles + @"\Microsoft Visual Studio .NET 2003\CompactFrameworkSDK\v1.0.5000\Windows CE" + "\n" +
                "  " + programFiles + @"\Microsoft Visual Studio 8\SmartDevices\SDK\CompactFramework\1.0\WindowsCE" + "\n" +
                "  " + programFiles + @"\Microsoft Visual Studio 8\SmartDevices\SDK\CompactFramework\2.0\WindowsCE";
            DialogResult response = MessageBox.Show(message, "CS-Script", MessageBoxButtons.OKCancel);
            if (response == DialogResult.OK)
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "Select the Compact Framework assemblies folder.";
                    dlg.ShowNewFolderButton = false;

                    if (DialogResult.OK == dlg.ShowDialog())
                    {
                        Settings settings = new Settings();
                        settings.SearchDirs += ";" + dlg.SelectedPath;
                        settings.Save(configFile);
                    }
                    else
                        throw new Exception("Operation canceled by user");
                }
            }
            else
                throw new Exception("Operation canceled by user");
        }

        static void RunScript(string scriptFileCmd)
        {
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = Path.Combine(HomeDir, "cscs.exe");
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

        static string HomeDir
        {
            get
            {
                string homeDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");
                if (homeDir == null)
                    homeDir = "";
                return homeDir;
            }
        }
    }
}