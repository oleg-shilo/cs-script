using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#if class_lib

namespace CSScriptLib
#else

namespace csscript
#endif
{
    internal static class WildCardExtensions
    {
        /// <summary>
        /// Gets the directories specified by either relative or absolute path `rootDir`.
        /// `rootDir` can contain wild-cards as per Git 'ignore specification'
        /// </summary>
        /// <param name="baseDir">The working dir.</param>
        /// <param name="rootDir">The root dir path as per Git 'ignore specification'.</param>
        /// <returns>The method result.</returns>
        static public string[] GetMatchingDirs(this string baseDir, string rootDir)
        {
            if (!Path.IsPathRooted(rootDir))
                rootDir = Path.Combine(baseDir, rootDir); //cannot use Path.GetFullPath as it crashes if '*' or '?' are present

            List<string> result = new List<string>();

            if (rootDir.Contains("*") || rootDir.Contains("?"))
            {
                bool useAllSubDirs = rootDir.EndsWith("**");

                string pattern = WildCardToRegExpPattern(useAllSubDirs ? rootDir.Remove(rootDir.Length - 1) : rootDir);

                var wildcard = new Regex(pattern, Runtime.IsWin ? RegexOptions.IgnoreCase : RegexOptions.None);

                int pos = rootDir.IndexOfAny(new char[] { '*', '?' });

                string newRootDir = rootDir.Remove(pos);

                pos = newRootDir.LastIndexOf(Path.DirectorySeparatorChar);
                newRootDir = rootDir.Remove(pos);

                if (Directory.Exists(newRootDir))
                {
                    foreach (string dir in Directory.GetDirectories(newRootDir, "*", SearchOption.AllDirectories))
                        if (wildcard.IsMatch(dir))
                        {
                            if (!result.Contains(dir))
                            {
                                result.Add(dir);

                                if (useAllSubDirs)
                                    foreach (string subDir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
                                        //if (!result.Contains(subDir))
                                        result.Add(subDir);
                            }
                        }
                }
            }
            else
                result.Add(rootDir);

            return result.ToArray();
        }

        public static Regex WildCardToRegExp(this string pattern)
            => new Regex(pattern.WildCardToRegExpPattern(), Runtime.IsWin ? RegexOptions.IgnoreCase : RegexOptions.None);

        //Credit to MDbg team: https://github.com/SymbolSource/Microsoft.Samples.Debugging/blob/master/src/debugger/mdbg/mdbgCommands.cs
        public static string WildCardToRegExpPattern(this string simpleExp)
        {
            var sb = new StringBuilder();
            sb.Append("^");
            foreach (char c in simpleExp)
            {
                switch (c)
                {
                    case '\\':
                    case '{':
                    case '|':
                    case '+':
                    case '[':
                    case '(':
                    case ')':
                    case '^':
                    case '$':
                    case '.':
                    case '#':
                    case ' ':
                        sb.Append('\\').Append(c);
                        break;

                    case '*':
                        sb.Append(".*");
                        break;

                    case '?':
                        sb.Append(".");
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }

            sb.Append("$");
            return sb.ToString();
        }
    }
}