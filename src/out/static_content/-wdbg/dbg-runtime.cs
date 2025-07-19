using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using csscript;
using CSScripting;
using wdbg;

public static class DBG
{
    static DBG()
    {
        if (Environment.GetEnvironmentVariable("CSS_WEB_DEBUGGING_URL") == null)
            throw new Exception("Server URL is unknown");

        if (Environment.GetEnvironmentVariable("pauseOnStart") != null)
        {
            StopOnNextInspectionPointInMethod = "*";
        }
    }

    public static string StopOnNextInspectionPointInMethod;
    static string debuggerUrl => Environment.GetEnvironmentVariable("CSS_WEB_DEBUGGING_URL");

    public static string ScriptPath => Environment.GetEnvironmentVariable("EntryScript");

    public static string SessionId => Environment.GetEnvironmentVariable("CSS_DBG_SESSION");

    static int GetDecorationOffset(string sourceFilePath)
    {
        int offset = 0;
        var decoretedScriptsDir = Path.GetDirectoryName(sourceFilePath);
        var isPrimaryScript = Path.GetFileName(sourceFilePath) == Path.GetFileName(decoretedScriptsDir);

        if (isPrimaryScript)
        {
            var bpFile = Directory.GetFiles(decoretedScriptsDir, "*.cs.bp").FirstOrDefault();
            var scriptSourceFilesCount = File.ReadAllLines(bpFile).Count();

            var importedScriptsCount = scriptSourceFilesCount - 1;
            offset += importedScriptsCount;
            offset += 1;  // for the import of dbg-runtime.cs
        }

        return offset;
    }

    public static string[] Breakpoints
    {
        get
        {
            var url = ToUri("/dbg/breakpoints");
            try
            {
                return DownloadString(url)
                    .Split('\n')
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x =>
                    {
                        // in the decorated script there is an extra line at top so increment the line number
                        var parts = x.Trim().Split(":");
                        var file = string.Join(":", parts[0..^1]);
                        var line = int.Parse(parts.Last());
                        line += GetDecorationOffset(file);
                        return $"{file}:{line}";
                    })
                    .ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot get breakpoints from " + url);
                if (e.InnerException is System.Net.Http.HttpRequestException &&
                    e.InnerException.InnerException is System.Security.Authentication.AuthenticationException)
                {
                    Console.WriteLine("=========");
                    Console.WriteLine("You may experience problems with self-signed SSL certificates in some environments. " +
                                      "In such cases you may switch to HTTP protocol. You can do it either via CLI argument " +
                                      "when you start the debugger or you can do it globally by setting the environment variable " +
                                      "'CSS_WEB_DEBUGGING_URL' to the desired URL (e.g. 'export CSS_WEB_DEBUGGING_URL=http://localhost:5001').");
                    Console.WriteLine("=========");
                    Console.WriteLine(e.InnerException);
                }
                throw;
            }
        }
    }

    public static string UserRequest
    {
        get
        {
            try { return DownloadString(ToUri("/dbg/userrequest")); }
            catch { return ""; }
        }
    }

    public static string UserInterrupt
    {
        get
        {
            try { return DownloadString(ToUri("/dbg/userinterrupt")); }
            catch { return ""; }
        }
    }

    public static string PostBreakInfo(string data)
    {
        try { return UploadString(ToUri("/dbg/break"), data); }
        catch { return ""; }
    }

    public static string PostObjectInfo(string name, object data)
    {
        try { return UploadString(ToUri("/dbg/object"), $"{name}:{data}"); }
        catch { return ""; }
    }

    public static string PostExpressionInfo(string data)
    {
        try { return UploadString(ToUri("/dbg/expressions"), data); }
        catch { return ""; }
    }

    public static void DebugOutputLine(string message)
    {
        try
        {
            var uri = ToUri("/dbg/output");
            UploadString(uri, message);
        }
        catch { }
    }

    static string ToUri(string endPoint) => $"{debuggerUrl}{endPoint}?script={ScriptPath}&session={SessionId}";

    static string DownloadString(string url) => GetAsync(url).Result;

    static async Task<String> GetAsync(string url)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        var result = await client.GetStringAsync(url);
        return result;
    }

    static string UploadString(string uri, string data) => PostAsync(uri, data).Result;

    static async Task<String> PostAsync(string uri, string data)
    {
        // var json = Newtonsoft.Json.JsonConvert.SerializeObject(person);

        using var client = new HttpClient();
        var response = await client.PostAsync(uri, new StringContent(data, Encoding.UTF8, "application/text"));

        string result = await response.Content.ReadAsStringAsync();

        return result;
    }

    public static BreakPoint Line([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        // Console.WriteLine($"DBG.Line() called from {sourceFilePath}:{sourceLineNumber} in {memberName}");

        var result = new BreakPoint
        {
            methodDeclaringType = new StackFrame(1).GetMethod().ReflectedType.ToString(),
            methodName = memberName,
            runtimeCallChain = new StackTrace(true).GetFrames().GetCallChain(),
            sourceFilePath = sourceFilePath,
            sourceLineNumber = sourceLineNumber
        };

        // Console.WriteLine($"DBG.Line(): {(string.Join(">", result.CallingMethods.Reverse()))}");

        return result;
    }
}

