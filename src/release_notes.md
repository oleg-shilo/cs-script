# Release v4.14.9.0

---

## Changes 

### CLI

- Added `-proj:out` to create a Visual Studio project file for building the exe file from the script."
- Improved cache cleanup collision control management
#467: SelfContained does not work anymore since version 4.14.4
  - updated XML docs
  - added a case specific warning.


### CSScriptLib

- Improved cache cleanup collision control management
- Added/exposed `dbg` and `dbg_extensions` available when running CLI scripts. This is to match convenience API like `object.print()`