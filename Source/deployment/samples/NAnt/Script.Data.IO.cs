using System;
using System.Windows.Forms;

class Script
{
	[STAThread]
	static public void Main(string[] args)
	{
		Console.WriteLine("strData = " + NAntRuntime.Project.Properties["strData"]);
		NAntRuntime.Project.Properties["data.out.value"] = "test return data";
	}
}

