using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;

static class WebDbgExtensions
{
    public static string GetScriptPath(this HttpRequest request)
        => request.Query["script"].FirstOrDefault()?.Replace(".dbg.cs", ".cs");

    public static (DbgSession session, ObjectResult error) FindServerSession(this HttpRequest request)
    {
        var sessionId = request.Query["session"].FirstOrDefault() ?? "<unknown>";

        // if (!Server.Sessions.ContainsKey(sessionId))
        if (!Server.UserSessions.ContainsKey(sessionId))
            return (null, new ObjectResult($"No active user/tab session found for the specified script ('{request.GetScriptPath()}').") { StatusCode = 500 });

        var session = Server.UserSessions[sessionId].DebugSession;
        return (session, null);
    }
}

static class Extensions
{
    public static Dictionary<string, StringValues> ParseQuery(this string queryString)
        => Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString.TrimStart('?'));

    public static Dictionary<string, StringValues> ParseUriQuery(this string uri)
        => new Uri(uri).Query.ParseQuery();

    public static string EnsureLength(this string text, int desiredLength) =>
        desiredLength > text.Length ?
            text + new string(' ', desiredLength - text.Length) :
            text;

    public static bool HasText(this string text) => !string.IsNullOrEmpty(text);

    public static string qt(this string path) => $"\"{path}\"";

    public static string GetDirName(this string path) => Path.GetDirectoryName(path);

    public static ValueTask<string>? ClearField(this IJSObjectReference module, string id)
        => module?.InvokeAsync<string>("clearInputField", id);

    public static string JoinBy(this IEnumerable<string> request, string separator)
        => string.Join(separator, request);

    public static string BodyAsString(this HttpRequest request)
    {
        using (var reader = new StreamReader(request.Body))
            return reader.ReadToEndAsync().Result;
    }

    public static int? Parse(this ref int? intObject, string value)
    {
        if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var result))
            intObject = result;
        return intObject;
    }

    public static string PathJoin(this string path, params string[] items)
    {
        return System.IO.Path.Combine(new[] { path }.Concat(items).ToArray());
    }

    public static string EnsureDir(this string dir)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    public static byte[] GetBytes(this string data)
    {
        return Encoding.UTF8.GetBytes(data);
    }

    public static string GetString(this byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }

    public static byte[] ReadAllBytes(this TcpClient client)
    {
        var bytes = new byte[client.ReceiveBufferSize];
        var len = client.GetStream()
                      .Read(bytes, 0, bytes.Length);
        var result = new byte[len];
        Array.Copy(bytes, result, len);
        return result;
    }

    public static string ReadAllText(this TcpClient client)
    {
        return client.ReadAllBytes().GetString();
    }

    public static void WriteAllBytes(this TcpClient client, byte[] data)
    {
        var stream = client.GetStream();
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }

    public static void WriteAllText(this TcpClient client, string data)
    {
        client.WriteAllBytes(data.GetBytes());
    }
}

