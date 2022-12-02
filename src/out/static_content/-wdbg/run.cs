using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Text;
using System;
using System.Linq;
using System.Threading;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Numerics;

static class Scipt
{
    static void Main(string[] args)
    {
        int port = 5001;
        string ip = "https://localhost";
        string url = $"{ip}:{port}";

        string script = args.Take(1).FirstOrDefault();
        if (script != null)
            script = Path.GetFullPath(script);

        Environment.SetEnvironmentVariable("CSS_WEB_DEBUGGING_PORT", $"{port}");
        Environment.SetEnvironmentVariable("CSS_WEB_DEBUGGING_PREROCESSOR", Path.Combine(Environment.CurrentDirectory, "dbg-decorate.cs"));

        if (args.Contains("-start-browser"))
            $"https://localhost:{port}".Start(UseShellExecute: true);

        var serverArgs = string.Join("\" \"", args.Skip(1));

        "dotnet".Start($"run \"{script}\" -url:{url} {serverArgs}", Path.Combine(Environment.CurrentDirectory, "dbg-server"))
                .WaitForExit();
    }

    static Process Start(this string exe, string args = "", string dir = null, bool UseShellExecute = false)
    {
        Process proc = new();
        proc.StartInfo.FileName = exe;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.UseShellExecute = UseShellExecute;
        proc.StartInfo.WorkingDirectory = dir;
        proc.Start();
        return proc;
    }
}