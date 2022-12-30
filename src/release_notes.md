# Release v4.6.1 

---

## Changes

### CLI

- WDBG: implemented fully functional web based debugger
  ```
  css -wdbg script.cs
  ``` 
- Issue #319: "Bad IL format" on self-contained linux-x64 builds if using namespace with same name as assembly
- Fixed false positive of OutOfDate detection. Caused by the imported files stored in the script file stamp without their full path

### CSScriptLib

- no changes





