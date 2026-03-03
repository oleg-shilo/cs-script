# Release v4.14.0

---

## Changes

The major changes in this release are around improved support for working with external compile engines in CodeDom evaluator mode. Now both, CLI and CSScriptLib APIs allow automatic download and deployment of the latest .NET compilers (nuget packages) on the target system so the user does not have to install the complete .NET SDK.

Caching and temp files management has also been improved dramatically and made more consistent and reliable.

Another significant change is heave refactoring and documentation improvements.

Special thank you to @maettu-this who helped with the requirements for all these changes.

========================================

### Misc

- #444: "The documentation texts of CompileAssemblyFrom..."
- Updated CodeDom-Evaluator-Dependencies.md to reference the new deployment option.
- Reworked and unified csc.exe probing for CodeDom compilation.
- Intensive code housekeeping
- Major rework of the .NET Framework samples. Triggered by #442

### CLI

- Improved cache cleanup mechanism to prevent endless growth of `csscript.core` directory
- Enabled `Runtime.CleanAbandonedCache()` to automatically remove cache directories when source scripts are deleted
- `-pkill` command improvements. Bump version to 1.0.1
- Added `-deploy-csc` switch for compiler deployment via NuGet
- Enhanced the LockCheck sample to better handle directory lock checks using handle.exe as a fallback.

### CSScriptLib

- #434: Could not load file or assembly 'Microsoft.CodeAnalysis, Version=4.14.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35' or one of its dependencies
- #435: /share compiler flag causes CS2007 error after upgrading to 4.13.1
- #436: SerializationException: Type 'CSScripting.CodeDom.CompilerError' in assembly 'CSScriptLib...
- #437: updated wrong resource referencing.
* #441: Change CodeDomEvaluator.IsCachingEnabled default to true
- #445: Missing information on resulting compilation details?
- #448: Using preprocessor directives with Roslyn / SourceCodeKind.Regular
- #449: RoslynEvaluator ignores disabling referencing AppDomain assemblies.
- #447: Proposing EvaluatorBase.CompileFile for consistency with EvaluatorBase.CompileAssemblyFromFile
        Add AttachedProperties and project context to compilation
- Enabled abandoned cache cleanup to prevent endless growth of `csscript.lib` directory
- Cache directories are now automatically cleaned when source script directories no longer exist
- Added NugetPackageDownloader and unified compiler discovery
- Deprecated CompilerLastOutput; add CompilerInput/Output
- Added DownloadLatestSdkToolset to NugetPackageDownloader for automated retrieval of compilers and reference assemblies. It improves support for environments without .NET SDK.

### Temporary Directory Structure

CS-Script now uses separate temporary directories for better isolation:

%TEMP% (or $TMPDIR on Linux)
├── csscript.core\    # CLI execution (cscs.exe, csws.exe) 
│   ├── cache\        # Compiled script cache 
│   ├── snippets\     # `-code` execution cache 
│   └── DbgAttach\    # Debug metadata 
└── csscript.lib\     # Library/hosted evaluation (CSScriptLib) 
    ├── cache\        # Compiled script cache 
    └── tmp files     # Temporary compilation artifacts