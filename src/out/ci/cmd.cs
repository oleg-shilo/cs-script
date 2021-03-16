using System.Net;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using static System.Environment;
using System.Security.Cryptography;
using System.Diagnostics;

public static class cmd
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

    static public void download(string url, string destinationPath, Action<long, long> onProgress = null)
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

    static public void xcopy(string src, string dest, string excludeExtensions = "")
    {
        src = Environment.ExpandEnvironmentVariables(src);
        dest = Environment.ExpandEnvironmentVariables(dest);

        string srcDir = Path.GetFullPath(Path.GetDirectoryName(src));
        xcopyImpl(srcDir, Path.GetFileName(src), srcDir, dest, excludeExtensions.ToLower().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
    }

    static public void rm(string path)
    {
        if (Directory.Exists(path))
        {
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                File.Delete(file);

            Directory.Delete(path, true);
        }
        else if (File.Exists(path))
            File.Delete(path);
        else
            throw new Exception($"Path '{path}' does not exist");
    }

    static public void move(string src, string dest) //move directory
    {
        if (Directory.Exists(src))
        {
            if (Directory.Exists(dest))
                Directory.Delete(dest, true);
            cmd.xcopy(Path.Combine(src, @"*.*"), dest + Path.DirectorySeparatorChar);
            System.Threading.Thread.Sleep(1000);
            rm(src);
        }
        else if (File.Exists(src))
            copy(src, dest);
        else
            throw new Exception($"Path '{src}' does not exist");
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

    static public string mkdir(string path)
    {
        Directory.CreateDirectory(path); return path;
    }

    static public string join(this string path1, string path2) => Path.Combine(path1, path2);

    static public string getFullPath(this string path) => Path.GetFullPath(path);

    static public void pause()
    {
        Console.WriteLine("\nPress 'Enter' to continue...");
        Console.ReadLine();
    }

    static public string replaceInFile(string file, string pattern, string replacement)
    {
        var content = File.ReadAllText(file);
        content = content.Replace(pattern, replacement);
        File.WriteAllText(file, content);
        return file;
    }

    static public string modiyFile(string file, Func<string, string> replace)
    {
        var content = File.ReadAllText(file);
        File.WriteAllText(file, replace(content));
        return file;
    }

    static public string run(string app, string args, bool echo = true)
    {
        int exitCode = 0;
        return run(app, args, ref exitCode, echo);
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
}

static class vs
{
    public static (string version, string lnx_version, string changes) parse_release_notes(string file)
    {
        var release_notes = File.ReadAllLines(file);

        var changes = release_notes.Skip(1).join_by(NewLine).Trim();

        // "# Release v1.4.5.0-NET5-RC5" -> "1.4.5.0-NET5-RC5"
        string version = release_notes.First()
                                      .SkipWhile(x => !char.IsDigit(x))
                                      .join_by("");

        // "1.4.5.0-NET5-RC5" -> "1.4-5"
        var lnx_version = version.TakeWhile(x => char.IsDigit(x) || x == '.')
                                 .Where(x => x != '.')
                                 .ToArray()
                                 .with(a => $"{a[0]}.{a[1]}-{a[2]}");

        return (version, lnx_version, changes);
    }
}

static class GenericExtensions
{
    public static string strip_text(this string version)
       => new string(version.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());

    public static string join_by(this IEnumerable<string> items, string separator)
        => string.Join(separator, items.ToArray());

    public static string join_by(this IEnumerable<char> items, string separator)
        => string.Join(separator, items.Select(x => x.ToString()).ToArray());

    public static T2 with<T, T2>(this T obj, Func<T, T2> process) => process(obj);
}