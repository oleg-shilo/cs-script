using System;

class Script
{
	[STAThread]
	static public void Main(string[] args)
	{
		throw new Exception("The script file has raised an error.");
	}
}

