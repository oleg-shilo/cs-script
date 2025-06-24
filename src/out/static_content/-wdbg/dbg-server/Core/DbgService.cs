using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;

public static class DbgService
{
    static public string Prepare(string script, Action<Process> onStart)
    {
        var output = Shell.RunScript(Shell.dbg_inject, script.qt(), onStart);

        var decoratedScripts = output.Split("\n")
            .Where(x => !string.IsNullOrEmpty(x))
            .Where(x => x.StartsWith("file:"))
            .Select(x => x.Substring("file:".Length))
            .ToArray();

        // the first file is the primary script file
        File.SetLastWriteTimeUtc(decoratedScripts.First(), File.GetLastWriteTimeUtc(script));

        return decoratedScripts.First();
    }

    // static (bool enabled,int line)[] GetBreakpoints(this string decoratedScriptFile)
    // {
    //     File.ReadAllLines(decoratedScriptFile);
    // }
}

public class VariableInfo
{
    // Do not use constructor to not interfere with the serialization. Or have two constructors, one for serialization and one for the UI.
    public static VariableInfo New(string name) => new VariableInfo { Name = name, Value = "(not evaluated)", Type = "" };

    public void Reset()
    {
        Value = "(not evaluated)";
        Type = "";
    }

    public string Name { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }

    public string DisplayValue
        => !Value.HasText() ?
            Value :
            Value.Length > 100 ? Value.Substring(0, 97) + "..." : Value;
}