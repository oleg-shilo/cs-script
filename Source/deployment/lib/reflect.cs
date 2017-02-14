using System;
using System.Reflection;

class Script
{
	const string usage = "Usage: csc reflect [assmblyName] ...\nPrints assembly reflection info.\n";

	static public void Main(string[] args)
	{
		if ((args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(usage);
		}
		else
		{
			Assembly assembly = Assembly.LoadFrom(args[0]);
			Print(assembly);
		}
	}

	static void Print(Assembly assembly)
	{
		Console.WriteLine("Assembly: "+assembly.FullName+"/"+assembly.GetName());
		foreach (string s in assembly.GetManifestResourceNames())
		{
			Console.WriteLine("Resource: "+s);
		}
		foreach (AssemblyName a in assembly.GetReferencedAssemblies())
		{
			Console.WriteLine("ReferencedAssembly: "+a.Name);
		}
		foreach (Module m in assembly.GetModules())
		{
			Console.WriteLine("Module: "+m);
			foreach (Type t in m.GetTypes())
			{
				Console.WriteLine("Type: "+t);
				foreach (MemberInfo mi in t.GetMembers())
				{
					Console.WriteLine(String.Format("\t{0}: {1} ", mi.MemberType, mi.Name));
				}
			}
		}
	}
}
