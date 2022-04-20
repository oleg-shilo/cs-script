# Release v4.4.3.0

---

## Deployment

_**Ubuntu (terminal)**_

```
repo=https://github.com/oleg-shilo/cs-script/releases/download/v4.4.3.0/; file=cs-script_4.4-3.deb; rm $file; wget $repo$file; sudo dpkg -i $file
```

_**Windows (choco)**_
_Not published yet_

_**Manual (Any OS)**_

Just unpack the corresponding 7z file and start using the script engine executable `cscs`.
If you prefer you can build a shim exe `css` for an easy launch of the script engine process:

```C#
dotnet cscs -self-exe
```

The same shim/symbolic link is created if you are installing the CS-Script as a package.

---

## Changes

### CLI

- Various stability improvements after feedback from integration with IDEs (ST3, Npp, VSCode)

### CSScriptLib

- Issue #290: CSScriptLib: CSScript.GlobalSettings.AddSearchDir() probing dirs are not processed
- Issue #289: CSScriptLib: referenced assemblies are not processed when hosting on .NET Framework



