//css_include global-usings
using System;
using static System.Console;
using System.Diagnostics;
using static System.Environment;
using CSScripting;
using static dbg;

var thisScript = GetEnvironmentVariable("EntryScript");

var help =
@$"Custom command for editing scripts.
v{thisScript.GetCommandScriptVersion()} ({thisScript})
  cscs -edit [args]
  (e.g. `cscs -edit script.cs`)";

if (!args.Any() || "?,-?,-help,--help".Split(',').Contains(args.FirstOrDefault()))
{
    WriteLine(help);
    return;
}

// -----------------------------------------------
// Command implementation
// -----------------------------------------------

WriteLine($"Executing edit for: [{string.Join(",", args)}]");

var file = args.FirstOrDefault();

if (!file.FileExists())
{
    // file = file.LocateIn( // `CSScriptLib.PathExtensions.LocateIn` API is not published yet
    file = Locate(file,
        [
            GetEnvironmentVariable("CSSCRIPT_COMMANDS"),
            SpecialFolder.CommonApplicationData.GetPath().PathJoin("cs-script", "commands"),
            GetEnvironmentVariable("cscs_exe_dir"),
            GetEnvironmentVariable("CSSCRIPT_ROOT"),
        ]);
}

if (file.FileExists())
{
    WriteLine($"Opening default editor for for: {file}");

    var process = new Process();
    process.StartInfo.FileName = file;
    process.StartInfo.UseShellExecute = true;
    process.Start();

    // or if you know the editor you want to use:
    // Process.Start(@"C:\Program Files\Sublime Text\sublime_text.exe", $"\"{file}\"");
}
else
    WriteLine($"Cannot find file: {file}");

// ------------------------

string Locate(string file, string[] probingDirs)
{
    var resolver = typeof(CSScriptLib.ScriptParser).Assembly?
        .GetType("CSScriptLib.FileParser")?
        .GetMethod("ResolveFile", BindingFlags.Static | BindingFlags.Public, [typeof(string), typeof(string[]), typeof(bool)]);

    return (string)resolver?.Invoke(null, [file, probingDirs, false]);
}