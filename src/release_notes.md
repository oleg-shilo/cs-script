# Release v4.14.12.0

---

## Changes

### CLI

- Fixed problem with `.build` directory being cleared/removed before another process using finishes the work. 
- Fixed problem with false positive if IsOutdated test for script caching. Was caused by the inclusion of the current dir in the evaluation. 
  This dir should not be considered as it is possible to execute global scripts from the multiple processes (with different currdirs) concurrently.

### CSScriptLib

no changes
