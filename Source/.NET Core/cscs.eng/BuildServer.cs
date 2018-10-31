using System;
using static System.Console;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using csscript;

namespace CSScripting.CodeDom
{
    public static class BuildServer
    {
        public static int serverPort = 17001;

        static public string SentRequest(string request)
        {
            using (var clientSocket = new TcpClient())
            {
                clientSocket.Connect(IPAddress.Loopback, serverPort);
                clientSocket.WriteAllBytes(request.GetBytes());
                return clientSocket.ReadAllBytes().GetString();
            }
        }

        public static void Stop()
        {
            try
            {
                using (var clientSocket = new TcpClient())
                {
                    clientSocket.Connect(IPAddress.Loopback, serverPort);
                    clientSocket.WriteAllText("-exit");
                }
            }
            catch { }
        }

        public static void Start()
        {
            // Task.Run(() => 
                Profiler.measure(">> Initialized: ", ()=> RoslynService.Init());

            try
            {
                var serverSocket = new TcpListener(IPAddress.Loopback, serverPort);
                serverSocket.Start();

                while (true)
                {
                    using (TcpClient clientSocket = serverSocket.AcceptTcpClient())
                    {
                        try
                        {
                            string request = clientSocket.ReadAllText();

                            if (request == "-exit")
                            {
                                try { clientSocket.WriteAllText("Bye"); } catch { }
                                break;
                            }

                            Profiler.measure(">> Processing client request: ", () =>
                            {
                                string response = RoslynService.process_build_remotelly_request(request);
                                clientSocket.WriteAllText(response);
                            });
                        }
                        catch (Exception e)
                        {
                            WriteLine(e.Message);
                        }
                    }
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
    }

}