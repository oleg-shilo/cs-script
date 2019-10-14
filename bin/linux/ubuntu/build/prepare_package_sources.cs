using System.Diagnostics;
using System.IO;
using System.Linq;
using System;

class Script
{
    static public void Main()
    {
        var version = "3.28.9.0";

        var src_dir = Path.GetFullPath(@"..\..\..\..\..\..\Releases\v" + version + @"\bin\lib\Bin\Linux");
        var dest_dir = $"cs-script_{version}";

        void copy(string file, string new_file_name = null)
        {
            var s = Path.Combine(src_dir, file);
            var d = Path.Combine(dest_dir, new_file_name ?? file);
            Console.WriteLine("Copying:\n\t" + s + "\n\t" + d);
            File.Copy(s, d, true);
        }

        void generate_from_template(string file, Func<string, string> generator)
        {
            File.WriteAllText(file, generator(File.ReadAllText(file + ".template")));
            Process.Start(editor, file);
        }

        Directory.CreateDirectory(dest_dir);
        copy("CSSRoslynProvider.dll");
        copy("cscs.exe");
        copy("-update.deb.cs", "-update.cs");

        File.WriteAllText(@"..\version.txt", version);

        // Mon, 14 Aug 2017 16:16:27 +1000
        var timestamp = $"{DateTime.Now:ddd, d MMM yyyy hh:mm:ss} " + $"{DateTime.Now:zzz} ".Replace(":", "");

        generate_from_template(@"debian\changelog",
                               text => text.Replace("${version}", version)
                                           .Replace("${date}", timestamp));

        generate_from_template(@"debian\install",
                               text => text.Replace("${version}", version));

        Process.Start(editor, "readme.md");
        Process.Start("7z.exe", @"a -r -tzip .\..\build.zip .\*");

        Console.WriteLine("=====================");
        Console.WriteLine("Prepared for building package cs-script_" + version);
        Console.WriteLine("The build folder '.\\..\\build.zip' is ready.");
    }

    static string editor = @"E:\Program Files\Sublime Text 3\sublime_text.exe";
}