using System;
using static System.Console;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSScripting.CodeDom
{
    public static partial class BuildServer
    {
        static string csc_asm_file;

        static public string csc
        {
            set
            {
                csc_asm_file = value;
            }

            get
            {
                if (csc_asm_file == null)
                {
                    // linux ~dotnet/.../3.0.100-preview5-011568/Roslyn/... (cannot find in preview)
                    // win: program_files/dotnet/sdk/<version>/Roslyn/csc.dll
                    var dotnet_root = "".GetType().Assembly.Location;

                    // find first "dotnet" parent dir by trimming till the last "dotnet" token
                    dotnet_root = String.Join(Path.DirectorySeparatorChar,
                                              dotnet_root.Split(Path.DirectorySeparatorChar)
                                                         .Reverse()
                                                         .SkipWhile(x => x != "dotnet")
                                                         .Reverse()
                                                         .ToArray());

                    var sdkDir = Path.Combine(dotnet_root, "sdk");
                    if (Directory.Exists(sdkDir)) // need to check as otherwise it will throw
                    {
                        var dirs = Directory.GetDirectories(sdkDir)
                                            .Where(dir => { var firstChar = Path.GetFileName(dir)[0]; return char.IsDigit(firstChar); })
                                            .OrderBy(x => Version.Parse(Path.GetFileName(x).Split('-').First()))
                                            .ThenBy(x => Path.GetFileName(x).Split('-').Length)
                                            .SelectMany(dir => Directory.GetDirectories(dir, "Roslyn"))
                                            .ToArray();

                        csc_asm_file = dirs.Select(dir => Path.Combine(dir, "bincore", "csc.dll"))
                                       .LastOrDefault(File.Exists);
                    }
                }
                return csc_asm_file;
            }
        }

        internal static int serverPort = 17001;

        static public string Request(string request, int? port)
        {
            using var clientSocket = new TcpClient();
            clientSocket.Connect(IPAddress.Loopback, port ?? serverPort);
            clientSocket.WriteAllBytes(request.GetBytes());
            return clientSocket.ReadAllBytes().GetString();
        }

        static public (string response, int exitCode) SendBuildRequest(string[] args, int? port)
        {
            int exitCode = 0;

            string get_response()
            {
                try
                {
                    // first arg is the compiler identifier: csc|vbc

                    string request = string.Join('\n', args);
                    string response = BuildServer.Request(request, port);

                    var responseItems = response.Split(new char[] { '|' }, 2);
                    if (responseItems.Count() < 2)
                    {
                        exitCode = 1;
                        return "Build server output is in unexpected format. The compiler exit code is not available.\n" +
                               "Try to restart the build server with 'css -server:stop' followed by 'css -server:start'.";
                    }

                    exitCode = int.Parse(responseItems[0]);
                    return responseItems[1];
                }
                catch (Exception e)
                {
                    return e.ToString();
                }
            }

            var response = get_response();

            var retry = 0;
            while (response.Contains("SocketException") && retry < 5)
            {
                Thread.Sleep(30);
                response = get_response();
            }

            return (response, exitCode);
        }

        static public bool IsServerAlive(int? port)
        {
            try
            {
                // BuildServer.Request("-ping", port);
                return IsRemoteInstanceRunning(port);
            }
            catch
            {
                return false;
            }
        }

        public static void EnsureServerRunning(int? port)
        {
            if (!IsServerAlive(port))
                StartRemoteInstance(port);
        }

        public static void RestartRemoteInstance(int? port)
        {
            StopRemoteInstance(port);
            StartRemoteInstance(port);
        }

        public static void StartRemoteInstance(int? port)
        {
            try
            {
                System.Diagnostics.Process proc = new();

                proc.StartInfo.FileName = "dotnet";
                proc.StartInfo.Arguments = $"{Assembly.GetExecutingAssembly().Location} -listen -port:{port} -csc:\"{BuildServer.csc}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
            }
            catch { }
        }

        public static string StopRemoteInstance(int? port)
        {
            try
            {
                return "-stop".SendTo(IPAddress.Loopback, port ?? serverPort);
            }
            catch { return "<no respone>"; }
        }

        public static bool IsRemoteInstanceRunning(int? port)
            => IPAddress.Loopback.IsOpen(port ?? serverPort);

        public static string PingRemoteInstance(int? port)
        {
            try
            {
                return "-ping".SendTo(IPAddress.Loopback, port ?? serverPort);
            }
            catch { return "<no respone>"; }
        }

        public static string PingRemoteInstances()
        {
            try
            {
                var buf = new StringBuilder();

                if (Directory.Exists(BuildServerActiveInstances))
                    foreach (string activeServer in Directory.GetFiles(BuildServerActiveInstances, "*.pid"))
                    {
                        var proc = GetProcess(int.Parse(Path.GetFileNameWithoutExtension(activeServer)));
                        if (proc != null)
                            buf.AppendLine($"pid:{proc.Id}, port:{File.ReadAllText(activeServer).ParseAsPort()}");
                    }

                return buf.ToString();
                //"-ping".SendTo(IPAddress.Loopback, port ?? serverPort);
            }
            catch { return "<no respone>"; }
        }

#if build_server
#pragma warning disable 414
        static bool closeSocketRequested = false;

        public static void SimulateCloseSocketSignal()
            => closeSocketRequested = true;

#pragma warning restore 414
#endif

        public static void SimulateCloseAppSignal()
        {
            Mutex mutex = new Mutex(true, "cs-script.build_server.shutdown");
        }

        static internal string BuildServerActiveInstances
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "cs-script",
                            "bin",
                            "compiler",
                            "active");

        public static Process GetProcess(int id)
        {
            try
            {
                return Process.GetProcesses().FirstOrDefault(x => x.Id == id);
            }
            catch
            {
            }
            return null;
        }

        public static void ReportRunning(int? port)
        {
            Directory.CreateDirectory(BuildServerActiveInstances);
            var pidFile = Path.Combine(BuildServerActiveInstances, $"{Environment.ProcessId}.pid");
            File.WriteAllText(pidFile, (port ?? serverPort).ToString());
        }

        public static void PurgeRunningHistory()
        {
            try
            {
                if (Directory.Exists(BuildServerActiveInstances))
                    foreach (string activeServer in Directory.GetFiles(BuildServerActiveInstances, "*.pid"))
                    {
                        var proc = GetProcess(int.Parse(Path.GetFileNameWithoutExtension(activeServer)));
                        if (proc == null)
                            File.Delete(activeServer);
                    }
            }
            catch { }
        }

        public static int? ParseAsPort(this string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;
            else
                return int.Parse(data);
        }

        public static void ReportExit()
        {
            var pidFile = Path.Combine(BuildServerActiveInstances, $"{Environment.ProcessId}.pid");

            if (File.Exists(pidFile))
                File.Delete(pidFile);
        }

        // public static void KillAllInstances()
        // {
        //     if (Directory.Exists(build_server_active_instances))
        //         foreach (string activeServer in Directory.GetFiles(build_server_active_instances, "*.pid"))
        //         {
        //             var proc = GetProcess(int.Parse(Path.GetFileNameWithoutExtension(activeServer)));
        //             try
        //             {
        //                 proc?.Kill();
        //                 File.Delete(activeServer);
        //             }
        //             catch { }
        //         }
        // }

        public static void ListenToRequests(int? port)
        {
            var serverSocket = new TcpListener(IPAddress.Loopback, port ?? serverPort);
            try
            {
                serverSocket.Start();

                while (true)
                {
                    using (TcpClient clientSocket = serverSocket.AcceptTcpClient())
                    {
                        try
                        {
                            string request = clientSocket.ReadAllText();

                            if (request == "-stop")
                            {
                                try { clientSocket.WriteAllText($"Terminating pid:{Environment.ProcessId}"); } catch { }
                                break;
                            }
                            else if (request == "-ping")
                            {
                                try
                                {
                                    clientSocket.WriteAllText(
                                        $"pid:{Environment.ProcessId}\n" +
                                        $"file: {Assembly.GetExecutingAssembly().Location}\n" +
                                        $"csc: {csc}");
                                }
                                catch { }
                            }
                            else if (request.StartsWith("-is_writable_dir:"))
                            {
                                var dir = request.Replace("-is_writable_dir:", "");
                                try { clientSocket.WriteAllText($"{dir.IsWritable()}"); } catch { }
                            }
                            else
                            {
                                var args = request.Split('\n');
                                var compiler = args.First();

                                if (compiler == "csc" || compiler == "vb")
                                    args = args.Skip(1).ToArray();
                                else
                                    compiler = "csc"; // the older engine may not send the compiler info at it is only expecting C# support

                                string response = Compile(compiler, args);

                                clientSocket.WriteAllText(response);
                            }
                        }
                        catch (Exception e)
                        {
                            WriteLine(e.Message);
                        }
                    }

                    Task.Run(PurgeRunningHistory);
                }

                serverSocket.Stop();
                WriteLine(" >> exit");
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10048)
                    WriteLine(">" + e.Message);
                else
                    WriteLine(e.Message);
            }
            catch (Exception e)
            {
                WriteLine(e);
            }
        }

        static string Compile(string compiler, string[] args)
        {
            // Debug.Assert(false);

            using (SimpleAsmProbing.For(Path.GetDirectoryName(csc)))
            {
                var oldOut = Console.Out;
                using StringWriter buff = new();

                Console.SetOut(buff);

                int exitCode = 0;
                try
                {
                    if (compiler == "csc")
                    {
                        exitCode = AppDomain.CurrentDomain.ExecuteAssembly(csc, args);
                    }
                    else
                    {
                        var vbc = Path.Join(Path.GetDirectoryName(csc), "vbc.dll");
                        exitCode = AppDomain.CurrentDomain.ExecuteAssembly(vbc, args);
                    }
                }
                catch (Exception e)
                {
                    return $"1|Build server error: {e}";
                }
                finally
                {
                    Console.SetOut(oldOut);
                }
                return $"{exitCode}|{buff.GetStringBuilder()}";
            }
        }

        static bool IsWritable(this string path)
        {
            var testFile = Path.Combine(path, Guid.NewGuid().ToString());
            try
            {
                File.WriteAllText(testFile, "");
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                try { testFile.DeleteIfExists(); } catch { }
            }
        }

        static string DeleteIfExists(this string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path);
            else if (File.Exists(path))
                File.Delete(path);
            return path;
        }
    }
}