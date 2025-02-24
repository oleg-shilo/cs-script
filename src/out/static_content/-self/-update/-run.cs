//css_include global-usings
using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Diagnostics;
using CSScripting;
using static dbg;
using static System.IO.Path;
using static System.Console;
using static System.Environment;

var thisScript = GetEnvironmentVariable("EntryScript");

var help =
@$"Custom command for updating the installed CS-Script.
v{thisScript.GetCommandScriptVersion()} ({thisScript})
  css -self-update [args]
  (e.g. `css -self-update test.txt`)";

if ("?,-?,-help,--help".Split(',').Contains(args.FirstOrDefault()))
{
    WriteLine(help);
    return;
}

// -----------------------------------------------
// Command implementation
// -----------------------------------------------

var currentProcessAssembly = Assembly.GetEntryAssembly().Location;
var cscs = Run("css", "-self");

if (string.Compare(currentProcessAssembly, cscs, true) == 0)
{
    // cannot update itself as the process locks the assembly
    // so start another process

    WriteLine($"Launching the update routine...");
    var dir = Combine(GetTempPath(), "cs-script.update");
    var newCscs = Combine(dir, GetFileName(cscs));
    var newScript = Combine(dir, GetFileName(thisScript));

    Directory.CreateDirectory(dir);

    File.Copy(thisScript, newScript, true);
    File.Copy(cscs, newCscs, true);
    File.Copy(ChangeExtension(cscs, ".runtimeconfig.json"), ChangeExtension(newCscs, ".runtimeconfig.json"), true);
    File.Copy(ChangeExtension(cscs, ".deps.json"), ChangeExtension(newCscs, ".deps.json"), true);

    var command = $"dotnet {newCscs} {newScript}";

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/k {command}",
            UseShellExecute = true,
            WorkingDirectory = dir,
            WindowStyle = ProcessWindowStyle.Normal
        });
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        // Linux: Try gnome-terminal, x-terminal-emulator, or konsole
        StartLinuxTerminal(command);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        // macOS: Use Terminal.app
        Process.Start("open", $"-a Terminal \"bash -c '{command}; exec bash'\"");
    }
    else
    {
        Console.WriteLine("Update is not supported on your OS.");
        Console.WriteLine("Update manually by executing 'dotnet tool update --global cs-script.cli'.");
    }

    return;
}

WriteLine($"Checking the current installation...");
WriteLine();

if (cscs.Contains(Path.Combine(".dotnet", "tools")))
{
    update(cscs,
           pm: ".NET Tool",
           cmdExe: "dotnet",
           cmdArgs: "tool update --global cs-script.cli",
           noUpdateLogPattern: "is already installed");
}
else if (cscs.Contains(Path.Combine("ProgramData", "chocolatey")))
{
    update(cscs,
           pm: "Chocolatey",
           cmdExe: "choco",
           cmdArgs: "upgrade cs-script -y",
           noUpdateLogPattern: "is the latest version available");
}
else if (cscs.Contains(Path.Combine("WinGet", "Links")))
{
    update(cscs,
       pm: "WinGet",
       cmdExe: "winget",
       cmdArgs: "update cs-script",
       noUpdateLogPattern: "No available upgrade found.");
}
else
    WriteLine("CS-Script is either not installed or installed by the method that does not support automatic update. You will need to install/update CS-Script manually.");

// -----------------------------------------------
void update(string css, string pm, string cmdExe, string cmdArgs, string noUpdateLogPattern)
{
    WriteLine($"CS-Script is currently installed as a {pm} package:");
    WriteLine(css);
    WriteLine();

    WriteLine("Trying to update...");

    var output = Run(cmdExe, cmdArgs);

    WriteLine(output);

    if (output.Contains(noUpdateLogPattern))
    {
        WriteLine("No available update found.");
    }
    else
    {
        output = Run("css", "-self-install");
        WriteLine(output);
    }
}

string Run(string app, string arguments, bool inExternalConsole = false)
{
    StringBuilder sb = new();

    try
    {
        Process process = new Process();
        process.StartInfo.FileName = app;

        process.StartInfo.Arguments = arguments;

        // Console.WriteLine(">>>: " + arguments);

        if (!inExternalConsole)
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.CreateNoWindow = true;
        }
        process.Start();

        Thread outputThread = new Thread(() => HandleOutput(process, sb));
        outputThread.IsBackground = true;
        outputThread.Start();

        Thread errorThread = new Thread(() => HandleErrors(process, sb));
        errorThread.IsBackground = true;
        errorThread.Start();

        Thread inputThread = new Thread(() => HandleInput(process));
        inputThread.IsBackground = false;
        inputThread.Start();

        process.WaitForExit();
        Environment.ExitCode = process.ExitCode;

        outputThread.Join(1000);
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }

    return sb.ToString().Trim();
}

void HandleOutput(Process process, StringBuilder sb)
{
    try
    {
        var buffer = new char[255];
        int count = 0;

        while (-1 != (count = process.StandardOutput.Read(buffer, 0, buffer.Length)))
        {
            if (sb != null)
                sb.Append(buffer, 0, count);
            else
                Console.Write(buffer, 0, count);
            if (process.StandardOutput.EndOfStream)
                break;
        }
    }
    catch
    {
    }
}

void HandleErrors(Process process, StringBuilder sb)
{
    try
    {
        var chars = new char[255];
        int count = 0;

        while (-1 != (count = process.StandardError.Read(chars, 0, chars.Length)))
        {
            var enc = process.StandardError.CurrentEncoding;

            if (sb != null)
            {
                sb.Append(chars, 0, count);
            }
            else
            {
                var bytes = enc.GetBytes(chars, 0, count);
                Console.OpenStandardError().Write(bytes, 0, bytes.Length);
            }

            if (process.StandardError.EndOfStream)
                break;
        }
    }
    catch (Exception)
    {
    }
}

void HandleInput(Process process)
{
    try
    {
        ConsoleKeyInfo cki;

        while (!process.HasExited)
        {
            cki = Console.ReadKey(false);
            if (cki.Key == ConsoleKey.Enter)
                process.StandardInput.WriteLine("");
            else
                process.StandardInput.Write(cki.KeyChar);
        }
    }
    catch
    {
    }
}

void StartLinuxTerminal(string command)
{
    string[] terminals = { "gnome-terminal", "konsole", "x-terminal-emulator", "xfce4-terminal", "lxterminal" };

    foreach (var terminal in terminals)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = terminal,
                Arguments = $"-e bash -c \"{command}; exec bash\"",
                UseShellExecute = true
            });
            return; // Stop after the first successful attempt
        }
        catch (Exception)
        {
            // Ignore and try the next terminal
        }
    }

    Console.WriteLine("No supported terminal found.");
}