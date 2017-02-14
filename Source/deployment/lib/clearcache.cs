using System;
using System.IO;
using CSScriptLibrary;
using csscript;

class Script
{
    const string usage = "Usage: cscscript clearCache [script]\n" +
                         "Deletes all compiled script files (.csc) from the ScriptLibrary directory. " +
                         "script - Deletes all .csc files from the cache directory of the specified script file." +
                         "Use this script when you need to force your scripts to be recompiled (e.g. when version of of target CLR is changed).\n";

    static public void Main(string[] args)
    {
        if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
        {
            Console.WriteLine(usage);
        }
        else
        {
            string dir = Path.Combine(Environment.GetEnvironmentVariable("CSSCRIPT_DIR"), "Lib");

            if (args.Length != 0)
                dir = Path.GetFullPath(CSSEnvironment.GetCacheDirectory(Path.GetFullPath(args[0])));

            foreach (string file in Directory.GetFiles(dir, "*.csc"))
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine("Cleared: " + file);
                }
                catch
                {
                }
            }
        }
    }
}