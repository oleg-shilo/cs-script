//css_args -nl
//css_ng csc
//css_include global-usings
using System;
using System.Diagnostics;
using CSScripting;
using static dbg;
using static System.Console;
using static System.Environment;
using System.IO;
using System.Text;
using Microsoft.Win32;

var thisScript = GetEnvironmentVariable("EntryScript");

var help =
@$"Custom command for printing detected locations of te specified file.
v{thisScript.GetCommandScriptVersion()} ({thisScript})
Usage:
  css -which <file>";

if (args.Length == 0 || "?,-?,-help,--help".Split(',').Contains(args.FirstOrDefault()))
{
    Console.WriteLine(help);
}
else
{
    string file = args[0].EndsWith(".exe") ? args[0] : args[0] + ".exe";
    string filePath = file;

    if (File.Exists(file))
    {
        Console.WriteLine(Path.GetFullPath(filePath));
    }
    else
    {
        var dirs = Environment.GetEnvironmentVariable("Path").Split(';').ToList();
        dirs.Insert(0, Environment.CurrentDirectory);

        foreach (string dir in dirs)
        {
            if (File.Exists(filePath = Path.Combine(dir, file)))
            {
                Console.WriteLine(filePath);
                break;
            }
        }
    }
}