# Release v4.0.2.0

Minor usability improvements of CSScriptLib: 

- Unloading script assembly. 
   _After .NET Framework ignoring the problem for ~14 years .NET Core fillally allows this feature to be implemented._
- Excluding assemblies from being auto referenced (assembly filtering).
- Implemented script caching that was available in the CS-Script edition for .NET Franework.

---

## Deployment
_**Ubuntu (terminal)**_
```
repo=https://github.com/oleg-shilo/cs-script/releases/download/v4.0.2.0/; file=cs-script_4.0-2.deb; rm $file; wget $repo$file; sudo dpkg -i $file
```
_**Windows (choco)**_
_Pending approval_
```
choco install cs-script --version=4.0.2.0 
```
It is highly recommended that you uninstall CS-Script.Core:
```
sudo choco uninstall cs-script.core
```

_**Manual**_
Just unpack the corresponding 7z file and start using the script engine executable `cscs`. 
If you prefer you can build a shim exe `css` for an easy launch of the script engine process: 
```
cscs -self-exe
```
The same shim/symbolic link is created if you are installing the CS-Script as a package.

---
## Changes 

### CLI

_No changes_

### CSScriptLib

- Added support for filtering referenced assemblies:
  ```C#
  dynamic script = CSScript
                       .Evaluator
                       .SetRefAssemblyFilter(asms =>
                           asms.Where(a => !a.FullName.StartsWith("Microsoft."))
                       .LoadCode(scriptCode);
  ```

- Added extension method for unloading script assembly after the execution
  ```C#
  ICalc calc = evaluator
                  .With(eval => eval.IsAssemblyUnloadingEnabledled = true)
                  .LoadMethod<ICalc>("int Sum(int a, int b) => a+b;");

  var result = calc.Sum(7, 3);

  calc.GetType()
      .Assembly
      .Unload();
  ```

- Added script caching. If caching is enabled (disabled by default) the script is to be recompiled only if it is changes since the last execution. It applies to both execution if script file and script code.
  ```C#
  dynamic script = CSScript.Evaluator
                           .With(eval => eval.IsCachingEnabled = true)
                           .LoadMethod(@"object print(string message)
                                         {
                                             Console.WriteLine(message);
                                         }");

  script.print("Hello...");

  ```
  