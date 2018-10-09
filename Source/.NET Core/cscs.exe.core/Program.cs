using csscript;
using CSScripting.CodeDom;
using System;
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

            if (args.Contains("-server:stop"))
                BuildServer.Stop();
            else if (args.Contains("-server") || args.Contains("-server:start"))
                BuildServer.Start();
            else
                CSExecutionClient.Main(args);
        }
    }
   
}