public class BreakPoint
{
    public string methodDeclaringType = "";
    public string methodName = "";
    public string[] runtimeCallChain = [];

    // "Program.Main (program.cs:10)"
    public string[] CallingMethods => runtimeCallChain.Select(x => x.Split('[').First().Trim()).ToArray();

    public string sourceFilePath = "";
    public int sourceLineNumber = 0;
    static bool monitorStarted = false;

    string id => $"{sourceFilePath.Replace(".dbg.cs", ".cs")}:{sourceLineNumber}";

    void WaitTillResumed((string name, object value)[] variables)
    {
        // keep resolved expressions for possible serialization requests
        var watchExpressions = new Dictionary<string, object>();

        while (true)
        {
            var request = DBG.UserRequest;

            // Console.WriteLine($"DBG.UserRequest: {request}");

            if (request.StartsWith("serializeObject:"))
                SerializeObject(variables, watchExpressions, request.Replace("serializeObject:", ""));

            if (request.StartsWith("evaluate:"))
                EvaluateExpression(variables, watchExpressions, request.Replace("evaluate:", ""));

            // Debug.WriteLine("Waiting for resuming. User request: " + request);

            // StepIn means just continue for the very next point of inspection
            // which can be either next line in the same method or in the called (child) method
            if (IsStepInRequested(request))
            {
                // DBG.DebugOutputLine($"Step-In/Pause requested in {methodName} at {sourceFilePath}:{sourceLineNumber}");
                DBG.StopOnNextInspectionPointInMethod = "*"; // very next breakpoint available in the script (even inside of the other method)
                break;
            }

            if (PauseRequested(request))
            {
                // DBG.DebugOutputLine($"Pause requested...");
                DBG.StopOnNextInspectionPointInMethod = "*"; // very next breakpoint available in the script
                break;
            }

            // continue to the next point of inspection
            if (IsStepOverRequested(request))
            {
                // DBG.DebugOutputLine($"Step-Over requested in {methodName} at {sourceFilePath}:{sourceLineNumber}");
                // DBG.StopOnNextInspectionPointInMethod = methodName;
                DBG.StopOnNextInspectionPointInMethod = CallingMethods.JoinBy("|");
                //DBG.StopOnNextInspectionPointInMethod = "*";
                break;
            }

            if (IsStepOutRequested(request))
            {
                // DBG.DebugOutputLine($"Step-Out requested in {methodName} at {sourceFilePath}:{sourceLineNumber}");
                DBG.StopOnNextInspectionPointInMethod = CallingMethods.Skip(1).JoinBy("|");
                break;
            }

            if (IsResumeRequested(request))
            {
                DBG.StopOnNextInspectionPointInMethod = null;
                break;
            }

            Thread.Sleep(700);
        }
        // Console.WriteLine($"DBG.StopOnNextInspectionPointInMethod: {DBG.StopOnNextInspectionPointInMethod}");
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

    void EvaluateExpression((string name, object value)[] variables, Dictionary<string, object> watchExpressions, string expression)
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

                // expression is the chain of static members (e.g. `System.Environment.TickCount`)
                if (expressionValue == null)
                {
                    if (DereferenceStatic(ref currrentObject, tokens))
                        expressionValue = currrentObject ?? "null";
                    else if (DereferenceStatic(ref currrentObject, new[] { methodDeclaringType }.Concat(tokens))) // in case if user did not specify well knows namespace
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

                if (typeProp != null || typeField != null)
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

        if (DBG.StopOnNextInspectionPointInMethod?.Split('|').Contains($"{methodDeclaringType}.{methodName}") == true)
        {
            DBG.StopOnNextInspectionPointInMethod = null;
            return true;
        }

        var bp = DBG.Breakpoints;
        Console.WriteLine($"----");
        Console.WriteLine($"  DBG.Breakpoints: {string.Join("\n                   ", bp.Select(x => Path.GetFileName(x)))}");
        Console.WriteLine($"  id: {id}");
        Console.WriteLine($"----");
        if (DBG.Breakpoints.Contains(id))
        {
            DBG.StopOnNextInspectionPointInMethod = null;
            return true;
        }
        return false;
    }

    bool IsStepOutRequested(string request) => request == "step_out";

    bool IsStepOverRequested(string request) => request == "step_over";

    bool IsStepInRequested(string request) => request == "step_in";

    bool PauseRequested(string request) => request == "pause";

    bool IsResumeRequested(string request) => request == "resume";

    public void Inspect(params (string name, object value)[] variables)
    {
        MonitorUserInterrupts();

        if (!ShouldStop())
            return;

        DBG.PostBreakInfo($"{sourceFilePath}|{sourceLineNumber}|{variables.ToJson()}\n{runtimeCallChain.JoinBy("|")}"); // let debugger to show BP as the start of the next line

        // Console.WriteLine($"DBG.Inspect: {runtimeCallChain.JoinBy("|")}");

        WaitTillResumed(variables);
    }

    // monitor user input and pause when pause request is received.
    static public void MonitorUserInterrupts()
    {
        lock (typeof(BreakPoint))
        {
            if (monitorStarted)
                return;
            monitorStarted = true;
        }

        Task.Run(() =>
        {
            while (true)
            {
                var userUpdate = DBG.UserInterrupt;
                if (!string.IsNullOrEmpty(userUpdate))
                {
                    if (userUpdate == "pause")
                    {
                        DBG.DebugOutputLine($"Pause requested...");
                        DBG.StopOnNextInspectionPointInMethod = "*";
                    }
                }
                Thread.Sleep(1000);
            }
        });
    }
}

static class dbg_extensions
{
    public static string ToView(this Type type)
    {
        string view = type.ToString();
        foreach (var key in Aliases.Keys)
        {
            view = view.Replace(key, Aliases[key]);
        }
        return view;
    }

