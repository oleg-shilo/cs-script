# Release v4.11.0.0

---

## Changes

### CLI
  - fixed problem with `css -ls <kill-all|ka>`
  - Added support for pre-release packages `//css_nuget -pre <package>`
  - WDBG:
    - Improved tracking of declaration scope
    - UX improvements
    - Inject dbg metadata is ported to pure Roslyn.
    - Detect debug metadata out of sync and report it (2)

### CSScriptLib
  - Added NativeAOT sample