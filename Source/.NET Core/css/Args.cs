using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace css
{
    static class Args
    {
        public static bool IsWin(this OperatingSystem os) => os.Platform == PlatformID.Win32NT;

        public static string ToCmdArgs(this IEnumerable<string> args)
            => string.Join(" ",
                           args.Select(x => (x.Contains(" ") || x.Contains("\t")) ? $"\"{x}\"" : x)
                               .ToArray());

        public static bool Same(string arg, params string[] patterns)
        {
            foreach (string pattern in patterns)
            {
                if (arg.StartsWith("-"))
                    if (arg.Length == pattern.Length + 1 && arg.IndexOf(pattern) == 1)
                        return true;

                if (Environment.OSVersion.IsWin() && arg[0] == '/')
                    if (arg.Length == pattern.Length + 1 && arg.IndexOf(pattern) == 1)
                        return true;
            }
            return false;
        }

        public static bool ParseValuedArg(string arg, string pattern, out string value)
        {
            value = null;

            if (Args.Same(arg, pattern))
                return true;

            pattern += ":";
            if (Args.StartsWith(arg, pattern))
            {
                value = Args.ArgValue(arg, pattern);
                return true;
            }

            return false;
        }

        public static bool StartsWith(string arg, string pattern)
        {
            if (arg.StartsWith("-"))
                return arg.IndexOf(pattern) == 1;
            if (Environment.OSVersion.IsWin())
                if (arg[0] == '/')
                    return arg.IndexOf(pattern) == 1;
            return false;
        }

        public static string ArgValue(string arg, string pattern)
        {
            return arg.Substring(pattern.Length + 1);
        }

        public static bool ParseValuedArg(this IEnumerable<string> args, string pattern, string pattern2, out string value)
        {
            value = null;
            foreach (var arg in args)
            {
                if (ParseValuedArg(arg, pattern, out value))
                    return true;

                if (ParseValuedArg(arg, pattern2, out value))
                    return true;
            }
            return false;
        }
    }
}