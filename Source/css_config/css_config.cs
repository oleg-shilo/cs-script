//css_dbg /t:winexe;
//css_inc VistaSecurity.cs;
//css_inc SplashForm.cs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

public class AppForm : Form
{
    public AppForm()
    {
        InitializeComponent();

        if (!VistaSecurity.IsAdmin())
            VistaSecurity.AddShieldToButton(GetElevation);
    }

    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.GetElevation = new System.Windows.Forms.Button();
        this.NoElevation = new System.Windows.Forms.Button();
        this.SuspendLayout();
        //
        // GetElevation
        //
        this.GetElevation.FlatStyle = System.Windows.Forms.FlatStyle.System;
        this.GetElevation.Location = new System.Drawing.Point(13, 12);
        this.GetElevation.Name = "GetElevation";
        this.GetElevation.Size = new System.Drawing.Size(154, 33);
        this.GetElevation.TabIndex = 0;
        this.GetElevation.Text = "Full Access Mode";
        this.GetElevation.Click += new System.EventHandler(this.GetElevation_Click);
        //
        // NoElevation
        //
        this.NoElevation.Location = new System.Drawing.Point(13, 51);
        this.NoElevation.Name = "NoElevation";
        this.NoElevation.Size = new System.Drawing.Size(154, 33);
        this.NoElevation.TabIndex = 1;
        this.NoElevation.Text = "Restricted Mode";
        this.NoElevation.Click += new System.EventHandler(this.NoElevation_Click);
        //
        // AppForm
        //
        this.ClientSize = new System.Drawing.Size(179, 101);
        this.Controls.Add(this.NoElevation);
        this.Controls.Add(this.GetElevation);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
        this.Name = "AppForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "CS-Script Configuration";
        this.TopMost = true;
        this.ResumeLayout(false);
    }

    #endregion Windows Form Designer generated code

    private System.Windows.Forms.Button GetElevation;
    private System.Windows.Forms.Button NoElevation;

    private void GetElevation_Click(object sender, EventArgs e)
    {
        this.Close();

        if (VistaSecurity.IsAdmin())
            StartConfig();
        else
            VistaSecurity.RestartElevated();
    }

    private void NoElevation_Click(object sender, EventArgs e)
    {
        this.Close();
        StartConfig();
    }

    static public void StartConfig()
    {
        //Debug.Assert(false);
        try
        {
            SplashScreen.ShowSplash();

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && (args[1].ToLower() == "/u" || args[1].ToLower() == "-u"))
            {
                //forced uninstall
                if (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") == null)
                    MessageBox.Show("Cannot perform uninstall operation.\nCS-Script is not currently installed.", "CS-Script Configuration");
                else
                    CSScriptInstaller.UnInstall();
            }
            else
            {
                string rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                string csws = Path.Combine(rootDir, "csws.exe");

                string configScript = Path.Combine(rootDir, @"lib\config.cs");

                args = new string[2];
                args[0] = "/dbg";
                args[1] = configScript;

                AppDomain.CurrentDomain.ExecuteAssembly(Path.Combine(rootDir, @"csws.exe"), args);

                //Cannot use process as it will start CLR specified in csws.exe.config
                //but it is required to start ConfigConsole under the highest CLR available
                //Process.Start(csws, "\"" + configScript + "\"");
            }
        }
        catch (UnauthorizedAccessException e)
        {
            MessageBox.Show(e.Message, "CS-Script Configuration");
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString(), "CS-Script Configuration");
        }
    }
}

class CSScriptInstaller //subset of CSScriptInstaller from Config.cs
{
    static string RemoveFromPath(string dir, string path)
    {
        bool removed = false;
        string retval = "";
        string[] pathDirs = path.Split(';');
        for (int i = 0; i < pathDirs.Length; i++)
            if (pathDirs[i].Trim().ToLower() == dir)
                removed = true;
            else
                retval += pathDirs[i].Trim() + ";";

        if (removed)
            return retval;
        else
            return path;
    }

