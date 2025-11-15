//css_inc cmd
using System;
using System.Collections.Generic;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Xml.Linq;

(string version,
 string lnx_version, _) = vs.parse_release_notes(args.FirstOrDefault() ?? Path.GetFullPath(@"..\..\release_notes.md"));

var shortVersion = version.Substring(0, version.LastIndexOf('.'));

var zip_url = $"https://github.com/oleg-shilo/cs-script/releases/download/v{version}/cs-script.win.v{version}.zip";
var _7z_url = $"https://github.com/oleg-shilo/cs-script/releases/download/v{version}/cs-script.win.v{version}.7z";
var zipChecksum = calcChecksum(zip_url);

// scoop
PatchFile(
     @".\..\..\..\scoop-bucket\cs-script.json",
     x => x.Trim().StartsWith("\"version\": \""),
     $"    \"version\": \"{version}\",");

PatchFile(
     @".\..\..\..\scoop-bucket\cs-script.json",
     x => x.Trim().StartsWith("\"url\": \"https://github.com/oleg-shilo/cs-script/releases"),
    $"    \"url\": \"{zip_url}\",");
PatchFile(

    @".\..\..\..\scoop-bucket\cs-script.json",
    x => x.Trim().StartsWith("\"hash\": \"sha256:"),
    $"    \"hash\": \"sha256:{zipChecksum}\",");

// choco
PatchFile(
    @".\..\..\chocolatey\update_package.cs",
    x => x.Trim().StartsWith("var url ="),
    $"var url = \"{_7z_url}\";");

PatchFile(
    @".\..\..\chocolatey\publish.cmd",
    x => x.Trim().StartsWith("choco push "),
    $"choco push cs-script.{shortVersion}.nupkg --source https://push.chocolatey.org/");

Console.WriteLine("'update_package.cs' script is set to target release: v" + version);

void PatchFile(string file, Predicate<string> filter, string replacement)
{
    var lines = File.ReadAllLines(file).ToList();

    var urlIndex = lines.FindIndex(filter);
    lines[urlIndex] = replacement;

    File.WriteAllLines(file, lines);
}

string calcChecksum(string url)
{
    var file = "cs-script.7z";
    cmd.download(url, file, (step, total) => Console.Write("\r{0}%\r", (int)(step * 100.0 / total)));
    Console.WriteLine();

    var checksum = cmd.run(@"C:\ProgramData\chocolatey\tools\checksum.exe", "-t sha256 -f \"" + file + "\"", echo: false).Trim();
    return checksum;
}