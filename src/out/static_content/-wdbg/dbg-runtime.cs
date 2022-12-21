//css_inc dbg-out.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public static class DBG
{
    static DBG()
    {
        if (Environment.GetEnvironmentVariable("CSS_WEB_DEBUGGING_URL") == null)
            Environment.SetEnvironmentVariable("CSS_WEB_DEBUGGING_URL", "https://localhost:5001");

        if (Environment.GetEnvironmentVariable("pauseOnStart") != null)
        {
            StopOnNextInspectionPointInMethod = "*";
        }
        // Console.WriteLine("DBG-Server: " + debuggerUrl);
    }

    public static string StopOnNextInspectionPointInMethod;
    static string debuggerUrl => Environment.GetEnvironmentVariable("CSS_WEB_DEBUGGING_URL");

    public static string PostObjectInfo(string name, object data)
    {
        try { return UploadString($"{debuggerUrl}/dbg/object", $"{name}:{data}"); }
        catch { return ""; }
    }

    public static string PostBreakInfo(string data)
    {
        try { return UploadString($"{debuggerUrl}/dbg/break", data); }
        catch { return ""; }
    }

    public static string PostExpressionInfo(string data)
    {
        try { return UploadString($"{debuggerUrl}/dbg/expressions", data); }
        catch { return ""; }
    }

    public static string UserRequest
    {
        get
        {
            try { return DownloadString($"{debuggerUrl}/dbg/userrequest"); }
            catch { return ""; }
        }
    }

    public static string[] Breakpoints
    {
        get
        {
            try
            {
                return DownloadString($"{debuggerUrl}/dbg/breakpoints")
                    .Split('\n')
                    .Select(x =>
                    {
                        // in the decorated script there is an extra line at top so increment the line number
                        var parts = x.Trim().Split(":");
                        var bp = $"{string.Join(":", parts[0..^1])}:{int.Parse(parts.Last()) + 1}";
                        return bp;
                    })
                    .ToArray();
            }
            catch { return new string[0]; }
        }
    }

    static string DownloadString(string url) => GetAsync(url).Result;

    static async Task<String> GetAsync(string url)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        var result = await client.GetStringAsync(url);
        return result;
    }

    static string UploadString(string url, string data) => PostAsync(url, data).Result;

    static async Task<String> PostAsync(string url, string data)
    {
        // var json = Newtonsoft.Json.JsonConvert.SerializeObject(person);

        using var client = new HttpClient();
        var response = await client.PostAsync(url, new StringContent(data, Encoding.UTF8, "application/text"));

        string result = await response.Content.ReadAsStringAsync();

        return result;
    }

    public static BreakPoint Line([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        => new BreakPoint
        {
            memberName = memberName,
            sourceFilePath = sourceFilePath,
            sourceLineNumber = sourceLineNumber
        };
}

public class BreakPoint
{
    public string memberName = "";
    public string sourceFilePath = "";
    public int sourceLineNumber = 0;

    string id => $"{sourceFilePath.Replace(".dbg.cs", ".cs")}:{sourceLineNumber}";

    void WaitTillResumed((string name, object value)[] variables)
    {
        var watchExpressions = new Dictionary<string, object>();

        while (true)
        {
            var request = DBG.UserRequest;

            if (request.StartsWith("serializeObject:"))
            {
                var varName = request.Replace("serializeObject:", "");
                Debug.WriteLine(varName);
                if (variables.Any(x => x.name == varName))
                {

                    var value = variables.First(x => x.name == varName).value;
                    var view = value.Serialize();
                    DBG.PostObjectInfo(varName, view);
                }
                else
                    DBG.PostObjectInfo(varName, "<cannot serialize variable>");
            }

            if (request.StartsWith("evaluate:"))
            {
                var expression = request.Replace("evaluate:", "");
                object expressionValue = "<unknown>";

                var localVar = variables.FirstOrDefault(x => x.name == expression);

                if (localVar.name != null)
                    expressionValue = localVar.value;

                // if not found then
                watchExpressions[expression] = expressionValue;

                var info = watchExpressions.Select(x => (x.Key, x.Value)).ToJson();
                DBG.PostExpressionInfo(info);
            }

            // Debug.WriteLine("Waiting for resuming. User request: " + request);

            // StepIn means just continue for the very next point of inspection
            // which can be either next line in the same method or in the called (child) method
            if (IsStepInRequested(request))
            {
                DBG.StopOnNextInspectionPointInMethod = "*";
                break;
            }

            // continue to the next point of inspection but only in the same method
            if (IsStepOverRequested(request))
            {
                DBG.StopOnNextInspectionPointInMethod = memberName;
                break;
            }

            if (IsResumeRequested(request))
            {
                DBG.StopOnNextInspectionPointInMethod = null;
                break;
            }

            Thread.Sleep(700);
        }
    }

    bool ShouldStop()
    {
        if (DBG.StopOnNextInspectionPointInMethod == "*")
        {
            DBG.StopOnNextInspectionPointInMethod = null;
            return true;
        }

        if (memberName == DBG.StopOnNextInspectionPointInMethod)
        {
            DBG.StopOnNextInspectionPointInMethod = null;
            return true;
        }

        var bp = DBG.Breakpoints;

        if (DBG.Breakpoints.Contains(id))
        {
            DBG.StopOnNextInspectionPointInMethod = null;
            return true;
        }
        return false;
    }

    bool IsStepOverRequested(string request) => request == "step_over";

    bool IsStepInRequested(string request) => request == "step_in";

    bool IsResumeRequested(string request) => request == "resume";

    public void Inspect(params (string name, object value)[] variables)
    {
        if (!ShouldStop())
            return;

        DBG.PostBreakInfo($"{sourceFilePath}|{sourceLineNumber - 1}|{variables.ToJson()}"); // let debugger to show BP as the start of the next line

        WaitTillResumed(variables);
    }
}

static class dbg_extensions
{
    static Type[] primitiveTypes = new[]
    { typeof(string),typeof(Boolean) ,typeof(Byte) ,typeof(SByte) ,typeof(Int16) ,typeof(UInt16) ,typeof(Int32) ,typeof(UInt32)
        ,typeof(Int64) ,typeof(UInt64) ,typeof(Char) ,typeof(Double) ,typeof(Single) };

    public static bool IsPrimitiveType(this object obj) => primitiveTypes.Contains(obj.GetType());

    public static string ToJson(this IEnumerable<(string name, object value)> variables)
    {
        return JsonSerializer.Serialize(variables.Select(x => new
        {
            Name = x.name,
            Value = x.value?.ToString()?.TruncateWithElipses(100),
            Type = x.value?.GetType().ToString()
        }));
    }
    public static string Serialize(this object obj)
    {
        // does not print read-only props
        // var view = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true, IgnoreReadOnlyProperties = false });
        return wdbg.dbg.print(obj);
    }

    public static string TruncateWithElipses(this string text, int maxLength)
    {
        if (text.Length > maxLength - 3)
            return text.Substring(maxLength - 3) + "...";
        return text;
    }

}