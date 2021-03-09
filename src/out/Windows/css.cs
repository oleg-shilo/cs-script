
using System;
using System.IO;
using System.Reflection;

class Program
{
	[STAThread]
	static void Main(string[] args)
	{
		Environment.SetEnvironmentVariable("Console.WindowWidth", Console.WindowWidth.ToString());
		Environment.SetEnvironmentVariable("ENTRY_ASM", Assembly.GetExecutingAssembly().GetName().Name);

		var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		var engine = Path.Combine(dir, "cscs.dll");
		AppDomain.CurrentDomain.ExecuteAssembly(engine, args);
	}
}
