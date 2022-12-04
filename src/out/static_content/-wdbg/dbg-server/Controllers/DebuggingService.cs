using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using wdbg.Controllers;

namespace wdbg.Controllers;

public static class InteropKeyPress
{
    static public Action<KeyboardEventArgs> OnKeyDown;
    static public Action<int> BreakpointAreaClicked;

    [JSInvokable]
    public static Task ToggeBreakpoint(object lineNumber)
    {
        // Console.WriteLine($"Toggle: {lineNumber}");
        try
        {
            int i = int.Parse(lineNumber.ToString());
            BreakpointAreaClicked?.Invoke(i);
        }
        catch { }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public static Task JsKeyDown(KeyboardEventArgs e)
    {
        // Console.WriteLine(e.Key);
        OnKeyDown?.Invoke(e);
        return Task.CompletedTask;
    }
}

[ApiController]
public class DbgController : ControllerBase
{
    [HttpGet("dbg/breakpoints")]
    public IEnumerable<string> GetBreakpoints()
    {
        return new string[]{
               @"D:\dev\Galos\Stack-Analyser\Program.cs:32",
               @"D:\dev\Galos\Stack-Analyser\Program.cs:35",
               @"D:\dev\Galos\Stack-Analyser\Program.cs:42",
               @"D:\dev\Galos\Stack-Analyser\Program.cs:35",
               @"D:\dev\Galos\Stack-Analyser\Program.cs:42",
               @"D:\dev\Galos\Stack-Analyser\Program.cs:24"};
    }

    [HttpGet("dbg/userrequest")]
    public ActionResult<string> GetRequest()
    {
        try
        {
            return Session.CurrentUserRequest;
        }
        finally
        {
            Session.CurrentUserRequest = null;
        }
    }

    [HttpPost("dbg/localvars")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> PostLocalVars()
    {
        Session.Variables = this.Request.BodyAsString();
        OnNewData?.Invoke();

        return "OK";
    }

    [HttpPost("dbg/break")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> PostBreakInfo()
    {
        var parts = this.Request.BodyAsString().Split('|', 3);

        Session.CurrentStackFrameFileName = parts[0]?.Replace(".dbg.cs", ".cs");
        Session.CurrentStackFrameLineNumber.Parse(parts[1]);
        Session.Variables = parts[2];

        OnBreak?.Invoke(Session.CurrentStackFrameFileName, Session.CurrentStackFrameLineNumber - 1, Session.Variables);

        return "OK";
    }

    static public Action OnNewData;
    static public Action<string, int?, string> OnBreak;
}

public class Session
{
    public static string Variables;
    public static string CurrentUserRequest;
    public static int? CurrentStackFrameLineNumber;
    public static string CurrentStackFrameFileName;
    public static List<int> CurrentBreakpoints = new();
}

public class DbgService
{
    static public string Prepare(string script)
        => Shell.RunScript(Shell.dbg_inject, script.qt());

    static public Process Start(string script, string args, Action<string> onOutputData)
        => Shell.StartScript(script, args, onOutputData);

    static public void StepOver()
        => Session.CurrentUserRequest = "step_over";

    static public void StepIn()
        => Session.CurrentUserRequest = "step_in";

    static public void Resume()
        => Session.CurrentUserRequest = "resume";
}

static class Extensions
{
    public static string qt(this string path) => $"\"{path}\"";

    public static string GetDirName(this string path) => Path.GetDirectoryName(path);

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
}

static class Shell
{
    public static string cscs => Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_ROOT%\cscs.dll");
    public static string dbg_inject => Environment.GetEnvironmentVariable("CSS_WEB_DEBUGGING_PREROCESSOR") ?? "<unknown path to dbg-inject.cs>";

    static public Process StartScript(this string script, string args, Action<string> onOutputData)
    {
        return Shell.StartProcess(
                            "dotnet",
                            $"{cscs.qt()} -dbg {script.qt()} {args}",
                            script.GetDirName(),
                            onOutputData,
                            onOutputData);
    }

    static public string RunScript(this string script, string args)
    {
        var output = "";

        var inject = Shell.StartProcess(
                            "dotnet",
                            $"{cscs.qt()} {script.qt()} {args}",
                            script.GetDirName(),
                            x => output += x,
                            x => output += x);

        inject.WaitForExit();
        if (inject.ExitCode == 0)
            return output;

        throw new Exception(output);
    }

    public static Process StartAssembly(this string assembly, string args, Action<string> onStdOut = null, Action<string> onErrOut = null)
        => StartProcess("dotnet", $"\"{assembly}\" +{args}", Path.GetDirectoryName(assembly), onStdOut, onErrOut);

    public static Process StartProcess(this string exe, string args, string dir, Action<string> onStdOut = null, Action<string> onErrOut = null)
    {
        Process proc = new();
        proc.StartInfo.FileName = exe;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.WorkingDirectory = dir;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
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