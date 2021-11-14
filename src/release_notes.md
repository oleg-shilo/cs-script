# Release v4.2.0.0

Maintenance release

---

## Deployment
_**Ubuntu (terminal)**_
```
repo=https://github.com/oleg-shilo/cs-script/releases/download/v4.2.0.0/; file=cs-script_4.2-0.deb; rm $file; wget $repo$file; sudo dpkg -i $file
```
_**Windows (choco)**_
_Pending approval_
```
choco install cs-script --version=4.2.0.0 
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

### _Misc_
- Added auto-generation of the CLI MD documentation with -help cli:md. To be used to generate GitHub wiki page during the build
- Fixed Debian packaging problem (/n/r needed replacement with \n)
- Issue #253: Supports both .Net Framework and .Net 5

### _CLI_

- Updated -speed and -code with the complete support -ng:* switches
- Added -servers:start and -servers:stop command to control both Roslyn and csc build servers at the same time
- Issue #258: Can not run scripts after installing VS2022
- Issue #257: Ability to catch AppDomain.UnhandledException in a not-hosted script (cscs)
- Issue #255: Relative path for cscs.exe -out option results in wrong output folder
- Issue #254: Script merger for hosted scripts
- Issue #252: System.NullReferenceException: Object reference not set to an instance of an object. (updated API doc)

### _CSScriptLib_

- Native API CacheEnabled marked as obsolete
- Added IEvaluator.IsCachingEnabled. It is always available from the concrete types implementing IEvaluator and now it is moved directly to the interface.


