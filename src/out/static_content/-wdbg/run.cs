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
using CSScripting;

static class Scipt
{
    static void Main(string[] args)
    {
        string urls = "https://localhost:5001";

        string script = args.FirstOrDefault()?.GetFullPath();           // first arg is the script to debug
        var serverArgs = "\"" + args.Skip(1).JoinBy("\" \"") + "\"";    // the rest of the args are to be interpreted by the server
        var serverDir = Path.Combine(Environment.CurrentDirectory, "dbg-server");
        var preprocessor = Path.Combine(Environment.CurrentDirectory, "dbg-decorate.cs");

        // --urls "http://localhost:5100;https://localhost:5101"
        urls = args.SkipWhile(x => x != "--urls").Skip(1).FirstOrDefault() ?? urls;

        // start wdbg server
        var proc = "dotnet".Start($"run \"{script}\" --urls \"{urls}\" -pre:{preprocessor} {serverArgs}", serverDir);

        // start browser with wdbg url
        if (!args.Contains("-suppress-browser"))
        {
            var url = urls.Split(';').FirstOrDefault();
            url.Start(UseShellExecute: true);
        }

        proc.WaitForExit();
    }

    static string GetFullPath(this string path) => path != null ? Path.GetFullPath(path) : null;

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