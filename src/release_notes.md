# Release v4.10.0.0

---

## Changes

### CLI
  - `-ng:csc` is now using running csc.exe with the "magic" `/shared` parameter that keeps `VBCSCompiler.exe` running and improves the compilation performance dramatically.
  - `-ng:csc` is now routed to `-ng:csc-inproc`
  - WDBG: 
    - Added disposing abandoned user sessions
    - fixed problem with local methods in the call stack

### CSScriptLib
- CSScript.CodeDomEvaluator local build is now using running csc.exe with the "magic" `/shared` parameter that keeps `VBCSCompiler.exe` running and improves the compilation performance dramatically.(triggered by #423)
- `CodeDomEvaluator.CompileOnServer` default value now is set to `true`.
- `CSScript.EvaluatorConfig.CompilerOptions` now allows removing some of default compiler options that you may find undesirable for whatever reason. This can be accomplished by specifying the option value with the `!no` prefix (e.g. `!no/shared` will remove `/shared`.