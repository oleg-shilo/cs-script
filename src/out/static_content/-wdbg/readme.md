## Overview    

CS-Script does not implement a true debugging when a break implies thread suspension. INstead it implements "soft debug break", which is nothing else but
a normal method execution where every debuggable line of a method can wait until the user instruction to continue is received.

Such conditional waiting is achieve by injecting the waiting routine (debug metacode) directly in the script code before the execution. 

The debug metacode (`DBG.Line().Inspect(...)`I is a method call, which is injected into every script line that can be debugged.

At runtime, when `DBG.Line().Inspect(...)` is executed and if user sets the breakpoint for that line from IDE, it starts polling the socket server 
(implemented in IDE) for the user instructions to either resume or stop the execution of the script. 

If user does not set the breakpoint, the corresponding `Inspect` call returns immediately. And script execution continues.


During the execution break `DBG.Line().Inspect(...)` notifies the IDE about the current execution context and the debug objects (variables) that are available 
at that point. The information about the variables is collected by passing tuples of method variable name and its value to the `DBG.Line().Inspect(...)` as parameters.
This way `Inspect` method can receive the accurate value of the variable at the execution point. This way also allows dealing with value types.

The IDE and the script communicate via a socket connection. The IDE is a server and the script is a client. 
The IDE is responsible for sending the user instructions to the script and receiving the debug information from it.

Such unorthodox debugging concept has several advantages:

- There is no need to implement a full scale debugger (e.g. as VSCode).
- Low dependency and small footprint.
- No PE mapping required. Everything is a standard.NET code.
- There is no need for a separate debugger process.
- There is no need for the debug symbols. Even release build can be debugged this way.

Though it has certain limitations:

- You can only analyze the context of a single method being in the "soft debug break" state.
- YOu can evaluate the variables but you cannot update them from the debugger.


Thus while the simplicity of "soft debugging" is a great advantage it is not a replacement for a full scale debugger.

## How does it work?

In order to inject metacode (`DBG.Line().Inspect(...)`) the original script code is compiled to produce the pdb file, which is a source of truth 
containing the accurate information about code scopes and scope variables. This is achieved by executing `dbg-inject` script against a given script file.

The `dbg-inject.cs` script is a special script that is responsible for injecting the debug metacode into the original script code. 
It reads the pdb file and injects the `DBG.Line().Inspect(...)` calls into the script code at the appropriate places.

Example of the injected code:

```C#
// Original script code
void foo(string name)
{
    int a = 1;
    int b = 2;
    int c = a + b;
}

// Injected code    
void foo(string name)
{   DBG.Line().Inspect(("name", name)); 
    DBG.Line().Inspect(("name", name)); int a = 1;
    DBG.Line().Inspect(("name", name),("a", a)); int b = 2;
    DBG.Line().Inspect(("name", name),("a", a), ("b", b)); int c = a + b;
    DBG.Line().Inspect(("name", name),("a", a), ("b", b), ("c", c)); 
}
```

Note: `DBG.Line().Inspect` implementation is located in the `dbg-runtime.cs` which is simply imported in the sprint being debugged with a normal `css_inc` directive.


## Challenges and limitations

The biggest challenge is that the `System.Reflection.Metadata` namespace is severely underdeveloped. Thus some even quite standard scenarios are not supported. IE 
method signatures/parameters are not implemented correctly so Roslyn based code analysis had to be used to extract method parameters for the method scope from the pdb.


Another interesting challenge is that there is no way to know which line of a method scope can evaluate which of the scope variables since the evaluation 
is just a C# syntax expression in the same method code.

```C#
void foo(string name)
{   // PDB indicates that both 'a' and `b` are available here. 
    // But from the syntax point of view `a` will only be available after the next code line. And `b` after the next two.
    int a = 1;
    int b = 2;
}
```

THis problem is solved by double-compiling the decorated (original code with the injections) code. The first run has all scope variables listed. 
And if any of them are not available at the corresponding line then they are eliminated based on the analysis of the compile error information before the 
final compilation takes place.