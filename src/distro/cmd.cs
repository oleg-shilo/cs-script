using System.Net;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
// using System.Windows.Forms;
using System.Security.Cryptography;
using System.Diagnostics;

public class cmd
{
    static public bool print = true;
    static public Action<string> printRoutine = (x) => Console.WriteLine(x);

    static public void copy(string src, string dest, bool throwOnError = true)
    {
        src = Environment.ExpandEnvironmentVariables(src);
        dest = Environment.ExpandEnvironmentVariables(dest);

        if (!IsMask(src))
        {
            if (!Directory.Exists(Path.GetDirectoryName(dest)))
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
            if (print)
                printRoutine(dest);

            string srcPath = Path.GetFullPath(src);
            string destPath = Path.GetFullPath(dest);

            try
            {
                File.Copy(src, dest, true);
            }
            catch
            {
                if (throwOnError) throw;
            }
        }
        else
        {
            string dir = Path.GetDirectoryName(src);

            if (Directory.Exists(dir))
            {
                foreach (string file in Directory.GetFiles(dir, Path.GetFileName(src)))
                {
                    string destFile = Path.Combine(dest, Path.GetFileName(file));

                    if (!Directory.Exists(dest))
                        Directory.CreateDirectory(dest);
                    if (print)
                        printRoutine(destFile);
                    try
                    {
                        File.Copy(file, destFile, true);
                    }
                    catch
                    {
                        if (throwOnError) throw;
                    }
                }
            }
        }
    }

    static public string sha256(string file)
    {
        byte[] checksum = new SHA256Managed().ComputeHash(File.ReadAllBytes(file));
        var sha256 = BitConverter.ToString(checksum).Replace("-", "");
        return sha256;
    }

    static public void DownloadBinary(string url, string destinationPath, Action<long, long> onProgress = null)
    {
        var sb = new StringBuilder();
        byte[] buf = new byte[1024 * 4];

        var request = WebRequest.Create(url);
        var response = (HttpWebResponse)request.GetResponse();

        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        using (var destStream = new FileStream(destinationPath, FileMode.CreateNew))
        using (var resStream = response.GetResponseStream())
        {
            int totalCount = 0;
            int count = 0;

            while (0 < (count = resStream.Read(buf, 0, buf.Length)))
            {
                destStream.Write(buf, 0, count);

                totalCount += count;
                if (onProgress != null)
                    onProgress(totalCount, response.ContentLength);
            }
        }

        if (File.ReadAllText(destinationPath).Contains("Error 404"))
            throw new Exception($"Resource {url} cannot be downloaded.");
    }

    static public void xcopy(string src, string dest)
    {
        xcopy(src, dest, "");
    }

