using static System.Reflection.Assembly; 
using static System.IO.Path; 
using static System.IO.File; 
using static System.Diagnostics.Process; 

using System;
using System.IO;
using System.Diagnostics; 


var engine_asm = GetEntryAssembly().Location;

string cscs; 

// to ensure we are not picking cscs.dll
if (Environment.OSVersion.Platform == PlatformID.Win32NT)
	cscs = ChangeExtension(engine_asm, ".exe"); 
else
    cscs = Combine(GetDirectoryName(engine_asm), GetFileNameWithoutExtension(engine_asm));

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
