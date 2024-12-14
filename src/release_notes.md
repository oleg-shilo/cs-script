# Release v4.8.23.0

---

## Changes

### CLI
- #396 Some NuGet packages are not recognized and not referenced
- #397: How to succeed in using NuGet packages with native binaries (like e.g. SkiaSharp)
- Added new command `-list` for printing all currently running scripts.
- Added support for nuget package native assets
- LegacyNugetSupport by defauls made false
- script compilation cache now stores probing dirs to allow recreation of PATH environemnt variable during the cached execution (e.g. to cover nuget native assets)
- Added support for `-self-install` command to set global `CSSCRIPT_ROOT` envar.
- Updated `//css_nuget` syntax CLI documentation


### CSScriptLib
- no changes
