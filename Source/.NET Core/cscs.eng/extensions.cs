#region Licence...

//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2004-2018 Oleg Shilo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------

#endregion Licence...

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using csscript;
using System.Xml.Linq;

/// <summary>
/// Credit to https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
/// </summary>
public static class CLIExtensions
{

    public static string TrimMatchingQuotes(this string input, char quote)
    {
        if (input.Length >= 2 && input.First() == quote && input.Last() == quote)
            return input.Substring(1, input.Length - 2);

        return input;
    }

    public static IEnumerable<string> Split(this string str, Func<char, bool> controller)
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

    public static string ArgValue(this string[] arguments, string prefix)
    {
        return (arguments.FirstOrDefault(x => x.StartsWith(prefix + ":"))
                        ?.Substring(prefix.Length + 1).TrimMatchingQuotes('"'))

               ?? arguments.Where(x => x == prefix).Select(x => "").FirstOrDefault();
    }

    public static string ArgValue(this string argument, string prefix)
    {
        return argument.StartsWith(prefix + ":") == false ? null : argument.Substring(prefix.Length + 1).TrimMatchingQuotes('"');
    }

    public static string[] SplitCommandLine(this string commandLine)
    {
        bool inQuotes = false;

        return commandLine.Split(c =>
                                 {
                                     if (c == '\"')
                                         inQuotes = !inQuotes;

                                     return !inQuotes && c == ' ';
                                 })
                          .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
                          .Where(arg => arg.IsNotEmpty())
                          .ToArray();
    }
}

public static class CoreExtensions
{
    /// <summary>
    /// A generic LINQ equivalent of C# foreach loop.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="action">The action.</param>
    /// <returns></returns>
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (T item in collection)
        {
            action(item);
        }
        return collection;
    }

    public static XElement SelectFirst(this XContainer element, string path)
    {
        string[] parts = path.Split('/');

        var e = element.Elements()
                       .Where(el => el.Name.LocalName == parts[0])
                       .GetEnumerator();

        if (!e.MoveNext())
            return null;

        if (parts.Length == 1) //the last link in the chain
            return e.Current;
        else
            return e.Current.SelectFirst(path.Substring(parts[0].Length + 1)); //be careful RECURSION
    }

    static string sdk_root = "".GetType().Assembly.Location.GetDirName();

    public static bool IsSharedAssembly(this string path) => path.StartsWith(sdk_root, StringComparison.OrdinalIgnoreCase);

    public static bool ToBool(this string text) => text.ToLower() == "true";
    public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);
    public static bool IsNotEmpty(this string text) => !string.IsNullOrEmpty(text);

    public static string[] SplitMergedArgs(this string[] args)
    {
        //because Linux shebang does not properly split arguments we need to take care of this
        //http://www.daniweb.com/software-development/c/threads/268382

        var result = args.SelectMany(arg => arg.Split('-')
                                               .Select(x => x.Trim())
                                               .Where(x => x != "")
                                               .Select(x => "-" + arg))
                                               .ToArray();
        return result;
    }
}

namespace csscript
{
    internal static class GenericExtensions
    {
        public static bool IsDirSectionSeparator(this string text)
        {
            return text != null && text.StartsWith(Settings.dirs_section_prefix) && text.StartsWith(Settings.dirs_section_suffix);
        }

        public static List<T> AddIfNotThere<T>(this List<T> items, T item)
        {
            if (!items.Contains(item))
                items.Add(item);
            return items;
        }

        public static Exception CaptureExceptionDispatchInfo(this Exception ex)
        {
            try
            {
                // on .NET 4.5 ExceptionDispatchInfo can be used
                // ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                typeof(Exception).GetMethod("PrepForRemoting", BindingFlags.NonPublic | BindingFlags.Instance)
                                 .Invoke(ex, new object[0]);
            }
            catch { }
            return ex;
        }

        public static List<string> AddIfNotThere(this List<string> items, string item, string section)
        {
            if (item != null && item != "")
            {
                bool isThere = items.Any(x => Utils.IsSamePath(x, item));

                if (!isThere)
                {
                    if (Settings.ProbingLegacyOrder)
                        items.Add(item);
                    else
                    {
                        var insideOfSection = false;
                        bool added = false;
                        for (int i = 0; i < items.Count; i++)
                        {
                            var currItem = items[i];
                            if (currItem == section)
                            {
                                insideOfSection = true;
                            }
                            else
                            {
                                if (insideOfSection && currItem.StartsWith(Settings.dirs_section_prefix))
                                {
                                    items.Insert(i, item);
                                    added = true;
                                    break;
                                }
                            }
                        }

                        // it's not critical at this stage as the whole options.SearchDirs (the reason for this routine)
                        // is rebuild from ground to top if it has no sections
                        var createMissingSection = false;

                        if (!added)
                        {
                            // just to the end
                            if (!insideOfSection && createMissingSection)
                                items.Add(section);

                            items.Add(item);
                        }
                    }
                }
            }
            return items;
        }
    }

    /// <summary>
    /// Based on "maettu-this" proposal https://github.com/oleg-shilo/cs-script/issues/78
    /// His `SplitLexicallyWithoutTakingNewLineIntoAccount` is taken/used practically without any change.
    /// </summary>
    internal static class ConsoleStringExtensions
    {
        // for free form text when Console is not attached; so make it something big...
        public static int MaxNonConsoleTextWidth = 500;

        static int GetMaxTextWidth()
        {
            try
            {
                // when running on mono under Node.js (VSCode) the Console.WindowWidth is always 0
                if (Console.WindowWidth != 0)
                    return Console.WindowWidth - 1;
                else
                    return 100;
            }
            catch (Exception) { }
            return MaxNonConsoleTextWidth;
        }

