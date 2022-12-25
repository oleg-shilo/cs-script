//css_inc dbg-out.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
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
        // keep resolved expressions for possible serialization requests 
        var watchExpressions = new Dictionary<string, object>();

        while (true)
        {
            var request = DBG.UserRequest;

            if (request.StartsWith("serializeObject:"))
                SerializeObject(variables, watchExpressions, request.Replace("serializeObject:", ""));

            if (request.StartsWith("evaluate:"))
                EvaluateExpression(variables, watchExpressions, request.Replace("evaluate:", ""));

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

    private static void SerializeObject((string name, object value)[] variables, Dictionary<string, object> watchExpressions, string varName)
    {
        // Debug.WriteLine(varName);
        if (variables.Any(x => x.name == varName))
        {
            var value = variables.First(x => x.name == varName).value;
            var view = value.Serialize();
            DBG.PostObjectInfo(varName, view);
        }
        else if (watchExpressions.ContainsKey(varName))
        {
            var value = watchExpressions[varName];
            var view = value.Serialize();
            DBG.PostObjectInfo(varName, view);
        }
        else
            DBG.PostObjectInfo(varName, "<cannot serialize variable>");
    }

    private static void EvaluateExpression((string name, object value)[] variables, Dictionary<string, object> watchExpressions, string expression)
    {
        object expressionValue = null;

        var localVar = variables.FirstOrDefault(x => x.name == expression);

        // expression is a name of the local variable
        if (localVar.name != null)
        {
            expressionValue = localVar.value ?? "null";
        }
        else
        {
            try
            {
                object currrentObject = null;

                object @this = variables.FirstOrDefault(x => x.name == "this").value;

                var tokens = expression.Split('.');


                // expression is the local variable member(s) (e.g. `myObj.Name`)
                var matchingVariable = variables.FirstOrDefault(x => x.name == tokens.First());
                if (matchingVariable.name != null)
                {
                    currrentObject = matchingVariable.value;
                    if (Dereference(ref currrentObject, tokens.Skip(1)))
                        expressionValue = currrentObject ?? "null";
                }

                // expression is the member of 'this' (e.g. `Name.Length`) but user did nos specify "this"
                if (expressionValue == null && @this != null)
                {
                    currrentObject = @this;
                    if (Dereference(ref currrentObject, tokens))
                        expressionValue = currrentObject ?? "null";
                }

                // expression is the chain of static members (e.g. `System.Environment.TickCOunt`)
                if (expressionValue == null)
                {

                    if (DereferenceStatic(ref currrentObject, tokens))
                        expressionValue = currrentObject ?? "null";
                    else if (DereferenceStatic(ref currrentObject, new[] { "System" }.Concat(tokens))) // in case if user did not specify well knows namespace
                        expressionValue = currrentObject ?? "null";

                }
            }
            catch (Exception ex)
            {
                expressionValue = $"<cannot evaluate '{ex.Message}'>";
            }
        }

        watchExpressions[expression] = expressionValue;

        var info = watchExpressions.Select(x => (x.Key, x.Value)).ToJson();
        DBG.PostExpressionInfo(info);
    }

    static bool DereferenceStatic(ref object obj, IEnumerable<string> expression)
    {
        bool dereferenced = false;
        Type rootType = null;
        string typeName = "";
        object currentObject = null;
        obj = null;

        foreach (var item in expression)
        {
            if (rootType == null)
            {
                if (typeName.Any())
                    typeName += "." + item;
                else
                    typeName = item;

                var matchingTypes = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetType(typeName)).Distinct().Where(x => x != null);
                if (matchingTypes.Any())
                {
                    if (matchingTypes.Count() > 1)
                    {
                        obj = "<the type is defined in multiple assemblies>";
                        break;
                    }
                    rootType = matchingTypes.First();
                }
            }
            else
            {
                var typeProp = rootType.GetProperty(item);
                var typeField = rootType.GetField(item);


                if (typeProp != null || typeField == null)
                {
                    if (typeProp != null)
                        currentObject = typeProp.GetValue(null);
                    else if (typeField != null)
                        currentObject = typeField.GetValue(null);

                    dereferenced = true;
                    rootType = currentObject?.GetType();
                }
                else
                {
                    obj = $"<cannot evaluate name '{item}'>";
                    break;
                }
            }
        }

        if (obj == null)
        {
            if (dereferenced)
                obj = currentObject;
            else
                obj = $"<cannot evaluate name '{string.Join('.', expression)}'>";
        }
        return dereferenced;
    }
    static bool Dereference(ref object obj, IEnumerable<string> members)
    {

        object currrentObject = obj;

        foreach (var memberName in members)
        {
            var objProp = currrentObject.GetType().GetProperty(memberName);
            var objField = currrentObject.GetType().GetField(memberName);

            if (objProp != null)
                currrentObject = objProp.GetValue(currrentObject);
            else if (objField != null)
                currrentObject = objField.GetValue(currrentObject);
            else
                return false;
        }

        obj = currrentObject;
        return true;
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
    public static bool IsPrimitiveType(this object obj) => Aliases.ContainsKey(obj.GetType());
    public static string ToView(this Type type) => Aliases.ContainsKey(type) ? Aliases[type] : type.ToString();

    public static readonly Dictionary<Type, string> Aliases = new Dictionary<Type, string>()
        {
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(void), "void" }
        };

    public static string ToJson(this IEnumerable<(string name, object value)> variables)
    {
        return JsonSerializer.Serialize(variables.Select(x => new
        {
            Name = x.name,
            Value = x.value?.ToString()?.TruncateWithElipses(100),
            Type = x.value?.GetType()?.ToView()
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