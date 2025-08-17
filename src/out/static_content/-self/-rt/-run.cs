//css_engine csc
//css_include global-usings
using static System.Console;
using System.Diagnostics;
using static System.Environment;
using static System.Reflection.Assembly;
using System.Text.Json;
using System.Text.Json.Nodes;

var arg1 = args.FirstOrDefault();

if (arg1 == "?" || arg1 == "/?" || arg1 == "-?" || arg1 == "-help")
{
    string version = Path.GetFileNameWithoutExtension(
                         Directory.GetFiles(Path.GetDirectoryName(GetEnvironmentVariable("EntryScript")), "*.version")
                                  .FirstOrDefault() ?? "0.0.0.0.version");

    WriteLine($@"v{version} ({Environment.GetEnvironmentVariable("EntryScript")})");
    WriteLine($"Sets the script engine target runtime to one of the available .NET ideployments.");
    WriteLine($"    css -self-rt [-help] ");
    return;
}

// ===============================================

Console.WriteLine("Available .NET SDKs:");

var sdk_list = "dotnet".run("--list-runtimes") // Microsoft.NETCore.App 9.0.4 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
    .Split(NewLine)
    .Where(line => !string.IsNullOrWhiteSpace(line))
    .Select(line => line.Split(' ').Skip(1).FirstOrDefault()?.Trim())
    .DistinctBy(line => line)
    .OrderBy(line => new Version(line.Split('-').FirstOrDefault())) // to split by `-` handle pre-releases (e.g. 10.0.0-preview.7.25380.108)
    .Select((line, i) => new { Index = i + 1, Version = line })
    .ToList();

foreach (var line in sdk_list)
{
    WriteLine($"{line.Index}:  {line.Version}");
}

WriteLine();
Write("Enter the index of the desired target SDK: ");
var input = ReadLine();

if (int.TryParse(input, out int index))
{
    var i = index - 1;
    if (i < 0 || i >= sdk_list.Count)
    {
        WriteLine($"Invalid index: {index}. Please enter a number between 1 and {sdk_list.Count}.");
        return;
    }

    WriteLine($"{NewLine}" +
        $"Setting target runtime{NewLine}" +
        $"  CLR: {sdk_list[i].Version}{NewLine}" +
        $"  Assembly: {GetEntryAssembly()?.Location}{NewLine}");

    var runtimeconfig = Path.ChangeExtension(GetEntryAssembly().Location, ".runtimeconfig.json");

    //update the .runtimeconfig.json file
    if (File.Exists(runtimeconfig))
    {
        var changedConfig = runtimeconfig.UpdateFrameworkVersionTo(sdk_list[i].Version);

        WriteLine($"The assembly's {Path.GetFileName(runtimeconfig)} has been updated:");
        try
        {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteLine(changedConfig);
        }
        finally
        {
            Console.ResetColor();
        }
    }
    else
    {
        WriteLine($"Runtime configuration file not found: {runtimeconfig}");
    }
}
else
    WriteLine($"Invalid input: '{input}'. Please enter a valid number.");

//===============================================
static class Extensions
{
    public static string UpdateFrameworkVersionTo(this string file, string version)
    {
        var ver = version.Split('.'); //10.0.100-preview.7.25380.108 or 9.0.304
        var tfm = $"net{ver[0]}.{ver[1]}";
        var json = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(file));
        json["runtimeOptions"]["tfm"] = tfm;
        json["runtimeOptions"]["framework"]["version"] = version;
        File.WriteAllText(file, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        return json["runtimeOptions"].ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    public static string run(this string exe, string args, string dir = null)
    {
        var proc = new Process();
        proc.StartInfo.FileName = exe;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.WorkingDirectory = dir;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.Start();
        return proc.StandardOutput.ReadToEnd()?.Trim();
    }
}