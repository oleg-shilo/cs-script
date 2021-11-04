# Release v4.1.4.0

- Updated `-speed` and `-code` with the complete support `-ng:*` switches
- Added `IEvaluator.IsCachingEnabled`. Ite as always available from the conctrete types implementing `IEvaluator` and now it is moved directly to the interface.
- Added `-servers:start` and `-servers:stop` command to control both Roslyn and csc build servers at the same time
- CSScriptLib: Native API `CacheEnabled` marked as obsolete
- Issue #258: Can not run scripts after installing VS2022
- Issue #257: Ability to catch AppDomain.UnhandledException in a not-hosted script (cscs) 
- Issue #255: Relative path for cscs.exe -out option results in wrong output folder
- Issue #254: Script merger for hosted scripts
- Issue #253: Supports both .Net Framework and .Net 5
- Issue #252: System.NullReferenceException: Object reference not set to an instance of an object. (updated API doc)
- Added auto-generation of the CLI MD documentation with `-help cli:md`. To be used to generate GitHub wiki page during the build
- Fixed Debian packaging problem (`/n/r` needed replacement with `\n`)

---

## Deployment
_**Ubuntu (terminal)**_
```
repo=https://github.com/oleg-shilo/cs-script/releases/download/v4.1.4.0/; file=cs-script_4.1-4.deb; rm $file; wget $repo$file; sudo dpkg -i $file
```
_**Windows (choco)**_
_Pending approval_
```
choco install cs-script --version=4.1.4.0 
```
It is highly recommended that you uninstall CS-Script.Core if it is present:
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

