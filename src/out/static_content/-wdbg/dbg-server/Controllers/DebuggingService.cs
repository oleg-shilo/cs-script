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
        lock (Session.Current.Breakpoints)
        {
            return Session.Current.Breakpoints.Select(x => $"{Session.Current.StackFrameFileName?.Replace(".dbg.cs", ".cs")}:{x}").JoinBy("\n");
        }
    }

    [HttpGet("dbg/userrequest")]
    public ActionResult<string> GetRequest()
    {
        lock (typeof(DbgController))
        {
            try
            {
                return Session.Current.UserRequest;
            }
            finally
            {
                Session.Current.UserRequest = null;
            }
        }
    }


    [HttpPost("dbg/object")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> OnObjectInfo()
    {
        var evaluationData = this.Request.BodyAsString();
        OnObjectInspection?.Invoke(evaluationData);

        return "OK";
    }

    [HttpPost("dbg/break")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> OnPostBreakInfo()
    {
        var parts = this.Request.BodyAsString().Split('|', 3);

        Session.Current.StackFrameFileName = parts[0]?.Replace(".dbg.cs", ".cs");
        Session.Current.StackFrameLineNumber.Parse(parts[1]);
        Session.Current.Variables = parts[2];

        OnBreak?.Invoke(Session.Current.StackFrameFileName, Session.Current.StackFrameLineNumber, Session.Current.Variables);

        return "OK";
    }

    [HttpPost("dbg/expressions")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> OnExpressionInfo()
    {
        Session.Current.Watch = this.Request.BodyAsString();

        OnExpressionEvaluation?.Invoke(Session.Current.Watch);

        return "OK";
    }

    static public Action<string> OnExpressionEvaluation; // watch expression is added and its details have arrived
    static public Action<string> OnObjectInspection; // clicked the variable details have arrived
    static public Action<string, int?, string> OnBreak;
}

public class Session
{
    static Session()
    {
        Current = new Session();
    }

    public static Session Current;
    public string Watch;
    public string Variables;
    public string UserRequest;
    public int? StackFrameLineNumber;
    public string StackFrameFileName;
    public List<int> Breakpoints = new();

    public void UpdateCurrentBreakpoints(int[] validBreakPointLines)
    {
        lock (Breakpoints)
        {
            if (validBreakPointLines != null) // valid lines for placing break points are known
            {
                var invalidBreakPoints = Breakpoints.Except(validBreakPointLines).ToArray();
                var validBreakPoints = Breakpoints.Except(invalidBreakPoints).ToArray();

                Breakpoints.Clear();
                Breakpoints.AddRange(validBreakPoints);

                foreach (int invalidBreakPoint in invalidBreakPoints)
                {
                    var nextValidBreaktPointLine = validBreakPointLines.SkipWhile(x => x < invalidBreakPoint).FirstOrDefault();

                    if (nextValidBreaktPointLine != 0) // since default for int is 0
                    {
                        if (!validBreakPoints.Contains(nextValidBreaktPointLine))
                        {
                            Breakpoints.Add(nextValidBreaktPointLine);
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

    static public void SerializeObject(string expression)
    {
        Debug.WriteLine(expression);
        Session.Current.UserRequest = $"serializeObject:{expression}";

    }

    static public void Evaluate(string expression)
        => Session.Current.UserRequest = $"evaluate:{expression}";

    static public void StepOver()
        => Session.Current.UserRequest = "step_over";

    static public void StepIn()
        => Session.Current.UserRequest = "step_in";

    static public void Resume()
        => Session.Current.UserRequest = "resume";
}

static class Extensions
{
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