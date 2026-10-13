//css_args /nl
//css_ng csc
//css_include global-usings
using System.Diagnostics;
using System;
using System.Runtime.InteropServices;
using System.Security;
using CSScripting;
using static System.Environment;

var thisScript = GetEnvironmentVariable("EntryScript");

var help =
@$"Custom command for terminating the application listening (locking) the specified port.
v{thisScript.GetCommandScriptVersion()} ({thisScript})
Usage:
  css -port-kill <port>";

if ("?,-?,-help,--help".Split(',').Contains(args.FirstOrDefault()))
{
    print(help);
}
else
{
    if (!args.Any())
    {
        print("Please specify the port to free.");
        return;
    }

    try
    {
        var targetPort = args[0];

        var result = "netstat".run("-ano");

        // Find the line with the port
        var line = result.output
                    .GetLines()
                    .FirstOrDefault(l =>
                        l.Contains($":{targetPort}") &&
                        l.Trim().StartsWith("TCP", StringComparison.OrdinalIgnoreCase) &&
                        l.Contains("LISTENING"));

        if (line == null)
        {
            print($"No process is listening on port {targetPort}.");
            return;
        }

        // PID is the last token
        var pid = line.TrimEnd().Split(' ').LastOrDefault()?.ToInt();

        print($"Found PID {pid} listening on port {targetPort}.");

        // Kill the process
        var p = Process.GetProcessById(pid.Value);
        print($"Killing process: {p.ProcessName} (PID {pid})...");
        p.Kill();

        print("Process terminated successfully.");
    }
    catch (Exception ex)
    {
        print("Error: " + ex.Message);
    }
}