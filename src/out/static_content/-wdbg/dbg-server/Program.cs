//css_include global-usings
//css_nuget Swashbuckle.AspNetCore;
//css_nuget MudBlazor;

using MudBlazor.Services;
using System.Collections.Generic;
using System.Threading;
using wdbg.Controllers;

// starting VS solution is more convenient from console:
// 1. set CSSCRIPT_ROOT=D:\dev\Galos\cs-script\src\out\Windows
// 2. server.sln

string arg(string name) => args.SkipWhile(x => x != name).Skip(1).Take(1).FirstOrDefault();

// ======================================

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddMudServices();
builder.Services.AddSingleton<DbgService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var script = arg("-script");
var urls = arg("--urls")?.Split(';');

if (script == null || urls == null)
{
    Console.WriteLine("Error: ensure you specify hosting URL (arg '--urls <url>') and script to debug (arg '-script <script_file>')");
    return;
}


var app = builder.Build();

Session.Current.StackFrameFileName = script;
builder.WebHost.UseUrls(urls);

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapControllers();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// let consumers down the chain know the environment context
string preprocessor = arg("-pre");
if (preprocessor != null)
    Environment.SetEnvironmentVariable("CSS_WEB_DEBUGGING_PREROCESSOR", preprocessor); // the envar may be already set so only overwrite it if user asked 
Environment.SetEnvironmentVariable("CSS_WEB_DEBUGGING_URL", urls.LastOrDefault());

var process = app.RunAsync();

void print(string message)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("info: ");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(message);
}

print("Script: " + Session.Current.StackFrameFileName);
print("Pre-processor: " + preprocessor);
app.Urls.ToList().ForEach(x => print($"Now listening on: {x}")); // otherwise enable in appsettings.json
print("Application started. Press Ctrl+C to shut down.");

process.Wait();