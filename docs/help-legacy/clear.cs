using System;
using System.Text;
using System.IO;

class Script
{
	static public void Main(string[] args)
	{
		Console.WriteLine("Cleaning:");
		foreach (string file in Directory.GetFiles(Environment.CurrentDirectory, "*.html"))
		{
			Console.WriteLine("  "+file); 

			string line;
			StringBuilder sb = new StringBuilder();

			using (StreamReader sr = new StreamReader(file))
				while ((line = sr.ReadLine()) != null)
					if (line.Replace(Convert.ToChar(0xd), ' ').Replace(Convert.ToChar(0xa), ' ').Replace(" ", "").Length != 0)
						sb.AppendLine(line);

			using (StreamWriter sw = new StreamWriter(file))
				sw.WriteLine(sb.ToString());
		}
	}
}

