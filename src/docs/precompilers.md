# Precompilers

A precompiler is a user defined C# routine that transforms script source (and compilation context) immediately before CS-Script compiles it. A precompiler can be either an assembly or a script fle. It is also available in both CLI and Hosted script execution.
At runtime script engine loads the prcompiler, locates the routine that corresponds Precompiler's `Compile` method signature and executes it against the script being executed.

Precompiler allos controlling script execution by letting the user manipulating the properties of the `PrecompilationContext context` object being available the `Compile` method:

```csharp
void Compile(PrecompilationContext context)
```

- *context.NewDependencies: `List<string>`*
    Collection of file names that should be checked for the modification every time the primary script is executed and the decision needs to be made whether to load the script from the cache or compile it from the sources if any source file (dependency) has been modified since the last execution.

- *context.NewSearchDirs: `List<string>`*
    Collection of extra directories that should be used for assembly and script probing during actual script compilation after precompilation is done.

- *context.NewReferences:  `List<string>`*
    Collection of extra referenced assemblies that should be used for assembly and script probing during actual script compilation after precompilation is done.

- *context.SearchDirs: `string[]`*
    Collection of directories that are used for assembly and script probing during actual script compilation after precompilation is done. This is a fixed collection, which is defined by the global CS-Script configuration.

- *context.CompilerOptions: `string`*
    String containing compiler options that are passed to the C# compiler during the compilation of the primary script.

Note: the legacy Compile signature is still supported even though it is no longer a preferred option:

```csharp
bool Compile(
    ref string code, 
    string scriptFile, 
    bool IsPrimaryScript, 
    Hashtable context)
```

 A standalone precompiler source file must reference the CS-Script assembly that defines `csscript.PrecompilationContext`. It's done by script engine automatically. But if you want to compile your precompiler into an assembly you will need to reference `cscs.dll` (the script engine assembly).

## CLI

Place the precompiler in a `.cs`, `.dll`, or `.exe` file and register it from the script with `//css_precompiler` (or its short alias, `//css_pc`). CS-Script searches the script directory and configured search directories; an extension can be omitted when the precompiler is a `.cs` or `.dll` file.

`hello.cs`:

```csharp
//css_pc precompiler.cs
Console.WriteLine(Greeting);
```

`precompiler.cs`:

```csharp
class GreetingPrecompiler
{
    public void Compile(csscript.PrecompilationContext context)
    {
        context.Content = "const string Greeting = \"Hello from a precompiler\";\n" + context.Content;
    }
}
```

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

In CLI execution, dependencies also let cached scripts be recompiled when their inputs change.

For a reusable file or assembly precompiler in a hosted application, add the same `//css_pc` directive to the loaded script. A context-based precompiler can expose `void Compile(PrecompilationContext context)` (or `dynamic context`) instead of the legacy four-argument signature shown above.
