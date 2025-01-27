//css_ng csc
//css_webapp
//css_include global-usings
using System.Net.Sockets;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using System.IO;

var derfaultUrl = $"http://localhost:{ThisOrNextAvailable(5050)}";

var help = $@"CS-Script custom command for starting a Web server for servicing static files.
v{GetVersion()} ({Environment.GetEnvironmentVariable("EntryScript")})

  css -web [web content folder] [-url:<url>] 
  Example: css -web d:\www -url:http://localhost:5000
           css -web .\
           css -web";

if (anyArgs("-?", "?", "-help", "--help"))
{
    print(help);
    return;
}

string root = args.FirstOrDefault() ?? Environment.CurrentDirectory;
var url = getArg("-url") ?? derfaultUrl;

if (root == null)
{
    print("Error: root folder is not specified. See `css web -?`.");
    return;
}

root = Path.GetFullPath(root);

//----------------

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);

var app = builder.Build();
app.UseDefaultFiles();
app.UseFileServer(new FileServerOptions { FileProvider = new PhysicalFileProvider(root) });
app.Lifetime.ApplicationStarted.Register(() =>
{
    print("URL:  ", app.Urls.FirstOrDefault());
    print("Root: ", root);
});
app.Run(url);

//----------------

bool anyArgs(params string[] names)
    => names.Any(argName => args.Any(x => x == argName));

string getArg(string name)
    => args.FirstOrDefault(x => x.StartsWith(name + ":"))?.Replace(name + ":", "");

//===============================================================================

string GetVersion()
{
    var verFile = Directory.GetFiles(Path.GetDirectoryName(Environment.GetEnvironmentVariable("EntryScript")), "*.version").FirstOrDefault() ?? "0.0.0.0.version";
    return Path.GetFileNameWithoutExtension(verFile);
}

int ThisOrNextAvailable(int port)
{
    for (int i = 0; i < 20; i++)
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port + i);
            listener.Start();
            listener.Stop();
            return port + i; // Port is available
        }
        // catch (SocketException)
        catch (Exception)
        {
            // Port is in use
        }
    return port;
}