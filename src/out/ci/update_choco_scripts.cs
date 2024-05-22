//css_inc cmd
using static System.Environment;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System;

(string version,
 string lnx_version, _) = vs.parse_release_notes(args.FirstOrDefault() ?? Path.GetFullPath(@"..\..\release_notes.md"));

var shortVersion = version.Substring(0, version.LastIndexOf('.'));


PatchFile(
    @".\..\..\chocolatey\update_package.cs",
    x => x.Trim().StartsWith("var url ="),
    $"var url = \"https://github.com/oleg-shilo/cs-script/releases/download/v{version}/cs-script.win.v{version}.7z\";");

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




