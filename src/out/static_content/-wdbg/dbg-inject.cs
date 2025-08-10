//css_ng csc
//css_ref Microsoft.CodeAnalysis.CSharp.dll
//css_ref Microsoft.CodeAnalysis.dll
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using csscript;
using CSScripting;

// using static System.Formats.Asn1.AsnWriter;

public static class Decorator
{
    static void RoslynProcessing()
    {
        var code = File.ReadAllText(@"C:\Users\oleg\OneDrive\Documents\CS-Script\NewScript.cs");
        BreakpointVariableAnalyzer.Analyze(code);
    }

    static int Main(string[] args)
    {
        // RoslynProcessing(); return 0;
        (string script, string decoratedScript, int[] breakpoints)[] items = Decorator.Process(args.FirstOrDefault() ?? "<unknown script>");

        Console.WriteLine("script-dbg:" + items[0].decoratedScript);

        foreach (var item in items)
        {
            Console.WriteLine("file:" + item.script);
            Console.WriteLine("file-dbg:" + item.decoratedScript);
            Console.WriteLine("bp:" + string.Join(",", item.breakpoints.Select(x => (x + 1).ToString())));
        }

        return 0;
    }

    static string global_usings_import = "global-usings";

    static string ChangeToCahcheDir(this string file, string dir) => Path.Combine(dir, Path.GetFileName(file));

    static string GetCacheDirectory(string script)
    {
        return Runtime.GetCacheDir(script);
        // If the above API is not ready yet in cscs.dll:
        var result = typeof(csscript.Runtime).Assembly
                         .GetType("csscript.CSExecutor")
                         .GetMethod("GetCacheDirectory")
                         .Invoke(null, new object[] { script }) as string;

        result = Path.Combine(result, ".wdbg", Path.GetFileName(script));
        Directory.CreateDirectory(result); // ensure the directory exists
        return result;
    }

    static Dictionary<string, string> DecoratedScriptsMap = new();

    public static (string script, string decoratedScript, int[] breakpoints)[] Process(string script)
    {
        if (File.Exists(script))
        {
            var dbgAgentScript = Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("EntryScript")), "dbg-runtime.cs");

            var result = new List<(string script, string decoratedScript, int[] breakpoints)>();

            var sourceFiles = Project.GenerateProjectFor(script).Files;
            var primaryScript = sourceFiles.First(); // always at least one script is present
            var cacheDir = GetCacheDirectory(primaryScript).PathJoin(".wdbg", script.GetFileName());
            cacheDir.EnsureDir();
            var importedScripts = sourceFiles.Skip(1).Where(x => Path.GetFileNameWithoutExtension(x) != global_usings_import);
            var decoratedPrimaryScript = primaryScript.ChangeToCahcheDir(cacheDir);

            var globalImport = $"//css_inc {dbgAgentScript}";
            var importedScriptsInfo = importedScripts.Select(x => new { script = x, decoratedScript = x.ChangeToCahcheDir(cacheDir) }).ToArray();

            DecoratedScriptsMap[primaryScript] = decoratedPrimaryScript;

            foreach (var item in importedScriptsInfo)
            {
                globalImport += Environment.NewLine + $"//css_inc {item.decoratedScript}";
                File.WriteAllText(item.decoratedScript, File.ReadAllText(item.script)); // copy the script content to the decorated script, later it will be decorated
                DecoratedScriptsMap[item.script] = item.decoratedScript;
            }

            var map_byRoslyn = new List<MethodDbgInfo>();
            map_byRoslyn.Add(new MethodDbgInfo
            {
                Scopes = BreakpointVariableAnalyzer.Map(primaryScript)
            });

            foreach (var item in importedScripts)
            {
                map_byRoslyn.Add(new MethodDbgInfo
                {
                    Scopes = BreakpointVariableAnalyzer.Map(item)
                });
            }

            var map = map_byRoslyn.ToArray();
            var check = new Func<string>(() => Check(decoratedPrimaryScript));

            invalidBreakpointVariables[primaryScript] = new Dictionary<int, List<string>>();
            foreach (var import in importedScriptsInfo)
                invalidBreakpointVariables[import.script] = new Dictionary<int, List<string>>();

