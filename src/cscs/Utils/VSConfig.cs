﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using csscript;
using static CSScripting.Utils;

namespace CSScripting
{
    class VSConfig
    {
        public static void Init(string arg)
        {
#if WIN_APP
            CSExecutor.print("Searching for Visual Studio executable is only available in console app. " +
                             "Use css or cscs script engine executable.");
#else
            var currentValue = Environment.GetEnvironmentVariable("CSSCRIPT_VSEXE");
            if (currentValue != null)
            {
                Console.WriteLine("Currently configured Visual Studio executable:");
                Console.WriteLine("  " + currentValue);
            }
            else
                Console.WriteLine("Visual Studio integration is not configured.");

            Console.WriteLine();
            Console.WriteLine("Searching for Visual Studio executable...");
            Console.WriteLine();

            var ides = FindDevenv();
            if (ides.Any())
            {
                Console.WriteLine("Visual Studio found:");
                ides.Select((x, i) => $"  {i}: {x}").ForEach(x => Console.WriteLine(x));

                string selectedIde = "";
                string input = "";
                if (ides.Count() > 1)
                {
                    var prompt = "Enter the option you want to use or 'x' to exit: ";
                    Console.WriteLine();
                    Console.Write(prompt);
                    int index;

                    if (arg.HasText())
                    {
                        input = arg;
                        Console.WriteLine(arg);
                    }
                    else
                        input = Console.ReadLine();

                    while (!int.TryParse(input, out index) && input != "x" && (index < 0 || ides.Count() < index))
                    {
                        Console.Write(prompt);
                        input = Console.ReadLine();
                    }

                    if (input != "x")
                        selectedIde = ides[index];
                }
                else
                    selectedIde = ides.LastOrDefault();

                if (input != "x")
                {
                    Console.WriteLine($"Setting environment variable\n" +
                        $"CSSCRIPT_VSEXE={selectedIde}");
                    Environment.SetEnvironmentVariable("CSSCRIPT_VSEXE", selectedIde, EnvironmentVariableTarget.User);
                    try { Win32.SetEnvironmentVariable("CSSCRIPT_VSEXE", selectedIde); } catch { } // so the child process can consume this var

                    IntPtr HWND_BROADCAST = (IntPtr)0xffff;
                    uint WM_SETTINGCHANGE = 0x1a;
                    uint SMTO_ABORTIFHUNG = 0x2;
                    var result = UIntPtr.Zero;

                    SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero, "Environment", SMTO_ABORTIFHUNG, 5000, out result);

                    Console.WriteLine();
                    Console.WriteLine($"You may need to restart the parent process before the change can take effect.\n" +
                        $"You can always check it with 'echo %CSSCRIPT_VSEXE%' or 'echo $env:CSSCRIPT_VSEXE'");
                }
            }
            else
            {
                Console.WriteLine("No Visual Studio found");
            }
#endif
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

        [DllImport("kernel32.dll")]
        static extern uint GetEnvironmentVariable(string lpName, [Out] StringBuilder lpBuffer, uint nSize);

        static string[] FindDevenv()
        {
            if (Runtime.IsWin)
            {
                // C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe
                // C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe

                var items = Directory.GetFiles(Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio"), "devenv.exe", SearchOption.AllDirectories)
                            .Concat(
                            Directory.GetFiles(Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio"), "devenv.exe", SearchOption.AllDirectories)
                                   ).OrderByDescending(x => x);

                return items.ToArray();
            }
            else
                return new string[0];
        }

        internal static string FindVSCode()
        {
            if (Runtime.IsWin)
            {
                // Common install paths
                string[] commonPaths =
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Microsoft VS Code\Code.exe"),
                    @"C:\Program Files\Microsoft VS Code\Code.exe",
                    @"C:\Program Files (x86)\Microsoft VS Code\Code.exe"
                };

                foreach (var path in commonPaths)
                    if (File.Exists(path))
                        return path;

                // Try PATH environment
                string found = "where".run("code").output?.GetLines().FirstOrDefault()?.Trim();
                if (found.FileExists())
                {
                    return found;
                }
            }
            else
            {
                // macOS or Linux
                string result = "which".run("code").output?.GetLines().FirstOrDefault()?.Trim();
                if (result.FileExists())
                    return result.Trim();

                // macOS possible GUI path
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    string macAppPath = "/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code";
                    if (macAppPath.FileExists())
                        return macAppPath;
                }
            }
            return null;
        }
    }
}