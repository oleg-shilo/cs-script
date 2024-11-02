//css_engine csc
//css_include global-usings
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

var arg1 = args.FirstOrDefault();

if (arg1 == null || arg1 == "/?" || arg1 == "-?" || arg1 == "-help")
{
    Console.WriteLine($"Sets the specified assembly(s) target runtime to the currently active .NET configuration.");
    Console.WriteLine($"    css -set-rt [-help] <assembly_1 [assembly_N]> ");
    return;
}

var sw = Stopwatch.StartNew();

Console.WriteLine($"Environment Version: {Environment.Version}");
Console.WriteLine("Generating the configuration file...");

var dir = Path.Combine(Path.GetTempPath(), "css-set-rt").createOrClear();
var output = "dotnet".run("new console", dir);
output = "dotnet".run("build", dir);

var configSrc = Directory.GetFiles(dir, "*.runtimeconfig.json", SearchOption.AllDirectories).FirstOrDefault();
var json = File.ReadAllText(configSrc);

foreach (var assembly in args)
{
    if (!File.Exists(assembly))
    {
        Console.WriteLine($"You mast specify the assembly file to update its runtime configuration.");
        Console.WriteLine($"File not found: {assembly}");
        return;
    }

    var configFile = Path.ChangeExtension(assembly, ".runtimeconfig.json");

    Console.WriteLine(configFile);
    File.WriteAllText(configFile, json);
}
Console.WriteLine("------------------");

Console.WriteLine(json);
Console.WriteLine(sw.Elapsed);

static class Extensions
{
    public static string createOrClear(this string dir)
    {
        Directory.CreateDirectory(dir);
        Environment.CurrentDirectory = dir;
        Directory.GetFiles(dir, "*", SearchOption.AllDirectories).ToList().ForEach(File.Delete);
        return dir;
    }

    public static string run(this string exe, string args, string dir)
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