# Precompilers

A precompiler transforms script source immediately before CS-Script compiles it. Use one to add generated code, directives, or references. Return `true` when the precompiler changed the source; return `false` otherwise.

## CLI

Place the precompiler in a `.cs`, `.dll`, or `.exe` file and register it from the script with `//css_precompiler` (or its short alias, `//css_pc`). CS-Script searches the script directory and configured search directories; an extension can be omitted when the precompiler is a `.cs` or `.dll` file.

`hello.cs`:

```csharp
//css_pc precompiler.cs

Console.WriteLine(Greeting);
```

`precompiler.cs`:

```csharp
using System.Collections;

class GreetingPrecompiler
{
    public static bool Compile(
        ref string code,
        string scriptFile,
        bool isPrimaryScript,
        Hashtable context)
    {
        code = "const string Greeting = \"Hello from a precompiler\";\n" + code;
        return true;
    }
}
```

### Typed context signature

As an alternative to the legacy four-argument method, a precompiler can expose `bool Compile(csscript.PrecompilationContext context)`. The method may be static or an instance method. CS-Script initializes `context.Content` with the source being compiled; update that property and return `true` to use the changed content.

```csharp
class GreetingPrecompiler
{
    public bool Compile(csscript.PrecompilationContext context)
    {
        context.Content =
            "const string Greeting = \"Hello from a precompiler\";\n" +
            context.Content;
        return true;
    }
}
```

The typed context also provides `scriptFile`, `IsPrimaryScript`, and `SearchDirs`, plus `NewReferences`, `NewDependencies`, `NewIncludes`, `NewSearchDirs`, and `NewCompilerOptions` for generated compilation inputs. A standalone precompiler source file using this signature must reference the CS-Script assembly that defines `csscript.PrecompilationContext`.

Run the script normally:

```text
css hello.cs
```

By default, CS-Script selects a class whose name ends in `Precompiler` and its `Compile` method. Select a specific type or method with `|`, for example `//css_pc precompiler.cs|GreetingPrecompiler:Compile`. Multiple precompilers can be listed in one directive, separated by commas.

## Hosted execution

For code hosted through `CSScriptLib`, attach a callback to the evaluator before loading or compiling the script. `WithPrecompiler` converts the callback into an internal precompiler directive, so it applies to the next script operation just like the CLI directive.

```csharp
using CSScriptLib;

dynamic script = CSScript.RoslynEvaluator
    .WithPrecompiler(context =>
    {
        context.Content =
            "#define GENERATED\n" + context.Content;
        return true;
    })
    .LoadMethod("""
        string Message()
        {
        #if GENERATED
            return "Generated source is enabled";
        #endif
        }
        """);

Console.WriteLine(script.Message());
```

The callback receives `PrecompilationContext`. Its `Content` property holds the current source; it also exposes `NewReferences`, `NewDependencies`, `NewIncludes`, `NewSearchDirs`, and `NewCompilerOptions` for precompilers that need to describe generated compilation inputs. In CLI execution, dependencies also let cached scripts be recompiled when their inputs change.

For a reusable file or assembly precompiler in a hosted application, add the same `//css_pc` directive to the loaded script. A context-based precompiler can expose `bool Compile(PrecompilationContext context)` (or `dynamic context`) instead of the legacy four-argument signature shown above.
