using static System.Reflection.Assembly;
using static System.IO.Path;
using static System.IO.File;
using static System.Environment;
using static System.Diagnostics.Process;

using System;
using System.Linq;
using System.IO;
using System.Diagnostics;

if (args.Contains("?") || args.Contains("-?") || args.Contains("-help"))
{
    string version = Path.GetFileNameWithoutExtension(
                         Directory.GetFiles(Path.GetDirectoryName(Environment.GetEnvironmentVariable("EntryScript")), "*.version")
                                  .FirstOrDefault() ?? "0.0.0.0.version");

    Console.WriteLine($@"v{version} ({Environment.GetEnvironmentVariable("EntryScript")})");
    Console.WriteLine(
        "Integrates CS-Script with the environment by setting the `CSSCRIPT_ROOT` " +
        "environment variable.");
    return;
}

var engine_asm_folder = Path.GetDirectoryName(GetEntryAssembly().Location);

$"CSSCRIPT_ROOT is set to `{engine_asm_folder}`".print();

Environment.SetEnvironmentVariable("CSSCRIPT_ROOT", engine_asm_folder, EnvironmentVariableTarget.User);

if (Environment.OSVersion.Platform == PlatformID.Win32NT)
{
    // WinGet and .NET Tools do not allow creating a link/shim to more than a single executable (Chocolatey does)
    // Thus creating csws.exe shim for WinGet and .NET Tools

    var alias = "csws.exe";
    var target = Path.Combine(engine_asm_folder, alias);
    var outDir = "";

    if (File.Exists(target)) // csws.exe is present in the distribution
    {
        if (engine_asm_folder.Contains("Microsoft\\WinGet\\Packages\\oleg-shilo.cs-script"))
        {
            var start = engine_asm_folder.IndexOf("\\Packages\\oleg-shilo.cs-script");
            outDir = Path.Combine(engine_asm_folder.Substring(0, start), "Links");
        }
        else if (engine_asm_folder.Contains(".dotnet\\tools\\.store\\cs-script.cli"))
        {
            var start = engine_asm_folder.IndexOf("\\.store\\cs-script.cli");
            outDir = engine_asm_folder.Substring(0, start);
        }

        if (!string.IsNullOrEmpty(outDir))
        {
            if (!mkshimExists())
                Console.WriteLine("The shim to `csws.exe` cannot be created. 'mkshim' utility was not found. You can install it from WinGet with `winget install mkshim`");
            else
                create(alias, target, outDir);
        }
    }
}

bool mkshimExists()
{
    try
    {
        var p = new Process();
        p.StartInfo.FileName = "mkshim";
        p.StartInfo.Arguments = $"-?";
        p.StartInfo.RedirectStandardOutput = true;
        p.Start();
        return true;
    }
    catch
    {
        return false;
    }
}

void create(string alias, string target, string outDir)
{
    if (File.Exists(Path.Combine(outDir, alias)))
        File.Delete(Path.Combine(outDir, alias));

    var p = new Process();
    p.StartInfo.FileName = "cmd";
    p.StartInfo.Arguments = $"/C \"mkshim {alias} \"{target}\"\"";
    p.StartInfo.WorkingDirectory = outDir;
    p.StartInfo.RedirectStandardOutput = true;
    p.Start();
    var result = p.StandardOutput.ReadToEnd()?.Trim();
    p.WaitForExit();
    Console.WriteLine(result);
}