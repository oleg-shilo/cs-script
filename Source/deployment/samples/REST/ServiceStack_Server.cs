//css_npp asadmin;
//css_inc ServiceStack.cs;
using System;
using ServiceStack;

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
        return new
        {
            Message = $"Hello {request.Name}!",
            Timestamp = DateTime.Now.ToString()
        };
    }
}