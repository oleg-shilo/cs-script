# Release v4.9.2

---

## Changes

### CLI

- Added new custom commands:
	- `css -pkill <process name pattern>`: terminates the process based on name.
	- `css -update`: updates already installed CS-Script (if detected)
	- `css -edit <script|custom command>`: opens the script in the default editor

* Improved dev experience for `css -new:cmd <name>`:
  - Now name can include dash as a prefix.
  - Handle the case when during command creation with `-new:cmd` the file name is a command that starts with two dashes (e.g. -ver vs --version)handle

### CSScriptLib

- no changes
