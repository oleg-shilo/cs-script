using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Script
{
    const string usage = "Usage: cscscript clearTemp\n" +
                         "Deletes all temporary files created by the script engine.\n";

    //Note because of the strong backwards compatibility requirement this script
    //doesn't use neither new (more convenient) IO API nor LONQ.
    //The nature of the script activity requires it to be very resilient and conservative.
    //Thus "swallow all errors and continue..." 
    static public void Main(string[] args)
    {
        if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
        {
            Console.WriteLine(usage);
        }
        else
        {
            string baseDir = Path.Combine(Path.GetTempPath(), "CSSCRIPT");

            if (Directory.Exists(baseDir))
            {
                foreach (string dir in Directory.GetDirectories(baseDir))
                {
                    if (IsInUseByExternalProcess(dir))
                        continue;

                    string name = Path.GetFileName(dir).ToLower();

                    //if (name == "csscriptnpp") ProcessCSScriptNppDir(dir); //should be moved into CSScript.Npp
                    if (name == "dynamic") ProcessDynamicScriptsDir(dir);
                    if (name.EndsWith(".shell")) ProcessVisualStudioProjDir(dir);
                    if (name == "cache") ProcessCacheDir(dir);
                }

                foreach (string file in Directory.GetFiles(baseDir))
                {
                    string name = Path.GetFileName(file).ToLower();

                    if (!name.EndsWith("_recent.txt") && name != "counter.txt")
                        DeleteFile(file);
                }
            }
        }
    }

    static void ProcessVisualStudioProjDir(string baseDir)
    {
        try
        {
            DeleteDir(baseDir);
        }
        catch { }
    }

    static void ProcessCacheDir(string baseDir)
    {
        try
        {
            foreach (string dir in Directory.GetDirectories(baseDir))
                DeleteCacheDir(dir);
        }
        catch { }
    }

    static void ProcessCSScriptNppDir(string baseDir)
    {
        try
        {
            foreach (string dir in Directory.GetDirectories(baseDir))
            {
                foreach (string file in Directory.GetFiles(dir, "*.cs.dbg"))
                {
                    string sourceFile = File.ReadAllLines(file)[0].Split(new[] { ':' }, 2)[1];
                    if (!File.Exists(sourceFile))
                        DeleteFile(file);
                }

                if (Directory.GetFiles(dir).Length == 0)
                    Directory.Delete(dir);
            }
        }
        catch { }
    }

    static void ProcessDynamicScriptsDir(string dir)
    {
        try
        {
            foreach (string file in Directory.GetFiles(dir))
                DeleteFile(file);
        }
        catch { }
    }

    static bool IsInUseByExternalProcess(string dir)
    {
        string pidFile = Path.Combine(dir, "host.pid");

        try
        {
            if (File.Exists(pidFile))
            {
                int id = int.Parse(File.ReadAllText(pidFile));
                if (Process.GetProcessById(id) != null)
                    return true; //the process using this project is still active
            }
        }
        catch { }
        return false;
    }

    static private void DeleteCacheDir(string dir)
    {
        //deletes folder recursively
        try
        {
            string infoFile = Path.Combine(dir, "css_info.txt");
            string sourceDirectory = "";

            try
            {
                sourceDirectory = File.ReadAllLines(infoFile)[1]; //second line
            }
            catch { }

            foreach (string file in Directory.GetFiles(dir))
            {
                if (file == infoFile) continue;
                try
                {
                    if (sourceDirectory != "")
                    {
                        string scriptName = Path.GetFileNameWithoutExtension(file);
                        if (scriptName.EndsWith(".attr.g"))
                            scriptName = scriptName.Replace(".attr.g", "");

                        //name of the potential script file that was used to produce the compiled 
                        //assembly or the injected attr file

                        if (Directory.GetFiles(sourceDirectory, scriptName + ".*").Length > 0)
                            continue; //script still exists

                        DeleteFile(file);
                    }
                }
                catch
                {
                }
            }

            foreach (string subDir in Directory.GetDirectories(dir))
                DeleteDir(subDir);

            if (File.Exists(infoFile) && Directory.GetFiles(dir).Length == 1 && Directory.GetDirectories(dir).Length == 0)
                File.Delete(infoFile);

            if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                Directory.Delete(dir);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static void DeleteFile(string file)
    {
        try
        {
            using (Mutex fileLock = new Mutex(false, file.Replace(Path.DirectorySeparatorChar, '|').ToLower()))
            {
                bool alreadtLocked = !fileLock.WaitOne(1);
                if (alreadtLocked)
                    return;

                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
        }
        catch
        {
        }
    }

    static void DeleteDir(string dir)
    {
        //deletes directory recursively
        try
        {
            foreach (string file in Directory.GetFiles(dir))
                DeleteFile(file);

            foreach (string subDir in Directory.GetDirectories(dir))
                DeleteDir(subDir);

            if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                Directory.Delete(dir);
        }
        catch
        {
        }
    }
}
