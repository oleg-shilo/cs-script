//css_ng csc
//css_webapp
//css_include global-usings

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using System.IO;

var derfaultUrl = "http://localhost:5000";

var help = $@"CS-Script custom command for starting a Web server for servicing static files.
  cscs <web content folder> [-url:<url>] 
  Example: css d:\www -url:{derfaultUrl}
           css d:\www";

if (anyArgs("-?", "?", "-help", "--help"))
{
    print(help);
    return;
}

string root = args.FirstOrDefault();
var url = getArg("-url") ?? derfaultUrl;

if (root == null)
{
    print("Error: root folder is not specified. See `css web -?`.");
    return;
}

print("URL:  ", url);
print("Root: ", root);

//----------------

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);

var app = builder.Build();
app.UseFileServer(new FileServerOptions { FileProvider = new PhysicalFileProvider(root) });
app.Run(url);

//----------------

bool anyArgs(params string[] names)
    => names.Any(argName => args.Any(x => x == argName));

string getArg(string name)
    => args.FirstOrDefault(x => x.StartsWith(name + ":"))?.Replace(name + ":", "");