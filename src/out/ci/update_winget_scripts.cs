//css_ng csc
//css_inc cmd
//css_nuget SharpYaml
using System;
using System.Collections.Generic;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SharpYaml.Serialization;

(string version, string lnx_version, string changes) = vs.parse_release_notes(args.FirstOrDefault() ?? Path.GetFullPath(@"..\..\release_notes.md"));

var cliChanges = string.Join(Environment.NewLine, changes.Split("\r\n")
    .SkipWhile(x => !x.StartsWith("### CLI"))
    .Skip(1)
    .TakeWhile(x => !x.StartsWith("### CSScriptLib"))
    .Select(x => $"  {x.Trim()}")
    .ToArray())
    .Trim();

// Console.WriteLine(cliChanges); return;

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

Environment.CurrentDirectory = releaseDir;

var data = Yaml.Load("oleg-shilo.cs-script.yaml");
data["PackageVersion"] = version;
data.Save();
// -----
data = Yaml.Load("oleg-shilo.cs-script.locale.en-US.yaml");
data["PackageVersion"] = version;
data["ReleaseNotesUrl"] = $"https://github.com/oleg-shilo/cs-script/releases/tag/v{version}";
data["ReleaseNotes"] = cliChanges;
data.Save();
// -----
data = Yaml.Load("oleg-shilo.cs-script.installer.yaml");
data["PackageVersion"] = version;
data["ReleaseDate"] = DateTime.Now.ToString("yyyy-MM-dd");
data["Installers"][0]["InstallerUrl"] = url;
data["Installers"][0]["InstallerSha256"] = checksum;
data.Save();
// -----

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

    Console.WriteLine("Updating the manifest checksum...");
    var cheksum = cmd.run(@"C:\ProgramData\chocolatey\tools\checksum.exe", "-t sha256 -f \"" + file + "\"", echo: false).Trim();
    Console.WriteLine();
    return cheksum;
}

public class Yaml
{
    string _filePath;
    string _header;
    Serializer _serializer => new Serializer();
    public Dictionary<string, dynamic> Data;

    public static Yaml Load(string filePath) => new Yaml(filePath);

    public void Save() => File.WriteAllText(_filePath, this.ToString());

    public override string ToString() => _header + NewLine + NewLine + _serializer.Serialize(Data);
    public Yaml(string filePath)
    {
        _filePath = filePath;

        var text = File.ReadAllText(filePath);
        _header = string.Join(NewLine, text.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x))
            .TakeWhile(x => x.StartsWith("#")));

        Data = _serializer.Deserialize<Dictionary<string, dynamic>>(text);
    }
    public dynamic this[string key] { get => Data[key]; set => Data[key] = value; }
}