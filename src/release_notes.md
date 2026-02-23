# Release v4.14.0.0

---

## Changes

### CLI
- Improved cache cleanup mechanism to prevent endless growth of `csscript.core` directory
- Enabled `Runtime.CleanAbandonedCache()` to automatically remove cache directories when source scripts are deleted

### CSScriptLib
- **Breaking Change (Minor)**: Changed temporary directory from `CSSCRIPT` to `csscript.lib` to clearly distinguish library usage from CLI
- Enabled abandoned cache cleanup to prevent endless growth of `csscript.lib` directory
- Cache directories are now automatically cleaned when source script directories no longer exist
- #436: SerializationException: Type 'CSScripting.CodeDom.CompilerError' in assembly 'CSScriptLib...
- #437: updated wrong resource referencing.
- #435: /share compiler flag causes CS2007 error after upgrading to 4.13.1

### Temporary Directory Structure

CS-Script now uses separate temporary directories for better isolation:

%TEMP% (or $TMPDIR on Linux)
├── csscript.core\    # CLI execution (cscs.exe, csws.exe) 
│   ├── cache\        # Compiled script cache 
│   ├── snippets\     # -code execution cache 
│   └── DbgAttach\    # Debug metadata 
└── csscript.lib\     # Library/hosted evaluation (CSScriptLib) 
    ├── cache\        # Compiled script cache 
    └── tmp files     # Temporary compilation artifacts
