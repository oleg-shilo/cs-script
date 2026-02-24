# Temporary Files and Cache Management

## Overview

CS-Script uses temporary directories to store compiled assemblies, cache files, and other artifacts. Starting with version 4.13.2, CS-Script uses separate temporary directories for different execution contexts.

## Directory Structure

### Location

By default, temporary files are stored in:
- **Windows**: `%LOCALAPPDATA%\Temp\` (typically `C:\Users\<USER>\AppData\Local\Temp\`)
- **Linux/macOS:** `$TMPDIR` or `/tmp/`

### Subdirectories
```txt
Temp Directory Root 
├── csscript.core\          # CLI execution engine 
│   ├── cache\              # Script compilation cache 
│   │   └── <hash>\         # Per-directory cache (hashed by script location) 
│   │       ├── css_info.txt 
│   │       ├── script.cs.dll 
│   │       └── script.cs.pdb 
│   ├── snippets\           # -code execution temporary files 
│   └── DbgAttach\          # Debug session metadata 
└── csscript.lib\           # Library/hosted evaluation 
    ├── cache\              # Evaluator compilation cache 
    └── <guid>.tmp.cs       # Temporary script files
```

## Custom Temporary Directory

You can override the default temporary directory location using the `CSS_CUSTOM_TEMPDIR` environment variable:

**Windows
```txt
set CSS_CUSTOM_TEMPDIR=D:\MyCustomTemp\CSScript css script.cs
```

## Cache Management

### Automatic Cleanup

CS-Script automatically cleans up temporary files:

1. **On Startup** (async, no performance impact):
   - Removes temp files from terminated processes
   - Deletes cache directories for deleted/moved scripts
   - Cleans old snippet files

2. **On Exit**:
   - Removes temp files created by current process

### Manual Cleanup

To manually clean CS-Script cache:

**Windows:**

```txt
rmdir /s /q "%TEMP%\csscript.core" rmdir /s /q "%TEMP%\csscript.lib"
```
**Linux/macOS:**

```txt
rm -rf /tmp/csscript.core rm -rf /tmp/csscript.lib
```
**Legacy .NET Framework (if still in use):**

```txt
rmdir /s /q "%TEMP%\CSSCRIPT" rmdir /s /q "%TEMP%\csscript"
```

## Cache Behavior

### Caching Algorithm

1. Script location is hashed to create cache directory name
2. Compiled assembly is stored in `cache\<hash>\`
3. `css_info.txt` records:
   - CLR version
   - Original script directory path

### Cache Invalidation

Cache is invalidated when:
- Script file is modified (timestamp check)
- Script dependencies change
- Source directory is deleted (cleanup)
- Compiler version changes

### Smart Caching

CS-Script uses metadata-based caching to track:
- Script file timestamps
- Imported script dependencies
- Referenced assemblies
- NuGet package versions

## Troubleshooting

### Cache Growing Too Large

If cache directories grow too large:

1. **Check for cleanup**: Verify `Runtime.CleanAbandonedCache()` is enabled
2. **Manual cleanup**: Delete cache directories (see above)
3. **Disable caching** (not recommended):

```c#
CSScript.Evaluator.With(eval => eval.IsCachingEnabled = false)
```

### Performance Issues

If cleanup causes startup delays:
- Cleanup runs **asynchronously** (no blocking)
- Only scans existing cache directories
- Typically completes in <200ms

### Permission Issues

If you encounter write permission errors:

```txt
You do not have write privileges for the CS-Script cache directory...
```

**Solution**: Set custom temp directory with proper permissions:

```txt
set CSS_CUSTOM_TEMPDIR=C:\Users<user>\csscript-temp
```

## Migration from Previous Versions

### From v4.13.1 and Earlier

CSScriptLib previously used `%TEMP%\CSSCRIPT`. This has changed to `%TEMP%\csscript.lib`.

**Action Required**: None (automatic)
- Old `CSSCRIPT` directory can be safely deleted
- New scripts will use `csscript.lib`

### From .NET Framework Version

Legacy CS-Script used `%TEMP%\csscript` (lowercase). Modern .NET version uses:
- `csscript.core` (CLI)
- `csscript.lib` (Library)

Both can coexist safely.

## API Reference

### Runtime.GetScriptTempDir()
Returns the root temporary directory for CS-Script files.

### Runtime.GetScriptCacheDir(string scriptFile)
Returns the cache directory for a specific script file.

### Runtime.CacheDir
Property that returns the common cache root directory.

### CSScript.StartPurgingOldTempFiles(bool ignoreCurrentProcess)
Starts asynchronous cleanup of old temporary files.
