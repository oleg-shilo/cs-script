using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// .NET Full/Mono app launcher for CS-Script .NET Core host. This executable is a simple process router that forks 
/// a .NET Core host for CS-Script engine class library assembly. It simply executes "dotnet cscs.dll script.cs" and redirects
/// STD input, error and output. 
/// It's just a convenient way to launch CS-Script .NET Core with minimal typing.
/// Runtime: .NET Framework 4.6.1
/// File name: cssc.exe
/// Example: "cssc.exe script.cs" 
/// </summary>

namespace cssc
{
    class Program
    {
        static int Main(string[] args)
        {
            var arguments = new List<string>();
            arguments.Add(@"E:\Galos\Projects\CS-Script\GitHub\cs-script\Source\.NET Core\cscs.exe.core\bin\Debug\netcoreapp2.1\cscs.dll");
            arguments.AddRange(args);

            var combinedArgs = string.Join(" ",
                                           arguments.Select(x => (x.Contains(" ") || x.Contains("\t")) ? $"\"{x}\"" : x)
                                                    .ToArray());

            return Run("dotnet", combinedArgs, null, Console.WriteLine, Console.WriteLine);
        }

        private static Thread StartMonitor(StreamReader stream, Action<string> action = null)
        {
            var thread = new Thread(x =>
            {
                try
                {
                    string line = null;
                    while (null != (line = stream.ReadLine()))
                        action?.Invoke(line);
                }
                catch { }
            });
            thread.Start();
            return thread;
        }

        private static int Run(string exe, string args, string dir = null, Action<string> onOutput = null, Action<string> onError = null)
        {
            var process = new Process();

            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = args;
            process.StartInfo.WorkingDirectory = dir;

            // hide terminal window
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            var error = StartMonitor(process.StandardError, onError);
            var output = StartMonitor(process.StandardOutput, onOutput);

            process.WaitForExit();

            try { error.Abort(); } catch { }
            try { output.Abort(); } catch { }

            return process.ExitCode;
        }
    }
}
