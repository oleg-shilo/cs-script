//css_ng csc
//css_nuget LockCheck -ver:0.9.32-g47722ab213
//css_include global-usings
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Text;
using CSScripting;
using LockCheck;

var thisScript = GetEnvironmentVariable("EntryScript");

var help = $@"Custom command for checking and unlocking a locked file or a directory.
v{thisScript.GetCommandScriptVersion()} ({thisScript})

  css -unlock <path>";

if (!args.Any() || args.ContainsAny("-?", "?", "-help", "--help"))
{
    print(help);
    return;
}

List<LockingProcessInfo> lockingProcesses = [];

string path = args.FirstOrDefault().Locate();

print($"Checking: {path}");

if (Directory.Exists(path))
{
    try
    {
        var handle_exe = @"D:\tools\handle\handle.exe";
        (var output, var exitCode) = handle_exe.run($"\"{path}\"");

        // cmd.exe            pid: 38644  type: File           108: D:\tools\handle
        var lines = output.Split(NewLine).Where(x => x.Contains("pid:"));
        foreach (var line in lines)
        {
            var parts = line.Split(new[] { "pid:", "type:" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var name = parts[0].Trim();
                var idPart = parts[1].Trim().Split(' ').FirstOrDefault();
                if (int.TryParse(idPart, out int id))
                {
                    if (!lockingProcesses.Any(x => x.id == id))
                        lockingProcesses.Add(new LockingProcessInfo(lockingProcesses.Count, name, id));
                }
            }
        }
    }
    catch
    {
        print($"Directories are not supported yet. Check https://www.nuget.org/packages/LockCheck " +
            $"for any release higher than v0.9.32.\n" +
            $"Alternatively, install SysInternals 'Handle' so it will be automatically used as a fallback mechanism.");
        return;
    }
}
else
{
    if (!File.Exists(path))
    {
        print($"Specified file path does not exists.");
        return;
    }

    var features = LockManagerFeatures.UseLowLevelApi;

    lockingProcesses = LockManager.GetLockingProcessInfos([path], features)
        .Select((x, i) => new LockingProcessInfo(
            i,
            x.ApplicationName,
            x.ProcessId))
        .ToList();
}

if (lockingProcesses.Any())
{
    print("Locked by:");

    foreach (var item in lockingProcesses)
    {
        $"  {item.index}: {item.name} ({item.id})".print();
    }

    print($"\nEnter 'Y' to kill the locking process(es)");
    var input = Console.ReadLine();

    if (input?.ToLower()?.Trim() == "y")
        lockingProcesses.ForEach(x =>
        {
            try
            {
                Process.GetProcessById(x.id).Kill();
            }
            catch { }
        });
}
else
    print("Not locked");

//===============================================================================
record struct LockingProcessInfo(int index, string name, int id);

static class Extensions
{
    public static bool ContainsAny(this string[] items1, params string[] items2) => items1.Intersect(items2).Any();

    public static string Locate(this string path)
    {
        string fullPath;
        var dirs = Environment.GetEnvironmentVariable("Path").Split(';').ToList();
        dirs.Insert(0, Environment.CurrentDirectory);

        foreach (string dir in dirs)
        {
            if (File.Exists(fullPath = Path.Combine(dir, path)))
                return fullPath;
            if (Directory.Exists(fullPath = Path.Combine(dir, path)))
                return fullPath;
        }
        return null;
    }
}