//css_engine csc
//css_include global-usings
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

var assembly = args.FirstOrDefault();

if (assembly == "/?" || assembly == "-?" || assembly == "-help")
{
    Console.WriteLine($"Sets the cs-script engine target runtime to the currently active .NET configuration.");
    Console.WriteLine($"    css -set-rt-self");
    return;
}

var cscs = Environment.GetEnvironmentVariable("CSScriptRuntimeLocation"); //cscs.dll
var csws = Path.Combine(Path.GetDirectoryName(cscs), "csws.dll");

Process.Start("css", $"-set-rt \"{cscs}\" \"{csws}\"").WaitForExit();