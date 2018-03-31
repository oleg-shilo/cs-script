//css_npp asadmin;
//css_inc ServiceStack.cs;
using System;
using ServiceStack;

public class Host : ConsoleHost
{
    static void Main()
    {
        int port = 5000;
        new Host()
                .Init()
                .Start($"http://*:{port}/");

        Console.WriteLine($"AppHost listening on http://localhost:{port}/\n\t sample: http://localhost:{port}/hello/John");
        Console.ReadKey();
    }
}

[Route("/hello")]
[Route("/hello/{Name}")]
public class Hello
{
    public string Name { get; set; }
}

public class StatusService : Service
{
    public object Get(Hello request)
    {
        return new
        {
            Message = $"Hello {request.Name}!",
            Timestamp = DateTime.Now.ToString()
        };
    }
}