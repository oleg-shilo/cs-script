using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CSScripting;

/// <summary>
/// Provides utility methods for common shell operations including file hashing, HTTP downloads,
/// file manipulation, and process execution.
/// </summary>
public static class shell
{
    /// <summary>
    /// Calculates the SHA256 hash of a file.
    /// </summary>
    /// <param name="file">The path to the file to hash.</param>
    /// <returns>The SHA256 hash as a hexadecimal string without dashes.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
    static public string sha256(string file)
    {
        byte[] checksum = SHA256.HashData(File.ReadAllBytes(file));
        var sha256 = BitConverter.ToString(checksum).Replace("-", "");
        return sha256;
    }

    /// <summary>
    /// Downloads the content of a URL as a string.
    /// </summary>
    /// <param name="url">The URL to download from.</param>
    /// <returns>The content of the URL as a string.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="TaskCanceledException">Thrown when the request times out.</exception>
    static public string downloadString(this string url)
    {
        using var client = new HttpClient();
        return client.GetStringAsync(url).Result;
    }

    /// <summary>
    /// Displays download progress as a percentage to the console.
    /// </summary>
    /// <param name="x">The current progress value.</param>
    /// <param name="y">The total/maximum value.</param>
    static public void progressToConsole(long x, long y) => Console.Write($"\r{(x * 100) / y}%");

    /// <summary>
    /// Downloads a file from a URL to a local destination with optional progress tracking.
    /// </summary>
    /// <param name="url">The URL to download from.</param>
    /// <param name="destinationPath">The local file path where the downloaded content will be saved.</param>
    /// <param name="onProgress">Optional callback to track download progress. Receives (bytesDownloaded, totalBytes).</param>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="Exception">Thrown when the Content-Length header is missing from the response.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the destination path is denied.</exception>
    static public void downloadTo(this string url, string destinationPath, Action<long, long>? onProgress = null)
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

    /// <summary>
    /// Reads a line of input from the console with an optional prompt.
    /// </summary>
    /// <param name="prompt">Optional prompt message to display before reading input.</param>
    /// <returns>The line of text entered by the user, or null if end of stream is reached.</returns>
    static public string readLine(string prompt = null)
    {
        if (prompt != null) Console.WriteLine(prompt);
        return Console.ReadLine();
    }

    /// <summary>
    /// Replaces all occurrences of a pattern in a file with a replacement string.
    /// </summary>
    /// <param name="file">The path to the file to modify.</param>
    /// <param name="pattern">The string pattern to search for.</param>
    /// <param name="replacement">The string to replace the pattern with.</param>
    /// <returns>The file path that was modified.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
    static public string replaceInFile(this string file, string pattern, string replacement)
    {
        var content = File.ReadAllText(file);
        content = content.Replace(pattern, replacement);
        File.WriteAllText(file, content);
        return file;
    }

    /// <summary>
    /// Executes an application with the specified arguments and returns the output and exit code.
    /// </summary>
    /// <param name="app">The application or command to execute.</param>
    /// <param name="args">The command-line arguments. Can contain newlines for multi-line arguments.</param>
    /// <param name="workingDir">Optional working directory for the process.</param>
    /// <returns>A tuple containing the standard output and the exit code of the process.</returns>
    /// <exception cref="Win32Exception">Thrown when the application cannot be started.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified application is not found.</exception>
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

    public static void copyFilesTo(this string srcDir, string destDir)
    {
        destDir.EnsureDir();
        Directory.GetFiles(srcDir, "*")
                 .ForEach(f => File.Copy(f, destDir.PathJoin(f.GetFileName()), true));
    }
}

/// <summary>
/// Provides extension methods for common string and path operations.
/// </summary>
static class Extensions
{
    public static T FromJson<T>(this string json) => JsonSerializer.Deserialize<T>(json);

    public static string ToJson(this object obj)
        => JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });

    public static HttpClient SetBasicAuthentication(this HttpClient client, string username, string password)
    {
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        return client;
    }
}