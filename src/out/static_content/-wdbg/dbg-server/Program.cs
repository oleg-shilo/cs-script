using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Web;
using wdbg.cs_script;

// starting VS solution is more convenient from console:
// 1. set CSSCRIPT_ROOT=D:\dev\Galos\cs-script\src\out\Windows
// 2. server.sln

// CLI
// --urls
//   webs server urls
// -pre
//   preprocessor script

string arg(string name) => args.SkipWhile(x => x != name).Skip(1).Take(1).FirstOrDefault();

_ = Tools.Locate(); // ensure we start tools lookup asap so we do not wast our time

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
// builder.Services.AddServerSideBlazor()
//     .AddHubOptions(options => {
//         // Increase timeout values
//         options.ClientTimeoutInterval = TimeSpan.FromMinutes(1);
//         options.HandshakeTimeout = TimeSpan.FromMinutes(1);
//         options.KeepAliveInterval = TimeSpan.FromSeconds(15);

//         // Enable detailed error reporting for debugging
//         options.EnableDetailedErrors = true;
//     })
//     .AddCircuitOptions(options => {
//         // Increase max buffered messages for reconnection
//         options.MaxBufferedUnacknowledgedRenderBatches = 20;
//
//         // Set disconnect timeout (how long server keeps circuit alive after disconnect)
//         options.DisconnectedCircuitMaxRetained = 100;
//         options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
//     });
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    });
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<CircuitHandler, CustomCircuitHandler>();

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

// app.Start();

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

Syntaxer.StartServer(true); // start the syntax server to provide syntax highlighting and code completion in the browser

process.Wait();

public class CustomCircuitHandler : CircuitHandler
{
    private readonly ILogger<CustomCircuitHandler> _logger;

    public CustomCircuitHandler(ILogger<CustomCircuitHandler> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit {CircuitId} connected", circuit.Id);
        return base.OnConnectionUpAsync(circuit, cancellationToken);
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit {CircuitId} disconnected", circuit.Id);
        return base.OnConnectionDownAsync(circuit, cancellationToken);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit {CircuitId} closed", circuit.Id);
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }
}