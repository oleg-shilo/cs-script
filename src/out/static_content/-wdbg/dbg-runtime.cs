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

public static class DBG
{
    static DBG()
    {
        Console.WriteLine("DBG-Server: " + debuggerUrl);
    }

    static string debuggerUrl => Environment.GetEnvironmentVariable("CSS_WEB_DEBUGGING_URL");// ?? "https://localhost:5001";

    public static string PostVars(string data)
    {
        try { return UploadString($"{debuggerUrl}/dbg/localvars", data); }
        catch { return ""; }
    }

    public static string PostBreak(string data)
    {
        try { return UploadString($"{debuggerUrl}/dbg/break", data); }
        catch { return ""; }
    }

    public static string UserRequest
    {
        get
        {
            try
            {
                return DownloadString($"{debuggerUrl}/dbg/userrequest");
            }
            catch { return ""; }
        }
    }

    public static string[] Breakpoints
    {
        get
        {
            try
            {
                return DownloadString($"{debuggerUrl}/dbg/breakpoints").Split('\n').Select(x => x.Trim()).ToArray();
            }
            catch { return new string[0]; }
        }
    }

    static string DownloadString(string url) => GetAsync(url).Result;

    static async Task<String> GetAsync(string url)
    {
        using var client = new HttpClient();
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

    public static BreakPoint bp([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
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

    string id => $"{sourceFilePath}:{sourceLineNumber}";

    void WaitTillResumed()
    {
        while (!IsStepOverRequested())
            Thread.Sleep(700);
    }

    bool IsStopRequested() => DBG.Breakpoints.Contains(id) || true;

    bool IsStepOverRequested() => DBG.UserRequest == "step_over";

    public void Inspect(params (string name, object value)[] variables)
    {
        // DBG.bp().Inspect(("testVar2", testVar2), ("testVar3", testVar3), ("i", i), ("testVar4", testVar4), ("testVar5", testVar5), ("yyy", yyy))

        if (!IsStopRequested())
            return;

        var localsJson = JsonSerializer.Serialize(
                            variables.Select(x => new
                            {
                                Name = x.name,
                                Value = x.value?.ToString(),
                                Type = x.value?.GetType().ToString()
                            }));

        DBG.PostBreak($"{sourceFilePath}|{sourceLineNumber}|{localsJson}");

        WaitTillResumed();
    }
}