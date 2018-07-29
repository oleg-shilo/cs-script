using csscript;
using System;
using System.IO;
using System.Linq;

/// <summary>
/// .NET Core host (app launcher) for CS-Script engine class library assembly.
/// Runtime: .NET Core 2.1
/// File name: cscs.dll
/// Example: "dotnet cscs.dll script.cs" 
/// </summary>

namespace cscs.exe.core
{
    class Program
    {
        static void Main(string[] args)
        {
            // var t = typeof(System.Text.RegularExpressions.Regex).Assembly.Location;

            // var v1 = "10.20";
            // var v2 = "2.20";
            // var v3 = "1.20";
            // var arr = new[] { v1, v2, v3 };
            // var rrr = arr.OrderByDescending(x=>x).ToArray();

            // var t = string.Compare(v1, v2);
            //var arr = "-noref -ng:\"-IncludePrerelease –version 1.0beta\" cs-script".SplitCommandLine().ToArray();

            Environment.SetEnvironmentVariable("css_nuget", null);
            CSExecutionClient.Main(args);
        }
    }
}
