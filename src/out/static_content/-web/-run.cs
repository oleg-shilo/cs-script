//css_ng csc
//css_webapp
//css_include global-usings

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using System.IO;

var url = getArg("-url") ?? "http://localhost:5000";
string staticFilesPath = getArg("-root")
              ?? "<error: `root is undefined`; use pass web content folder with the `root:<folder>` argument.>";

print("URL:  ", url);
print("Root: ", staticFilesPath);

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);

var app = builder.Build();

app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(staticFilesPath)
});

app.Run(url);

string getArg(string name)
    => args.FirstOrDefault(x => x.StartsWith(name + ":"))?.Replace(name + ":", "");