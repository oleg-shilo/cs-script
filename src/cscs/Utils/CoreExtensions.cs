using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using csscript;
using CSScripting;
using CSScripting.CodeDom;

#if class_lib

namespace CSScriptLib
#else

namespace csscript
#endif
{
    /// <summary>
    /// </summary>
    public static partial class CoreExtensions
    {
        internal static Process RunAsync(this string exe, string args, string dir = null)
        {
            var process = InitProc(exe, args, dir);
            process.Start();
            return process;
        }

        internal static Process InitProc(this string exe, string args, string dir = null)
        {
            var process = new Process();

            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = args;
            process.StartInfo.WorkingDirectory = dir;

            // hide terminal window
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            return process;
        }

        internal static int Run(this string exe, string args, string dir = null, Action<string> onOutput = null,
            Action<string> onError = null, int timeout = -1, bool terminateOnTimeout = true, string buildArtifact = null)
        {
            var process = InitProc(exe, args, dir);

            // starting draining streams from another thread creates a problem as `WaitForExit()`
            // returns before streams are closed (at least in .NET6) so use blocking `ReadToEnd()`
            // Seems to be a problem with .NET implementation.
            // To make things worse, on some Win systems process (e.g. dotnet.exe) does not return even
            // if it was successful.

            // onErrro will be used when this functionality is restored.
            // var error = StartMonitor(process.StandardError, onError);
            // var output = StartMonitor(process.StandardOutput, onOutput);

            var output = process.StandardOutput.ReadToEnd();
            onOutput?.Invoke(output);

            // disabling it as it actually starts another instance of the process. It was a clumsy work around at a stage
            // process.Start(); // important to call even if it is already started, otherwise WLS2 fails to catch the output

            if (buildArtifact.HasText())
            {
                // wait only till the desired outcome is achieved

                int count = timeout;
                int exitCode = 0;

                while (count > 0)
                    try
                    {
                        if (buildArtifact.FileExists() || process.HasExited)
                            break;

                        var interval = 100;
                        Thread.Sleep(interval);
                        count -= interval;
                    }
                    catch { }

                if (!buildArtifact.FileExists())
                {
                    if (!process.HasExited)
                        onOutput?.Invoke($"Process '{exe}' is taking too long to finish. Terminating it forcefully.");
                    exitCode = -1;
                }

                try { process.Kill(); } catch { }

                return exitCode;
            }
            else if (!process.WaitForExit(timeout))
            {
                if (timeout != -1) // of course it is not -1 if we are here :)
                    onOutput?.Invoke($"Process '{exe}' is taking too long to finish. Terminating it forcefully.");

                if (terminateOnTimeout)
                    try { process.Kill(); } catch { }

                // return typical timeout code for CLI apps
                return -1;
            }

            return process.ExitCode;
        }

        static internal void NormaliseFileReference(ref string file, ref int line)
        {
            try
            {
                if ((file.EndsWith(".g.csx") || file.EndsWith(".g.cs"))
                    && file.Contains(Runtime.CacheDir, ignoreCase: true))
                {
                    //it is an auto-generated file so try to find the original source file (logical file)
                    string dir = Path.GetDirectoryName(file);
                    string infoFile = Path.Combine(dir, "css_info.txt");
                    if (File.Exists(infoFile))
                    {
                        string[] lines = File.ReadAllLines(infoFile);
                        if (lines.Length > 1 && Directory.Exists(lines[1]))
                        {
                            string logicalFile = Path.Combine(lines[1], Path.GetFileName(file).Replace(".g.csx", ".csx").Replace(".g.cs", ".cs"));
                            if (File.Exists(logicalFile))
                            {
                                string code = File.ReadAllText(file);
                                int pos = code.IndexOf("///CS-Script auto-class generation");
                                if (pos != -1)
                                {
                                    int injectedLineNumber = code.Substring(0, pos).Split('\n').Count() - 1;
                                    if (injectedLineNumber <= line)
                                        line -= 1; //a single line is always injected
                                }
                                file = logicalFile;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        internal static Thread StartMonitor(StreamReader stream, Action<string> action = null)
        {
            var thread = new Thread(x =>
            {
                try
                {
                    string line = null;
                    while (null != (line = stream.ReadLine()))
                        action?.Invoke(line);
                }
                catch { }
            });
            thread.Start();
            return thread;
        }

        /// <summary>
        /// Selects the first element that satisfies the specified path.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="path">The path.</param>
        /// <returns>Selected XML element</returns>
        internal static XElement SelectFirst(this XContainer element, string path)
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

        /// <summary>
        /// Removes the duplicated file system path items from the collection.The duplicates are
        /// identified based on the path being case sensitive depending on the hosting OS file system.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns>A list with the unique items</returns>
        internal static string[] RemovePathDuplicates(this string[] list)
        {
            return list.Where(x => x.IsNotEmpty())
                       .Select(x =>
                       {
                           var fullPath = Path.GetFullPath(x);
                           if (File.Exists(fullPath))
                               return fullPath;
                           else
                               return x;
                       })
                       .Distinct()
                       .ToArray();
        }

        static string sdk_root = "".GetType().Assembly.Location.GetDirName();

        /// <summary>
        /// Determines whether [is shared assembly].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is shared assembly] [the specified path]; otherwise, <c>false</c>.</returns>
        internal static bool IsSharedAssembly(this string path) => path.StartsWith(sdk_root, StringComparison.OrdinalIgnoreCase);

        internal static bool IsPossibleFacadeAssembly(this string path) => SharedAssembliesNames.Contains(path.GetFileName());

        internal static string[] SharedAssembliesNames => Directory.GetFiles(sdk_root, "*.dll").Select(Path.GetFileName).ToArray();

        /// <summary>
        /// Converts to bool.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <summary>
        /// Removes the assembly extension.
        /// </summary>
        /// <param name="asmName">Name of the asm.</param>
        /// <returns>Result of the string manipulation</returns>
        public static string RemoveAssemblyExtension(this string asmName)
        {
            if (asmName.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) || asmName.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
                return asmName.Substring(0, asmName.Length - 4);
            else
                return asmName;
        }

        /// <summary>
        /// Compares two path strings. Handles path being case-sensitive based on the OS file system.
        /// </summary>
        /// <param name="path1">The path1.</param>
        /// <param name="path2">The path2.</param>
        /// <returns>The result of the test.</returns>
        public static bool SamePathAs(this string path1, string path2) =>
            string.Compare(path1, path2, Runtime.IsWin) == 0;

        /// <summary>
        /// Captures the exception dispatch information.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns>Processed exception instanse</returns>
        public static Exception CaptureExceptionDispatchInfo(this Exception ex)
        {
            try
            {
                // on .NET 4.5 ExceptionDispatchInfo can be used ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                typeof(Exception).GetMethod("PrepForRemoting", BindingFlags.NonPublic | BindingFlags.Instance)
                                 .Invoke(ex, new object[0]);
            }
            catch { /* non critical exception */ }
            return ex;
        }

        internal static Exception ToNewException(this Exception ex, string message, bool encapsulate = true)
        {
            var topLevelMessage = message;
            Exception childException = ex;
            if (!encapsulate)
            {
                topLevelMessage += Environment.NewLine + ex.Message;
                childException = null;
            }
            var constructor = ex.GetType().GetConstructor(new Type[] { typeof(string), typeof(Exception) });
            if (constructor != null)
                return (Exception)constructor.Invoke(new object[] { topLevelMessage, childException });
            else
                return new Exception(message, childException);
        }

        /// <summary>
        /// Files the delete.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="rethrow">if set to <c>true</c> [rethrow].</param>
        internal static void FileDelete(this string filePath, bool rethrow)
        {
            if (filePath.IsNotEmpty())
            {
                //There are the reports about
                //anti viruses preventing file deletion
                //See 18 Feb message in this thread https://groups.google.com/forum/#!topic/cs-script/5Tn32RXBmRE

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        if (File.Exists(filePath))
                            File.Delete(filePath);
                        break;
                    }
                    catch
                    {
                        if (rethrow && i == 2)
                            throw;
                    }

                    Thread.Sleep(300);
                }
            }
        }

#if !class_lib

        internal static List<string> AddPathIfNotThere(this List<string> items, string item, string section)
        {
            if (item != null && item != "")
            {
                bool isThere = items.Any(x => x.SamePathAs(item));

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

                        // it's not critical at this stage as the whole options.SearchDirs (the
                        // reason for this routine) is rebuild from ground to top if it has no sections
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

#endif

        internal static string Escape(this char c)
        {
            return "\\u" + ((int)c).ToString("x4");
        }

        internal static bool IsWhiteSpace(this char c)
        {
            return char.IsWhiteSpace(c) || c == '\uFEFF';
        }

#if class_lib

        internal static ExpandStatementDelegate ExpandAlgorithm { get; set; }

        internal static string Expand(this string text) => ExpandAlgorithm?.Invoke(text) ?? Environment.ExpandEnvironmentVariables(text);

        internal static string UnescapeExpandTrim(this string text) =>
            CSharpParser.UnescapeDirectiveDelimiters(Expand(text)).Trim();

#else

        internal static string Expand(this string text) => Environment.ExpandEnvironmentVariables(text);

        internal static string UnescapeExpandTrim(this string text) =>
            CSharpParser.UnescapeDirectiveDelimiters(Environment.ExpandEnvironmentVariables(text)).Trim();

#endif

        internal static void NotePathExtraDirs(this Process p, IEnumerable<string> dirs)
        {
            Environment.SetEnvironmentVariable("CSSCRIPT_PROCESS_PATH_EXTRA_" + p.Id, dirs.JoinBy(";"));
        }

        internal static string[] RetreiveProcessPathExtraDirs(this Process p)
        {
            var dirs = Environment.GetEnvironmentVariable("CSSCRIPT_PROCESS_PATH_EXTRA_" + p.Id);
            return dirs?.Split(';') ?? new string[0];
        }

        internal static void AddToSystemPath(this string[] dirs)
        {
            if (dirs.Any())
            {
                var newPathDirs = dirs
                    .Where(x => x.HasText())
                    .Select(x => x);

                var currentPath = Environment.GetEnvironmentVariable("PATH").Split(';').Where(x => x.HasText());

                Environment.SetEnvironmentVariable("PATH", currentPath.Concat(newPathDirs).Distinct().JoinBy(";"));
            }
        }

        internal static string NormaliseAsDirectiveOf(this string statement, string parentScript, char multiPathDelimiter)
        {
            var pathItems = statement.Split(multiPathDelimiter);
            var result = pathItems.Select(x => NormaliseAsDirectiveOf(x.Trim(), parentScript))
                             .JoinBy(multiPathDelimiter.ToString());

            return result;
        }

        internal static string NormaliseAsDirectiveOf(this string statement, string parentScript)
        {
            var text = CSharpParser.UserToInternalEscaping(statement);

            if (text.Length > 1 && (text[0] == '.' && text[1] != '.')) // just a single-dot start dir
                text = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(parentScript), text));

            return Environment.ExpandEnvironmentVariables(text).Trim();
        }

        internal static string NormaliseAsDirective(this string statement)
        {
            var text = CSharpParser.UnescapeDirectiveDelimiters(statement);
            return Environment.ExpandEnvironmentVariables(text).Trim();
        }

        internal static T2 MapValue<T1, T2>(this T1 value, params (T1, Func<object, T2>)[] patterenMap) where T1 : class
        {
            foreach (var (pattern, result) in patterenMap)
                if (value.Equals(pattern))
                    return result(null);

            return default;
        }

        /// <summary>
        /// Converts an array to a tuple.
        /// <para>Based on this beautiful solution: https://stackoverflow.com/questions/49190830/is-it-possible-for-string-split-to-return-tuple</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="first">The first.</param>
        /// <param name="rest"></param>
        public static void Deconstruct<T>(this IList<T> list, out T first, out IList<T> rest)
        {
            first = list.Count > 0 ? list[0] : default(T); // or throw
            rest = list.Skip(1).ToList();
        }

        /// <summary>
        /// Converts an array to a tuple.
        /// <para>Based on this beautiful solution: https://stackoverflow.com/questions/49190830/is-it-possible-for-string-split-to-return-tuple</para>
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="first">The first.</param>
        /// <param name="second">The second.</param>
        /// <param name="rest">The rest.</param>
        public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest)
        {
            first = list.Count > 0 ? list[0] : default(T); // or throw
            second = list.Count > 1 ? list[1] : default(T); // or throw
            rest = list.Skip(2).ToList();
        }
    }

    /// <summary>
    /// Collection of temp files to be removed during cleanup
    /// </summary>
    public class TempFileCollection
    {
        /// <summary>
        /// Gets or sets the items (file paths) composing the temporary files collections.
        /// </summary>
        /// <value>The items.</value>
        public List<string> Items { get; set; } = new List<string>();

        /// <summary>
        /// Clears the collection.
        /// </summary>
        public void Clear() => Items.ForEach(File.Delete);
    }

    static class TempTilesManagement
    {
        public static bool IsParentProcessRunning(this string tempFile)
        {
            string name = Path.GetFileName(tempFile);

            int pos = name.IndexOf('.');

            if (pos > 0)
            {
                string pidValue = name.Substring(0, pos);

                if (int.TryParse(pidValue, out int pid))
                {
                    //Didn't use GetProcessById as it throws if pid is not running
                    if (Process.GetProcesses().Any(p => p.Id == pid))
                        return true; //still running
                }
            }
            return false;
        }
    }
}