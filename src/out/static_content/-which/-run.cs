//css_args -nl
//css_ng csc
//css_include global-usings
using CSScripting;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static dbg;
using static System.Console;
using static System.Environment;

var thisScript = GetEnvironmentVariable("EntryScript");

var help =
@$"Custom command for printing detected locations of the specified file.
v{thisScript.GetCommandScriptVersion()} ({thisScript})
Usage:
  css -which <file>";

if (args.Length == 0 || "?,-?,-help,--help".Split(',').Contains(args.FirstOrDefault()))
{
    Console.WriteLine(help);
}
else
{
    string file = args[0];
    string filePath = file;

    if (File.Exists(file))
    {
        Console.WriteLine(Path.GetFullPath(filePath));
    }
    else
    {
        var isWin = Environment.OSVersion.Platform != PlatformID.Win32NT;

        var dirs = Environment.GetEnvironmentVariable("Path").Split(';').ToList();
        dirs.Insert(0, Environment.CurrentDirectory);

        foreach (string dir in dirs)
        {
            if (File.Exists(filePath = Path.Combine(dir, file)) ||
                (isWin && File.Exists(filePath = Path.Combine(dir, file + ".exe")))) //on win .exe might be dropped
            {
                Console.WriteLine(filePath);
                break;
            }
        }
    }
}