# Release v4.3.0.0

.NET 6 release

---

## Deployment
_**Ubuntu (terminal)**_
```
repo=https://github.com/oleg-shilo/cs-script/releases/download/v4.3.0.0/; file=cs-script_4.3-0.deb; rm $file; wget $repo$file; sudo dpkg -i $file
```
_**Windows (choco)**_
_Pending approval_
```
choco install cs-script --version=4.3.0.0 
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
- Issue #271: Any Special considerations running in a linux docker container?
- Added support for `packages.config` and `NuGet.config`
  Triggered by PR #263: Use and forward additional nuget arguments
- Various changes for .NET 6 porting
- Create NuGetCache directory on environments without an existing package directory
- Open main script in Visual Studio when project is loaded
- PR #265: Do not overwrite return code set in Environment.ExitCode
- Issue #264: Spelling issue?
- Issue #260: Double Entry Point Definition


### CSScriptLib

- _None_


