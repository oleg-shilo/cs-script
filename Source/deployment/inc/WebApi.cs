////css_nuget Microsoft.AspNet.WebApi.Core; //cascading resolve for dependencies //VS2015 already has Microsoft.AspNet.WebApi.Core installed
//css_nuget Newtonsoft.Json; 
using System.Web.Http.Description;
using System.Reflection;
using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.SelfHost;
using System.Collections.Generic;
using WebApi;
using System.Net;

//http://www.asp.net/web-api/overview/older-versions/self-host-a-web-api

//simple SSE: http://forums.asp.net/t/1885055.aspx?ASP+NET+Web+API+and+HTML+5+Server+Sent+Events+aka+EventSource+
//http://aspnetwebstack.codeplex.com/discussions/359056

namespace WebApi
{
    public static class SimpleHost
    {
        static void Main_Sample()
        {
            WebApi.SimpleHost
                  .StartAsConosle("http://localhost:8080",
                                  server =>
                                  {
                                      Console.WriteLine("---------------------");
                                      Console.WriteLine("Press Enter to quit.");
                                      Console.ReadLine();
                                  });
        }

        static string Newtonsoft_Json_dll = typeof(Newtonsoft.Json.Formatting).Assembly.Location;

        public static void Start(Action run)
        {
            //Microsoft.AspNet.WebApi.Core is built against wrong Newtonsoft.Json version so custom probing is needed
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                (e.Name.StartsWith("Newtonsoft.Json,")) ? System.Reflection.Assembly.LoadFrom(Newtonsoft_Json_dll) : null;

            run();
        }

        static public void StartAsConosle(string baseAddress, Action<HttpSelfHostServer> afterOpen)
        {
            SimpleHost.Start(() =>
            {
                var config = new HttpSelfHostConfiguration(baseAddress);
                config.Routes.MapHttpRoute("API Default", "api/{controller}/{id}", new { id = RouteParameter.Optional });

                using (var server = new HttpSelfHostServer(config))
                {
                    server.Open();
                    afterOpen(server);
                }
            });
        }

        public static HttpSelfHostServer Open(this HttpSelfHostServer server)
        {
            server.OpenAsync().Wait();
            return server;
        }

        public static HttpConfiguration OutputRouts(this HttpConfiguration config, Action<string> writeLine)
        {
            IApiExplorer apiExplorer = config.Services.GetApiExplorer();
            var selfConfig = config as HttpSelfHostConfiguration;

            foreach (ApiDescription api in apiExplorer.ApiDescriptions)
            {
                writeLine($"HTTP method: {api.HttpMethod}");
                writeLine($"URI: {selfConfig?.BaseAddress}{api.RelativePath}");
                foreach (ApiParameterDescription parameter in api.ParameterDescriptions)
                    writeLine($"Parameter: {parameter.Name} - {parameter.Source}");
                writeLine("");
            }

            return config;
        }
    }
}