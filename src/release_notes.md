# Release v4.14.10.0

---

## Changes

### CLI

- Improvements in the `-csproj` and `-vs` command implementation.
  - Added handling Web projects.
  - Improved invalid directory handling in the command handlers.

### CSScriptLib

- #467 triggered null-ref bug
   Add null-safety for Scoop install check in Runtime.cs.
- SelfContained example updated with the correct comments in Program.cs, including Roslyn limitations.