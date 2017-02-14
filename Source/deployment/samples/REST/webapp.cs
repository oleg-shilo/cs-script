//css_nuget Microsoft.Owin.Hosting, Microsoft.Owin.Host.HttpListener, Microsoft.Owin.Diagnostics, Owin.Extensions

using System.Threading;
using System;
using Microsoft.Owin.Hosting;
using Owin;

public class HelloApp
{
    static void Main()
    {
        using (WebApp.Start<HelloApp>("http://localhost:8000"))
        {
            Console.ReadKey();
        }
    }

    public void Configuration(IAppBuilder app)
    {
        app.Run(context =>
           {
               context.Response.ContentType = "text/plain";
               return context.Response.WriteAsync("Hello World!\n");
           });
    }
}