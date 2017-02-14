//css_imp crc32.cs;
using System;
using System.Reflection;
using CSScriptLibrary;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class Host
{
    static string encryptionKey = "test password";
    static byte[] keySalt = Encoding.Unicode.GetBytes(encryptionKey);

    static void Main()
    {
        EncryptFile(Path.GetFullPath("Script.cs"), Path.GetFullPath("Script.cs.encrypted"), encryptionKey);
        //EncryptFile(Path.GetFullPath("Script.cs"), Path.GetFullPath("Script.cs.encrypted1"), encryptionKey);
        //DecryptData(Path.GetFullPath("Script.cs.encrypted1"), Path.GetFullPath("Script.cs.dencrypted"), encryptionKey);

        ExecutePlainScript();
        ExecuteEncryptedScript();
        ExecuteCRCScript(938774409);
    }

    static void ExecutePlainScript()
    {
        var Print = new AsmHelper(CSScript.Load("Script.cs"))
                                          .GetStaticMethod("*.Print", "");

        Print("ExecutePlainScript: Hello World!");
    }

    static void ExecuteEncryptedScript()
    {
        var code = Decrypt(Path.GetFullPath("Script.cs.encrypted"), encryptionKey);

        var Print = new AsmHelper(CSScript.LoadCode(code))
                                          .GetStaticMethod("*.Print", "");

        Print("ExecuteEncryptedScript: Hello World!");
    }

    static void ExecuteCRCScript(int expectedCRC)
    {
        using (var sr = new StreamReader(Path.GetFullPath("Script.cs")))
        {
            string code = sr.ReadToEnd();
            var crc = CRC32.Compute(code);
            if (crc != expectedCRC)
            {
                Console.WriteLine("ExecuteCRCScript: Cannot execute Script.cs as it has been altered.");
            }
            else
            {
                var Print = new AsmHelper(CSScript.LoadCode(code))
                                                  .GetStaticMethod("*.Print", "");

                Print("ExecuteCRCScript: Hello World!");
            }
        }
    }

    public static string Decrypt(string inName, string password)
    {
        PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, keySalt);

        Rijndael alg = Rijndael.Create();
        alg.Key = pdb.GetBytes(32);
        alg.IV = pdb.GetBytes(16);

        using (var fin = new FileStream(inName, FileMode.Open, FileAccess.Read))
        using (var fout = new MemoryStream())
        using (var cs = new CryptoStream(fout, alg.CreateDecryptor(), CryptoStreamMode.Write))
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            do
            {
                bytesRead = fin.Read(buffer, 0, buffer.Length);
                cs.Write(buffer, 0, bytesRead);
            }
            while (bytesRead != 0);

            cs.FlushFinalBlock();
            return Encoding.ASCII.GetString(fout.ToArray());
        }
    }

    public static void EncryptFile(string inName, string outName, string password)
    {
        var pdb = new PasswordDeriveBytes(password, keySalt);

        var alg = Rijndael.Create();
        alg.Key = pdb.GetBytes(32);
        alg.IV = pdb.GetBytes(16);

        using (var fin = new FileStream(inName, FileMode.Open, FileAccess.Read))
        using (var fout = new FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write))
        using (var cs = new CryptoStream(fout, alg.CreateEncryptor(), CryptoStreamMode.Write))
        {
            var buffer = new byte[4096];
            int bytesRead;

            do
            {
                bytesRead = fin.Read(buffer, 0, buffer.Length);
                cs.Write(buffer, 0, bytesRead);
            }
            while (bytesRead != 0);
        }
    }
}

