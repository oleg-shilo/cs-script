//css_nuget NuGet.Versioning
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NuGet.Versioning;

void move(string mask)
{
    // CS-Script.4.9.9-pre.nupkg
    var package = Directory.GetFiles(@"..\bin\Release", mask)
                           .OrderByDescending(x => ExtractVersionFromFileName(x))
                           .FirstOrDefault();
    File.Copy(package, Path.GetFileName(package), true);
    Console.WriteLine(package);
}

NuGetVersion ExtractVersionFromFileName(string fileName)
{
    // This regex captures the version part, including pre-release suffixes.
    // It looks for a sequence of numbers and dots, optionally followed by a hyphen and any characters (for pre-release).
    // It ensures this pattern is followed by ".nupkg".
    string pattern = @"(?<version>\d+(\.\d+){1,3}(-\w+)?)\.nupkg$";
    Match match = Regex.Match(Path.GetFileName(fileName), pattern);

    if (match.Success)
    {
        return NuGetVersion.Parse(match.Groups["version"].Value);
    }
    else
    {
        return null; // Or throw an exception, depending on desired error handling
    }
}

Directory
    .GetFiles(@".\", "*.*nupkg")
    .ToList()
    .ForEach(x => File.Delete(x));

move("CS-Script.*.nupkg");
move("CS-Script.*.snupkg");