//css_args /nl
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Security;
using HANDLE = System.IntPtr;

namespace WTSsessions
{
	class Script
	{
		// Using Wtsapi32 (only available on Windows Server 2003 and XP)
		// Get Session information using Terminal server API's

		const string usage = "Usage: cscscript who ...\nPrints information about current login session. Can be used even from services\n"+
							 "This is an example of usage Terminal server API from C# code.\n";

		static public void Main(string[] args)
		{
			if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
			{
				Console.WriteLine(usage);
			}
			else
			{
				Console.WriteLine("Retrieved by CLR...");
				Console.WriteLine("User: "+Environment.UserName);
				Console.WriteLine("Domain: "+Environment.UserDomainName);
				Console.WriteLine("");
				
				Console.WriteLine("Retrieved by WTSAPI...");
				TerminalServer WTSinfo = new TerminalServer();
			
				Console.Write("Currently logged user: "); 
				if (WTSinfo.IsUserLogged())
					Console.WriteLine(WTSinfo.GetUserLogged()+"\n");
				else
					Console.WriteLine("<no user>" + "\n");

				WTSinfo.GetSessionInfo();
			}
		}


		public class TerminalServer
		{
			#region Imports...
			[DllImport("wtsapi32", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
			static extern int WTSEnumerateSessions(System.IntPtr hServer,
													int Reserved,
													int Version,
													ref System.IntPtr ppSessionInfo,
													ref int pCount);
			[DllImport("wtsapi32", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
			static extern bool WTSQuerySessionInformation(System.IntPtr hServer,
															int sessionId,
															WTSInfoClass wtsInfoClass,
															ref System.IntPtr ppBuffer,
															ref uint pBytesReturned);
			[DllImport("wtsapi32", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
			static extern IntPtr WTSOpenServer(string ServerName);
			[DllImport("wtsapi32", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
			static extern void WTSCloseServer(HANDLE hServer);
			[DllImport("wtsapi32", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
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
			#endregion

			public void GetSessionInfo()
			{
				HANDLE hServer = IntPtr.Zero;
				IntPtr pInfo = IntPtr.Zero;
				IntPtr pInfoSave = IntPtr.Zero;
				WTSSessionInfo WTSsi; // Reference to ProcessInfo struct
				IntPtr ppBuffer = IntPtr.Zero;
				uint bCount = 0;
				int count = 0;
				int iPtr = 0;
				try
				{
					hServer = WTSOpenServer("");
					if (hServer == IntPtr.Zero)
						Console.WriteLine(Marshal.GetLastWin32Error());
					if (WTSEnumerateSessions(hServer, 0, 1, ref pInfo, ref count) != 0)
					{
						pInfoSave = pInfo;
						Console.WriteLine("Number of sessions: {0}", count);
						for (int n = 0; n < count; n++)
						{
							WTSsi = (WTSSessionInfo)Marshal.PtrToStructure(pInfo, typeof(WTSSessionInfo));
							iPtr = (int)(pInfo) + Marshal.SizeOf(WTSsi);
							pInfo = (IntPtr)(iPtr);
							Console.WriteLine(WTSsi.SessionId + "\t" + WTSsi.pWinStationName + " - " + (WTSConnectState)WTSsi.State);
							if (WTSQuerySessionInformation(hServer, (int)WTSsi.SessionId, WTSInfoClass.UserName, ref ppBuffer, ref bCount))
								Console.WriteLine("\tUser: {0}", Marshal.PtrToStringAuto(ppBuffer));
							if (WTSQuerySessionInformation(hServer, (int)WTSsi.SessionId, WTSInfoClass.DomainName, ref ppBuffer, ref bCount))
								Console.WriteLine("\tDomain: {0}", Marshal.PtrToStringAuto(ppBuffer));
							if (WTSQuerySessionInformation(hServer, (int)WTSsi.SessionId, WTSInfoClass.WinStationName, ref ppBuffer, ref bCount))
								Console.WriteLine("\tWindowstation: {0}", Marshal.PtrToStringAuto(ppBuffer));
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
			
			[DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
			static extern int WTSGetActiveConsoleSessionId();
			public bool IsUserLogged()
			{
				bool retval = false;
							
				IntPtr state = IntPtr.Zero;
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
				if (WTSQuerySessionInformation(	System.IntPtr.Zero,	//WTS_CURRENT_SERVER_HANDLE
												0,					//WTS_CURRENT_SESSION
												WTSInfoClass.UserName, ref ppBuffer, ref bCount))
				{
					retval = Marshal.PtrToStringAuto(ppBuffer);
					WTSFreeMemory(ppBuffer);
				}
				return retval;
			}
		
		}
	}
}
