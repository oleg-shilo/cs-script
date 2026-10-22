# Release v4.14.13.0

---

## Changes

The following changes have been made in the implementation of Precompiler.
- The default expected precompiler method signature now is `void Compile(PrecompilationContext context)` 
- Its alternative return type variation `bool Compile(PrecompilationContext context)` signature is still supported for backward compatibility but the return value will be ignored.
- The old precompiler syntax `bool/void Compile(ref string scriptCode, string scriptFile, bool isPrimaryScript, Hashtable context)` is made obsolete, though it will be supported for timebeing.

### CLI

- Precompiler changes
 
### CSScriptLib

- Precompiler changes
- #470 Hosted Script Preprocessors.
