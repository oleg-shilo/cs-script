using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CSScriptLib
{
    class BuildServer
    {
        public static int serverPort = "CSS_BUILDSERVER_CSC_PORT".GetEnvar().ToInt(defaultValue: 17001);

        static public string Request(string request, int? port)
        {
            using (var clientSocket = new TcpClient())
            {
                clientSocket.Connect(IPAddress.Loopback, port ?? serverPort);
                clientSocket.WriteAllBytes(request.GetBytes());
                return clientSocket.ReadAllBytes().GetString();
            }
        }

        static public string SendBuildRequest(string[] args, int? port)
        {
            try
            {
                // first arg is the compiler identifier: csc|vbc

                string request = string.Join("\n", args);
                string response = BuildServer.Request(request, port);

                return response;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        static public bool IsServerAlive(int? port)
        {
            try
            {
                return IPAddress.Loopback.IsOpen(port ?? serverPort);
            }
            catch
            {
                return false;
            }
        }

        public static string PingRemoteInstance(int? port)
        {
            try
            {
                return "-ping".SendTo(IPAddress.Loopback, port ?? serverPort);
            }
            catch { return "<no respone>"; }
        }
    }
}