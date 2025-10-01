//css_args /nl
//css_ng csc
//css_include global-usings
using System.Text;
using System;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using CSScripting;

var thisScript = GetEnvironmentVariable("EntryScript");

var help = $@"Custom command for scrambling files. 
v{thisScript.GetCommandScriptVersion()} ({thisScript})

The scrambling is reversible. Thus repeating the scrambling on already scrambled file restores the original content.

  css -scramble src dest [key]

src  - original file path
dest - transformed (scrambled/unscrambled) file path
key  - any text to be used for XOR scrambling. If not specified then the string `secret` will be used.
";

if (!args.Any() || args.ContainsAny("-?", "?", "-help", "--help"))
{
    print(help);
    return;
}

// -----------------------------------------------
// Command implementation
// -----------------------------------------------

string src_file = args.FirstOrDefault();
string dest_file = args.Skip(1).FirstOrDefault();
string key_text = args.Skip(2).FirstOrDefault() ?? "secret";

TransformFile(src_file, dest_file, Encoding.UTF8.GetBytes(key_text));
Console.WriteLine($"Processed: \n{src_file}\n  └─> {dest_file}");

//--------------

void TransformFile(string inputPath, string outputPath, byte[] key)
{
    if (!File.Exists(inputPath))
        throw new FileNotFoundException("File not found.", inputPath);

    using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
    using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

    int b;
    int i = 0;

    while ((b = input.ReadByte()) != -1)
    {
        byte scrambledByte = (byte)(b ^ key[i % key.Length]);
        output.WriteByte(scrambledByte);
        i++;
    }
}