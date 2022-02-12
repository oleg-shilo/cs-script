# Release v4.4.0.0

---

## Deployment

_**Ubuntu (terminal)**_
```
repo=https://github.com/oleg-shilo/cs-script/releases/download/v4.4.0.0/; file=cs-script_4.4-0.deb; rm $file; wget $repo$file; sudo dpkg -i $file
```
_**Windows (choco)**_
_Pending approval_
```
choco install cs-script --version=4.4.0.0 
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

- Issue #277: Installation and use problems, NET6, targeting (4.3.0.0)
  - Added warning on SDK not installed
  - Added auto-switch to Roslyn if SDK is not found
  - Improved check for SDK install. Added ignoring SDK of lower version.


### CSScriptLib

- Issue #278: Support for embedded PDB?
  - Added setting PDB format via `CSScript.EvaluatorConfig.PdbFormat`


