using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

class Program
{
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessWithLogonW(
        string lpUsername,
        string lpDomain,
        string lpPassword,
        int dwLogonFlags,
        string lpApplicationName,
        string lpCommandLine,
        int dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    private const int LOGON_WITH_PROFILE = 1;
    private const int LOGON_NETCREDENTIALS_ONLY = 2;
    private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: runas_clone /user:<UserName> [/netonly] <Command>");
            return;
        }

        string username = null;
        bool netOnly = false;
        string command = null;

        foreach (var arg in args)
        {
            if (arg.StartsWith("/user:", StringComparison.OrdinalIgnoreCase))
            {
                username = arg.Substring(6);
            }
            else if (arg.Equals("/netonly", StringComparison.OrdinalIgnoreCase))
            {
                netOnly = true;
            }
            else
            {
                command = arg;
            }
        }

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(command))
        {
            Console.WriteLine("Invalid parameters.");
            return;
        }

        Console.Write("Enter password: ");
        SecureString password = GetPassword();

        IntPtr passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);

        try
        {
            STARTUPINFO si = new STARTUPINFO();
            PROCESS_INFORMATION pi;

            bool success = CreateProcessWithLogonW(
                username,
                null,
                Marshal.PtrToStringUni(passwordPtr),
                netOnly ? LOGON_NETCREDENTIALS_ONLY : LOGON_WITH_PROFILE,
                null,
                command,
                CREATE_UNICODE_ENVIRONMENT,
                IntPtr.Zero,
                null,
                ref si,
                out pi);

            if (!success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Console.WriteLine($"Process started with PID: {pi.dwProcessId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
        }
    }

    private static SecureString GetPassword()
    {
        SecureString password = new SecureString();
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.RemoveAt(password.Length - 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.AppendChar(key.KeyChar);
                Console.Write("*");
            }
        }
        Console.WriteLine();
        password.MakeReadOnly();
        return password;
    }
}