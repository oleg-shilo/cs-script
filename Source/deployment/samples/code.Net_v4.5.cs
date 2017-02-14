using System;
using System.Diagnostics;
using System.IO;

class Program
{
    static void Main()
    {
        Action<string> PrepareDir = dir =>
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
        };

        string baseDir = Path.Combine(Path.GetTempPath(), "SC-Script.Testing");
        string startDir = Path.Combine(baseDir, "start");
        string endDir = Path.Combine(baseDir, "end");

        PrepareDir(startDir);
        PrepareDir(endDir);

        File.WriteAllText(Path.Combine(startDir, "test.txt"), "simple test");
        File.WriteAllText(Path.Combine(startDir, "test.log"), "simple test log");

        Test(startDir, endDir);

        Console.WriteLine("Synch started...");
        
        Console.ReadLine();
    }

    static async void Test(string startDir, string endDir)
    {
        foreach (string fileName in Directory.EnumerateFiles(startDir))
        {
            using (FileStream srcStream = File.Open(fileName, FileMode.Open))
            using (FileStream destStream = File.Create(Path.Combine(endDir, Path.GetFileName(fileName))))
            {
                await srcStream.CopyToAsync(destStream);
            }
        }

        Process.Start("explorer.exe", "\"" + endDir + "\"");
        Console.WriteLine("Synch completed...");
    }
}