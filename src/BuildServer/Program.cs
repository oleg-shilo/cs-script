using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CSScripting.CodeDom;

namespace compile_server
{
    class Program
    {
        static int Main(string[] app_args)
        {
            var exitCode = 0;
            try
            {
                var args = app_args.Where(x => !x.StartsWith("-port:")).ToArray();

                int? port = app_args.FirstOrDefault(x => x.StartsWith("-port:"))?
                                    .Replace("-port:", "")
                                    .ParseAsPort();

                var csc_file = args.Where(x => x.StartsWith("-csc:"))
                                      .Select(x => x.Substring("-csc:".Length).Trim('"'))
                                      .FirstOrDefault();

                if (!string.IsNullOrEmpty(csc_file))
                {
                    App.Log($"Setting csc.exe path");

                    BuildServer.csc = csc_file;
                }

                if (args.FirstOrDefault() == "-start")
                {
                    App.Log($"Starting remote instance...");
                    BuildServer.StartRemoteInstance(port);
                }
                else if (args.FirstOrDefault() == "-kill")
                {
                    App.Log($"Stopping all remote instance...");
                    SendShutdownRequest();
                    BuildServer.PurgeRunningHistory();
                }
                else if (args.FirstOrDefault() == "-stop")
                {
                    App.Log($"Stopping remote instance...");
                    App.Log(BuildServer.StopRemoteInstance(port));
                }
                else if (args.FirstOrDefault() == "-restart")
                {
                    App.Log($"Restarting remote instance...");
                    BuildServer.RestartRemoteInstance(port);
                }
                else if (args.FirstOrDefault() == "-ping")
                {
                    App.Log($"Pinging remote instance...");
                    App.Log(BuildServer.PingRemoteInstance(port));
                }
                else if (args.FirstOrDefault() == "-list")
                {
                    App.Log($"Listing remote instances...");
                    App.Log(BuildServer.PingRemoteInstances());
                }
                else if (args.FirstOrDefault() == "-listen")
                {
                    ListenToShutdownRequest();

                    BuildServer.ReportRunning(port);
                    App.Log($"Starting server pid:{ Environment.ProcessId}, port:{port} ");
                    BuildServer.ListenToRequests(port);
                }
                else
                {
                    BuildServer.StartRemoteInstance(port);
                    (var buildLog, _) = BuildServer.SendBuildRequest(args, port);

                    // keep Console as app.log may be swallowing the messages
                    // and the parent process needs to read the console output
                    Console.WriteLine(buildLog);
                }
            }
            catch (Exception e)
            {
                App.Log(e.ToString());
            }
            finally
            {
                BuildServer.ReportExit();
                BuildServer.PurgeRunningHistory();
            }
            return exitCode;
        }

        static void SendShutdownRequest()
        {
            using var m = new Mutex(true, "cs-script.build.stop");
            Thread.Sleep(2000);
        }

        static void ListenToShutdownRequest()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (Mutex.TryOpenExisting("cs-script.build.stop", out var mutex))
                    {
                        BuildServer.ReportExit();
                        Process.GetCurrentProcess().Kill();
                    }
                    Thread.Sleep(1000);
                }
            });
        }
    }

    static partial class App
    {
        static public void Log(string message)
        {
            Console.WriteLine(message);
            // File.WriteAllText(Path.Combine(BuildServer.DefaultJobQueuePath, "server.log"),
            //     $"{System.Diagnostics.Process.GetCurrentProcess().Id}:{DateTime.Now.ToString("-s")}:{message}{Environment.NewLine}");
        }
    }
}