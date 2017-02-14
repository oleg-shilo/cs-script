using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace Scripting
{
	class Script
	{	
		const string usage = "Usage: cscscript encrypt [/d] fileToProcess processedFile password ...\nEncrypts/Decrypts('/d') content of file and saves it asencrypt another file.\n";
	
		static private byte[] keySalt = Encoding.Unicode.GetBytes("This is a Cryptography script");//new byte[] {0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76};
	
		static public void Main(string[] args)
		{
			if (args.Length < 3 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")) ||
				(args.Length == 4 && args[0].ToLower() != "/d"))
			{
				Console.WriteLine(usage);
			}
			else
			{
				try
				{
					DateTime strat = DateTime.Now;
					if (args[0].ToLower() != "/d")
					{
						EncryptData(args[0], args[1], args[2]);
					}
					else
					{
						DecryptData(args[1], args[2], args[3]);
					}
					Console.WriteLine("Processing time is {0} seconds", (long)((TimeSpan)(DateTime.Now - strat)).TotalSeconds);
				}
				catch(Exception e)
				{
					Console.WriteLine(e);
					return;
				}
			}
		}
	
		public static void DecryptData(String inName, String outName, String password)
		{	
			FileStream fin = new FileStream(inName, FileMode.Open, FileAccess.Read); 
			FileStream fout = new FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write); 
	  
			PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, keySalt); 
			
			Rijndael alg = Rijndael.Create(); 
			alg.Key = pdb.GetBytes(32); 
			alg.IV = pdb.GetBytes(16); 
	
			CryptoStream cs = new CryptoStream(fout, alg.CreateDecryptor(), CryptoStreamMode.Write); 
	  
			int bufferLen = 4096; 
			byte[] buffer = new byte[bufferLen]; 
			int bytesRead; 
	
			do 
			{ 
				bytesRead = fin.Read(buffer, 0, bufferLen); 
				cs.Write(buffer, 0, bytesRead); 
	
			} 
			while(bytesRead != 0); 
	
			cs.Close(); 
			fin.Close();				   
		}
		
		public static void EncryptData(String inName, String outName, String password)
		{  
			FileStream fin = new FileStream(inName, FileMode.Open, FileAccess.Read);
			FileStream fout = new FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write);
		   
			PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, keySalt); 
	
			Rijndael alg = Rijndael.Create(); 
			alg.Key = pdb.GetBytes(32); 
			alg.IV = pdb.GetBytes(16); 
	
			CryptoStream cs = new CryptoStream(fout, alg.CreateEncryptor(), CryptoStreamMode.Write); 
	
			int bufferLen = 4096; 
			byte[] buffer = new byte[bufferLen]; 
			int bytesRead; 
	
			do 
			{ 
				bytesRead = fin.Read(buffer, 0, bufferLen); 
				cs.Write(buffer, 0, bytesRead); 
			} 
			while(bytesRead != 0); 
	
			cs.Close(); 
			fin.Close();	 
		}
	}
}
