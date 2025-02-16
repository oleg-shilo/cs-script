//css_include global-usings
using System.Text;
using System;
using System.Diagnostics;
using CSScripting;
using static dbg;
using static System.Console;
using static System.Environment;

var thisScript = GetEnvironmentVariable("EntryScript");

var help =
@$"CS-Script custom command for...
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

WriteLine($"Checking the current installation...");
WriteLine();

var css = Run("css", "-self");
if (css.Contains(Path.Combine(".dotnet", "tools")))
{
    WriteLine("CS-Script is currently installed as a .NET Tool:");
    WriteLine(css);
    WriteLine();

    WriteLine("Trying to update...");
    var output = Run("dotnet", "tool update --global cs-script.cli");

    if (output.Contains("is already installed"))
    {
        WriteLine("No available upgrade found.");
    }
    else
    {
        output = Run("css", "-self-install");
        WriteLine(output);
    }
}
else if (css.Contains(Path.Combine("ProgramData", "chocolatey")))
{
    WriteLine("CS-Script is currently installed as a Chocolatey package:");
    WriteLine(css);
    WriteLine();

    WriteLine("Trying to update...");

    var output = Run("choco", "upgrade cs-script -y");

    if (output.Contains("is the latest version available"))
    {
        WriteLine("No available upgrade found.");
    }
    else
    {
        output = Run("css", "-self-install");
        WriteLine(output);
    }
}
else if (css.Contains(Path.Combine("WinGet", "Links")))
{
    WriteLine("CS-Script is currently installed as a WinGet package:");
    WriteLine(css);
    WriteLine();

    WriteLine("Trying to update...");

    var output = Run("winget", "update");

    if (output.Contains("No available upgrade found."))
    {
        WriteLine("No available upgrade found.");
    }
    else
    {
        output = Run("css", "-self-install");
        WriteLine(output);
    }
}
else
    WriteLine("CS-Script is either not installed or installed by the method that does not support automatic update. You will need to install/update CS-Script manually.");

// -----------------------------------------------

string Run(string app, string arguments)
{
    StringBuilder sb = new();

    try
    {
        Process process = new Process();
        process.StartInfo.FileName = app;

        process.StartInfo.Arguments = arguments;

        // Console.WriteLine(">>>: " + arguments);

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.CreateNoWindow = true;
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

        if (inputThread.IsAlive)
        {
            inputThread.IsBackground = true;
            try { inputThread.Abort(); } catch { }
        }

        // background threads anyway
        try { errorThread.Abort(); } catch { }
        try { outputThread.Abort(); } catch { }
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