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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using csscript;
using CSScripting;

// using static System.Formats.Asn1.AsnWriter;

public static class Decorator
{
    static int Main(string[] args)
    {
        (string decoratedScript, int[] breakpoints)[] items = Decorator.Process(args.FirstOrDefault() ?? "<unknown script>");

        foreach (var item in items)
        {
            Console.WriteLine("file:" + item.decoratedScript);
            Console.WriteLine("bp:" + string.Join(",", item.breakpoints.Select(x => x.ToString())));
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

    public static (string decoratedScript, int[] breakpoints)[] Process(string script)
    {
        if (File.Exists(script))
        {
            var dbgAgentScript = Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("EntryScript")), "dbg-runtime.cs");

            var result = new List<(string decoratedScript, int[] breakpoints)>();

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

            // inject dbg info into the primary and imported scripts

            var pdbFile = BuildPdb(script);
            if (pdbFile.IsEmpty())
                throw new Exception($"Cannot build PDB for the script: {script}. Please ensure the script is compilable.");

            var pdb = new Pdb(pdbFile);
            var map = pdb.Map();
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
            finally
            {
                pdb.provider.Dispose();

                try { File.Delete(pdbFile); }
                catch { }
            }

            var breakPointsFile = decoratedPrimaryScript + ".bp";

            var lines = result.Select((info) =>
                $"{info.decoratedScript}|{(info.breakpoints.Select(x => $"-{x}").JoinBy(","))}").ToArray();
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

    static (string decoratedScript, int[] breakpoints) InjectDbgInfo(string script, string decoratedScript, MethodDbgInfo[] map, Func<string> check, string globalImport = null)
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
                // if (scope.BelongsToFile(script) && scope != method.Scopes.Last())
                if (scope.BelongsToFile(script))
                {
                    // scope is 1-based
                    var lineIndex = scope.StartLine - 1;
                    var line = lines[lineIndex].TrimEnd();
                    // string prevLine = ";";
                    var trimmedLine = line.TrimStart();

                    if (trimmedLine.StartsWith("public") ||
                        trimmedLine.StartsWith("private") ||
                        trimmedLine.StartsWith("internal") ||
                        trimmedLine.StartsWith("static"))
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

                    if (methodInfo != null)
                        variablesToAnalyse.AddRange(methodInfo.Params);
                    else
                        variablesToAnalyse.Add("args"); // top-level statement; if it's wrong, the first compilation will invalidate this variable anyway

                    if (invalidVariables.Any())
                    {
                        // if errors are detected then we are dealing with a decorated script with extra lines on top
                        var indexInErrorOutput = scope.StartLine + extraImports; // adjust for an injected line on top
                        if (invalidVariables.ContainsKey(indexInErrorOutput))
                            variablesToAnalyse = variablesToAnalyse.Except(invalidVariables[indexInErrorOutput]).ToList();
                    }

                    var inspectionObjects = string.Join(", ", variablesToAnalyse.Select(x => $"(\"{x}\", {x})"));

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

        if (globalImport.HasText())
            lines.Insert(0, globalImport);

        File.WriteAllLines(decoratedScript, lines);

        breakpoints = lines.Select((x, i) => new { index = i, line = x })
                           .Where(x => x.line.Contains("DBG.Line().Inspect("))
                           .Select(x => x.index)
                           .ToArray();

        return (script, breakpoints);
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

    static string BuildPdb(string script)
    {
        List<string> tempFiles = new();
        tempFiles.Add(script + ".x");
        tempFiles.Add(script + ".x.exe");
        tempFiles.Add(script + ".x.dll");
        tempFiles.Add(script + ".x.deps.json");
        tempFiles.Add(script + ".x.runtimeconfig.json");

        var sw = Stopwatch.StartNew();
        try
        {
            var assembly = script + ".x.exe";

            var output = "";
            var err = "";
            var compilation = Shell.StartProcess(
                "dotnet", $"\"{css}\" -e -dbg -out:\"{assembly}\" \"{script}\"", // you can add -verbose to see the compilation details
                Path.GetDirectoryName(script),
                line => output += line + "\n",
                line => err += line + "\n");
            compilation.WaitForExit();

            if (err.HasText())
                throw new Exception($"Error building PDB for {script}: {err}");

            var ellapsed = sw.Elapsed;

            return compilation.ExitCode == 0 ? (script + ".x.pdb") : null;
        }
        finally
        {
            foreach (var item in tempFiles)
                try { File.Delete(item); } catch { }
        }
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

public class LocalVariable
{
    public LocalVariable(int index, string name, bool compilerGenerated)
    {
        Index = index;
        Name = name;
        CompilerGenerated = compilerGenerated;
    }

    public int Index { get; set; }
    public SequencePoint SequencePoint { get; set; }
    public string Name { get; set; }
    public bool CompilerGenerated { get; set; }

    public override string ToString()
    {
        return Name;
    }
}

public class Pdb
{
    public MetadataReader reader;
    public MetadataReaderProvider provider;

    public Pdb(string pdbPath)
    {
        var stream = new StreamReader(pdbPath);
        provider = MetadataReaderProvider.FromPortablePdbStream(stream.BaseStream, MetadataStreamOptions.PrefetchMetadata, 0);
        reader = provider.GetMetadataReader();
    }

    internal MethodDbgInfo[] Map()
    {
        // https://csharp.hotexamples.com/examples/System.Reflection.Metadata/MetadataReader/GetMethodDefinition/php-metadatareader-getmethoddefinition-method-examples.html
        return reader
            .MethodDebugInformation
            .Where(h => !h.IsNil)
            .Select(x => new { MethodDebugInfo = reader.GetMethodDebugInformation(x), Token = MetadataTokens.GetToken(x.ToDefinitionHandle()) })
            .Where(x => !x.MethodDebugInfo.SequencePointsBlob.IsNil)
            .Select(x => new MethodDbgInfo
            {
                Token = x.Token,
                Scopes = x.MethodDebugInfo
                          .GetSequencePoints()
                          .Where(x => !x.IsHidden)
                          .Select(sp =>
                                  {
                                      var document = reader.GetDocument(sp.Document);
                                      var name = reader.GetString(document.Name);
                                      // var def = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(item.Token));
                                      // BlobReader signatureReader = reader.GetBlobReader(def.Signature);
                                      // SignatureHeader header = signatureReader.ReadSignatureHeader();

                                      var variables = GetLocalVariableNamesForMethod(x.Token);

                                      var document1 = reader.GetDocument(x.MethodDebugInfo.Document);

                                      return new ScopeInfo
                                      {
                                          File = name,
                                          StartLine = sp.StartLine,
                                          EndLine = sp.EndLine,
                                          ScopeVariables = variables.Select(x => x.Name).ToArray()
                                      };
                                  })
                          .OrderBy(x => x.StartLine)
                          .ToArray()
            })
            .ToArray();
    }

    void ProbeScopeForLocals(List<LocalVariable> variables, LocalScopeHandle localScopeHandle)
    {
        var localScope = reader.GetLocalScope(localScopeHandle);

        // var ttt = reader.GetMethodDefinition(localScope.Method);
        // var name1 = reader.GetString(ttt.Name);

        foreach (var localVariableHandle in localScope.GetLocalVariables())
        {
            var localVariable = reader.GetLocalVariable(localVariableHandle);
            var name = reader.GetString(localVariable.Name);

            bool compilerGenerated = (localVariable.Attributes & LocalVariableAttributes.DebuggerHidden) != 0;
            variables.Add(new LocalVariable(localVariable.Index, name, compilerGenerated));
        }
    }

    public IEnumerable<LocalVariable> GetLocalVariableNamesForMethod(int methodToken)
    {
        var methodHandle = MetadataTokens.MethodImplementationHandle(methodToken);
        var methodImpl = reader.GetMethodImplementation(methodHandle);
        // var method_token = MetadataTokens.GetToken(methodSpec.Method);

        // 'methodSpec.Signature' threw an exception of type 'System.BadImageFormatException'
        // var methodSpecHandle = MetadataTokens.MethodSpecificationHandle(methodToken);
        // var methodSpec = reader.GetMethodSpecification(methodSpecHandle);
        // var method_token = MetadataTokens.GetToken(methodSpec.Method);

        // Handle the potential BadImageFormatException when reading parameters
        ParameterHandleCollection parameters = default;
        try
        {
            var handle = MetadataTokens.MethodDefinitionHandle(methodToken);
            var definition = reader.GetMethodDefinition(handle);
            parameters = definition.GetParameters();
        }
        catch (BadImageFormatException)
        {
            // Parameter information couldn't be read from the PDB
            // Continue with default (empty) parameters collection
        }

        var debugInformationHandle = MetadataTokens.MethodDefinitionHandle(methodToken).ToDebugInformationHandle();
        var localScopes = reader.GetLocalScopes(debugInformationHandle);
        var variables = new List<LocalVariable>();

        // note the scope variable may not be available for the evaluation at the start of the scope
        foreach (var localScopeHandle in localScopes)
        {
            ProbeScopeForLocals(variables, localScopeHandle);
        }
        return variables;
    }
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