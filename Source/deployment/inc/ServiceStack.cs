using ServiceStack.Configuration;
using System.Net.Sockets;
using System.Net;
using System;
using System.Linq;
//css_nuget ServiceStack;
//css_nuget ServiceStack.Api.Swagger;
//css_nuget ServiceStack.Client;
//css_nuget ServiceStack.Common;
//css_nuget ServiceStack.Interfaces;
//css_nuget ServiceStack.Text
//css_dir %css_nuget%\ServiceStack\ServiceStack.Interfaces.*\lib\**;
//css_ref ServiceStack.Interfaces.dll;

namespace ServiceStack
{
    public class ConsoleHost : AppSelfHostBase
    {
        public ConsoleHost()
        : base("HttpListener Self-Host", System.Reflection.Assembly.GetExecutingAssembly())
        { }

        public ConsoleHost(string serviceName)
        : base(serviceName, System.Reflection.Assembly.GetExecutingAssembly())
        { }

        public ConsoleHost(string serviceName, params System.Reflection.Assembly[] assembliesWithServices)
        : base(serviceName, assembliesWithServices)
        { }

        public string RootPath = Environment.CurrentDirectory;

        public override void Configure(Funq.Container container)
        {
            // var new_root = "~/".MapProjectPath();

            // Plugins.Add(new SwaggerFeature());

            if (!RootPath.IsNullOrEmpty())
            {
                Console.WriteLine();
                Console.WriteLine("Static content root: " + RootPath);
                Console.WriteLine();
                SetConfig(new HostConfig
                {
                    WebHostPhysicalPath = RootPath,
                    DebugMode = true,
                    AddRedirectParamsToQueryString = true,
                    UseCamelCase = true,

                });
            }
        }

        public static string GetLocalIP()
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                      .AddressList
                      .First(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                      .ToString();
        }
    }
}

/*
Sample:
public class Host : ConsoleHost
{
    static void Main()
    {
        new Host().Init()
                  .Start("http://*:8080/");

        Console.WriteLine("Root: " + Environment.CurrentDirectory + "\nAppHost listening on http://localhost:8080/\n\t sample: http://localhost:8080/hello/John");
        Console.ReadKey();
    }
}

[Route("/hello/{Name}")]
public class Hello
{
    public string Name { get; set; }
}

public class StatusService : Service
{
    public object Get(Hello request)
    {
        return new { Message = $"Hello {request.Name}!", Timestamp = DateTime.Now.ToString() };
    }
}
*/
