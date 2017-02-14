using System;
using System.IO;
using System.Windows.Forms;
using System.Text;

class Script 
{
	const string usage = "Usage: cscscript cutFile filename [size]\n"+
						 "Cuts file in pieces and prepares batch file to reassemble the original file.\n"+
						 "\n"+
						 "Size can be specified in units (etc. 1024K, 500, 2M):\n"+
						 "  <no unit> - bytes\n"+						 
						 "  K		 - kilobytes\n"+
						 "  M		 - megabytes\n"+
						 "Default single piece size is 1024K (1M)\n"+
						 "\n"+
						 "Note:\n"+
						 "No script is required to reassemble the original file. Just run <filename>.bat.\n";

	static public void Main(string[] args)
	{
		if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(usage);
		}
		else
		{
			try
			{
				string file = args[0];
				int size = 1024*1024; //1M
				if (args.Length > 1)
				{
					string sizeStr = args[1].Trim();
					int factor = 1;
					if (sizeStr.EndsWith("M") || sizeStr.EndsWith("m"))
					{	
						sizeStr = sizeStr.Substring(0, sizeStr.Length-1);
						factor = 1024*1024;
					}
					else if (sizeStr.EndsWith("K") || sizeStr.EndsWith("k"))
					{
						sizeStr = sizeStr.Substring(0, sizeStr.Length-1);
						factor = 1024;
					}
					size = Convert.ToInt32(sizeStr) * factor;
				}

				string ressembleCmd = "copy /b ";
				using (FileStream fin = new FileStream(file, FileMode.Open, FileAccess.Read))
				{				
					byte[] buffer = new byte[1024*100]; //100K
					int bytesRead = 0, bytesReadTotal = 0, bytesWritten = 0, fileCount = 0;
					
					do
					{
						string currFileName = file+"."+fileCount.ToString("####0000");
						using (FileStream fout = new FileStream(currFileName, FileMode.OpenOrCreate, FileAccess.Write))
						{
							do 
							{ 
								int bytesToRead = (bytesWritten + buffer.Length) <= size ?
									buffer.Length :
									size - bytesWritten;

								bytesRead = fin.Read(buffer, 0, bytesToRead); 
								if (bytesRead != 0)
								{		
									fout.Write(buffer, 0, bytesRead);
									bytesWritten += bytesRead;
									bytesReadTotal += bytesRead;
								}
								if (bytesWritten >= size)
								{
									bytesWritten = 0;
									break;
								}
							} 
							while(bytesRead != 0); 
						}
						Console.WriteLine(Path.GetFileName(currFileName));
						
						if (fileCount > 0)
							ressembleCmd += "+\""+Path.GetFileName(currFileName)+"\"";
						else
							ressembleCmd += "\""+Path.GetFileName(currFileName)+"\"";
						
						fileCount++;
					}
					while (bytesRead != 0); 
				}

				ressembleCmd += " "+Path.GetFileName(file);

				using (StreamWriter sw = new StreamWriter(file+".bat")) 
				{
					sw.WriteLine(ressembleCmd); 
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}