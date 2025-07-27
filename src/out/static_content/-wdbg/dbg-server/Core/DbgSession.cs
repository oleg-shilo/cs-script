using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using wdbg.Pages;

static class Server
{
    public static Dictionary<string, UserSession> UserSessions = new();

    public static async Task PurgeAbandonedSessions()
    {
        Debug.WriteLine("UserSessions: " + Server.UserSessions.Count());
        foreach (var session in Server.UserSessions.Values.ToArray())
        {
            var connected = await session.IsConnectedToBrowser();
            if (!connected)
                lock (UserSessions)
                {
                    try
                    {
                        Debug.WriteLine($"Server.UserSessions.Remove({session.Id})");
                        Server.UserSessions.Remove(session.Id);
                    }
                    catch { }
                }
        }
    }

    public static async Task<(UserSession, bool created)> FindOrCreateUserSessionFor(this IJSRuntime interop, CodeMirrorPage mainPage)
    {
        // Despite the promise scoped DI objects are not unique for the browser tab. IE the builder.Services.AddScoped<UINotificationService>();
        // provides the same object for two tabs in the browser. This makes it impossible to host the independent debugging sessions.
        // Thus we need to create a globally available session entities based on the JS-based tab id (getOrCreateTabId).

        var sessionId = await interop.InvokeAsync<string>("getOrCreateTabId");
        UserSession session;
        bool created;

        lock (UserSessions)
        {
            created = !Server.UserSessions.ContainsKey(sessionId);
            session =
                Server.UserSessions.ContainsKey(sessionId) ?
                Server.UserSessions[sessionId] :
                Server.UserSessions[sessionId] = new(sessionId, mainPage, interop);
        }

        if (session.Editor.MainPage == null)
        {
            session.Editor.MainPage = mainPage; // ensure the page is set for the session editor
        }
        else
        {
            if (session.Editor.MainPage != mainPage)
            {
                // user refreshed the page so dispose the old page if it is not the same as the current one
                session.Editor.MainPage.Dispose();
                session.Editor.MainPage = mainPage;
            }
        }
        return (session, created);
    }
}

public class UserSession
{
    public string Id;

    public UserSession(string id, CodeMirrorPage page, IJSRuntime interop)
    {
        Id = id;
        Document = new();
        UIEvents = new();
        DebugSession = new(id, UIEvents);
        Interop = interop;
        Editor = new(page, interop, UIEvents);
    }

    public DbgSession DebugSession;
    public Document Document;
    public Ide Editor;
    public UINotificationService UIEvents;
    public IJSRuntime Interop;

    public bool IsLocalClient;

    /// <summary>
    /// Checks if the browser session is still connected by attempting to call JavaScript
    /// </summary>
    /// <returns>True if connected, false if disconnected</returns>
    public async Task<bool> IsConnectedToBrowser()
    {
        try
        {
            // Try to get the tab ID from the browser - this is a lightweight JS call
            var tabId = await Interop.InvokeAsync<string>("getOrCreateTabId");

            // If we got a response and it matches our session ID, the connection is alive
            bool isConnected = !string.IsNullOrEmpty(tabId) && tabId == Id;

            return isConnected;
        }
        catch (JSDisconnectedException)
        {
            // Browser tab was closed or connection lost
            return false;
        }
        catch (TaskCanceledException)
        {
            // Timeout or cancellation - likely disconnected
            return false;
        }
        catch (Exception)
        {
            // Any other JS interop error - likely disconnected
            return false;
        }
    }
}

/// <summary>
/// Class that holds the state of a debugging session.
/// </summary>
public class DbgSession
{
    public string Id;

    public DbgSession(string id, UINotificationService uiEvents)
    {
        this.Id = id; this.UIEvents = uiEvents ?? new();
    }

    public Dictionary<string, string> dbgScriptMaping = new();

    public Queue<string> UserRequest = new();
    public string UserInterrupt;
    public Process RunningScript;
    public string StackFrameFileName;
    public int? StackFrameLineNumber; // 1-based
    public int CurrentStepLineNumber => StackFrameLineNumber.HasValue ? StackFrameLineNumber.Value : -1; // 0-based
    public bool IsInBreakMode => StackFrameLineNumber.HasValue;
    public bool IsScriptExecutionInProgress => RunningScript != null;
    public UINotificationService UIEvents;
    public Dictionary<string, int[]> Breakpoints = new();

    static public DbgSession Current => throw new NotImplementedException();

    public void RequestObjectInfo(string expression)
    {
        lock (UserRequest)
            UserRequest.Enqueue($"serializeObject:{expression}");
    }

    public void RequestEvaluate(string expression)
    {
        lock (UserRequest)
            UserRequest.Enqueue($"evaluate:{expression}");
    }

    public void RequestStepOver()
    {
        lock (UserRequest)
            UserRequest.Enqueue("step_over");
        StackFrameLineNumber = null;
    }

    public void RequestStepIn()
    {
        lock (UserRequest)
            UserRequest.Enqueue("step_in");
        StackFrameLineNumber = null;
    }

    public void RequestStepOut()
    {
        lock (UserRequest)
            UserRequest.Enqueue("step_out");
        StackFrameLineNumber = null;
    }

    public void RequestPause()
    {
        UserInterrupt = "pause";
    }

    public void RequestResume()
    {
        lock (UserRequest)
            UserRequest.Enqueue("resume");
        StackFrameLineNumber = null;
    }

    public void Reset()
    {
        StackFrameLineNumber = null;
        StackFrameFileName = null;
        UserInterrupt = null;
        UserRequest.Clear();
        Breakpoints.Clear();
        RunningScript?.Dispose();
        RunningScript = null;

        UIEvents.NotifyDbgChanged(null);
    }

    // public void UpdateCurrentBreakpoints(int[] validBreakPointLines)
    // {
    //     lock (Breakpoints)
    //     {
    //         if (validBreakPointLines != null) // valid lines for placing break points are known
    //         {
    //             var invalidBreakPoints = Breakpoints.Except(validBreakPointLines).ToArray();
    //             var validBreakPoints = Breakpoints.Except(invalidBreakPoints).ToArray();

    //             Breakpoints.Clear();
    //             Breakpoints.AddRange(validBreakPoints);

    //             foreach (int invalidBreakPoint in invalidBreakPoints)
    //             {
    //                 var nextValidBreaktPointLine = validBreakPointLines.SkipWhile(x => x < invalidBreakPoint).FirstOrDefault();

    //                 if (nextValidBreaktPointLine != 0) // since default for int is 0
    //                 {
    //                     if (!validBreakPoints.Contains(nextValidBreaktPointLine))
    //                     {
    //                         Breakpoints.Add(nextValidBreaktPointLine);
    //                     }
    //                 }
    //             }
    //         }
    //     }
    // }
}