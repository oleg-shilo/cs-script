//css_include global-usings
using static System.Reflection.Assembly;
using static System.IO.Path;
using static System.IO.File;
using static System.Environment;
using static System.Diagnostics.Process;

using System;
using System.IO;
using System.Diagnostics;

var engine_asm = GetEntryAssembly().Location;

if (args.Contains("?") || args.Contains("-?") || args.Contains("-help"))
{
    string version = Path.GetFileNameWithoutExtension(
                         Directory.GetFiles(Path.GetDirectoryName(Environment.GetEnvironmentVariable("EntryScript")), "*.version")
                                  .FirstOrDefault() ?? "0.0.0.0.version");

    Console.WriteLine($@"v{version} ({Environment.GetEnvironmentVariable("EntryScript")})");
    Console.WriteLine("Builds `css.exe` that executes the script engine CLI executable (an alternative to the symbolic links or shims).");
    return;
}

// to ensure we are not picking cscs.dll
if (Environment.OSVersion.Platform != PlatformID.Win32NT)
{
    Console.WriteLine("Building `css.exe` executable is only useful on windows. On Linux you " +
                      "have a much better option `alias`. You can enable it as below: " + NewLine +
                      "alias css='dotnet /usr/local/bin/cs-script/cscs.dll'" + NewLine +
                      "After that you can invoke CS-Script engine from anywhere by just typing 'css'.");

    return;
}

var cscs = ChangeExtension(engine_asm, ".exe");
var css_cs = Combine(GetDirectoryName(cscs), "css.cs");

WriteAllText(css_cs, @"
using System;
using System.IO;
using System.Reflection;

class Program
{
	[STAThread]
	static void Main(string[] args)
	{
		Environment.SetEnvironmentVariable(""Console.WindowWidth"", Console.WindowWidth.ToString());
		Environment.SetEnvironmentVariable(""ENTRY_ASM"", Assembly.GetExecutingAssembly().GetName().Name);

		var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		var engine = Path.Combine(dir, ""cscs.dll"");
		AppDomain.CurrentDomain.ExecuteAssembly(engine, args);
	}
}
");

Start(cscs, $"-e \"{css_cs}\"").WaitForExit();