        public static string Repeat(this char c, int count)
        {
            return new string(c, count);
        }

        public static string[] SplitSubParagraphs(this string text)
        {
            var lines = text.Split(new[] { "${<==}" }, 2, StringSplitOptions.None);
            if (lines.Count() == 2)
            {
                lines[1] = "${<=-" + lines[0].Length + "}" + lines[1];
            }
            return lines; ;
        }

        public static int Limit(this int value, int min, int max)
        {
            return value < min ? min :
                   value > max ? max :
                   value;
        }

        static string[] newLineSeparators = new string[] { Environment.NewLine, "\n", "\r" };
        // =============================================

        public static string ToConsoleLines(this string text, int indent)
        {
            return text.SplitIntoLines(GetMaxTextWidth(), indent);
        }

        public static string SplitIntoLines(this string text, int maxWidth, int indent)
        {
            var lines = new StringBuilder();
            string left_indent = ' '.Repeat(indent);

            foreach (string line in text.SplitLexically(maxWidth - indent))
                lines.Append(left_indent)
                     .AppendLine(line);

            return lines.ToString();
        }

        public static string[] SplitLexically(this string text, int desiredChunkLength)
        {
            var result = new List<string>();

            foreach (string item in text.Split(newLineSeparators, StringSplitOptions.None))
            {
                string paragraph = item;
                var extraIndent = 0;
                var merge = false;
                int firstDesiredChunkLength = desiredChunkLength;
                int allDesiredChunkLength = desiredChunkLength;

                string[] fittedLines = null;

                const string prefix = "${<=";

                if (paragraph.StartsWith(prefix)) // e.g. "${<=12}" or "${<=-12}" (to merge with previous line)
                {
                    var indent_info = paragraph.Split('}').First();

                    paragraph = paragraph.Substring(indent_info.Length + 1);
                    int.TryParse(indent_info.Substring(prefix.Length).Replace("}", ""), out extraIndent);

                    if (extraIndent != 0)
                    {
                        firstDesiredChunkLength =
                        allDesiredChunkLength = desiredChunkLength - Math.Abs(extraIndent);

                        if (extraIndent < 0)
                        {
                            merge = true;
                            if (result.Any())
                            {
                                firstDesiredChunkLength = desiredChunkLength - result.Last().Length;
                                firstDesiredChunkLength = firstDesiredChunkLength.Limit(0, desiredChunkLength);
                            }

                            if (firstDesiredChunkLength == 0)  // not enough space to merge so break it to the next line
                            {
                                merge = false;
                                firstDesiredChunkLength = allDesiredChunkLength;
                            }

                            extraIndent = -extraIndent;
                        }

                        fittedLines = SplitLexicallyWithoutTakingNewLineIntoAccount(paragraph, firstDesiredChunkLength, allDesiredChunkLength);

                        for (int i = 0; i < fittedLines.Length; i++)
                            if (i > 0 || !merge)
                                fittedLines[i] = ' '.Repeat(extraIndent) + fittedLines[i];
                    }
                }

                if (fittedLines == null)
                    fittedLines = SplitLexicallyWithoutTakingNewLineIntoAccount(paragraph, firstDesiredChunkLength, allDesiredChunkLength);

                if (merge && result.Any())
                {
                    result[result.Count - 1] = result[result.Count - 1] + fittedLines.FirstOrDefault();
                    fittedLines = fittedLines.Skip(1).ToArray();
                }

                result.AddRange(fittedLines);
            }
            return result.ToArray();
        }

        static string[] SplitLexicallyWithoutTakingNewLineIntoAccount(this string str, int desiredChunkLength)
        {
            return SplitLexicallyWithoutTakingNewLineIntoAccount(str, desiredChunkLength, desiredChunkLength);
        }

        static string[] SplitLexicallyWithoutTakingNewLineIntoAccount(this string str, int firstDesiredChunkLength, int desiredChunkLength)
        {
            var spaces = new List<int>();
            // Retrieve all spaces within the string:
            int i = 0;
            while ((i = str.IndexOf(' ', i)) >= 0)
            {
                spaces.Add(i);
                i++;
            }

            // Add an extra space at the end of the string to ensure that the last chunk is split properly:
            spaces.Add(str.Length);

            // Split the string into the desired chunk size taking word boundaries into account:
            int startIndex = 0;
            var chunks = new List<string>();
            int _desiredChunkLength = firstDesiredChunkLength;

            while (startIndex < str.Length)
            {
                if (startIndex != 0)
                    _desiredChunkLength = desiredChunkLength;

                // Find the furthermost split position:
                int spaceIndex = spaces.FindLastIndex(value => (value <= (startIndex + _desiredChunkLength)));

                int splitIndex;
                if (spaceIndex >= 0)
                    splitIndex = spaces[spaceIndex];
                else
                    splitIndex = (startIndex + _desiredChunkLength);

                // Limit to split within the string and execute the split:
                splitIndex = splitIndex.Limit(startIndex, str.Length);
                int length = (splitIndex - startIndex);
                chunks.Add(str.Substring(startIndex, length));
                Debug.WriteLine(chunks.Count());
                startIndex += length;

                // Remove the already used spaces from the collection to ensure those spaces are not used again:
                if (spaceIndex >= 0)
                {
                    spaces.RemoveRange(0, (spaceIndex + 1));
                    startIndex++; // Advance an extra character to compensate the space.
                }
            }

            return (chunks.ToArray());
        }

        // =============================================
    }
}