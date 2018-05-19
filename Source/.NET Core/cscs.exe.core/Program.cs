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
            Environment.SetEnvironmentVariable("css_nuget", null);
            CSExecutionClient.Main(args);
        }
    }
}
