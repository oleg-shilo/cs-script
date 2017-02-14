using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace Scripting
{
	class Script
	{	
		const string usage = "Usage: cscscript scramble fileToProcess processedFile shiftValue ...\nShifts all bytes in the file. This is a very primitive form of file obfuscation.\n"+
							 "shift - byte increment/decrement (default: 1)\n";
	
		static public void Main(string[] args)
		{
			if (args.Length < 3 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
			{
				Console.WriteLine(usage);
			}
			else
			{
				try
				{
					DateTime strat = DateTime.Now;
					Shift(args[0], args[1], (args[2].StartsWith("-") ? byte.Parse(args[2].Substring(1)) : byte.Parse(args[2])), !args[2].StartsWith("-"));
					Console.WriteLine("Processing time is {0} seconds", (long)((TimeSpan)(DateTime.Now - strat)).TotalSeconds);
				}
				catch(Exception e)
				{
					Console.WriteLine(e);
					return;
				}
			}
		}
	
		public static void Shift(String inName, String outName, byte shift, bool obfuscate)
		{	
			int bufferLen = 4096; 
			byte[] buffer = new byte[bufferLen]; 
			int bytesRead; 

			using (FileStream fin = new FileStream(inName, FileMode.Open, FileAccess.Read))
			{
 				using (FileStream fout = new FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write))
				{
					do 
					{ 
						bytesRead = fin.Read(buffer, 0, bufferLen); 
						for (int i = 0; i < bytesRead; i++)
							if (obfuscate) 
								buffer[i] += shift;
							else
								buffer[i] -= shift;
						fout.Write(buffer, 0, bytesRead); 
					} 
					while(bytesRead != 0); 
				}
			}
		}
	}
}