            try
            {
                string error = null;

                for (int i = 0; i < 3; i++) // repeat until succeed but no more than 3 times
                {
                    var metadata = InjectDbgInfo(primaryScript, decoratedPrimaryScript, map, check, globalImport);
                    result.Add(metadata);

                    // inject dbg info into imported scripts as well
                    foreach (var import in importedScriptsInfo)
                    {
                        metadata = InjectDbgInfo(import.script, import.decoratedScript, map, check, globalImport: null); // no global import for imported scripts
                        result.Add(metadata);
                    }

                    error = check();

                    if (error.IsEmpty())
                        break;
                    else
                        result.Clear();

                    foreach (var item in ExtractInvalidVariableDeclarations(error))
                    {
                        var fileName = item.Key;

                        var invalidVariables = invalidBreakpointVariables[fileName];

                        foreach (var line in item.Value)
                        {
                            if (!invalidVariables.ContainsKey(line.Key))
                                invalidVariables[line.Key] = new List<string>();
                            foreach (var varName in line.Value)
                            {
                                if (!invalidVariables[line.Key].Contains(varName))
                                    invalidVariables[line.Key].Add(varName);
                            }
                        }
                    }
                }

                if (error?.Trim().HasText() == true)
                    throw new Exception(error);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error injecting debug info into the script: {script}\n{ex.Message}", ex);
            }

            var breakPointsFile = decoratedPrimaryScript + ".bp";

            var lines = result.Select((info) =>
            {
                return $"{info.script}|{info.decoratedScript}|{(info.breakpoints.Select(x => $"-{x + 1}").JoinBy(","))}";
            })
            .ToArray();
            File.WriteAllLines(breakPointsFile, lines);

