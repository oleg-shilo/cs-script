//Te code is provided by hackman3vilGuy
//http://www.codeproject.com/vista-security/UAC_Shield_for_Elevation.asp
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Principal;

public class VistaSecurity
{
    [DllImport("user32")]
    public static extern UInt32 SendMessage(IntPtr hWnd, UInt32 msg, UInt32 wParam, UInt32 lParam);

    internal const int BCM_FIRST = 0x1600;
    internal const int BCM_SETSHIELD = (BCM_FIRST + 0x000C);

    static internal bool IsVistaOrHigher()
    {
        return Environment.OSVersion.Version.Major <= 6;
    }

    /// <summary>
    /// Checks if the process is elevated
    /// </summary>
    /// <returns>true if elevated</returns>
    static internal bool IsAdmin()
    {
        WindowsIdentity id = WindowsIdentity.GetCurrent();
        WindowsPrincipal p = new WindowsPrincipal(id);
        return p.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Add a shield icon to a button
    /// </summary>
    /// <param name="b">The button</param>
    static internal void AddShieldToButton(Button b)
    {
        b.FlatStyle = FlatStyle.System;
        SendMessage(b.Handle, BCM_SETSHIELD, 0, 0xFFFFFFFF);
    }

    /// <summary>
    /// Restart the current process with administrator credentials
    /// </summary>
    internal static void RestartElevated()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        
        string arguments = "";
        string[] args = Environment.GetCommandLineArgs();
        for(int i = 1; i < args.Length; i++ )
            arguments +="\""+args[i]+"\" ";

        startInfo.Arguments = arguments.TrimEnd();
        startInfo.UseShellExecute = true;
        startInfo.WorkingDirectory = Environment.CurrentDirectory;
        startInfo.FileName = Application.ExecutablePath;
        startInfo.Verb = "runas";
        try
        {
            Process p = Process.Start(startInfo);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return; //If cancelled, do nothing
        }

        Application.Exit();
    }
}

