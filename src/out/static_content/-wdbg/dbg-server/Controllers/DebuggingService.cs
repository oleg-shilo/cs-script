using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop;
using System.Diagnostics;

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
    public string GetBreakpoints()
    {
        lock (Session.CurrentBreakpoints)
        {
            return Session.CurrentBreakpoints.Select(x => $"{Session.CurrentStackFrameFileName?.Replace(".dbg.cs", ".cs")}:{x}").JoinBy("\n");
        }
    }

    [HttpGet("dbg/userrequest")]
    public ActionResult<string> GetRequest()
    {
        lock (typeof(DbgController))
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
    }


    [HttpPost("dbg/localvar")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> PostVar()
    {
        var evaluationData = this.Request.BodyAsString();
        OnVarEvaluation?.Invoke(evaluationData);

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

        OnBreak?.Invoke(Session.CurrentStackFrameFileName, Session.CurrentStackFrameLineNumber, Session.Variables);

        return "OK";
    }

    static public Action OnNewData;
    static public Action<string> OnVarEvaluation;
    static public Action<string, int?, string> OnBreak;
}

public class Session
{
    public static string Variables;
    public static string CurrentUserRequest;
    public static int? CurrentStackFrameLineNumber;
    public static string CurrentStackFrameFileName;
    public static List<int> CurrentBreakpoints = new();

    public static void UpdateCurrentBreakpoints(int[] validBreakPointLines)
    {
        lock (CurrentBreakpoints)
        {
            if (validBreakPointLines != null) // valid lines for placing break points are known
            {
                var invalidBreakPoints = CurrentBreakpoints.Except(validBreakPointLines).ToArray();
                var validBreakPoints = CurrentBreakpoints.Except(invalidBreakPoints).ToArray();

                CurrentBreakpoints.Clear();
                CurrentBreakpoints.AddRange(validBreakPoints);

                foreach (int invalidBreakPoint in invalidBreakPoints)
                {
                    var nextValidBreaktPointLine = validBreakPointLines.SkipWhile(x => x < invalidBreakPoint).FirstOrDefault();

                    if (nextValidBreaktPointLine != 0) // since default for int is 0
                    {
                        if (!validBreakPoints.Contains(nextValidBreaktPointLine))
                        {
                            CurrentBreakpoints.Add(nextValidBreaktPointLine);
                        }
                    }
                }
            }
        }
    }
}

public class DbgService
{
    static public (string decoratedScript, int[] breakpouints) Prepare(string script)
    {
        var output = Shell.RunScript(Shell.dbg_inject, script.qt());
        var lines = output.Split("\n").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x));

        var decoratedScript = lines.First();
        var breakpoints = lines.Skip(1).SelectMany(x => x.Split(',').Select(i => int.Parse(i))).ToArray();

        return (decoratedScript, breakpoints);
    }

    static public Process Start(string script, string args, Action<string> onOutputData)
        => Shell.StartScript(script, args, onOutputData);

    static public void EvaluateExpression(string expression)
        => Session.CurrentUserRequest = $"evaluateExpression:{expression}";
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
                            x => output += x + Environment.NewLine,
                            x => output += x + Environment.NewLine);

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
        proc.StartInfo.RedirectStandardInput = true;
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