    static void CopyKeyValue(string keyName, string srcValueName, string destValueName)
    {
        object value = GetKeyValue(keyName, srcValueName);
        if (value != null)
            SetKeyValue(keyName, destValueName, value);
    }

    static object GetKeyValue(string name, string valueName)
    {
        using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(name, false))
        {
            if (key == null)
                return null;
            else
                return key.GetValue(valueName);
        }
    }

    static void SetKeyValue(string keyName, string name, object value)
    {
        using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(keyName, true))
        {
            if (key == null)
            {
                Registry.ClassesRoot.CreateSubKey(keyName);
                using (RegistryKey newKey = Registry.ClassesRoot.OpenSubKey(keyName, true))
                {
                    newKey.SetValue(name, value);
                }
            }
            else
                key.SetValue(name, value);
        }
    }

    static void DeleteKey(string name)
    {
        using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(name))
            if (key != null)
                Registry.ClassesRoot.DeleteSubKeyTree(name);
    }

    static void DeleteKeyValue(string name, string valueName)
    {
        using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(name, true))
        {
            if (key != null)
                key.DeleteValue(valueName, false);
        }
    }

    static RegistryKey GetKey(string name, bool writable)
    {
        RegistryKey key = Registry.ClassesRoot.OpenSubKey(name, writable);
        if (key == null)
        {
            Registry.ClassesRoot.CreateSubKey(name);
            key = Registry.ClassesRoot.OpenSubKey(name, writable);
        }
        return key;
    }

    static public void UnInstall()
    {
        Action<string> deleteFile = (file) => { try { if (File.Exists(file)) File.Delete(file); } catch { } };

        try
        {
            string oldHomeDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");

            // if (CSScriptInstaller.IsComShellExtInstalled() && File.Exists(CSScriptInstaller.comShellEtxDLL32))
            //     CSScriptInstaller.UninstallComShellExt();

            string path = "";
            using (RegistryKey envVars = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", true))
            {
                path = Win32.RegGetValueExp(Win32.HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "Path");
                RemoveFromPath(@"%CSSCRIPT_DIR%", path);
                envVars.DeleteValue("CSSCRIPT_DIR", false);
            }

            path = RemoveFromPath(@"%CSSCRIPT_DIR%", path);
            path = RemoveFromPath(@"%CSSCRIPT_DIR%\lib", path);
            Win32.RegSetStrValue(Win32.HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "Path", path);

            DeleteKey("CsScript");
            DeleteKey(@".cs\ShellNew");
            DeleteKey(@".ccs");

            //restore the original file type
            CopyKeyValue(".cs", "pre_css_default", "");
            CopyKeyValue(".cs", "pre_css_contenttype", "Content Type");

            DeleteKeyValue(".cs", "pre_css_default");
            DeleteKeyValue(".cs", "pre_css_contenttype");

            using (RegistryKey csKey = GetKey(".cs", true))
                if (csKey.GetValue("OldDefault") != null)
                    csKey.SetValue("", csKey.GetValue("OldDefault").ToString());

            deleteFile(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\cscs.exe.config"));
            deleteFile(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\csws.exe.config"));
            deleteFile(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\css_config.xml"));

            SplashScreen.HideSplash();
            MessageBox.Show("CS-Script has been deactivated.\nPlease note that some files may still be locked by external applications (e.g. Windows Explorer)", "CS-Script Configuration");
        }
        catch (Exception e)
        {
            MessageBox.Show("Cannot perform uninstall operation.\n" + e.ToString(), "CS-Script Configuration");
        }
    }

    // static bool IsComShellExtInstalled()
    // {
    //     using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\CLSID\{25D84CB0-7345-11D3-A4A1-0080C8ECFED4}\InProcServer32"))
    //     {
    //         if (regKey != null)
    //         {
    //             string dll = regKey.GetValue("").ToString().ToUpper();
    //             return (dll == comShellEtxDLL32.ToUpper() || dll == comShellEtxDLL64.ToUpper());
    //         }
    //         else
    //             return false;
    //     }
    // }

    // static string comShellEtxDLL32 = Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\ShellExtensions\CS-Script\ShellExt.cs.{25D84CB0-7345-11D3-A4A1-0080C8ECFED4}.dll");
    // static string comShellEtxDLL64 = Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\ShellExtensions\CS-Script\ShellExt64.cs.{25D84CB0-7345-11D3-A4A1-0080C8ECFED4}.dll");

    // public static void UninstallComShellExt()
    // {
    //     if (Directory.Exists(Environment.ExpandEnvironmentVariables(@"%windir%\SysWOW64")))
    //     {
    //         RunApp(Environment.ExpandEnvironmentVariables(@"%windir%\SysWOW64\regsvr32.exe"), "/u /s \"" + comShellEtxDLL32 + "\"");
    //         RunApp(Environment.ExpandEnvironmentVariables(@"%windir%\System32\regsvr32.exe"), "/u /s \"" + comShellEtxDLL64 + "\"");
    //     }
    //     else
    //     {
    //         RunApp(Environment.ExpandEnvironmentVariables(@"%windir%\System32\regsvr32.exe"), "/u /s \"" + comShellEtxDLL32 + "\"");
    //     }
    // }

    static string RunApp(string app, string args)
    {
        Process myProcess = new Process();
        myProcess.StartInfo.FileName = app;
        myProcess.StartInfo.Arguments = args;
        myProcess.StartInfo.UseShellExecute = false;
        myProcess.StartInfo.RedirectStandardOutput = true;
        myProcess.StartInfo.CreateNoWindow = true;
        myProcess.Start();

        StringBuilder sb = new StringBuilder();
        string line = null;
        while (null != (line = myProcess.StandardOutput.ReadLine()))
        {
            sb.Append(line + "\n");
            Console.WriteLine(line);
        }
        myProcess.WaitForExit();
        return sb.ToString();
    }
}

class Win32
{
    [DllImport("advapi32.dll", EntryPoint = "RegQueryValueEx")]
    static extern int RegQueryValueEx(int hKey, string lpValueName, int lpReserved, out uint lpType, StringBuilder lpData, ref int lpcbData);

    [DllImport("Advapi32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    static extern int RegOpenKeyEx(uint hKey, string lpSubKey, uint ulOptions, int samDesired, out int phkResult);

    [DllImport("advapi32.dll", EntryPoint = "RegSetValueExA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
    private static extern int RegSetValueEx(int hKey, string lpValueName, int Reserved, int dwType, [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpData, int cbData);

    [DllImport("Advapi32.dll")]
    static extern uint RegCloseKey(int hKey);

    public const uint HKEY_LOCAL_MACHINE = 0x80000002;

    static public string RegGetValueExp(uint key, string subKey, string valName)
    {
        //this method is required in order to rtreive REG_EXPAND_SZ registry value
        const int KEY_READ = 0x00000001;
        int hkey = 0;
        try
        {
            if (0 == Win32.RegOpenKeyEx(key, subKey, 0, KEY_READ, out hkey))
            {
                StringBuilder sb = new StringBuilder(1024 * 10);
                int lpcbData = sb.Capacity;
                uint lpType;
                if (0 == RegQueryValueEx(hkey, valName, 0, out lpType, sb, ref lpcbData))
                    return sb.ToString();
            }
        }
        finally
        {
            if (0 != hkey)
                RegCloseKey(hkey);
        }
        return null;
    }

    static public int RegSetStrValue(uint key, string subKey, string valName, string val)
    {
        //this method is required in order to set REG_EXPAND_SZ registry value
        const int KEY_WRITE = 0x00020006;
        const int KEY_READ = 0x00000001;
        int lResult = 0;
        int hkey = 0;
        try
        {
            lResult = RegOpenKeyEx(key, subKey, 0, KEY_WRITE | KEY_READ,
                out hkey);
            if (lResult == 0)
            {
                lResult = RegSetValueEx(hkey, valName, 0, 2, ref val, val.Length);
            }
        }
        finally
        {
            if (0 != hkey)
                RegCloseKey(hkey);
        }
        return lResult;
    }
}