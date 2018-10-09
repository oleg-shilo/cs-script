using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        [DllImport("kernel32", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        static void Main(string[] args)
        {
            bool hideConsole = false;
            if (args.Contains("-noconsole"))
            {
                hideConsole = true;
                args = args.Where(a => a != "-noconsole").ToArray();
            }

            if (args.Contains("-nc"))
            {
                hideConsole = true;
                args = args.Where(a => a != "-nc").ToArray();
            }

            var arguments = new List<string>();
            arguments.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cscs.dll"));
            arguments.AddRange(args);

            var combinedArgs = arguments.Select(x => (x.Contains(" ") || x.Contains("\t")) ? $"\"{x}\"" : x)
                                        .ToArray();

            // ScriptLauncher.ShowConsole(); // interferes with Conspole.ReadKey

            if (hideConsole)
                ScriptLauncher.HideConsole();

            ScriptLauncher.Run("dotnet", string.Join(" ", combinedArgs));
        }
    }
}
