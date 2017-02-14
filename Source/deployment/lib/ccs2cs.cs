using System;
using System.IO;

//css_import ccscompiler.cs;

class Script
{
	const string usage = "Usage: cscscript ccs2cs fileIn fileOut ...\nConverts the cassless C# into standard C#.\n" +
								"fileIn - file with classless C# code.\n" +
								"fileOut - file with standard C# code.\n"+
								"Note: the resulting C# code may require refactoring.\n";
	[STAThread]
	static public void Main(string[] args)
	{
		try
		{
			if (args.Length != 2 || args[0].ToLower() == "-?" || args[0].ToLower() == "/?")
				Console.WriteLine(usage);
			else
			{
				CSScript.CCSharpParser ccs = new CSScript.CCSharpParser(args[0]);
				if (ccs.isClassless)
					using (StreamWriter sw = new StreamWriter(args[1]))
						sw.Write(ccs.CSharpScriptCode);
				else
					throw new Exception("Error: The file "+args[0]+" content is not recognised as a classless C# code.");

				Console.WriteLine("Conversion successfully completed");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}
	}
}

