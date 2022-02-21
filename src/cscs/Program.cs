using System;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using csscript;
using CSScripting;
using CSScripting.CodeDom;

/*
 TODO:

   csc_builder
     - add configurable exit on idle

   cscs
     - code cleanup
     - VB support
     - Unify namespaces
     - Migrate app settings to json
     - remove old not used settings
     - clean help content from unused stuff
     - implement config for port number

   CSSCriptLib
     - VB support
     - implement config for port number
*/

namespace cscs
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                try { SetEnvironmentVariable("Console.WindowWidth", Console.WindowWidth.ToString()); } catch { }
                SetEnvironmentVariable("ENTRY_ASM", Assembly.GetEntryAssembly().GetName().Name);
                SetEnvironmentVariable("DOTNET_SHARED", typeof(string).Assembly.Location.GetDirName().GetDirName());
                SetEnvironmentVariable("WINDOWS_DESKTOP_APP", Runtime.DesktopAssembliesDir);
                SetEnvironmentVariable("WEB_APP", Runtime.WebAssembliesDir);
                SetEnvironmentVariable("css_nuget", null);

                Runtime.GlobalIncludsDir?.EnsureDir();
                Runtime.CustomCommandsDir.EnsureDir();

                var serverCommand = args.LastOrDefault(x => x.StartsWith("-server"));

                if (serverCommand.HasText() && !args.Any(x => x == "?"))
                {
                    if (serverCommand == "-server:stop") Globals.StopBuildServer();
                    else if (serverCommand == "-server:start") Globals.StartBuildServer();
                    else if (serverCommand == "-server:restart") Globals.RestartBuildServer();
                    else if (serverCommand == "-server:ping") Globals.Ping();
                    else if (serverCommand == "-server:reset") Globals.ResetBuildServer();
                    else if (serverCommand == "-server:add") Globals.DeployBuildServer();
                    else if (serverCommand == "-server:remove") Globals.RemoveBuildServer();
                    else if (serverCommand == "-servers:start") { CSScripting.Roslyn.BuildServer.Start(); Globals.StartBuildServer(); }
                    else if (serverCommand == "-servers:stop") { CSScripting.Roslyn.BuildServer.Stop(); Globals.StopBuildServer(); }
                    else if (serverCommand == "-server_r:start") CSScripting.Roslyn.BuildServer.Start();
                    else if (serverCommand == "-server_r:stop") CSScripting.Roslyn.BuildServer.Stop();
                    else Globals.PrintBuildServerInfo();
                }
                else
                    CSExecutionClient.Run(args);

                ThreadPool.QueueUserWorkItem(x =>
                {
                    Thread.Sleep(200);
                    // alive too long on WLS2
                    Process.GetCurrentProcess().Kill(); // some background monitors may keep the app
                });

                return Environment.ExitCode;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
        }
    }
}