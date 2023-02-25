using static System.Reflection.Assembly;
using static System.IO.Path;
using static System.IO.File;
using static System.Environment;
using static System.Diagnostics.Process;

using System;
using System.IO;
using System.Diagnostics;

var engine_asm = GetEntryAssembly().Location;

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