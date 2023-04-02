# CS-Script (CLI)

CS-Script is a CLR based scripting system which uses ECMA-compliant C# as a programming language.

CS-Script is one of the most mature C# scripting solutions. It became publicly available in 2004, just two years after the first release of .NET. And it was the first comprehensive scripting platform for .NET.

CS-Script allows direct single-step execution of files containing ECMA-compliant C# code (either from shell or from the application).

Canonical "Hello World" script (script.cs):

```C#
using System;
Console.WriteLine("Hello World!");
```

The script can be executed fom the terminal:
_`css` is a native executable. It simply launches the script engine assembly (cscs.dll) without the need to invoke .NET launcher `dotnet`._

```txt
css script.cs
```

You can read in more details about script engine on the product home page: https://github.com/oleg-shilo/cs-script/wiki/CLI-Script-Execution.

From the start, CS-Script was heavily influenced by Python and the developer experiences it delivers. Thus, it tries to match the most useful Python features (apart from the Python syntax). Here are some highlights of the CS-Script features.
    - Scripts are written in plain vanilla CLS-compliant C#. Though classless scripts (top-level statements) are also supported.
    - Remarkable execution speed matching performance of compiled managed applications.
    - Including (referencing) dependency scripts from the main script.
    - Referencing external assemblies from the script.
    - Automatic referencing external assemblies based on analyses of the script-imported namespaces ('usings').
    - Automatic resolving (downloading and referencing) NuGet packages.
    - Building a self-sufficient executable from the script.
    - Possibility to plug in external compiling services for supporting alternative script syntax (e.g. VB, C++)
    - Scripts can be executed on Windows, Linux and MacOS (.NET5 needs to be present).
    - Full integration with Windows, Visual Studio, VSCode, Notepad++ (CS-Script plugin for Notepad++ brings true IntelliSense to the 'peoples editor'), Sublime Text 3

Note, when upgrading or uninstalling CS-Script CLI .NET tool you may need to ensure that you stop any running instances of the script engine. You can do that by executing this simple command: `css -servers:stop` (or even simpler: `css -kill`).