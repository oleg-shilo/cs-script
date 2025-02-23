//css_ng csc
//css_nuget LockCheck -ver:0.9.32-g47722ab213
//css_include global-usings
using System.Diagnostics;
using static System.Environment;
using System.IO;
using CSScripting;
using LockCheck;

var thisScript = GetEnvironmentVariable("EntryScript");

var help = $@"Custom command for checking and unlocking a locked file or a directory.
v{thisScript.GetCommandScriptVersion()} ({thisScript})

  css -unlock <path>";

if (args.ContainsAny("-?", "?", "-help", "--help"))
{
    print(help);
    return;
}

string path = args.FirstOrDefault() ?? CurrentDirectory;

if (Directory.Exists(path))
{
    print($"Directories are not supported yet. Check https://www.nuget.org/packages/LockCheck for any release higher than v0.9.32.");
    return;
}

if (!File.Exists(path))
{
    print($"Specified file path does not exists.");
    return;
}

print($"Checking: {path}");

var features = LockManagerFeatures.UseLowLevelApi;

var lockingProcesses = LockManager.GetLockingProcessInfos([path], features)
    .Select((x, i) => new
    {
        index = i,
        name = x.ApplicationName,
        id = x.ProcessId,
        exe = x.ExecutableFullPath
    })
    .ToList();

if (lockingProcesses.Any())
{
    print("Locked by:");

    foreach (var item in lockingProcesses)
    {
        $"  {item.index}: {item.exe} ({item.id})".print();
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
static class Extensions
{
    public static bool ContainsAny(this string[] items1, params string[] items2) => items1.Intersect(items2).Any();
}