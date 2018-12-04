using csscript;
using CSScripting.CodeDom;
using System;
using System.Linq;

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