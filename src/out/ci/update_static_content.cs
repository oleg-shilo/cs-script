//css_ng csc
//css_inc cmd
using System;
using System.Collections.Generic;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

// copy static content to the output folders
// update the cscs.csproj with the new tools paths

string custom_cmd_src = @"..\static_content";
string cscs_proj = @"..\..\cscs\cscs.csproj";

var comamnds = Directory.GetDirectories(custom_cmd_src, "-*");

var nuspecInjection = new List<string>();

foreach (var dir in comamnds)
{
    var name = Path.GetFileName(dir);
    Console.WriteLine($"--- Processing {name}...");

    // -------------------
    var destWinDir = $@"..\..\out\Windows\{name}";
    var destLinuxDir = $@"..\..\out\Linux\{name}";

    if (Directory.Exists(destWinDir))
        cmd.rm(destWinDir);
    if (Directory.Exists(destLinuxDir))
        cmd.rm(destLinuxDir);

    cmd.xcopy($@"..\..\out\static_content\{name}\*", destWinDir);

    if (name != "-mkshim")
        cmd.xcopy($@"..\..\out\static_content\{name}\*", destLinuxDir);

    // -------------------

    var nuspecTemplate = $"    <Content Include=\"..\\out\\static_content\\${name}\\**\\*\" Link=\"ToolPackage/{name}\" Pack=\"true\" PackagePath=\"tools/$[runtime]/any/{name}\" />";

    nuspecInjection.Add(nuspecTemplate.Replace("$[runtime]", "net9.0"));

    if (name != "-wdbg")
        nuspecInjection.Add(nuspecTemplate.Replace("$[runtime]", "net8.0"));
}

var proj = File.ReadLines(cscs_proj);

var startMarker = proj.First(x => x.TrimStart().StartsWith("<!-- start: nuget tool package"));
var endMarker = proj.First(x => x.TrimStart().StartsWith("<!-- end: nuget tool package"));

var proj_top = proj.TakeWhile(x => x != startMarker).ToList();
proj_top.Add(startMarker);

var proj_bottom = proj.SkipWhile(x => x != endMarker).ToList();

var finalContent = proj_top.Concat(nuspecInjection).Concat(proj_bottom);

File.WriteAllLines(cscs_proj, finalContent, Encoding.UTF8);