static class Shell
{
    static string cscs
    {
        get
        {
            // Engine file probing priorities:
            // 1. check if we are hosted by the script engine
            // 2. check if some f the parent folders contains the script engine assembly
            // 3. check if css shim is present on OS
            // 4. check the install directory (%CSSCRIPT_ROOT%) in case css is fully installed

            // 1
            var found =
                Environment.GetEnvironmentVariable("CSScriptRuntimeLocation") ??
                (Assembly.GetExecutingAssembly().GetName().Name == "cscs" ?
                    Assembly.GetExecutingAssembly().Location :
                    Assembly.GetEntryAssembly().GetName().Name == "cscs" ?
                        Assembly.GetEntryAssembly().Location :
                        null);

            if (found.HasText() && File.Exists(found))
                return found;
            else
                found = null;

            // 2
            var dir = Environment.CurrentDirectory;

            while (dir != null)
            {
                var candidate = Path.Combine(dir, "cscs.dll");
                if (File.Exists(candidate))
                {
                    found = candidate;
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }

            if (found.HasText() && File.Exists(found))
                return found;
            else
                found = null;

            // 3
            try
            {
                StringBuilder output = new();
                Shell.StartProcess("css", "-self", onStdOut: line => output.AppendLine(line?.Trim())).WaitForExit();
                found = output.ToString().Trim();

                if (File.Exists(found)) // may throw
                    return found;
            }
            catch { }

            // 4. Fallback to environment variable if not found
            found = Path.Combine(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_ROOT%"), "cscs.dll");

            if (!found.HasText() || !File.Exists(found))
                throw new Exception($"Cannot find cscs.dll. Please ensure it is available in the current directory or set the CSSCRIPT_ROOT environment variable.");
            return found;
        }
    }

    public static string dbg_inject => Environment.GetEnvironmentVariable("CSS_WEB_DEBUGGING_PREROCESSOR") ?? "<unknown path to dbg-inject.cs>";

    static public Process StartScript(this string script, string args, Action<string> onOutputData)
    {
        return Shell.StartProcess("dotnet",
                                  $"{cscs.qt()} -dbg {script.qt()} {args}",
                                  script.GetDirName(),
                                  onOutputData,
                                  onOutputData);
    }

    static public string RunScript(this string script, string args, Action<Process> onStart)
    {
        var output = "";

        // Console.WriteLine($"{cscs.qt()} {script.qt()} {args}");

        var inject = Shell.StartProcess(
                           "dotnet",
                           $"{cscs.qt()} {script.qt()} {args}",
                            script.GetDirName(),
                            x => output += x + Environment.NewLine,
                            x => output += x + Environment.NewLine);
        try
        {
            onStart(inject);
            inject.WaitForExit();
            if (inject.ExitCode == 0)
                return output;
        }
        catch
        {
        }

        throw new ApplicationException(output);
    }

    public static Process StartAssembly(this string assembly, string args, Action<string> onStdOut = null, Action<string> onErrOut = null)
        => StartProcess("dotnet", $"\"{assembly}\" +{args}", Path.GetDirectoryName(assembly), onStdOut, onErrOut);

    public static string StartProcessInTerminal(this string exe, string exeArgs, string workingDir)
    {
        var result = "";
        if (OperatingSystem.IsWindows())
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k \"{exe} {exeArgs}\"",
                WorkingDirectory = workingDir,
                UseShellExecute = true
            };
            Process.Start(psi);
            result = $"Started in external terminal.";
        }
        else if (OperatingSystem.IsLinux())
        {
            // Try gnome-terminal, konsole, xterm in order
            string[] terminals = { "gnome-terminal", "konsole", "xterm" };
            bool started = false;

            foreach (var terminal in terminals)
            {
                string args = terminal switch
                {
                    "gnome-terminal" => $"-- bash -c '{exe} {exeArgs}; exec bash'",
                    "konsole" => $"-e bash -c '{exe} {exeArgs}; exec bash'",
                    "xterm" => $"-e '{exe} {exeArgs}; bash'",
                    _ => ""
                };

                var psi = new ProcessStartInfo
                {
                    FileName = terminal,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    UseShellExecute = true
                };

                try
                {
                    Process.Start(psi);
                    result = $"Started in {terminal}.";
                    started = true;
                    break;
                }
                catch
                {
                    // Try next terminal
                }
            }

            if (!started)
                result = "No supported terminal emulator found (tried gnome-terminal, konsole, xterm).";
        }
        else
        {
            result = "External terminal launch is only supported on Windows and Linux.";
        }

        return result;
    }

    public static Process StartProcess(this string exe, string args, string dir = "", Action<string> onStdOut = null, Action<string> onErrOut = null)
    {
        Process proc = new();
        proc.StartInfo.FileName = exe;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.WorkingDirectory = dir;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.StartInfo.RedirectStandardInput = true;
        proc.StartInfo.EnvironmentVariables["CSS_WEB_DEBUGGING_URL"] = Environment.GetEnvironmentVariable("CSS_WEB_DEBUGGING_URL");
        proc.EnableRaisingEvents = true;
        proc.ErrorDataReceived += (_, e) => onErrOut?.Invoke(e.Data);
        proc.OutputDataReceived += (_, e) => onStdOut?.Invoke(e.Data);
        proc.Start();

        proc.BeginErrorReadLine();
        proc.BeginOutputReadLine();

        // var output = proc.StandardOutput.ReadToEnd();

        return proc;
    }
}