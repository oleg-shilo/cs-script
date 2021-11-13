# Release v4.1.0.0

Implementation of Roslyn engine that allows execution of scripts (both CLI and hosted) on the target system without .NET 5 SDK installed.

---

## Deployment
_**Ubuntu (terminal)**_
```
repo=https://github.com/oleg-shilo/cs-script/releases/download/v4.1.0.0/; file=cs-script_4.1-0.deb; rm $file; wget $repo$file; sudo dpkg -i $file
```
_**Windows (choco)**_
_Pending approval_
```
choco install cs-script --version=4.1.0.0 
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

### _CLI_

- Added support for Roslyn engine (no SDK required). See [this wiki](https://github.com/oleg-shilo/cs-script/wiki/Choosing-Compiler-Engine) for details.

  **_Per-execution_**

  From command line:

  ```
  css -engine:roslyn <script file>
  or
  css -ng:roslyn <script file>
  ```

  From script code:

  ```C#
  //css_engine roslyn
  or
  //css_ng roslyn
  ```

  **_Global_**

  ```ps
  css -config:set:DefaultCompilerEngine=roslyn
  ```

- Added option to configure build server ports from environment variables
- Issue #235: csc engine cannot compile dll

### _CSScriptLib_

- Issue #245: .Net 5 SDK project, could not run "CompileAssemblyFromCode"
- Issue #244: Some questions about 4.0.2
  `RefernceDomainAsemblies` made obsolete and renamed to `ReferenceDomainAssemblies`
  Added extension methods `IEvaluator.ExcludeReferencedAssemblies`

