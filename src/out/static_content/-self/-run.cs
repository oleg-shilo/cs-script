//css_include global-usings
using csscript;
using CSScripting;
using static dbg;
using static System.Reflection.Assembly;
using static System.Environment;

if (args.ContainsAny("?", "-?", "-help", "--help"))
{
    var thisScript = GetEnvironmentVariable("EntryScript");

    WriteLine($"""
        Prints path to the script engine CLI executable.
        v{thisScript.GetCommandScriptVersion()} ({GetEnvironmentVariable("EntryScript")})
          css -self
        """);
    return;
}

// -----------------------------------------------
// Command implementation
// -----------------------------------------------
GetEntryAssembly().Location.print();