using System.IO;
using System;

class Script
{
	static public void Main()
	{
		string text = File.ReadAllText("ConfigConsole.cs");

        //"{ 25D84CB0" is a formatting CSScript.Npp artefact 
        if(text.Contains("{ 25D84CB0"))
			Console.WriteLine("!!!!!!!!!!!! ConfigConsole.cs -  GUIDS are miss formatted !!!!!!!!!!");
	}
}

