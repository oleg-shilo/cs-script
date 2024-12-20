# Release v4.8.24.0

---

## Changes

### CLI

- #400: Timeout in CI/CD script
- Assorted nuget support improvements triggered by #400
	- removed doc zip files to not to upset WinDefender
	- removed unnecessary nuget restore step for adding package dll's location to the search dir. It was adding no value since search dirs play no role in nuget related scenarios.
- Now nuget restore and asembly lookup are both respecting `CSSCRIPT_NUGET_PACKAGES` nvar. Previously only lookup did. Triggered by  #400.

### CSScriptLib
- no changes
