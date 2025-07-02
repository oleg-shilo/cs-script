using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("dbg")]
public class DbgController : ControllerBase
{
    [HttpGet("breakpoints")]
    public IActionResult GetBreakpoints()
    {
        (DbgSession session, ObjectResult error) = Request.FindServerSession();

        if (error != null)
            return error;

        var fileWithBreakpoints = Request.GetScriptPath(); // debug host is requesting the breakpoints for the script file being executed
        var breakpoints = session.Breakpoints;// zos in the future we will need to read the breakpoints for a specific script file even if it is not open (e.g. multi-file script)

        lock (session)
        {
            // note, dbg host lines are 1-based

            Debug.WriteLine($"GetBreakpoints");
            foreach (var kvp in breakpoints)
            {
                var file = kvp.Key;
                var lines = kvp.Value;
                if (lines.Any())
                    Debug.WriteLine($"  {file?.GetFileName()}: {lines.Select(x => (x + 1).ToString()).JoinBy(",")}");
            }

            var result = breakpoints.SelectMany(x => x.Value.Select(line => $"{x.Key}:{line + 1}")).JoinBy("\n");

            return Ok(result);
        }
    }

    [HttpPost("break")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> OnPostBreakInfo()
    {
        (DbgSession session, ObjectResult error) = Request.FindServerSession();

        lock (session)
        {
            // if debugging is not in progress the session.StackFrameFileName will be empty
            // note, dbg host lines are 1-based

            if (error != null)
                return error;

            var parts = this.Request.BodyAsString().Split('|', 3);

            var dbgFile = parts[0]?.Trim();
            string scriptFile = session.dbgScriptMaping.First(x => x.Value == dbgFile).Key;

            session.StackFrameFileName = scriptFile;
            session.StackFrameLineNumber = parts[1].ToInt() - 1;

            Debug.WriteLine($"OnPostBreakInfo: {scriptFile?.GetFileName()} at line {session.StackFrameLineNumber}");

            session.UIEvents.NotifyStateChanged(); // to refresh the output window
            session.UIEvents.NotifyDbgChanged(variables: parts[2]);   // to show the current debug step
        }
        return "OK";
    }

    [HttpPost("output")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> DbgOutput()
    {
        var message = this.Request.BodyAsString();
        Debug.WriteLine(message);
        return "OK";
    }

    [HttpGet("userrequest")]
    public ActionResult<string> GetRequest()
    {
        (DbgSession session, ObjectResult error) = Request.FindServerSession();

        if (error != null)
            return error;

        lock (typeof(DbgController))
        {
            lock (session.UserRequest)
            {
                if (session.UserRequest.Count == 0)
                    return "";

                var request = session.UserRequest.Dequeue();
                // Debug.WriteLine($"Pass Request to: {request}");
                return request;
            }
        }
    }

    [HttpGet("userinterrupt")]
    public ActionResult<string> GetInterrupt()
    {
        (DbgSession session, ObjectResult error) = Request.FindServerSession();

        if (error != null)
            return error;

        lock (typeof(DbgController))
        {
            try
            {
                return session.UserInterrupt;
            }
            finally
            {
                session.UserInterrupt = null;
            }
        }
    }

    [HttpPost("object")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> OnObjectInfo()
    {
        (DbgSession session, ObjectResult error) = Request.FindServerSession();

        var objectInfo = this.Request.BodyAsString();
        session.UIEvents.NotifyObjectValueReceived("objectInfo:" + objectInfo);

        return "OK";
    }

    [HttpPost("expressions")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> OnExpressionInfo()
    {
        (DbgSession session, ObjectResult error) = Request.FindServerSession();

        var variables = this.Request.BodyAsString();
        session.UIEvents.NotifyObjectValueReceived("variables:" + variables);   // to show the current debug frame variables

        return "OK";
    }
}