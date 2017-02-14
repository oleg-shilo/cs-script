//css_args /nl
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;

class Script
{
    const string usage = "Usage: fkill [file path #0]...[file path #n] ...\nDeletes (kills) file(s). If file is locked by some process it will wait until file is released and than kill it.\n";

    static public void Main(string[] args)
    {
        if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
        {
            Console.WriteLine(usage);
        }
        else
        {
            List<string> files = new List<string>(args);

            while (files.Count != 0)
            {
                foreach (var file in files)
                {
                    try
                    {
                        if (File.Exists(file))
                            File.Delete(file);
							
                        files.Remove(file);
                    }
                    catch { }
                }

                Thread.Sleep(1000);
            }
        }
    }
}

