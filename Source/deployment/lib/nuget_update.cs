//css_ref System.Linq;
//css_ref System.Core; 
using System.Collections;
using System.Text;
using System.Linq;
using System.IO;
using System;
using System.Diagnostics;

//Can be used as follows
// "//css_precompiler nuget_update;" to update every time the script is changed
// "//css_pre nuget_update(wixsharp);" to update every time the script is executed

public class Precompiler
{
    public static bool Compile(ref string code, string scriptFile, bool isPrimaryScript, Hashtable context)
    {
        if(isPrimaryScript)
        {
            // //css_nuget -noref -ng:"-IncludePrerelease –version 1.0beta" cs-script, wixsharp;
            var packages = code.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                               .Take(50)
                               .Select(x => x.TrimStart())
                               .Where(x => x.StartsWith("//css_nuget "))
                               .SelectMany(x => x.Split(',').Select(p => p.TrimEnd(';').Trim()))
                               .ToArray();

            packages[0] = packages[0].Split(' ').Last(); // '//css_nuget...' item

            Main(packages);
        }

        return false; //false as the code has not been modified
    }

    static void Main(string[] packages)
    {
        foreach (var item in packages)
            Update(item);
    }

    static void Update(string package)
    {
        string latest = LatestVersion(package);
        string[] downloaded = DownloadedVersions(package);

        if (downloaded.Any() && latest != null && !downloaded.Contains(latest))
        {
            Console.WriteLine("Downloading "+package+" ...");
            Download(package);
        }
    }

    static string[] DownloadedVersions(string package)
    {
        var packageCache = Path.Combine(nugetCache, package);

        if (Directory.Exists(packageCache))
            return Directory.GetDirectories(packageCache, package+"*")
                            .Select(x =>Path.GetFileName(x).ToLower())
                            .ToArray();
        else
            return new string[0];
    }

    static void Download(string package)
    {
        var packageCache = Path.Combine(nugetCache, package);

        var proc = new Process();
        proc.StartInfo.FileName = nuget;
        proc.StartInfo.Arguments = "install " + package + " -OutputDirectory \"" + packageCache + "\"";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.CreateNoWindow = true;
        proc.Start();

        try
        {
            string line = null;
            while (null != (line = proc.StandardOutput.ReadLine()))
                Console.WriteLine(line);
        }
        catch { }

        proc.WaitForExit();
    }

    static string nugetCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CS-Script", "nuget");
    static string nuget = Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\lib\nuget.exe");

    static string LatestVersion(string package)
    {
        var proc = new Process();
        proc.StartInfo.FileName = nuget;
        proc.StartInfo.Arguments = "list " + package;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.CreateNoWindow = true;
        proc.Start();

        string line = null;
        while (null != (line = proc.StandardOutput.ReadLine()))
            if(line.StartsWith(package + " ", StringComparison.OrdinalIgnoreCase))
                return line.Replace(" ", ".").ToLower();

        proc.WaitForExit();
        return null;
    }
}