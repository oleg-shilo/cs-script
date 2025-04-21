//css_inc cmd
using static System.Environment;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System;

(string version,
 string lnx_version, _) = vs.parse_release_notes(args.FirstOrDefault() ?? Path.GetFullPath(@"..\..\release_notes.md"));

var winget_repo = Path.GetFullPath(@"..\..\..\..\winget-pkgs");

cmd.run("git", $"branch oleg-shilo_cs-script_{version}", winget_repo);
cmd.run("git", $"checkout oleg-shilo_cs-script_{version}", winget_repo);

var releaseDir = Path.Combine(winget_repo, $@"manifests\o\oleg-shilo\cs-script\{version}");

Console.WriteLine($"Creating new winget manifest files ...");

cmd.xcopy(
    Path.Combine(winget_repo, @"manifests\o\oleg-shilo\cs-script\4.9.4.0\*"),
    releaseDir);

Console.WriteLine();

cmd.run("git", $"add -A", winget_repo);

// ---------------------------

var url = $"https://github.com/oleg-shilo/cs-script/releases/download/v{version}/cs-script.win.v{version}.zip";

var checksum = CalcChecksum(url);

PatchFile(
    Path.Combine(releaseDir, "oleg-shilo.cs-script.yaml"),
    x => x.StartsWith("PackageVersion: "),
    $"PackageVersion: {version}");

PatchFile(
    Path.Combine(releaseDir, "oleg-shilo.cs-script.locale.en-US.yaml"),
    x => x.StartsWith("PackageVersion: "),
    $"PackageVersion: {version}");

PatchFile(
    Path.Combine(releaseDir, "oleg-shilo.cs-script.installer.yaml"),
    x => x.StartsWith("PackageVersion: "),
    $"PackageVersion: {version}");

PatchFile(
    Path.Combine(releaseDir, "oleg-shilo.cs-script.installer.yaml"),
    x => x.StartsWith("- InstallerUrl:"),
    $"- InstallerUrl: {url}");

PatchFile(
    Path.Combine(releaseDir, "oleg-shilo.cs-script.installer.yaml"),
    x => x.StartsWith("  InstallerSha256:"),
    $"  InstallerSha256: {checksum}");

Console.WriteLine("----------------------------");
Console.WriteLine("Manifest files are updated to target release: v" + version);

Console.WriteLine("----------------------------");
cmd.run("winget", @"validate .\manifests\o\oleg-shilo\cs-script\" + version, winget_repo);

Console.WriteLine("Optionally check the installation with:");
Console.WriteLine("cd " + winget_repo);
Console.WriteLine(@"winget install -m .\manifests\o\oleg-shilo\cs-script\" + version + " --ignore-local-archive-malware-scan");
Console.WriteLine(@"winget uninstall -m .\manifests\o\oleg-shilo\cs-script\" + version);

Console.WriteLine("============================");
Console.WriteLine($"Commit winget-pkgs branch now from \"{winget_repo}\"");
Console.WriteLine("");
// ----------------------------
void PatchFile(string file, Predicate<string> filter, string replacement)
{
    var lines = File.ReadAllLines(file).ToList();

    var urlIndex = lines.FindIndex(filter);
    lines[urlIndex] = replacement;

    File.WriteAllLines(file, lines);
}

string CalcChecksum(string url)
{
    var file = Path.GetFileName(url);
    Console.WriteLine($"Downloading {file} ...");
    cmd.download(url, file, (step, total) => Console.Write("\r{0}%\r", (int)(step * 100.0 / total)));

    Console.WriteLine("Updating the manifest checksu m...");
    var cheksum = cmd.run(@"C:\ProgramData\chocolatey\tools\checksum.exe", "-t sha256 -f \"" + file + "\"", echo: false).Trim();
    Console.WriteLine();
    return cheksum;
}