//css_include global-usings
using CSScripting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static dbg;
using static System.Console;
using static System.Environment;

var thisScript = GetEnvironmentVariable("EntryScript");

var help =
@$"Custom command for gernerating C# project file for a given script.
v{thisScript.GetCommandScriptVersion()} ({thisScript})
  css -csproj <script>
  (e.g. `css -csproj test.cs`)";

if (args.IsEmpty() || "?,-?,-help,--help".Split(',').Contains(args.FirstOrDefault()))
{
    WriteLine(help);
    return;
}

// -----------------------------------------------
// Command implementation
// -----------------------------------------------

var script = args[0];

print($"Creating project for '{script}'");

var output = "css".run($"-proj:csproj \"{script}\"");

var inputDir = output.Replace("project:", "").GetDirName(); // "project:c:\temp.test.csproj"

var outputDir = Path.Combine(
        script.GetDirName(),
        script.GetFileNameWithoutExtension());

inputDir.copyFilesTo(outputDir);

inputDir.PathJoin("properties")
        .copyFilesTo(outputDir.PathJoin("properties"));

print($"Created in: {outputDir}");

// -----------------------------------------------
static class Extensions
{
    public static void copyFilesTo(this string srcDir, string destDir)
    {
        destDir.EnsureDir();
        Directory.GetFiles(srcDir, "*")
                 .ForEach(f => File.Copy(f, destDir.PathJoin(f.GetFileName()), true));
    }

    public static string run(this string exe, string args)
    {
        var proc = new Process();
        proc.StartInfo.FileName = exe;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.Start();
        return proc.StandardOutput.ReadToEnd()?.Trim();
    }
}