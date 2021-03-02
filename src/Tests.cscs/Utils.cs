using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static System.Reflection.BindingFlags;

class FactWinOnlyAttribute : Attribute
{
}

static class Extensions
{
    public static string Run(this string exe, string args = null, string dir = null)
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
        process.WaitForExit();

        return output;
    }
}