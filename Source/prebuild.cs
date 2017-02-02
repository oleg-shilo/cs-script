using System.IO;
using System.Reflection;
using System.Linq;
using System;
using System.Windows.Forms;

void main(string[] args)
{
    var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    // var dir = Environment.CurrentDirectory;

    var source = File.ReadAllText(Path.Combine(dir, "dbg.cs"));

    source = escape(source);

    var res = Path.Combine(dir, "dbg.res.cs");

    var lines = File.ReadAllLines(res).ToArray();

    for (int i = 0; i < lines.Length; i++)
        if (lines[i].StartsWith("    public static string dbg_source"))
        {
            lines[i] = "    public static string dbg_source = \"" + source + "\";";
            break;
        }

    File.WriteAllLines(res, lines);
}

string escape(string text)
{
    return text
               .Replace("\r", "{$R}")
               .Replace("\n", "{$N}")
               .Replace("\t", "{$T}")
               .Replace("\\", "{$S}")
               .Replace("\"", "{$Q}")
               .Replace("{$R}", @"\r")
               .Replace("{$N}", @"\n")
               .Replace("{$T}", @"\t")
               .Replace("{$S}", @"\\")
               .Replace("{$Q}", @"\""");
}