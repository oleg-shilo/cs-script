using System;
using static System.Environment;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using static dbg; // to use 'print' instead of 'dbg.print'
using System.IO;

namespace Xunit
{
    public class IClassFixture<T> where T : new()
    {
        T fixture;

        public IClassFixture()
        {
            fixture = new T();
        }
    }

    class FactAttribute : Attribute
    {
    }

    class FactWinOnlyAttribute : Attribute
    {
    }

    class TestFailureException : ApplicationException
    {
        public TestFailureException(string? message) : base(message)
        {
        }
    }

    class Assert
    {
        static public void False(bool actualValue)
        {
            if (actualValue != false)
                throw new TestFailureException($"Failed: the actual value is 'false'{NewLine}");
        }

        static public void True(bool actualValue)
        {
            if (actualValue != true)
                throw new TestFailureException($"Failed: the actual value is 'true'{NewLine}");
        }

        static public void Contains<T>(IEnumerable<T> collection, T item)
        {
            if (!collection.Contains(item))
                throw new TestFailureException($"Failed: the expected item '{item}'" +
                                               $"does not belong to the collection");
        }

        static public void Empty<T>(IEnumerable<T> collection)
        {
            if (collection.Any())
                throw new TestFailureException($"Failed: the collection is expected to be empty but it is not");
        }

        static public void Contains(string expected, string actual)
        {
            if (!actual.Contains(expected))
                throw new TestFailureException($"Failed: the expected value {NewLine}" +
                                               $"\t'{expected}'{NewLine}" +
                                               $"is not found in actual value{NewLine}" +
                                               $"\t'{actual}'");
        }

        static public void StartsWith(string expected, string actual)
        {
            if (!actual.StartsWith(expected))
                throw new TestFailureException($"Failed: the actual value {NewLine}" +
                                               $"\t'{actual}'{NewLine}" +
                                               $"does not start with {NewLine}" +
                                               $"\t'{actual}'");
        }

        static public void Equal(string expected, string actual)
        {
            if (actual != expected)
                throw new TestFailureException($"Failed: the expected value {NewLine}" +
                                               $"\t'{expected}'{NewLine}" +
                                               $"is different to actual value{NewLine}" +
                                               $"\t'{actual}'");
        }
    }

    static class Extensions
    {
        // public static string GetFullPath(this string path) => Path.GetFullPath(path);

        // public static string GetDirName(this string path) => path == null ? null : Path.GetDirectoryName(path);

        // public static string PathJoin(this string path, params object[] parts)
        // {
        //     var allParts = new[] { path ?? "" }.Concat(parts.Select(x => x?.ToString() ?? ""));
        //     return Path.Combine(allParts.ToArray());
        // }

        // public static string EnsureDir(this string path, bool rethrow = true)
        // {
        //     try
        //     {
        //         Directory.CreateDirectory(path);

        //         return path;
        //     }
        //     catch { if (rethrow) throw; }
        //     return null;
        // }

        public static bool IsWin(this OperatingSystem sys)
            => !(Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX);

        public static string Run(this string exe, string args = null, string dir = null)
        {
            var process = new Process();

            // Console.WriteLine("run>>>");
            // Console.WriteLine(exe + " " + args);
            // Console.WriteLine("run<<<<");

            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = args;
            process.StartInfo.WorkingDirectory = dir;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }
    }
}