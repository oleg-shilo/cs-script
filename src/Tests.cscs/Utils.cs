using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static System.Reflection.BindingFlags;
using Xunit.Sdk;

class FactWinOnlyAttribute : Attribute
{
}

// public static class Testing
// {
//     public static void SkipIfOutsideIde()
//     {
//         if (IsMsTest())
//             throw SkipException.ForSkip("This test can only be run in Visual Studio");
//     }

//     public static bool IsMsTest()
//     {
//         if (Environment.OSVersion.Platform == PlatformID.Win32NT)
//         {
//             // Debugger.Launch();
//             return Process.GetCurrentProcess().ProcessName == "testhost";
//         }
//         return false;
//     }
// }

static class Extensions
{
    public static (string output, int exitCode) Run(this string exe, string args = null, string dir = null)
    {
        var process = new Process();

        process.StartInfo.FileName = exe;
        process.StartInfo.Arguments = args;
        process.StartInfo.WorkingDirectory = dir;

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        output += process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (output, process.ExitCode);
    }
}