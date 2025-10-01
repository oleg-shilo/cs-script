using System;
using System.Collections.Generic;
using System.Diagnostics;

using static System.Environment;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

public static class shell
{
    static public string sha256(this string file)
    {
        byte[] checksum = SHA256.HashData(File.ReadAllBytes(file));
        var sha256 = BitConverter.ToString(checksum).Replace("-", "");
        return sha256;
    }

    static public string downloadString(string url)
    {
        using var client = new HttpClient();
        return client.GetStringAsync(url).Result;
    }

    static public void downloadFile(string url, string destinationPath, Action<long, long> onProgress = null)
    {
        using var client = new HttpClient();
        using var response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result;
        response.EnsureSuccessStatusCode();

        if (!response.Content.Headers.ContentLength.HasValue)
            throw new Exception("Content-Length header is missing in the response.");

        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        var contentLength = response.Content.Headers.ContentLength.Value;
        var buf = new byte[81920]; // 80 KB chunks
        int totalCount = 0;
        int count = 0;

        using var contentStream = response.Content.ReadAsStream();
        using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        while (0 < (count = contentStream.Read(buf, 0, buf.Length)))
        {
            destStream.Write(buf, 0, count);

            totalCount += count;
            if (onProgress != null)
                onProgress(totalCount, contentLength);
        }
    }

    static public string readLine(string prompt = null)
    {
        if (prompt != null) Console.WriteLine(prompt);
        return Console.ReadLine();
    }

    static public string replaceInFile(this string file, string pattern, string replacement)
    {
        var content = File.ReadAllText(file);
        content = content.Replace(pattern, replacement);
        File.WriteAllText(file, content);
        return file;
    }

    public static (string output, int exitCode) run(this string app, string args, string workingDir = null)
    {
        var appArgs = Environment.ExpandEnvironmentVariables(args);
        var info =
            (args.Contains('\n')) ?
                new ProcessStartInfo(app, appArgs.Split("\n").Select(x => x.Trim())) :
                new ProcessStartInfo(app, appArgs);

        info.RedirectStandardOutput = true;
        info.WorkingDirectory = workingDir;

        using var p = Process.Start(info);
        return (p.StandardOutput.ReadToEnd(), p.ExitCode);
    }
}

static class Extensions
{
    static public string GetFullPath(this string path) => Path.GetFullPath(path);

    static public string PathJoin(this string path1, string path2) => Path.Combine(path1, path2);

    public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text);

    public static bool HasText(this string text) => !text.IsEmpty();

    public static DateTime? ToDateTime(this string text)
    {
        if (DateTime.TryParse(text, out var date))
            return date;
        return null;
    }

    public static int ToInt(this string text, int defaultValue = default)
        => int.TryParse(text, out var result) ? result : defaultValue;

    public static string TrimLength(this string text, int length, bool padIfShorter = true)
    {
        var result = text.Length > length ?
            text.Substring(0, length - 3) + "..." :
            text;

        if (padIfShorter && result.Length < length)
            result += new string(' ', length - result.Length);
        return result;
    }

    public static string[] GetLines(this string text, bool deflate = true)
        => text?.Split('\n')?
                .Select(x => deflate ? x.Trim() : x)
                .Where(x => deflate ? !x.IsEmpty() : true)
                .ToArray() ?? [];
}