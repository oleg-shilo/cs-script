using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CSScripting
{
    /// <summary>
    /// Various PATH extensions
    /// </summary>
    public static class PathExtensions
    {
        /// <summary>
        /// Copies the file.
        /// </summary>
        /// <param name="src">The source path to the file.</param>
        /// <param name="dest">The destination path to the file.</param>
        /// <param name="ignoreErrors">if set to <c>true</c> [ignore errors].</param>
        public static void FileCopy(this string src, string dest, bool ignoreErrors = false)
        {
            try
            {
                File.Copy(src, dest, true);
            }
            catch
            {
                if (!ignoreErrors) throw;
            }
        }

        /// <summary>
        /// Changes the extension of the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="extension">The extension.</param>
        /// <returns>A new path</returns>
        public static string ChangeExtension(this string path, string extension) => Path.ChangeExtension(path, extension);

        /// <summary>
        /// Gets the extension.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>File extension</returns>
        public static string GetExtension(this string path) => Path.GetExtension(path);

        /// <summary>
        /// Gets the file name part of the full path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The method result.</returns>
        public static string GetFileName(this string path) => Path.GetFileName(path);

        /// <summary>
        /// Checks if the directory exists.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The result of the test.</returns>
        public static bool DirExists(this string path) => path.IsNotEmpty() ? Directory.Exists(path) : false;

        /// <summary>
        /// Gets the full path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The path</returns>
        public static string GetFullPath(this string path) => Path.GetFullPath(path);

        /// <summary>
        /// Determines whether the path is directory.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if the specified path is dir; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDir(this string path) => Directory.Exists(path);

        /// <summary>
        /// Determines whether the specified path string is valid (does not contain invalid characters).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if the path is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidPath(this string path) => path.IndexOfAny(Path.GetInvalidPathChars()) == -1;

        /// <summary>
        /// A more convenient API version of <see cref="Path.Combine(string[])"/>.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="parts">The parts.</param>
        /// <returns>A new path.</returns>
        public static string PathJoin(this string path, params object[] parts)
        {
            var allParts = new[] { path ?? "" }.Concat(parts.Select(x => x?.ToString() ?? ""));
            return Path.Combine(allParts.ToArray());
        }

        /// <summary>
        /// Gets the special folder path.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <returns>A folder path.</returns>
        public static string GetPath(this Environment.SpecialFolder folder)
        {
            return Environment.GetFolderPath(folder);
        }

        internal static bool IsWritable(this string path)
        {
            var testFile = path.PathJoin(Guid.NewGuid().ToString());
            try
            {
                File.WriteAllText(testFile, "");
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                try { testFile.DeleteIfExists(); } catch { }
            }
        }

        internal static string DeleteIfExists(this string path, bool recursive = false)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive);
            else if (File.Exists(path))
                File.Delete(path);
            return path;
        }

        /// <summary>
        /// Ensures the directory exists.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="rethrow">if set to <c>true</c> [rethrow].</param>
        /// <returns>Path of the created/existing directory </returns>
        public static string EnsureDir(this string path, bool rethrow = true)
        {
            try
            {
                Directory.CreateDirectory(path);

                return path;
            }
            catch { if (rethrow) throw; }
            return null;
        }

        /// <summary>
        /// Ensures the parent directory of the file exists.
        /// </summary>
        /// <param name="file">The file path.</param>
        /// <param name="rethrow">if set to <c>true</c> [rethrow].</param>
        /// <returns>Path of the file</returns>
        public static string EnsureFileDir(this string file, bool rethrow = true)
        {
            try
            {
                file.GetDirName().EnsureDir();
                return file;
            }
            catch { if (rethrow) throw; }
            return null;
        }

        /// <summary>
        /// Deletes the directory and its all content.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="handleExceptions">if set to <c>true</c> [handle exceptions].</param>
        /// <returns>The original directory path</returns>
        public static string DeleteDir(this string path, bool handleExceptions = false)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    void del_dir(string d)
                    {
                        try { Directory.Delete(d); }
                        catch (Exception)
                        {
                            Thread.Sleep(1);
                            Directory.Delete(d);
                        }
                    }

                    var dirs = new Queue<string>();
                    dirs.Enqueue(path);

                    while (dirs.Any())
                    {
                        var dir = dirs.Dequeue();

                        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                            File.Delete(file);

                        Directory.GetDirectories(dir, "*", SearchOption.AllDirectories)
                                 .ForEach(dirs.Enqueue);
                    }

                    var emptyDirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                                             .Reverse();

                    emptyDirs.ForEach(del_dir);

                    del_dir(path);
                }
                catch
                {
                    if (!handleExceptions) throw;
                }
            }
            return path;
        }

        /// <summary>
        /// Checks if the file exists.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The result of the test.</returns>
        public static bool FileExists(this string path)
        {
            try { return path.IsNotEmpty() ? File.Exists(path) : false; }
            catch { return false; }
        }

        /// <summary>
        /// Gets the directory name from the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The directory path.</returns>
        public static string GetDirName(this string path)
            => path == null ? null : Path.GetDirectoryName(path);

        /// <summary>
        /// Changes the name of the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>A new path.</returns>
        public static string ChangeFileName(this string path, string fileName) => path.GetDirName().PathJoin(fileName);

        /// <summary>
        /// Gets the file name without the extension.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>File name</returns>
        public static string GetFileNameWithoutExtension(this string path) => Path.GetFileNameWithoutExtension(path);

        /// <summary>
        /// Normalizes directory separators in the given path by ensuring the separators are compatible with the target file system.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>A new normalized path.</returns>
        public static string PathNormaliseSeparators(this string path)
        {
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Gets the subdirectories of the specified directory path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mask">The mask.</param>
        /// <returns>A list of the discovered directories.</returns>
        public static string[] PathGetDirs(this string path, string mask)
        {
            return Directory.GetDirectories(path, mask);
        }

#if !class_lib

        public static bool IsDirSectionSeparator(this string text)
        {
            return text != null && text.StartsWith(csscript.Settings.dirs_section_prefix) && text.StartsWith(csscript.Settings.dirs_section_suffix);
        }

#endif
    }
}