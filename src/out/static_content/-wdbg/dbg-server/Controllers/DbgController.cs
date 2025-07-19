using System;
using System.Diagnostics;
using System.Text;
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
            // we are sending the line indexes of the decorated file

            // Debug.WriteLine($"GetBreakpoints");
            var buffer = new StringBuilder();
            foreach (var kvp in breakpoints)
            {
                var file = kvp.Key;
                var lines = kvp.Value;
                var isPrimary = (file == breakpoints.Keys.First());

                lines = lines.Select(x => x + 1).ToArray();

                foreach (var line in lines)
                {
                    buffer.Append($"{file}:{line}\n");
                }
                // Debug.WriteLine($"  {file?.GetFileName()}: {lines.Select(x => x.ToString()).JoinBy(",")}");
            }

            var result = buffer.ToString();

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

            var lines = this.Request.BodyAsString().Split('\n');
            var parts = lines.First().Split('|', 3);
            var variables = parts[2];
            var primaryScript = session.dbgScriptMaping.First().Key;

            var dbgFile = parts[0]?.Trim();
            string sourceFileUnderBreak = session.dbgScriptMaping.First(x => x.Value == dbgFile).Key;

            session.StackFrameFileName = sourceFileUnderBreak;
            session.StackFrameLineNumber = parts[1].ToInt() - 1;

            int primaryScriptExtraLinesCount = 0;
            primaryScriptExtraLinesCount++;                                      // for dbg-runtime.cs
            primaryScriptExtraLinesCount += session.Breakpoints.Skip(1).Count(); // for any imported script file

            var isPrimary = (sourceFileUnderBreak.GetFileName() == session.Breakpoints.Keys.First().GetFileName());

            if (isPrimary)
            {
                session.StackFrameLineNumber -= primaryScriptExtraLinesCount;
            }

            // Program.<Main>$[test.cs:9]
            var callStack = lines.Skip(1).FirstOrDefault().Split('|')
                .Select(x =>
                        {
                            var parts = x.Split('[');
                            var method = parts[0].Trim();
                            var fileAndLine = parts[1].TrimEnd(']').Split(':');
                            var file = fileAndLine[0].Trim();
                            var line = fileAndLine[1].ToInt();
                            if (primaryScript.GetFileName() == fileAndLine.First()) // primary script
                            {
                                line -= primaryScriptExtraLinesCount;
                            }
                            return $"{method}:{file}:{line}";
                        }).JoinBy("|");

            session.UIEvents.NotifyStateChanged(); // to refresh the output window
            session.UIEvents.NotifyDbgChanged(variables, callStack);   // to show the current debug step and call stack
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