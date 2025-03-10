//css_args /nl
//css_ng csc
//css_include global-usings
using System;
using System.Runtime.InteropServices;
using System.Security;
using CSScripting;
using static System.Environment;

var thisScript = GetEnvironmentVariable("EntryScript");

var help =
@$"Custom command for printing information about current login session.
Can be used either form the user session applications or from services.
v{thisScript.GetCommandScriptVersion()} ({thisScript})
Usage:
  css -who";

if ("?,-?,-help,--help".Split(',').Contains(args.FirstOrDefault()))
{
    Console.WriteLine(help);
}
else
{
    Console.WriteLine("User: " + Environment.UserName);
    Console.WriteLine("Domain: " + Environment.UserDomainName);

    if (OperatingSystem.IsWindows())
    {
        Console.WriteLine("========================");

        Console.WriteLine("Windows Terminal Session:");

        var WTSinfo = new TerminalServer();

        if (!WTSinfo.IsUserLogged())
            Console.WriteLine("<no terminal user>");

        WTSinfo.GetSessionInfo();
    }
}

public class TerminalServer
{
    [DllImport("wtsapi32", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurity]
    static extern int WTSEnumerateSessions(IntPtr hServer,
                                           int Reserved,
                                           int Version,
                                           ref IntPtr ppSessionInfo,
                                           ref int pCount);

    [DllImport("wtsapi32", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurity]
    static extern bool WTSQuerySessionInformation(IntPtr hServer,
                                                  int sessionId,
                                                      WTSInfoClass wtsInfoClass,
                                                      ref IntPtr ppBuffer,
                                                      ref uint pBytesReturned);

    [DllImport("wtsapi32", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurity]
    static extern IntPtr WTSOpenServer(string ServerName);

    [DllImport("wtsapi32", SetLastError = true), SuppressUnmanagedCodeSecurity]
    static extern void WTSCloseServer(IntPtr hServer);

    [DllImport("wtsapi32", SetLastError = true), SuppressUnmanagedCodeSecurity]
    static extern void WTSFreeMemory(IntPtr pMemory);

    [StructLayout(LayoutKind.Sequential)]
    struct WTSSessionInfo
    {
        internal uint SessionId;

        [MarshalAs(UnmanagedType.LPTStr)]
        internal string pWinStationName;

        internal uint State;
    }

    enum WTSConnectState
    {
        Active,
        Connected,
        ConnectQuery,
        Shadow,
        Disconnected,
        Idle,
        Listen,
        Reset,
        Down,
        Init
    }

    enum WTSInfoClass
    {
        InitialProgram,
        ApplicationName,
        WorkingDirectory,
        OEMId,
        SessionId,
        UserName,
        WinStationName,
        DomainName,
        ConnectState,
        ClientBuildNumber,
        ClientName,
        ClientDirectory,
        ClientProductId,
        ClientHardwareId,
        ClientAddress,
        ClientDisplay,
        ClientProtocolType
    }

    public void GetSessionInfo()
    {
        var hServer = IntPtr.Zero;
        var pInfo = IntPtr.Zero;
        var pInfoSave = IntPtr.Zero;
        WTSSessionInfo WTSsi;
        var ppBuffer = IntPtr.Zero;
        uint bCount = 0;
        int count = 0;

        try
        {
            hServer = WTSOpenServer("");
            if (hServer == IntPtr.Zero)
                Console.WriteLine(Marshal.GetLastWin32Error());

            if (WTSEnumerateSessions(hServer, 0, 1, ref pInfo, ref count) != 0)
            {
                pInfoSave = pInfo;
                Console.WriteLine("Number of sessions: {0}\n", count);

                for (int n = 0; n < count; n++)
                {
                    WTSsi = Marshal.PtrToStructure<WTSSessionInfo>(pInfo);
                    pInfo = IntPtr.Add(pInfo, Marshal.SizeOf<WTSSessionInfo>());

                    Console.WriteLine(WTSsi.SessionId + "\t" + WTSsi.pWinStationName + " - " + (WTSConnectState)WTSsi.State);
                    if (WTSQuerySessionInformation(hServer, (int)WTSsi.SessionId, WTSInfoClass.UserName, ref ppBuffer, ref bCount))
                        Console.WriteLine("\tUser: {0}", Marshal.PtrToStringUni(ppBuffer));
                    if (WTSQuerySessionInformation(hServer, (int)WTSsi.SessionId, WTSInfoClass.DomainName, ref ppBuffer, ref bCount))
                        Console.WriteLine("\tDomain: {0}", Marshal.PtrToStringUni(ppBuffer));
                    if (WTSQuerySessionInformation(hServer, (int)WTSsi.SessionId, WTSInfoClass.WinStationName, ref ppBuffer, ref bCount))
                        Console.WriteLine("\tWindowStation: {0}", Marshal.PtrToStringUni(ppBuffer));

                    Console.WriteLine();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            WTSFreeMemory(pInfoSave);
            WTSCloseServer(hServer);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
    static extern int WTSGetActiveConsoleSessionId();

    public bool IsUserLogged()
    {
        bool retval = false;

        var state = IntPtr.Zero;
        uint bCount = 0;

        if (WTSQuerySessionInformation(
            System.IntPtr.Zero,
                   WTSGetActiveConsoleSessionId(),
                   WTSInfoClass.ConnectState, ref state, ref bCount))
        {
            WTSConnectState st = (WTSConnectState)Marshal.PtrToStructure(state, typeof(Int32));
            retval = (st == WTSConnectState.Active);
            WTSFreeMemory(state);
        }
        return retval;
    }

    public string GetUserLogged()
    {
        string retval = "";
        IntPtr ppBuffer = IntPtr.Zero;
        uint bCount = 0;

        if (WTSQuerySessionInformation(System.IntPtr.Zero,  //WTS_CURRENT_SERVER_HANDLE
                                       0,                   //WTS_CURRENT_SESSION
                                       WTSInfoClass.UserName, ref ppBuffer, ref bCount))
        {
            retval = Marshal.PtrToStringUni(ppBuffer);
            WTSFreeMemory(ppBuffer);
        }
        return retval;
    }
}