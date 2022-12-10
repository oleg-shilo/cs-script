//css_ref Microsoft.CodeAnalysis.CSharp.dll
//css_ref Microsoft.CodeAnalysis.dll
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using static System.Formats.Asn1.AsnWriter;
using System.Reflection;
using System.ComponentModel;

public class Decorator
{
#if EXPERIMENT

    static int _Main(string[] args)
#else

    static int Main(string[] args)
#endif
    {
        var testScript = @"D:\dev\Galos\cs-script\src\out\static_content\-wdbg\test3.cs";
        var decoratedScript = Decorator.InjectDbgInfo(args.FirstOrDefault() ?? testScript);
        Console.WriteLine(Path.GetFullPath(decoratedScript));
        return 0;
    }

    static MethodSyntaxInfo[] GetMethodSignatures(string code)
    {
        var nodes = CSharpSyntaxTree.ParseText(code)
                                    .GetRoot()
                                    .DescendantNodes();

        return nodes.OfType<MethodDeclarationSyntax>()
                    .Select(x => new MethodSyntaxInfo
                    {
                        Method = x.Identifier.Text,
                        Params = x.ParameterList.Parameters.Select(y => y.ToString().Split(' ', '\t').Last()).ToArray(),
                        StartLine = x.GetLocation().GetLineSpan().StartLinePosition.Line,
                        EndLine = x.GetLocation().GetLineSpan().EndLinePosition.Line
                    }).Concat(
               nodes.OfType<LocalFunctionStatementSyntax>()
                    .OrderBy(x => x.FullSpan.End)
                    .Select(x => new MethodSyntaxInfo
                    {
                        Method = x.Identifier.Text,
                        Params = x.ParameterList.Parameters.Select(y => y.ToString().Split(' ', '\t').Last()).ToArray(),
                        StartLine = x.GetLocation().GetLineSpan().StartLinePosition.Line,
                        EndLine = x.GetLocation().GetLineSpan().EndLinePosition.Line
                    }))
               .OrderBy(x => x.StartLine)
               .ToArray();
    }

