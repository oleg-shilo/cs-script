using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

// -c:0 "E:\Galos\Projects\CS-Script\GitHub\cs-script\Source\.NET Core\spike\script.cs"

/// <summary>
/// .NET Full/Mono app launcher for CS-Script .NET Core host. This executable is a simple process router that forks
/// a .NET Core host for CS-Script engine class library assembly. It simply executes "dotnet cscs.dll script.cs" and redirects
/// STD input, error and output.
/// It's just a convenient way to launch CS-Script .NET Core with minimal typing.
/// Runtime: .NET Framework 4.6.1
/// File name: cssc.exe
/// Example: "cssc.exe script.cs"
/// </summary>
namespace css
{
    static class Program
    {
        static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("Console.WindowWidth", Console.WindowWidth.ToString());
            bool hideConsole = false;

            if (args.Contains("-noconsole") || args.Contains("-nc"))
            {
                hideConsole = true;
                args = args.Where(a => a != "-noconsole" && a != "-nc").ToArray();
            }

            if (args.ParseValuedArg("engine", "eng", out string value))
            {
                var full_env = Environment.OSVersion.IsWin() ? ".NET" : "Mono";

                args = args.Where(a => !(a.StartsWith("-engine") || a.StartsWith("-eng"))).ToArray();
                if (value == null)
                {
                    if (args.Contains("?") || args.Contains("help"))
                    {
                        Console.WriteLine("-eng|-engine[:<core|net>]\n" +
                                          "    Sets the execution engine to .NET Core or the full version of .NET/Mono.");
                    }
                    else
                    {
                        if (File.Exists(RedirectFileName))
                            Console.WriteLine($"The execution engine is set to {full_env}");
                        else
                            Console.WriteLine($"The execution engine is set to .NET Core");
                    }
                }
                else
                {
                    switch (value.ToLower())
                    {
                        case "net":
                            File.WriteAllText(RedirectFileName, "");
                            Console.WriteLine($"The execution engine is set to {full_env}");
                            break;

                        case "core":
                            if (File.Exists(RedirectFileName))
                                File.Delete(RedirectFileName);
                            Console.WriteLine($"The execution engine is set to .NET Core");
                            break;
                    }
                }
                return;
            }

            var host = "dotnet";
            var arguments = new List<string>(args);

            if (ConfiguredFullDotNetLauncher != null)
            {
                if (Environment.OSVersion.IsWin())
                {
                    host = ConfiguredFullDotNetLauncher;
                }
                else
                {
                    host = "mono";
                    arguments.Insert(0, ConfiguredFullDotNetLauncher);
                }
            }
            else
            {
                host = "dotnet";
                arguments.Insert(0, Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cscs.dll"));
            }

            // ScriptLauncher.ShowConsole(); // interferes with Conspole.ReadKey

            if (hideConsole)
                ScriptLauncher.HideConsole();

            ScriptLauncher.Run(host, arguments.ToCmdArgs());
        }

        static string RedirectFileName
        {
            get => Assembly.GetExecutingAssembly().Location + ".net_redirect";
        }

        static string ConfiguredFullDotNetLauncher
        {
            get
            {
                if (Environment.OSVersion.IsWin())
                    return null;

                if (!File.Exists(RedirectFileName))
                    return null;

                var css_dir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");
                if (css_dir != null)
                {
                    var launcher = Path.Combine(css_dir, "cscs.exe");
                    if (File.Exists(launcher))
                        return launcher;
                }

                return null;
            }
        }
    }
}