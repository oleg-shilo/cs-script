using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

// starting VS solution is more convenient from console:
// 1. set CSSCRIPT_ROOT=D:\dev\Galos\cs-script\src\out\Windows
// 2. server.sln

// CLI
// --urls
//   webs server urls
// -pre
//   preprocessor script

string arg(string name) => args.SkipWhile(x => x != name).Skip(1).Take(1).FirstOrDefault();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var urls = arg("--urls")?.Split(';');

if (urls == null)
{
    Console.WriteLine("Error: ensure you specify hosting URL (arg '--urls <url>') ");
    return;
}

builder.WebHost.UseUrls(urls);

// let the downstream consumers know the environment context
string preprocessor = arg("-pre");
if (preprocessor != null)
    Environment.SetEnvironmentVariable("CSS_WEB_DEBUGGING_PREROCESSOR", preprocessor); // the envar may be already set so only overwrite it if user asked
Environment.SetEnvironmentVariable("CSS_WEB_DEBUGGING_URL", urls.LastOrDefault());

var app = builder.Build();

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
app.UseStaticFiles();
app.MapControllers();
app.UseRouting();
app.UseHttpsRedirection();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// app.Run();

var process = app.RunAsync();

void print(string message)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("info: ");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(message);
}

// print("Script: " + DbgSession.Current.StackFrameFileName);
print("Pre-processor: " + preprocessor);
app.Urls.ToList().ForEach(x => print($"Now listening on: {x}")); // otherwise enable in appsettings.json
print("Debugger started. Press Ctrl+C to shut down.");

process.Wait();