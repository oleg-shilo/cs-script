using csscript;
using CSScripting.CodeDom;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace cscs.exe.core
{
    class Program
    {
        static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("css_nuget", null);
            var css_dir = Assembly.GetExecutingAssembly().Location.GetD
            (Environment.SetEnvironmentVariable("CSSCRIPT_DIR")

            if (args.Contains("-server:stop"))
                BuildServer.Stop();
            else if (args.Contains("-server") || args.Contains("-server:start"))
                BuildServer.Start();
            else
                CSExecutionClient.Main(args);
        }
    }
}