    public static string InjectDbgInfo(string script)
    {
        string error;
        var decoratedScript = Path.ChangeExtension(script, ".dbg.cs");
        var compiledScript = Compile(script, out error);
        var dbegAgentScript = Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("EntryScript")), "dbg-runtime.cs");
        var pdbFile = Path.ChangeExtension(compiledScript, ".pdb");

        try
        {
            if (compiledScript != null)
            {
                MethodSyntaxInfo[] methodDeclarations = GetMethodSignatures(File.ReadAllText(script));

                var invalidVariables = new Dictionary<int, string[]>();

                var pdb = new Pdb(pdbFile);
                var map = pdb.Map();

                for (int i = 0; i < 3; i++) // repeat until succeed but no more than 3 times
                {
                    var lines = File.ReadAllLines(script).ToList();

                    foreach (var method in map)
                        foreach (var scope in method.Scopes)
                        {
                            if (scope.BelongsToFile(script) && scope != method.Scopes.Last())
                            {
                                // scope is 1-based
                                var lineIndex = scope.StartLine - 1;
                                var line = lines[lineIndex].TrimEnd();

                                // if (!line.EndsWith(";") && !line.EndsWith("{"))
                                if (!line.EndsWith(";") && !line.EndsWith("}"))
                                    continue;

                                var variablesToAnalyse = scope.ScopeVariables.ToList();

                                // the scope is within the method declaration. Note local function scope will always belong to
                                // more than one method declaration
                                var methodInfo = methodDeclarations
                                    .Where(x => (x.StartLine) <= lineIndex && lineIndex <= (x.EndLine))
                                    .OrderBy(x => lineIndex - x.StartLine) // take the internal/nested method declaration (e.g. local function)
                                    .FirstOrDefault();

                                if (methodInfo != null)
                                    variablesToAnalyse.AddRange(methodInfo.Params);

                                if (invalidVariables.Any())
                                {
                                    // if errors are detected then we are dealing with a decorated script with extra line on top
                                    var indexInErrorOutput = scope.StartLine + 1; // adjust for an injected line on top
                                    if (invalidVariables.ContainsKey(indexInErrorOutput))
                                        variablesToAnalyse = variablesToAnalyse.Except(invalidVariables[indexInErrorOutput]).ToList();
                                }

                                var inspectionObjects = string.Join(", ", variablesToAnalyse.Select(x => $"(\"{x}\", {x})"));

                                if (scope.File == script && lines.Count() > scope.StartLine)
                                {
                                    var trimmedLine = lines[lineIndex].TrimStart();

                                    var indent = new string(' ', line.Length - trimmedLine.Length);

                                    if (trimmedLine.TrimStart().StartsWith("{"))
                                    {
                                        lines[lineIndex] = $"{indent}{{ DBG.Line().Inspect({inspectionObjects});" + trimmedLine.Substring(1);
                                    }
                                    else
                                    {
                                        // avoid injecting inspection in the bracketless scope statements like `if(true)\nInspect(...);foo();
                                        var prevLine = "";
                                        if (lineIndex > 1)
                                            prevLine = lines[lineIndex - 1].Trim();
                                        if (!prevLine.StartsWithAny("if", "until", "do", "foreach", "for"))
                                            lines[lineIndex] = $"{indent}DBG.Line().Inspect({inspectionObjects});" + trimmedLine;
                                    }
                                }
                                // lines[lineIndex] += $"/*[{scope.StartLine}:{methodInfo?.Method}]*/DBG.Line().Inspect({inspectionObjects});";
                            }
                        }

                    lines.Insert(0, "//css_inc " + dbegAgentScript);

                    File.WriteAllLines(decoratedScript, lines);

                    error = Check(decoratedScript);

                    if (string.IsNullOrEmpty(error))
                        break;

                    foreach (var item in ExtractInvalidVariableDeclarations(error))
                        invalidVariables[item.Key] = item.Value;
                }

                pdb.provider.Dispose();
            }
        }
        finally
        {
            try
            {
                File.Delete(compiledScript);
                File.Delete(pdbFile);
            }
            catch { }
        }

        if (error != null)
            throw new Exception(error);
        else
            return decoratedScript;
    }

    static Dictionary<int, string[]> ExtractInvalidVariableDeclarations(string error)
    {
        // D:\test.cs(24,94): error CS0841:  Cannot use local variable 'testVar3' before it is declared
        // D:\test.cs(24,111): error CS0103:  The name 'testVar2' does not exist in the current context
        // D:\test.cs(24,131): error CS0165:  Use of unassigned local variable 'i'

        return error.Split('\n', '\r')
            .Select(x => x.Split("): "))
            .Where(x => x.Length > 1 && (x[1].StartsWith("error CS0841:") || x[1].StartsWith("error CS0103:") || x[1].StartsWith("error CS0165:")))
            .Select(x => new
            {
                Line = x[0].Split("(").LastOrDefault().Split(',').FirstOrDefault()?.ToInt(),
                // File = x[0], // we are only doing single file scripts so no need to parce file name
                Variable = x[1].Split('\'').Skip(1).FirstOrDefault()
            })
            .Where(x => x.Line.HasValue)
            .GroupBy(x => x.Line)
            .ToDictionary(g => g.Key.Value, g => g.Select(x => x.Variable).ToArray());
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

    static string Compile(string script, out string error)
    {
        var assembly = script + ".dll";
        var output = "";
        var err = "";
        var compilation = Shell.StartProcess(
            "dotnet", $"\"{css}\" -cd -dbg -out:\"{assembly}\" \"{script}\"",
            Path.GetDirectoryName(script),
            line => output += line + "\n",
            line => err += line + "\n");
        compilation.WaitForExit();
        error = err;
        return compilation.ExitCode == 0 ? assembly : null;
    }

    static string css => Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_ROOT%\cscs.dll");
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
}

public class MethodSyntaxInfo
{
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
        var methodSpec = reader.GetMethodImplementation(methodHandle);
        // var method_token = MetadataTokens.GetToken(methodSpec.Method);

        // var methodHandle = MetadataTokens.MethodSpecificationHandle(methodToken);
        // var methodSpec = reader.GetMethodSpecification(methodHandle);
        // var method_token = MetadataTokens.GetToken(methodSpec.Method);

        // var handle = MetadataTokens.MethodDefinitionHandle(methodToken);
        // var definition = reader.GetMethodDefinition(handle);
        // var parent = definition.GetDeclaringType();

        var debugInformationHandle = MetadataTokens.MethodDefinitionHandle(methodToken).ToDebugInformationHandle();
        var localScopes = reader.GetLocalScopes(debugInformationHandle);
        var variables = new List<LocalVariable>();

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
        proc.StartInfo.WorkingDirectory = dir;
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