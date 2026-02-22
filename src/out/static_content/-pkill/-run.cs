//css_ng csc
//css_include global-usings
using System;
using System.Diagnostics;
using CSScripting;
using static dbg;
using static System.Console;
using static System.Environment;

var thisScript = GetEnvironmentVariable("EntryScript");

var help =
@$"Custom command for terminating the process based on name.
v{thisScript.GetCommandScriptVersion()} ({thisScript})
  css -pkill <process name pattern>
  (e.g. `css -pkill sublime`)";

if (args.IsEmpty() || "?,-?,-help,--help".Split(',').Contains(args.FirstOrDefault()))
{
    WriteLine(help);
    return;
}

// -----------------------------------------------
// Command implementation
// -----------------------------------------------

string pattern = args.FirstOrDefault();
string extrtaArg = args.Skip(1).FirstOrDefault();
Process[] matches;
int pId = 0;

if (int.TryParse(pattern, out pId))
    matches = Process.GetProcesses()
        .Where(x => x.Id == pId)
        .ToArray();
else
    matches = Process.GetProcesses()
       .Where(x => x.ProcessName.Contains(pattern))
       .ToArray();

if (!matches.Any())
{
    WriteLine("Cannot find any process matching the specified name pattern");
    return;
}

var maxNameLength = matches.Max(x => x.ProcessName.Length);
var i = 1;

WriteLine("");
WriteLine($"   # | {"Name".PadRight(maxNameLength)} | PID");
WriteLine($"-----|-{"".PadRight(maxNameLength, '-')}-|-------");

foreach (var p in matches)
    WriteLine($" {i++,3} | {p.ProcessName.PadRight(maxNameLength)} | {p.Id}");

string input = extrtaArg;

if (input == null)
{
    WriteLine("");
    WriteLine("Enter: ");
    WriteLine("  - the index of the process to terminate");
    WriteLine("  - 'A' to terminate all");
    WriteLine("  - 'X' to exit");

    input = ReadLine();
}

if (int.TryParse(input, out int index))
{
    index--;
    if (matches.Length > index)
        try { matches[index].Kill(); }
        catch { }
}
else if (input.ToLower() == "x")
{
    // do nothing
}
else if (input.ToLower() == "a")
{
    matches.ToList().ForEach(p =>
    {
        try { p.Kill(); }
        catch { }
    });
}
else
{
    WriteLine();
    WriteLine("Invalid input.");
}