            return result.ToArray();
        }
        else
        {
            throw new FileNotFoundException($"Cannot find the script file: {script}");
        }
    }

    // file, breakpoint line, list of invalid local variables
    static Dictionary<string, Dictionary<int, List<string>>> invalidBreakpointVariables = new();

    static (string script, string decoratedScript, int[] breakpoints) InjectDbgInfo(string script, string decoratedScript, MethodDbgInfo[] map, Func<string> check, string globalImport = null)
    {
        string error = null;

        var code = File.ReadAllText(script) + Environment.NewLine;

        MethodSyntaxInfo[] methodDeclarations = GetMethodSignatures(code);

        int[] breakpoints = new int[0]; // line that user can put a break point at.

        var invalidVariables = invalidBreakpointVariables[script];

        int extraImports = globalImport?.Split(Environment.NewLine).Length ?? 0;

        var lines = File.ReadAllLines(script).ToList();

        foreach (var method in map)
        {
            foreach (var scope in method.Scopes)
            {
                if (scope.BelongsToFile(script))
                {
                    // scope is 1-based
                    var lineIndex = scope.StartLine - 1;
                    var line = lines[lineIndex].TrimEnd();
                    // string prevLine = ";";
                    var trimmedLine = line.TrimStart();

                    if (trimmedLine.StartsWithAny(
                        "public", "private", "internal", "static",
                        "=>", ".",
                        "catch", "finally"))
                    {
                        continue; // skip public method declarations (e.g `string GetName()=>_name;`)
                    }

                    bool certainlyValidLine = line.EndsWith("{") || line.EndsWith("}");
                    bool certainlyInvalidLine = line.EndsWith(".") || trimmedLine.StartsWith(".") || trimmedLine.Length == 0;

                    if (certainlyValidLine && certainlyInvalidLine)
                    {
                        continue;
                    }

                    var variablesToAnalyse = scope.ScopeVariables.ToList();

                    // the scope is within the method declaration. Note local function scope will always belong to
                    // more than one method declaration
                    var methodInfo = methodDeclarations
                        .Where(x => x.StartLine <= lineIndex && lineIndex <= x.EndLine)
                        .OrderBy(x => lineIndex - x.StartLine) // take the internal/nested method declaration (e.g. local function)
                        .FirstOrDefault();

                    if (methodInfo?.IsStatic == false)
                        variablesToAnalyse.Add("this");

                    // not needed any more because BreakpointVariableAnalyzer extracts method params anyway
                    // if (methodInfo != null)
                    //     variablesToAnalyse.AddRange(methodInfo.Params);
                    // else
                    //     variablesToAnalyse.Add("args"); // top-level statement; if it's wrong, the first compilation will invalidate this variable anyway

                    if (invalidVariables.Any())
                    {
                        // if errors are detected then we are dealing with a decorated script with extra lines on top
                        var indexInErrorOutput = scope.StartLine + extraImports; // adjust for an injected line on top

                        if (invalidVariables.ContainsKey(indexInErrorOutput))
                            variablesToAnalyse = variablesToAnalyse.Except(invalidVariables[indexInErrorOutput]).ToList();
                    }

                    // var inspectionObjects = string.Join(", ", variablesToAnalyse.Select(x => $"(\"{x}\", {x})"));
                    var inspectionObjects = string.Join(", ", variablesToAnalyse.Select(x => $"{x}.v()"));

                    if (scope.File == script && lines.Count() >= scope.StartLine)
                    {
                        if (!lines[lineIndex].Contains("DBG.Line().Inspect")) // not processed yet
                        {
                            var indent = new string(' ', line.Length - trimmedLine.Length);

                            if (trimmedLine.TrimStart().StartsWith("{"))
                            {
                                lines[lineIndex] = $"{indent}{{ DBG.Line().Inspect({inspectionObjects});" + trimmedLine.Substring(1);
                            }
                            else
                            {
                                // hand the bracket-less scope statements like `if(true)\nInspect(...);foo();
                                if (IsInsideBracketlessScope(code, lineIndex))
                                {
                                    // create a scope with the brackets so the inspection code can be injected
                                    lines[lineIndex] = $"{{ {indent}DBG.Line().Inspect({inspectionObjects});" + trimmedLine + "}";
                                }
                                else
                                {
                                    lines[lineIndex] = $"{indent}DBG.Line().Inspect({inspectionObjects});" + trimmedLine;
                                }
                            }
                        }
                    }
                }
            }
        }

        for (int j = 0; j < lines.Count; j++)
        {
            if (lines[j].IndexOf(global_usings_import) != -1)
                continue; // skip global usings import line

            // prevent script from processing the script again
            if (lines[j].IndexOf("//css_imp") != -1)
                lines[j] = lines[j].Replace("//css_imp", "//css_diasbled_imp");
            if (lines[j].IndexOf("//css_inc") != -1)
                lines[j] = lines[j].Replace("//css_inc", "//css_diasbled_inc");
        }

        int offset = 0;

        if (globalImport.HasText())
        {
            lines.Insert(0, globalImport); // even if globalImport is a multiline text it will take only one item in the `lines` list
            offset++;
        }

        File.WriteAllLines(decoratedScript, lines);

        // lines contains as many elements as the original undecorated script file + one extra line if global imports were inserted

        breakpoints = lines.Select((x, i) => new { index = i, line = x })
                           .Where(x => x.line.Contains("DBG.Line().Inspect("))
                           .Select(x => x.index - offset) // adjust for the global import line
                                                          // .Select(x => x.index)
                           .ToArray();

        return (script, decoratedScript, breakpoints);
    }

    static bool IsInsideBracketlessScope(string code, int line)
    {
        SyntaxNode codeTree = CSharpSyntaxTree.ParseText(code).GetRoot();

        // Find the statement at the given line
        var statement = codeTree.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
            .FirstOrDefault(s =>
            {
                var span = s.GetLocation().GetLineSpan();
                // Lines are 0-based in Roslyn, but 0-based in most editors
                return span.StartLinePosition.Line == line;
            });

        if (statement == null)
            return false;

        var parent = statement.Parent;

        // Check if parent is a flow control statement and not a block
        if (parent is Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax ifStmt)
            return ifStmt.Statement == statement && !(ifStmt.Statement is Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax);

        if (parent is Microsoft.CodeAnalysis.CSharp.Syntax.ElseClauseSyntax elseClause)
            return elseClause.Statement == statement && !(elseClause.Statement is Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax);

        if (parent is Microsoft.CodeAnalysis.CSharp.Syntax.ForEachStatementSyntax forEachStmt)
            return forEachStmt.Statement == statement && !(forEachStmt.Statement is Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax);

        if (parent is Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax forStmt)
            return forStmt.Statement == statement && !(forStmt.Statement is Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax);

        if (parent is Microsoft.CodeAnalysis.CSharp.Syntax.WhileStatementSyntax whileStmt)
            return whileStmt.Statement == statement && !(whileStmt.Statement is Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax);

        if (parent is Microsoft.CodeAnalysis.CSharp.Syntax.DoStatementSyntax doStmt)
            return doStmt.Statement == statement && !(doStmt.Statement is Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax);

        return false;
    }

    // static SyntaxNode codeTree;

    static MethodSyntaxInfo[] GetMethodSignatures(string code)
    {
        SyntaxNode codeTree = CSharpSyntaxTree.ParseText(code).GetRoot();
        var nodes = codeTree.DescendantNodes();

        return nodes.OfType<MethodDeclarationSyntax>()
                    .Select(x => new MethodSyntaxInfo
                    {
                        Method = x.Identifier.Text,
                        Params = x.ParameterList.Parameters.Select(y => y.ToString().Split(' ', '\t').Last()).ToArray(),
                        StartLine = x.GetLocation().GetLineSpan().StartLinePosition.Line,
                        EndLine = x.GetLocation().GetLineSpan().EndLinePosition.Line,
                        IsStatic = x.Modifiers.Any(x => x.Text == "static")
                    }).Concat(
               nodes.OfType<LocalFunctionStatementSyntax>()
                    .OrderBy(x => x.FullSpan.End)
                    .Select(x => new MethodSyntaxInfo
                    {
                        Method = x.Identifier.Text,
                        Params = x.ParameterList.Parameters.Select(y => y.ToString().Split(' ', '\t').Last()).ToArray(),
                        StartLine = x.GetLocation().GetLineSpan().StartLinePosition.Line,
                        EndLine = x.GetLocation().GetLineSpan().EndLinePosition.Line,
                        IsStatic = true // while it is not static, local functions cannot difference "this" so deal with it as with static method
                    }))
               .OrderBy(x => x.StartLine)
               .ToArray();
    }

    static Dictionary<string, Dictionary<int, string[]>> ExtractInvalidVariableDeclarations(string error)
    {
        // D:\test.cs(24,94): error CS0841:  Cannot use local variable 'testVar3' before it is declared...
        // D:\test.cs(24,111): error CS0103:  The name 'testVar2' does not exist in the current context...
        // D:\util.cs(21,113): error CS0103:  The name 'testVar7' does not exist in the current context...
        // D:\test.cs(24,131): error CS0165:  Use of unassigned local variable 'i'...
        // D:\test.cs(28,131): error CS0026:  Keyword 'this' is not valid in a static property, static method...

        bool isVariableNameError(string error) => error.StartsWith("error CS0841:")
                                                  || error.StartsWith("error CS0103:")
                                                  || error.StartsWith("error CS0165:")
                                                  || error.StartsWith("error CS0026:");

        Func<string, string> toOriginalFileName = (string file) => DecoratedScriptsMap.FirstOrDefault(x => x.Value == file).Key;

        return error.Split('\n', '\r')
            .Select(x => x.Split("): "))
            .Where(x => x.Length > 1 && isVariableNameError(x[1]))
            .Select(x => new
            {
                Line = x[0].Split("(").LastOrDefault().Split(',').FirstOrDefault()?.ToInt(),
                File = toOriginalFileName(x[0].Split("(").FirstOrDefault()), // remap it to the original file name
                Variable = x[1].Split('\'').Skip(1).FirstOrDefault()
            })
            .Where(x => x.Line.HasValue)
            .GroupBy(x => x.File)
            .ToDictionary(
                g => g.Key, // file name
                g => g.GroupBy(x => x.Line) // group by line number
                      .ToDictionary(g1 => g1.Key.Value, // line number
                                    g1 => g1.Select(x => x.Variable).ToArray()));
    }

    static string Check(string script)
    {
        var assembly = script + ".dll";
        var output = "";
        var err = "";
        var compilation = Shell.StartProcess(
            "dotnet", $"\"{css}\" -check \"{script}\"",
            Path.GetDirectoryName(script),
            line => output += line + "\n",
            line => err += line + "\n");
        compilation.WaitForExit();

        if (output.StartsWith("Compile: OK"))
            return null;
        else
            return output;
    }

    static string css
    {
        get
        {
            // Engine file probing priorities:
            // 1. check if we are hosted by the script engine
            // 2. check if some f the parent folders contains the script engine assembly
            // 3. check if css shim is present on OS
            // 4. check the install directory (%CSSCRIPT_ROOT%) in case css is fully installed
            // ======================================

            // 1
            var found =
                Environment.GetEnvironmentVariable("CSScriptRuntimeLocation") ??
                (Assembly.GetExecutingAssembly().GetName().Name == "cscs" ?
                    Assembly.GetExecutingAssembly().Location :
                    Assembly.GetEntryAssembly().GetName().Name == "cscs" ?
                        Assembly.GetEntryAssembly().Location :
                        null);

            if (found.HasText() && File.Exists(found))
                return found;
            else
                found = null;

            // 2
            var dir = Environment.CurrentDirectory;

            while (dir != null)
            {
                var candidate = Path.Combine(dir, "cscs.dll");

                if (File.Exists(candidate))
                {
                    found = candidate;
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }

            if (found.HasText() && File.Exists(found))
                return found;
            else
                found = null;

            // 3
            try
            {
                StringBuilder output = new();
                Shell.StartProcess("css", "-self", null, onStdOut: line => output.AppendLine(line?.Trim())).WaitForExit();
                found = output.ToString().Trim();

                if (File.Exists(found)) // may throw
                    return found;
            }
            catch { }

            // 4. Fallback to environment variable if not found
            found = Path.Combine(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_ROOT%"), "cscs.dll");

            if (!found.HasText() || !File.Exists(found))
                throw new Exception($"Cannot find cscs.dll. Please ensure it is available in the current directory or set the CSSCRIPT_ROOT environment variable.");
            return found;
        }
    }
}

public class MethodDbgInfo
{
    public int Token;
    public ScopeInfo[] Scopes;
}

public class ScopeInfo
{
    public string File;
    public int StartLine;
    public int EndLine;
    public string[] ScopeVariables;

    public override string ToString()
    => $"{StartLine}:{string.Join(", ", ScopeVariables)}:{File}";
}

static class Extensions
{
    static public bool StartsWithAny(this string text, params string[] patterns)
        => patterns.Any(x => text.StartsWith(x));

    static public int ToInt(this string text)
    {
        int.TryParse(text, out var result);
        return result;
    }

    static public bool BelongsToFile(this ScopeInfo scope, string file)
    {
        return 0 == string.Compare(Path.GetFullPath(scope.File), Path.GetFullPath(file), ignoreCase: OperatingSystem.IsWindows());
    }

    static public int IndexOfClosestMatchingItem(this string[] items, string pattern)
    {
        if (items == null || items.Length == 0 || string.IsNullOrEmpty(pattern))
            return -1;

        // Fast path: look for exact matches first
        for (int i = 0; i < items.Length; i++)
        {
            if (string.Equals(items[i], pattern, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        // Fast path: look for prefix matches
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        // If no exact or prefix match, find closest match using Levenshtein distance
        int closestIndex = -1;
        int smallestDistance = int.MaxValue;

        for (int i = 0; i < items.Length; i++)
        {
            int distance = LevenshteinDistance(items[i].ToLowerInvariant(), pattern.ToLowerInvariant());

            // If this is a better match than what we've seen so far
            if (distance < smallestDistance)
            {
                smallestDistance = distance;
                closestIndex = i;
            }
        }

        // Only return if the match is reasonably close (threshold is ~30% of pattern length)
        int threshold = Math.Max(2, pattern.Length / 3);
        return smallestDistance <= threshold ? closestIndex : -1;
    }

    // Calculate Levenshtein (edit) distance between two strings
    static private int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t))
            return s.Length;

        int[] v0 = new int[t.Length + 1];
        int[] v1 = new int[t.Length + 1];

        // Initialize v0 (previous row of distances)
        for (int i = 0; i <= t.Length; i++)
            v0[i] = i;

        for (int i = 0; i < s.Length; i++)
        {
            // Calculate v1 (current row distances) from v0
            v1[0] = i + 1;

            for (int j = 0; j < t.Length; j++)
            {
                int cost = (s[i] == t[j]) ? 0 : 1;
                v1[j + 1] = Math.Min(
                    Math.Min(v1[j] + 1, v0[j + 1] + 1),
                    v0[j] + cost);
            }

            // Swap v0 and v1 for next iteration
            (v0, v1) = (v1, v0);
        }

        return v0[t.Length];
    }
}

public class MethodSyntaxInfo
{
    public bool IsStatic;
    public string Method;
    public string[] Params;
    public int StartLine;
    public int EndLine;
}

static class Shell
{
    public static Process ExecuteAssembly(string assembly, string args, Action<string> onStdOut = null, Action<string> onErrOut = null)
        => StartProcess("dotnet", $"\"{assembly}\" +{args}", Path.GetDirectoryName(assembly), onStdOut, onErrOut);

    public static Process StartProcess(string exe, string args, string dir, Action<string> onStdOut = null, Action<string> onErrOut = null)
    {
        Process proc = new();
        proc.StartInfo.FileName = exe;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.WorkingDirectory = dir ?? "";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.EnableRaisingEvents = true;
        proc.ErrorDataReceived += (_, e) => onErrOut?.Invoke(e.Data);
        proc.OutputDataReceived += (_, e) => onStdOut?.Invoke(e.Data);
        proc.Start();

        proc.BeginErrorReadLine();
        proc.BeginOutputReadLine();

        return proc;
    }
}

public class BreakpointVariableAnalyzer
{
    public static ScopeInfo[] Map(string file)
    {
        var result = new List<ScopeInfo>();

        var code = File.ReadAllText(file);
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Demo",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var model = compilation.GetSemanticModel(tree);

        var variablesPerLine = ExtractVariablesPerLine(tree, model);

        foreach (var kv in variablesPerLine)
        {
            result.Add(new ScopeInfo
            {
                File = file,
                StartLine = kv.Key,
                EndLine = kv.Key,
                ScopeVariables = kv.Value.ToArray()
            });
        }

        return result.ToArray();
    }

    public static void Analyze(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Demo",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var model = compilation.GetSemanticModel(tree);

        var variablesPerLine = ExtractVariablesPerLine(tree, model);

        foreach (var kv in variablesPerLine)
        {
            Console.WriteLine($"Line {kv.Key}: {string.Join(", ", kv.Value)}");
        }
    }

    public static List<int> BreakpointCompatibleLines(SyntaxTree tree)
    {
        var lines = new HashSet<int>();
        var root = tree.GetRoot();

        foreach (var statement in root.DescendantNodes().OfType<StatementSyntax>())
        {
            var line = tree.GetLineSpan(statement.Span).StartLinePosition.Line + 1;

            // Simple heuristic: skip empty blocks, braces, or declarations without bodies
            if (!(statement is BlockSyntax) &&
                !(statement is EmptyStatementSyntax) &&
                !(statement is LocalFunctionStatementSyntax lf && lf.Body == null && lf.ExpressionBody == null))
            {
                lines.Add(line);
            }
        }

        return lines.OrderBy(l => l).ToList();
    }

    public static Dictionary<int, List<string>> ExtractVariablesPerLine(SyntaxTree tree, SemanticModel model)
    {
        var result = new Dictionary<int, List<string>>();
        var root = tree.GetRoot();

        // Tracks declared symbol and line number where it becomes available
        var symbolDeclarations = new List<(ISymbol Symbol, int DeclaredLine)>();

        // 1 Gather method parameters
        foreach (var method in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
        {
            var paramSymbols = method.ParameterList?.Parameters
                .Select(p => model.GetDeclaredSymbol(p))
                .Where(s => s != null);

            var methodStartLine = tree.GetLineSpan(method.Span).StartLinePosition.Line + 2;
            foreach (var p in paramSymbols)
                symbolDeclarations.Add((p!, methodStartLine));
        }

        // Gather local variables & local functions
        foreach (var local in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(local);
            if (symbol != null)
            {
                var declLine = tree.GetLineSpan(local.Span).StartLinePosition.Line + 2;
                symbolDeclarations.Add((symbol, declLine));
            }
        }

        foreach (var localFunc in root.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
        {
            // local function parameters
            var paramSymbols = localFunc.ParameterList?.Parameters
                .Select(p => model.GetDeclaredSymbol(p))
                .Where(s => s != null);

            var funcStartLine = tree.GetLineSpan(localFunc.Span).StartLinePosition.Line + 2;
            foreach (var p in paramSymbols)
                symbolDeclarations.Add((p!, funcStartLine));
        }

        //  Assign symbols to every line AFTER they’re declared
        var breakLines = BreakpointCompatibleLines(tree);
        foreach (var line in breakLines)
        {
            var available = symbolDeclarations
                .Where(s => s.DeclaredLine <= line)
                .Select(s => s.Symbol.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            result[line] = available;
        }

        return result;
    }
}