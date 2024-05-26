using System;
using System.Collections.Generic;
using System.Linq;
using csscript;

namespace CSScripting
{
    /// <summary>
    /// Various string extensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Determines whether the string is empty (or null).
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>
        ///   <c>true</c> if the specified text is empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);


        /// <summary>
        /// Determines whether the string is not empty (or null).
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>
        ///   <c>true</c> if [is not empty] [the specified text]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNotEmpty(this string text) => !string.IsNullOrEmpty(text);

        /// <summary>
        /// Determines whether this instance has text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>
        ///   <c>true</c> if the specified text has text; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasText(this string text) => !string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text);

        /// <summary>
        /// Trims a single character form the head and the end of the string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="trimChars">The trim chars.</param>
        /// <returns>The result of trimming.</returns>
        public static string TrimSingle(this string text, params char[] trimChars)
        {
            if (text.IsEmpty())
                return text;

            var startOffset = trimChars.Contains(text[0]) ? 1 : 0;
            var endOffset = (trimChars.Contains(text.Last()) ? 1 : 0);

            if (startOffset != 0 || endOffset != 0)
                return text.Substring(startOffset, (text.Length - startOffset) - endOffset);
            else
                return text;
        }

        /// <summary>
        /// Gets the lines.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The method result.</returns>
        public static string[] GetLines(this string str) =>// too simplistic though adequate
            str.Replace("\r\n", "\n").Split('\n');

        /// <summary>
        /// Determines whether this string contains the substring defined by the pattern.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns>
        ///   <c>true</c> if [contains] [the specified pattern]; otherwise, <c>false</c>.
        /// </returns>
        public static bool Contains(this string text, string pattern, bool ignoreCase)
            => text.IndexOf(pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase : default(StringComparison)) != -1;

        /// <summary>
        /// Compares two strings.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns>The result of the test.</returns>
        public static bool SameAs(this string text, string pattern, bool ignoreCase = true)
            => 0 == string.Compare(text, pattern, ignoreCase);

        /// <summary>
        /// Checks if the given string matches any of the provided patterns.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="patterns">The patterns</param>
        /// <returns>The method result.</returns>
        public static bool IsOneOf(this string text, params string[] patterns)
            => patterns.Any(x => x == text);

        /// <summary>
        /// Joins strings the by the specified separator.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>The method result.</returns>
        public static string JoinBy(this IEnumerable<string> values, string separator)
            => string.Join(separator, values);

        /// <summary>
        /// The custom implementation of the <see cref="string.GetHashCode"/> method.
        /// </summary>
        /// <param name="text">The text to generate the hash for.s.</param>
        /// <returns>The method result.</returns>
        public static int GetHashCodeEx(this string text)
        {
            //during the script first compilation GetHashCodeEx is called ~10 times
            //during the cached execution ~5 times only
            //and for hosted scenarios it is twice less

            //The following profiling demonstrates that in the worst case scenario hashing would
            //only add ~2 microseconds to the execution time

            //Native executions cost (milliseconds)=> 100000: 7; 10 : 0.0007
            //Custom Safe executions cost (milliseconds)=> 100000: 40; 10: 0.004
            //Custom Unsafe executions cost (milliseconds)=> 100000: 13; 10: 0.0013
#if !class_lib
            if (csscript.ExecuteOptions.options.customHashing)
            {
                // deterministic GetHashCode; useful for integration with third party products (e.g. CS-Script.Npp)
                return text.GetHashCode32();
            }
            else
            {
                return text.GetHashCode();
            }
#else
            return text.GetHashCode();
#endif
        }

        //needed to have reliable HASH as x64 and x32 have different algorithms; This leads to the inability of script clients calculate cache directory correctly
    }
}

namespace CSScripting
{
    internal static class CommandArgParser
    {
        public static string TrimMatchingQuotes(this string input, char quote)
        {
            if (input.Length >= 2)
            {
                //"-sconfig:My Script.cs.config"
                if (input.First() == quote && input.Last() == quote)
                {
                    return input.Substring(1, input.Length - 2);
                }
                //-sconfig:"My Script.cs.config"
                else if (input.Last() == quote)
                {
                    var firstQuote = input.IndexOf(quote);
                    if (firstQuote != input.Length - 1) //not the last one
                        return input.Substring(0, firstQuote) + input.Substring(firstQuote + 1, input.Length - 2 - firstQuote);
                }
            }
            return input;
        }

        public static IEnumerable<string> Split(this string str,
                                                     Func<char, bool> controller)
        {
            int nextPiece = 0;

            for (int c = 0; c < str.Length; c++)
            {
                if (controller(str[c]))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }

        public static string[] SplitCommandLine(this string commandLine)
        {
            bool inQuotes = false;
            bool isEscaping = false;

            return commandLine.Split(c =>
            {
                if (c == '\\' && !isEscaping) { isEscaping = true; return false; }

                if (c == '\"' && !isEscaping)
                    inQuotes = !inQuotes;

                isEscaping = false;

                return !inQuotes && char.IsWhiteSpace(c)/*c == ' '*/;
            })
            .Select(arg => arg.Trim().TrimMatchingQuotes('\"').Replace("\\\"", "\""))
            .Where(arg => !string.IsNullOrEmpty(arg))
            .ToArray();
        }
    }
}