    static public void xcopy(string src, string dest, string excludeExtensions)
    {
        src = Environment.ExpandEnvironmentVariables(src);
        dest = Environment.ExpandEnvironmentVariables(dest);

        string srcDir = Path.GetFullPath(Path.GetDirectoryName(src));
        xcopyImpl(srcDir, Path.GetFileName(src), srcDir, dest, excludeExtensions.ToLower().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
    }

    static public void rd(string path) //remove directory
    {
        if (Directory.Exists(path))
        {
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                File.Delete(file);

            Directory.Delete(path, true);
        }
    }

    static public void md(string src, string dest) //move directory
    {
        if (Directory.Exists(src))
        {
            if (Directory.Exists(dest))
                Directory.Delete(dest, true);
            cmd.xcopy(Path.Combine(src, @"*.*"), dest + Path.DirectorySeparatorChar);
            System.Threading.Thread.Sleep(1000);
            rd(src);
        }
    }

    static public void cd(string dir) //create directory
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    static public void del(string path) //delete file
    {
        path = Environment.ExpandEnvironmentVariables(path);

        if (IsMask(path))
        {
            foreach (string file in Directory.GetFiles(Path.GetFullPath(Path.GetDirectoryName(path)), Path.GetFileName(path)))
            {
                if (print)
                    printRoutine("Delete: " + file);
                File.Delete(file);
            }
        }
        else if (File.Exists(path))
        {
            File.Delete(path);
            if (print)
                printRoutine("Delete: " + path);
        }
    }

    static public string readFile(string path)
    {
        using (StreamReader sr = new StreamReader(path))
        {
            return sr.ReadToEnd();
        }
    }

    static public void writeFile(string path, string text)
    {
        using (StreamWriter sw = new StreamWriter(path))
        {
            sw.Write(text);
        }
    }

    static public void pause()
    {
        Console.WriteLine("\nPress 'Enter' to continue...");
        Console.ReadLine();
    }

    static public string run(string app, string args, bool echo = true)
    {
        int exitCode = 0;
        return run(app, args, ref exitCode, echo);
    }

    static public string runAndCheck(string app, string args, bool echo = true)
    {
        int exitCode = 0;
        var retval = run(app, args, ref exitCode, echo);

        if (exitCode != 0) throw new Exception(Path.GetFileName(app) + " failure...");

        return retval;
    }

    static public string run(string app, string args, ref int exitCode, bool echo = true)
    {
        app = Environment.ExpandEnvironmentVariables(app);
        return run(app, args, Environment.CurrentDirectory, ref exitCode, echo);
    }

    static public string run(string app, string args, string workingDir, ref int exitCode, bool echo = true)
    {
        StringBuilder sb = new StringBuilder();
        Process myProcess = new Process();
        myProcess.StartInfo.FileName = app;
        myProcess.StartInfo.Arguments = args;
        myProcess.StartInfo.WorkingDirectory = workingDir;
        myProcess.StartInfo.UseShellExecute = false;
        myProcess.StartInfo.RedirectStandardOutput = true;
        myProcess.StartInfo.CreateNoWindow = true;
        myProcess.Start();

        string line = null;

        while (null != (line = myProcess.StandardOutput.ReadLine()))
        {
            Console.WriteLine(line);
            sb.Append(line);
        }
        myProcess.WaitForExit();
        exitCode = myProcess.ExitCode;
        return sb.ToString();
    }

    // static public void runLocal(string app, string[] args)
    // {
    //     AppDomain.CurrentDomain.ExecuteAssembly(Path.GetFullPath(app), null, args, null);
    // }

    static private void xcopyImpl(string src, string mask, string roolSrcDir, string roolDestDir, string[] exclude)
    {
        foreach (string extension in exclude)
        {
            if (extension.StartsWith("dir:"))
            {
                var excludeDirData = extension.Replace("dir:", "").ToLower();
                string[] excludeDirs = excludeDirData.Split(';');

                foreach (string excludeDir in excludeDirs)
                    if (src.ToLower().EndsWith(excludeDir))
                        return;
            }
        }

        foreach (string file in Directory.GetFiles(src, mask))
        {
            string destFile = Path.Combine(roolDestDir + Path.GetDirectoryName(file).Substring(roolSrcDir.Length),
                                            Path.GetFileName(file));

            foreach (string extension in exclude)
            {
                if (Path.GetExtension(destFile).ToLower() == "." + extension)
                {
                    destFile = "";
                    break;
                }
            }
            if (destFile != "")
            {
                if (print)
                {
                    printRoutine(destFile);
                }
                if (!Directory.Exists(Path.GetDirectoryName(destFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));

                File.Copy(file, destFile, true);
            }
        }
        foreach (string dir in Directory.GetDirectories(src))
        {
            string destDir = roolDestDir + dir.Substring(roolSrcDir.Length);

            if (print)
                printRoutine(destDir + Path.DirectorySeparatorChar);
            xcopyImpl(dir, mask, roolSrcDir, roolDestDir, exclude);
        }
    }

    static private bool IsMask(string path)
    {
        return path.IndexOfAny("*?\"<>|".ToCharArray()) != -1;
    }

    public static void ForAllFiles(string src, string filter, Action<string> handler)
    {
        int iterator = 0;
        List<string> dirList = new List<string>();

        dirList.Add(src);

        while (iterator < dirList.Count)
        {
            foreach (string dir in Directory.GetDirectories(dirList[iterator]))
                dirList.Add(dir);

            foreach (string file in Directory.GetFiles(dirList[iterator], filter))
                handler(file);

            iterator++;
        }
    }
}