    public static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>()
        {
            { typeof(byte).ToString(), "byte" },
            { typeof(sbyte).ToString(), "sbyte" },
            { typeof(short).ToString(), "short" },
            { typeof(ushort).ToString(), "ushort" },
            { typeof(int).ToString(), "int" },
            { typeof(uint).ToString(), "uint" },
            { typeof(long).ToString(), "long" },
            { typeof(ulong).ToString(), "ulong" },
            { typeof(float).ToString(), "float" },
            { typeof(double).ToString(), "double" },
            { typeof(decimal).ToString(), "decimal" },
            { typeof(object).ToString(), "object" },
            { typeof(bool).ToString(), "bool" },
            { typeof(char).ToString(), "char" },
            { typeof(string).ToString(), "string" },
            { typeof(void).ToString(), "void" }
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

namespace wdbg
{
    public class dbg
    {
        public static bool publicOnly = true;
        public static bool propsOnly = false;
        public static int max_items = 25;
        public static int depth = 1;

        public static string print(object @object)
        {
            try
            {
                if (@object == null)
                    return "<null>";

                var buffer = new StringBuilder();
                var d = new dbg
                {
                    WriteLine = x => buffer.AppendLine(x),
                    Write = x => buffer.Append(x)
                };

                d.WriteObject(@object);
                return buffer.ToString();
            }
            catch { return "<cannot serialize>"; }
        }

        //===============================
        int level = 0;

        string indent = "  ";

        Action<string> WriteLine = Console.Out.WriteLine;

        Action<string> Write = Console.Out.Write;

        void write(object @object = null)
        {
            if (@object != null)
                Write(@object.ToString().ReplaceClrAliaces());
        }

        void writeLine(object @object = null)
        {
            write(@object);
            WriteLine("");
        }

        string Indent
        {
            get { return new string('0', level).Replace("0", indent); }
        }

        string DisplayName(IEnumerable obj)
        {
            if (obj is Array)
            {
                var arr = obj as Array;
                return "{" + obj + "} - Length: " + arr.Length + " item" + (arr.Length == 1 ? "" : "s");
            }
            else if (obj is IDictionary)
            {
                var arr = obj as IDictionary;
                return "{IDictionary} - Count: " + arr.Count;
            }
            else if (obj is IList)
            {
                var arr = obj as IList;
                return "{IList} - Count: " + arr.Count;
            }
            else
            {
                var count = obj.Cast<object>().Count();
                return "{IEnumerable} - " + count + " item" + (count == 1 ? "" : "s");
            }
        }

        static public string CustomPrimitiveTypes = "Newtonsoft.Json.Linq.JValue;";

        static bool isPrimitive(object obj)
        {
            if (obj == null || obj.GetType().IsPrimitive || obj is decimal || obj is string)
                return true;
            else if (CustomPrimitiveTypes != null)
                return CustomPrimitiveTypes.Split(new char[] { ';' }).Contains(obj.GetType().ToString());
            return false;
        }

        void WriteObject(object obj)
        {
            level++;
            if (isPrimitive(obj))
            {
                writeLine(obj);
            }
            // else if (obj is IDictionary dictionaryElement)
            // {
            // }
            else if (obj is IEnumerable enumerableElement)
            {
                writeLine(DisplayName(enumerableElement));

                int index = 0;

                foreach (object item in enumerableElement)
                {
                    write(Indent);
                    if (index > max_items) //need to have some limit
                    {
                        writeLine("... truncated ...");
                        break;
                    }

                    if (obj is IDictionary)
                        write($"{index++} - ");
                    else
                        write("[" + (index++) + "]: ");

                    if (level < (depth + 1))
                    {
                        level++;
                        WriteValue(item);
                        // WriteObject(item);
                        level--;
                    }
                    writeLine("");
                }
            }
            else
            {
                writeLine("{" + obj + "}");

                foreach (MemberInfo m in GetMembers(obj))
                {
                    write(Indent);
                    write("." + m.Name);
                    write(" = ");

                    object value = GetMemberValue(obj, m);

                    if (isPrimitive(value) || (level >= depth))
                    {
                        WriteValue(value);
                        writeLine("");
                    }
                    else
                        WriteObject(value);
                }
            }
            level--;
        }

        object GetMemberValue(object element, MemberInfo m)
        {
            FieldInfo f = m as FieldInfo;
            PropertyInfo p = m as PropertyInfo;

            if (f != null || p != null)
            {
                try
                {
                    Type t = f != null ? f.FieldType : p.PropertyType;
                    return f != null ? f.GetValue(element) : p.GetValue(element, null);
                }
                catch
                {
                    return "{???}";
                }
            }
            return null;
        }

        void WriteValue(object o)
        {
            if (o == null)
                write("{null}");
            else if (o is DateTime)
                write("{" + o + "}");
            else if (o is DictionaryEntry entry)
            {
                // write($"[{entry.Key}]: {entry.Value}");
                write($"[{entry.Key}]: ");
                WriteValue(entry.Value);
            }
            else if (o is ValueType)
                write(o);
            else if (o is string)
                write("\"" + o + "\"");
            else
                write("{" + o.ToString().TrimStart('{').TrimEnd('}') + "}");
        }

        MemberInfo[] GetMembers(object obj)
        {
            Func<MemberInfo, bool> relevant_types = x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property;

            if (propsOnly)
                relevant_types = x => x.MemberType == MemberTypes.Property;

            MemberInfo[] members = obj.GetType()
                                      .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(relevant_types)
                                          .OrderBy(x => x.Name)
                                          .ToArray();

            var private_members = new MemberInfo[0];

            if (!publicOnly)
                private_members = obj.GetType()
                                              .GetMembers(BindingFlags.NonPublic | BindingFlags.Instance)
                                              .Where(relevant_types)
                                              .OrderBy(x => x.Name)
                                              .OrderBy(x => char.IsLower(x.Name[0]))
                                              .OrderBy(x => x.Name.StartsWith("_"))
                                              .ToArray();

            var items = members.Concat(private_members);
            return items.ToArray();
        }
    }

    static class Extension
    {
        static public string ReplaceWholeWord(this string text, string pattern, string replacement)
        {
            return Regex.Replace(text, @"\b(" + pattern + @")\b", replacement);
        }

        static public string ReplaceClrAliaces(this string text, bool hideSystemNamespace = false)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            else
            {
                var retval = text.ReplaceWholeWord("System.Object", "object")
                                 .ReplaceWholeWord("System.Boolean", "bool")
                                 .ReplaceWholeWord("System.Byte", "byte")
                                 .ReplaceWholeWord("System.SByte", "sbyte")
                                 .ReplaceWholeWord("System.Char", "char")
                                 .ReplaceWholeWord("System.Decimal", "decimal")
                                 .ReplaceWholeWord("System.Double", "double")
                                 .ReplaceWholeWord("System.Single", "float")
                                 .ReplaceWholeWord("System.Int32", "int")
                                 .ReplaceWholeWord("System.UInt32", "uint")
                                 .ReplaceWholeWord("System.Int64", "long")
                                 .ReplaceWholeWord("System.UInt64", "ulong")
                                 .ReplaceWholeWord("System.Object", "object")
                                 .ReplaceWholeWord("System.Int16", "short")
                                 .ReplaceWholeWord("System.UInt16", "ushort")
                                 .ReplaceWholeWord("System.String", "string")
                                 .ReplaceWholeWord("System.Void", "void")
                                 .ReplaceWholeWord("Void", "void");

                if (hideSystemNamespace && retval.StartsWith("System."))
                {
                    string typeName = retval.Substring("System.".Length);

                    if (!typeName.Contains('.')) // it is not a complex namespace
                        retval = typeName;
                }

                return retval.Replace("`1", "<T>")
                             .Replace("`2", "<T, T1>")
                             .Replace("`3", "<T, T1, T2>")
                             .Replace("`4", "<T, T1, T2, T3>");
            }
        }

        // for reflecting dynamic objects look at dbg.dynamic.cs

        public static string[] GetCallChain(this StackFrame[] frames)
        {
            var result = new List<string>();

            // Skip frame 0 (current Line method) and start from frame 1
            for (int i = 1; i < frames.Length; i++)
            {
                var frame = frames[i];
                var method = frame.GetMethod();

                if (method.DeclaringType.Assembly != Assembly.GetExecutingAssembly())
                    break;

                if (method != null)
                {
                    var declaringType = method.DeclaringType?.Name ?? "UnknownType";

                    var methodName = method.Name;
                    var fileName = frame.GetFileName();
                    var lineNumber = frame.GetFileLineNumber();

                    // Format: TypeName.MethodName (file:line)
                    var frameInfo = $"{declaringType}.{methodName}";
                    if (!string.IsNullOrEmpty(fileName) && lineNumber > 0)
                    {
                        frameInfo += $"[{Path.GetFileName(fileName)}:{lineNumber}]";
                    }

                    result.Add(frameInfo);
                }
            }

            return result.ToArray();
        }
    }
}