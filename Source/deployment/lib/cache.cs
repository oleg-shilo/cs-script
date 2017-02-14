using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using CSScriptLibrary;

class Script
{
    static string usage = "Usage: cscs cache <ls|trim|[scriptFile]> ...\nPrints, purges, clears cache or opens the cache directory for a given C# script file.\n";

    static public void Main(string[] args)
    {
        if (args.Length == 0 || (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
            Console.WriteLine(usage);
        else if (args[0] == "ls")
            List();
        else if (args[0] == "trim")
            Trim();
        else
        {
            string path = Path.GetDirectoryName(CSScript.GetCachedScriptPath(Path.GetFullPath(args[0])));

            if (Directory.Exists(path))
                Process.Start(path);
            else
                MessageBox.Show("The cache directory " + path + " does not exist.");
        }
    }

    static void List()
    {
        string cacheRootDir = Path.Combine(CSScript.GetScriptTempDir(), "cache");

        foreach (var cacheDir in Directory.GetDirectories(cacheRootDir))
        {
            string cachName = Path.GetFileName(cacheDir);

            string infoFile = Path.Combine(cacheDir, "css_info.txt");

            if (!File.Exists(infoFile))
            {
                Console.WriteLine(cachName + ":\tunknown");
                continue;
            }

            string sourceDir = File.ReadAllLines(infoFile).Last();
            Console.WriteLine(cachName + ":\t" + sourceDir);
        }
    }

    static void Trim()
    {
        string cacheRootDir = Path.Combine(CSScript.GetScriptTempDir(), "cache");

        bool printed = false;
        //--------------------------------
        Action<string> print = (message) =>
        {
            if (!printed)
            {
                Console.WriteLine("Removing:");
                printed = true;
            }
            Console.WriteLine(message);
        };

        Action<string> deleteDir = (path) =>
        {
            try { Directory.Delete(path, true); }
            catch { }
        };

        Action<string> deleteFile = (path) =>
        {
            try { File.Delete(path); } catch { }
        };
        //--------------------------------

        foreach (var cacheDir in Directory.GetDirectories(cacheRootDir))
        {
            string infoFile = Path.Combine(cacheDir, "css_info.txt");

            string cachName = Path.GetFileName(cacheDir);

            if (!File.Exists(infoFile))
            {
                print(cachName + ":\tUNKNOWN");
                deleteDir(cacheDir);
            }
            else
            {
                string sourceDir = File.ReadAllLines(infoFile).Last();

                if (!Directory.Exists(sourceDir))
                {
                    print(cachName + ":\t" + sourceDir);
                    deleteDir(cacheDir);
                }
                else
                    //path\script.cs.compiled
                    foreach (string file in Directory.GetFiles(cacheDir, "*.compiled"))
                    {
                        var name = Path.GetFileNameWithoutExtension(file);//script.cs

                        var baseName = Path.GetFileNameWithoutExtension(name);//script

                        var scriptFile = Path.Combine(sourceDir, name);

                        if (!File.Exists(scriptFile))
                        {
                            print(cachName + ":\t" + scriptFile);
                            foreach (string cacheFile in Directory.GetFiles(cacheDir, baseName + ".*"))
                                deleteFile(cacheFile);

                            string[] leftOvers = Directory.GetFiles(cacheDir);

                            if (leftOvers.Length == 0 || (leftOvers.Length == 1 && leftOvers[0].EndsWith("css_info.txt")))
                                deleteDir(cacheDir);
                        }
                    }
            }
        }

        Console.WriteLine("Done");
    }
}