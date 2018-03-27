//css_nuget ServiceStack;
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

        public override void Configure(Funq.Container container)
        {
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

        Console.WriteLine("AppHost listening on http://localhost:8080/\n\t sample: http://localhost:8080/hello/John");
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