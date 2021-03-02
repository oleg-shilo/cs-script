using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

static class Tcp
{
    public static string SendTo(this string data, IPAddress address, int port)
    {
        using (var clientSocket = new TcpClient())
        {
            clientSocket.Connect(address, port);
            clientSocket.WriteAllBytes(data.GetBytes());
            return clientSocket.ReadAllBytes().GetString();
        }
    }
}

static class SocketExtensions
{
    public static byte[] GetBytes(this string data) => Encoding.UTF8.GetBytes(data);

    public static string GetString(this byte[] data) => Encoding.UTF8.GetString(data);

    public static byte[] ReadAllBytes(this TcpClient client)
    {
        var bytes = new byte[client.ReceiveBufferSize];
        var len = client.GetStream()
                        .Read(bytes, 0, bytes.Length);
        var result = new byte[len];
        Array.Copy(bytes, result, len);
        return result;
    }

    public static string ReadAllText(this TcpClient client) => client.ReadAllBytes().GetString();

    public static void WriteAllBytes(this TcpClient client, byte[] data)
    {
        var stream = client.GetStream();
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }

    public static void WriteAllText(this TcpClient client, string data) => client.WriteAllBytes(data.GetBytes());
}