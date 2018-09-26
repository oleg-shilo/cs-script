using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;

//todo: start as shortcut scenario:
//create event here and set it in the cscs.exe as mutex claimed in the cscs.exe may be released if cscs.exe does not live long enough
//so css would believe console should not be visible
class ScriptLauncher
{
    static bool done = false;

    static Process process = new Process();
    static Thread inputThread = null;
    static bool cmdConsole = false;
    static bool nologo = false;
    static int lineCount = 0;

    static public void Run(string app, string arguments)
    {
        try
        {
            process.StartInfo.FileName = app;

            process.StartInfo.Arguments = arguments;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            Thread outputThread = new Thread(HandleOutput);
            outputThread.IsBackground = true;
            outputThread.Start();

            Thread errorThread = new Thread(HandleErrors);
            errorThread.IsBackground = true;
            errorThread.Start();

            inputThread = new Thread(HandleInput);
            inputThread.IsBackground = false;
            inputThread.Start();

            process.WaitForExit();
            Environment.ExitCode = process.ExitCode;

            outputThread.Join(1000);
            
            inputThread.IsBackground = true;
            inputThread.Abort();

            // background threads anyway
            errorThread.Abort(); 
            outputThread.Abort();
        }
        catch
        {
        }
    }

    static void HandleOutput()
    {
        try
        {
            var buffer = new char[255];
            int count = 0;
            while (-1 != (count = process.StandardOutput.Read(buffer, 0, buffer.Length)))
            {
                Console.Write(buffer, 0, count);
                if (process.StandardOutput.EndOfStream)
                    break;
            }

            done = true;
        }
        catch
        {
        }
    }

    static void HandleErrors()
    {
        try
        {
            var chars = new char[255];
            int count = 0;
            while (-1 != (count = process.StandardError.Read(chars, 0, chars.Length)))
            {
                Encoding enc = process.StandardError.CurrentEncoding;
                var bytes = enc.GetBytes(chars, 0, count);
                Console.OpenStandardError().Write(bytes, 0, bytes.Length);
                if (process.StandardError.EndOfStream)
                    break;
            }

            done = true;
        }
        catch
        {
        }
    }

    static void HandleInput()
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
}

