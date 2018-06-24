using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        static void Main(string[] args)
        {

            var arguments = new List<string>();
            arguments.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cscs.dll"));
            arguments.AddRange(args);

            var combinedArgs = arguments.Select(x => (x.Contains(" ") || x.Contains("\t")) ? $"\"{x}\"" : x)
                                        .ToArray();

            ScriptLauncher.Run("dotnet", string.Join(" ", combinedArgs));
            // Console.WriteLine("===========");
            // ScriptLauncher.Run("dotnet", string.Join(" ", combinedArgs));

            // Console.WriteLine("The end");
        }
    }
}
