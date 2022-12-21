//css_include global-usings
//css_nuget Swashbuckle.AspNetCore;
//css_nuget MudBlazor;

using MudBlazor.Services;
using System.Collections.Generic;
using System.Threading;
using wdbg.Controllers;

#if DEBUG
Environment.SetEnvironmentVariable("CSSCRIPT_ROOT", @"D:\dev\Galos\cs-script\src\out\Windows");
#else

don 't forget to reste cscs path
#endif


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddMudServices();
builder.Services.AddSingleton<DbgService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// builder.WebHost.UseUrls("http://localhost:5003", "https://localhost:5004");

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
app.MapControllers();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Session.Current.StackFrameFileName = args.FirstOrDefault() ?? @"D:\dev\Galos\cs-script\src\out\static_content\-wdbg\test2.cs";
var preprocessor = args.FirstOrDefault(x => x.StartsWith("-pre:"))?.Replace("-pre:", "")?.Trim('"') ?? @"D:\dev\Galos\cs-script\src\out\static_content\-wdbg\dbg-inject.cs";

var process = app.RunAsync();

Environment.SetEnvironmentVariable("CSS_WEB_DEBUGGING_PREROCESSOR", preprocessor);
Environment.SetEnvironmentVariable("CSS_WEB_DEBUGGING_URL", app.Urls.LastOrDefault());

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