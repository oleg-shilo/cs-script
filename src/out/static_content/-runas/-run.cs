//css_ng csc
//css_include global-usings
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using CSScripting;

var thisScript = GetEnvironmentVariable("EntryScript");

var help = $@"Custom command that implemets functionality of `runas` Windows utility.
v{thisScript.GetCommandScriptVersion()} ({thisScript})

The command can be used for executing specified command (e.g. executable) with specific user credentials.

Usage:
  css -runas -user:<user> [-pwd:<password>] [-netonly] <command>

  user     - Should be in form USER@DOMAIN or DOMAIN\USER
  pwd      - Password for the user. If this argument is omitted the password will be requested interactively.
  netonly  - Use if the credentials specified are for remote access only.
  command  - Command to be executed";

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("The command is designed for Windows only.");
    return;
}

if (args.ContainsAny("-?", "?", "-help", "--help") || args.Length < 2)
{
    print(help);
    return;
}

string username = null;
string pwd = null;
bool netOnly = false;
string command = null;

foreach (var arg in args)
{
    if (arg.StartsWith("-user:", true)) username = Environment.ExpandEnvironmentVariables(arg.Substring(6);
    else if (arg.StartsWith("-pwd:", true)) pwd = Environment.ExpandEnvironmentVariables(arg.Substring(5));
    else if (arg.SameAs("-netonly", true)) netOnly = true;
    else command = arg;
}

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(command))
{
    Console.WriteLine("Invalid parameters.");
    return;
}

SecureString password;

if (!string.IsNullOrEmpty(pwd))
{
    password = new SecureString();
    pwd.ForEach(password.AppendChar);
    password.MakeReadOnly();
}
else
{
    Console.Write("Enter password: ");
    password = GetPassword();
}

IntPtr passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);

try
{
    var si = new STARTUPINFO();
    PROCESS_INFORMATION pi;

    int LOGON_WITH_PROFILE = 1;
    int LOGON_NETCREDENTIALS_ONLY = 2;
    int CREATE_UNICODE_ENVIRONMENT = 0x00000400;

    bool success = CreateProcessWithLogonW(username, null, Marshal.PtrToStringUni(passwordPtr), netOnly ? LOGON_NETCREDENTIALS_ONLY : LOGON_WITH_PROFILE, null, command, CREATE_UNICODE_ENVIRONMENT, IntPtr.Zero, null, ref si, out pi);

    if (!success)
        throw new Win32Exception(Marshal.GetLastWin32Error());

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

//===============================================================================
static SecureString GetPassword()
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

[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
static extern bool CreateProcessWithLogonW(
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

[StructLayout(LayoutKind.Sequential)]
struct STARTUPINFO
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
struct PROCESS_INFORMATION
{
    public IntPtr hProcess;
    public IntPtr hThread;
    public int dwProcessId;
    public int dwThreadId;
}