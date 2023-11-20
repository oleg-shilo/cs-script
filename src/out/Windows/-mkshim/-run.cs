//css_ref D:\dev\mkshim\src\packages\IconExtractor.1.0.1\lib\netstandard2.0\IconExtractor.dll
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Toolbelt.Drawing;

static class MkShim
{
    static StringBuilder compileLog = new StringBuilder();

    static string templateFile = Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("EntryScript") ??
                                                                    typeof(IconExtractor).Assembly.Location ??
                                                                    Assembly.GetExecutingAssembly().Location ??
                                                                    @".\dummy"),
                                              "ConsoleShim.cstemplate");

    static void Main(string[] args)
    {
        if (HandleUserInput(args))
            return;

        var shim = args[0];
        var exe = args[1];

        if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"You mast specify the executable file to create the shim for.");
            return;
        }

        var buildDir = Path.Combine(Path.GetTempPath(), $"mkshim-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(buildDir);

            var icon = exe.ExtractFirstIconToFolder(buildDir);
            var csFile = exe.GetShimSourceCodeFor(buildDir);

            var build = csc.Run($"-out:\"{shim}\" /win32icon:\"{icon}\" \"{csFile}\"");
            build.WaitForExit();

            if (build.ExitCode == 0)
            {
                Console.WriteLine($"The shim has been created");
                Console.WriteLine($"  {shim}");
                Console.WriteLine($"     `-> {exe}");
            }
            else
            {
                Console.WriteLine($"Cannot build the shim.");
                Console.WriteLine($"Error: ");
                Console.WriteLine(compileLog);
            }
        }
        finally { try { Directory.Delete(buildDir, true); } catch { } }
    }

    static bool HandleUserInput(string[] args)
    {
        if (args.Contains("-h") || args.Contains("-?") || args.Contains("-help"))
        {
            Console.WriteLine($@"Usage:");
            Console.WriteLine($@"   css -mkshim <shim_name> <mapped_executable>");
            return true;
        }

        if (!args.Any() || args.Count() < 2)
        {
            Console.WriteLine($@"Please run 'css -mkshim -?' for help menu.");
            return true;
        }

        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            Console.WriteLine("Creating a shim to an executable file this way is only useful on windows. On Linux you " +
                              "have a much better option `alias`. You can enable it as below: " + Environment.NewLine +
                              "alias css='dotnet /usr/local/bin/cs-script/cscs.exe'" + Environment.NewLine +
                              "After that you can invoke CS-Script engine from anywhere by just typing 'css'.");
            return true;
        }

        return false;
    }

    static string ExtractFirstIconToFolder(this string binFilePath, string outDir)
    {
        string iconFile = Path.Combine(outDir, Path.GetFileNameWithoutExtension(binFilePath) + ".ico");
        using (var s = File.Create(iconFile))
            IconExtractor.Extract1stIconTo(binFilePath, s);

        return iconFile;
    }

    public static string ArgValue(this string[] args, string name)
    {
        return args.FirstOrDefault(x => x.StartsWith($"-{name}:"))?.Split(new[] { ':' }, 2).LastOrDefault();
    }

    static string GetShimSourceCodeFor(this string exe, string outDir)
    {
        var version = exe.GetFileVersion();
        var template = File.ReadAllText(templateFile);
        var csFile = Path.Combine(outDir, Path.GetFileName(exe) + ".cs");

        var code = template.Replace("//{version}", $"[assembly: System.Reflection.AssemblyFileVersionAttribute(\"{version}\")]")
                           .Replace("//{appFile}", $"static string appFile = @\"{exe}\";");

        File.WriteAllText(csFile, code);

        return csFile;
    }

    static Process Run(this string exe, string args)
    {
        var p = new Process();
        p.StartInfo.FileName = exe;
        p.StartInfo.Arguments = args;
        p.StartInfo.UseShellExecute = false;
        // ChildProcess.StartInfo.WorkingDirectory = workingDir;
        p.StartInfo.RedirectStandardError = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardInput = false;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;

        p.Start();

        string line;
        while (null != (line = p.StandardOutput.ReadLine()))
        {
            if (line.Trim() != "" && !line.Trim().StartsWith("This compiler is provided as part of the Microsoft (R) .NET Framework,"))
                compileLog.AppendLine("> " + line);
        }
        return p;
    }

    static string GetFileVersion(this string file)
        => FileVersionInfo.GetVersionInfo(file).FileVersion;

    static string csc
    => @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe";
}