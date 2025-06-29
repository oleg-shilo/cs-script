using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;
using wdbg.cs_script;

public static class DbgService
{
    static public string Prepare(string script, Action<Process> onStart)
    {
        var output = Shell.RunScript(Shell.dbg_inject, script.qt(), onStart);

        var decoratedPrimaryScript = output
            .GetLines()
            .FirstOrDefault(x => x.StartsWith("script-dbg:")).Substring("script-dbg:".Length).Trim();

        File.SetLastWriteTimeUtc(decoratedPrimaryScript, File.GetLastWriteTimeUtc(script));

        return decoratedPrimaryScript;
    }
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