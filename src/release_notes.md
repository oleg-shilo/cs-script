# Release v4.12.0.0

---

## Changes

- CS-Script build for .NET 9 is no longer available. The supported version ar now the last two .NET LTS releases: .NET 8 and .NET 10.

### CLI

- CS-Script ported to .NET 10 
- Added '-l' option for auto-selecting the latest available .NET Runtime with the call: `css -self-rt -l` 
- Added support for .NET 10 file-based execution directives: `#:package` and `#r`.
- Changed the search dir priorities to low local overwrite of the distributed included scrips (libs): `%CSSCRIPT_INC%` is now higher than "%CSSCRIPT_ROOT%/lib`
- In the `css.exe` status print out is now saying `<not set>` instead of `<not integrated>` for the InstallationDir field. 
  It's not really about the integration but setting the CSSCRIPT_ROOT envar.
- Updated `ProjectBuilder.GenerateProjectFor` to include the script engine assembly and global includes (e.g. `global-usings`)


### CSScriptLib
- #428: Script.Evaluator.Eval() exception
- Added `/shared` option for CodeDomEvaluator. To dramatically speedup "next" compilation.
