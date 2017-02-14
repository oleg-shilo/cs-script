using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class EncryptionPrecompiler
{
    static public bool Compile(ref string code, string scriptFile, bool IsPrimaryScript, Hashtable context)
    {
        code = File.ReadAllBytes(scriptFile)
                   .Decrypt()
                   .GetString();

        var newRefs = (List<string>)context["NewReferences"];
        newRefs.Add("System.Windows.Forms.dll");

        return true;
    }
}

static class Extensions
{
    static byte shift = 7;

    public static string GetString(this byte[] data)
    {
        return Encoding.Default.GetString(data);
    }

    public static byte[] Encrypt(this byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)(data[i] + shift);
        return data;
    }

    public static byte[] Decrypt(this byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)(data[i] - shift);
        return data;
    }

    static void Main(string[] args) //use for testing only to encrypt test script
    {
        //  /enc <src> <dest>
        if (args[0] == "/enc")
            File.WriteAllBytes(args[2],
                               File.ReadAllBytes(args[1])
                                   .Encrypt());
    }
}