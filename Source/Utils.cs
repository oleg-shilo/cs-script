#region Licence...

//-----------------------------------------------------------------------------
// Date:	25/10/10
// Module:	Utils.cs
// Classes:	...
//
// This module contains the definition of the utility classes used by CS-Script modules
//
// Written by Oleg Shilo (oshilo@gmail.com)
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
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;

using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.CSharp;
using Microsoft.Win32;
using CSScriptLibrary;

namespace csscript
{
    class Runtime
    {
        public static bool IsNet45Plus()
        {
            // Class "ReflectionContext" exists from .NET 4.5 onwards.
            return Type.GetType("System.Reflection.ReflectionContext", false) != null;
        }

        public static bool IsNet40Plus()
        {
            return Environment.Version.Major >= 4;
        }

        public static bool IsNet20Plus()
        {
            return Environment.Version.Major >= 2;
        }

        public static bool IsRuntimeCompatibleAsm(string file)
        {
            try
            {
                System.Reflection.AssemblyName.GetAssemblyName(file);
                return true;
            }
            catch { }
            return false;
        }

        public static bool IsWin
        {
            get { return !IsLinux; }
        }

        public static bool IsLinux
        {
            get
            {
                // Note it is not about OS being exactly Linux but rather about OS having Linux type of file system.
                // For example path being case sensitive
                return (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX);
            }
        }

        static bool isMono = (Type.GetType("Mono.Runtime") != null);

        public static bool IsMono
        {
            get { return isMono; }
        }

        internal static void SetMonoRootDirEnvvar()
        {
            if (Environment.GetEnvironmentVariable("MONO") == null && isMono)
                Environment.SetEnvironmentVariable("MONO", MonoRootDir);
        }

        public static string MonoRootDir
        {
            get
            {
                var runtime = Type.GetType("Mono.Runtime");
                if (runtime != null)
                    try
                    {
                        // C:\Program Files(x86)\Mono\lib\mono\4.5\*.dll
                        // C:\Program Files(x86)\Mono\lib\mono
                        return Path.GetDirectoryName(Path.GetDirectoryName(runtime.Assembly.Location));
                    }
                    catch { }
                return null;
            }
        }

        public static string[] MonoGAC
        {
            get
            {
                try
                {
                    // C:\Program Files(x86)\Mono\lib\mono\gac
                    var gacDir = Path.Combine(MonoRootDir, "gac");
                    return Directory.GetDirectories(gacDir).Select(x => Path.GetFileName(x)).ToArray();
                }
                catch { }
                return new string[0];
            }
        }
    }

    internal class CurrentDirGuard : IDisposable
    {
        string currentDir = Environment.CurrentDirectory;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
                Environment.CurrentDirectory = currentDir;

            disposed = true;
        }

        ~CurrentDirGuard()
        {
            Dispose(false);
        }

        bool disposed = false;
    }

    /// <summary>
    /// Class containing all information about script compilation context and the compilation result.
    /// </summary>
    public class CompilingInfo
    {
        /// <summary>
        /// The script file that the <c>CompilingInfo</c> is associated with.
        /// </summary>
        public string ScriptFile;

        /// <summary>
        /// The script parsing context containing the all CS-Script specific compilation/parsing info (e.g. probing directories,
        /// NuGet packages, compiled sources).
        /// </summary>
        public ScriptParsingResult ParsingContext;

        /// <summary>
        /// The script compilation result.
        /// </summary>
        public CompilerResults Result;

        /// <summary>
        /// The compilation context object that contain all information about the script compilation input
        /// (referenced assemblies, compiler symbols).
        /// </summary>
        public CompilerParameters Input;
    }

    internal static class Utils
    {
        public static char[] LineWhiteSpaceCharacters = " \t\v".ToCharArray();
        public static char[] WhiteSpaceCharacters = " \t\r\n\v".ToCharArray();

        public static void TunnelConditionalSymbolsToEnvironmentVariables(this string directive)
        {
            // "/define:DEBUG"
            // "/d:DEBUG"
            // "-define:DEBUG"
            // "-d:DEBUG"
            // "-d:DEBUG;NET4 -d:PRODUCTION"

            // Need to handle both `-` and  `/`prefixes as older compilers use
            // `/`

            // `;` in compiler options interferes with `//css_...` directives so try to avoid it.
            // Use `-d:DEBUG -d:NET4` instead of `-d:DEBUG;NET4`

            var symbols = directive.Split(' ')
                                   .Where(x => x.HasText() &&
                                               (x.StartsWith("-d:") ||
                                                x.StartsWith("/d:") ||
                                                x.StartsWith("-define:") ||
                                                x.StartsWith("/define:")))
                                   .Select(x => x.Split(':').Last())
                                   .SelectMany(x => x.Split(';'))
                                   .ToArray();

            foreach (string item in symbols)
            {
                if (Environment.GetEnvironmentVariable(item) == null)
                    Environment.SetEnvironmentVariable(item, "true");
            }
        }

        public static Exception ToNewException(this Exception ex, string message, bool encapsulate)
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

        internal static string Expand(this string text)
        {
            return Environment.ExpandEnvironmentVariables(text).Trim();
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

        public static string[] ConcatWith(this string[] array1, IEnumerable<string> array2)
        {
            return array1.Concat(array2).ToArray();
        }

        public static string[] ConcatWith(this string[] array, string item)
        {
            return array.Concat(new[] { item }).ToArray();
        }

        public static string[] ConcatWith(this string item, IEnumerable<string> array)
        {
            return new[] { item }.Concat(array).ToArray();
        }

        public static string[] RemovePathDuplicates(this string[] list)
        {
            return list.Where(x => !string.IsNullOrEmpty(x))
                       .Select(x =>
                               {
                                   var fullPath = Path.GetFullPath(x);
                                   if (File.Exists(fullPath))
                                       return fullPath;
                                   else
                                       return x;
                               })
                       .Distinct().ToArray();
        }

        public static string[] RemoveDuplicates(this string[] list)
        {
            return list.Distinct().ToArray();
        }

        public static bool NotEmpty(this string text)
        {
            return !string.IsNullOrEmpty(text);
        }

        // Mono doesn't like referencing assemblies without dll or exe extension
        public static string EnsureAsmExtension(this string asmName)
        {
            if (asmName != null && Runtime.IsMono)
            {
                if (!asmName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                    !asmName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    asmName = asmName + ".dll";
            }

            return asmName;
        }

        public static IEnumerable<T> Map<T>(this IEnumerable<T> source, params Func<T, T>[] selectors)
        {
            IEnumerable<T> result = source;
            foreach (Func<T, T> sel in selectors)
                result = result.Select(sel);
            return result;
        }

        public static string PathNormaliseSeparators(this string path)
        {
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        //to avoid throwing the exception
        public static string GetAssemblyDirectoryName(this Assembly asm)
        {
            string location = asm.Location();
            return location == "" ? "" : Path.GetDirectoryName(location);
        }

        //to avoid throwing the exception
        public static string Location(this Assembly asm)
        {
            if (asm.IsDynamic())
            {
                string location = Environment.GetEnvironmentVariable("location:" + asm.GetHashCode());
                if (location == null)
                    return "";
                else
                    return location ?? "";
            }
            else
                return asm.Location;
        }

        public static string RemoveAssemblyExtension(this string asmName)
        {
            if (asmName.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) || asmName.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
                return asmName.Substring(0, asmName.Length - 4);
            else
                return asmName;
        }

        public static string PathCombine(this string path, params string[] parts)
        {
#if net35

            string result = (path ?? "");
            foreach (string item in parts)
                result = Path.Combine(result, item);
            return result;
#else
            var allParts = new[] { path ?? "" }.Concat(parts.Select(x => x ?? ""));
            return Path.Combine(allParts.ToArray());
#endif
        }

        public static string GetDirName(this string path)
        {
            return Path.GetDirectoryName(path ?? "");
        }

        public static bool IsSamePath(this string path1, string path2)
        {
            return string.Compare(path1.GetFullPath(), path2.GetFullPath(), Runtime.IsWin) == 0;
        }

        public static bool IsEmpty(this string text)
        {
            return string.IsNullOrEmpty(text);
        }

        public static bool IsNotEmpty(this string text)
        {
            return !string.IsNullOrEmpty(text);
        }

        public static void ClearFile(string path)
        {
            string parentDir = null;

            if (File.Exists(path))
                parentDir = Path.GetDirectoryName(path);

            FileDelete(path, false);

            if (parentDir != null && Directory.GetFiles(parentDir).Length == 0)
                try
                {
                    Directory.Delete(parentDir);
                }
                catch { }
        }

        class Win32
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);
        }

        public static void SetEnvironmentVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);
            if (Runtime.IsWin)
                try { Win32.SetEnvironmentVariable(name, value); } catch { }
        }

        public static void FileDelete(string path)
        {
            FileDelete(path, false);
        }

        public static void CleanUnusedTmpFiles(string dir, string pattern, bool verifyPid)
        {
            if (!Directory.Exists(dir))
                return;

            string[] oldTempFiles = Directory.GetFiles(dir, pattern);

            foreach (string file in oldTempFiles)
            {
                try
                {
                    if (verifyPid)
                    {
                        string name = Path.GetFileName(file);

                        int pos = name.IndexOf('.');

                        if (pos > 0)
                        {
                            string pidValue = name.Substring(0, pos);

                            int pid = 0;

                            if (int.TryParse(pidValue, out pid))
                            {
                                //Didn't use GetProcessById as it throws if pid is not running
                                if (Process.GetProcesses().Any(p => p.Id == pid))
                                    continue; //still running
                            }
                        }
                    }

                    Utils.FileDelete(file);
                }
                catch { }
            }
        }

        //public static Mutex FileLock_(string file, object context)
        //{
        //    if (!IsLinux())
        //        file = file.ToLower(CultureInfo.InvariantCulture);

        //    string mutexName = context.ToString() + "." + CSSUtils.GetHashCodeEx(file).ToString();

        //    if (Utils.IsLinux())
        //    {
        //        //Utils.Ge
        //        //scriptTextCRC = Crc32.Compute(Encoding.UTF8.GetBytes(scriptText));
        //    }

        //    return new Mutex(false, mutexName);
        //}

        //public static bool Wait(Mutex @lock, int millisecondsTimeout)
        //{
        //    return @lock.WaitOne(millisecondsTimeout, false);
        //}

        //public static void ReleaseFileLock(Mutex @lock)
        //{
        //    if (@lock != null)
        //        try { @lock.ReleaseMutex(); }
        //        catch { }
        //}

        public delegate string ProcessNewEncodingHandler(string requestedEncoding);

        public static ProcessNewEncodingHandler ProcessNewEncoding = DefaultProcessNewEncoding;
        public static bool IsDefaultConsoleEncoding = true;

        static string DefaultProcessNewEncoding(string requestedEncoding)
        {
            return requestedEncoding;
        }

        /// <summary>
        /// Waits for file idle.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="delay">The delay.</param>
        /// <returns><c>true</c> if the wait is successful.</returns>
        public static bool WaitForFileIdle(string file, int delay)
        {
            if (file == null || !File.Exists(file)) return true;

            //very conservative "file in use" checker
            int start = Environment.TickCount;

            while ((Environment.TickCount - start) <= delay && IsFileLocked(file))
            {
                Thread.Sleep(200);
            }
            return IsFileLocked(file);
        }

        static bool IsFileLocked(string file)
        {
            try
            {
                using (File.Open(file, FileMode.Open)) { }
            }
            catch (IOException e)
            {
                int errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);

                return errorCode == 32 || errorCode == 33;
            }

            return false;
        }

        public static void FileDelete(string path, bool rethrow)
        {
            //There are the reports about
            //anti viruses preventing file deletion
            //See 18 Feb message in this thread https://groups.google.com/forum/#!topic/cs-script/5Tn32RXBmRE

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
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

        public static Assembly AssemblyLoad(string asmFile)
        {
            try
            {
                return Assembly.LoadFrom(asmFile);
            }
            catch (FileNotFoundException e)
            {
                if (!Runtime.IsMono)
                    throw e;
                else
                    try
                    {
                        // GAC assemblies on Mono are returned as asm partial name not a file path.
                        // So file_not_found failures are expected so try load by name.
                        return Assembly.Load(asmFile);
                    }
                    catch
                    {
                        throw e;
                    }
            }
        }

        public static string DbgFileOf(string assemblyFileName)
        {
            return DbgFileOf(assemblyFileName, Runtime.IsMono);
        }

        internal static string DbgFileOf(string assemblyFileName, bool is_mono)
        {
            // .NET changes the asm extension to '.pdb'
            // Mono adds '.mdb' to the asm file name
            if (is_mono)
                return assemblyFileName + ".mdb";
            else
                return Path.ChangeExtension(assemblyFileName, ".pdb");
        }

        public static bool ContainsPath(string path, string subPath)
        {
            return path.Substring(0, subPath.Length).IsSamePath(subPath);
        }

        public static bool IsNullOrWhiteSpace(string text)
        {
#if net4
            return string.IsNullOrWhiteSpace(text);
#else
            return text == null || text.Trim() == "";
#endif
        }

        /// <summary>
        /// Adds compiler options to the CompilerParameters in a manner that it does separate every option by the space character
        /// </summary>
        static public void AddCompilerOptions(CompilerParameters compilerParams, string option)
        {
            compilerParams.CompilerOptions += option + " ";
        }
    }

    internal static class CSSUtils
    {
        internal static void VerbosePrint(this string message, ExecuteOptions options)
        {
            if (options.verbose)
                Console.WriteLine(message);
        }

        public static string DbgInjectionCode = DbgInjectionCodeInterface;

        internal static string DbgInjectionCodeInterface = @"// Auto-generated file

public static class dbg_extensions
{
    static public T dump<T>(this T @object, params object[] args)
    {
        dbg.print(@object, args);
        return @object;
    }

    static public T print<T>(this T @object, params object[] args)
    {
        dbg.print(@object, args);
        return @object;
    }
}

partial class dbg
{
    public static bool publicOnly = true;
    public static bool propsOnly = false;
    public static int max_items = 25;
    public static int depth = 1;
    public static void printf(string format, params object[] args) { }
    public static void print(object @object, params object[] args) { }
}";

        internal static string CreateDbgInjectionInterfaceCode(string scriptFileName)
        {
            var file = CSExecutor.GetScriptTempDir().PathCombine("Cache", "dbg.cs");

            try { File.WriteAllText(file, DbgInjectionCodeInterface); }
            catch { }
            return file;
        }

        internal static string GetScriptedCodeDbgInjectionCode(string scriptFileName)
        {
            if (DbgInjectionCode == null)
                return null;

            string dbg_injection_version = DbgInjectionCode.GetHashCode().ToString();

            using (SystemWideLock fileLock = new SystemWideLock("CS-Script.dbg.injection", dbg_injection_version))
            {
                //Infinite timeout is not good choice here as it may block forever but continuing while the file is still locked will
                //throw a nice informative exception.
                if (!Runtime.IsLinux)
                    fileLock.Wait(1000);

                var cache_dir = Path.Combine(CSExecutor.GetScriptTempDir(), "Cache");
                var dbg_file = Path.Combine(cache_dir, "dbg.inject." + dbg_injection_version + ".cs");
                var dbg_interface_file = Path.Combine(cache_dir, "dbg.cs");

                if (!File.Exists(dbg_file))
                    File.WriteAllText(dbg_file, DbgInjectionCode);

                CreateDbgInjectionInterfaceCode(scriptFileName);

                foreach (var item in Directory.GetFiles(cache_dir, "dbg.inject.*.cs"))
                    if (item != dbg_file)
                        try
                        {
                            File.Delete(item);
                        }
                        catch { }

                return dbg_file;
            }
        }

        internal static string GetScriptedCodeAttributeInjectionCode(string scriptFileName)
        {
            using (SystemWideLock fileLock = new SystemWideLock(scriptFileName, "attr"))
            {
                //Infinite timeout is not good choice here as it may block forever but continuing while the file is still locked will
                //throw a nice informative exception.
                if (Runtime.IsWin)
                    fileLock.Wait(1000);

                string code = string.Format("[assembly: System.Reflection.AssemblyDescriptionAttribute(@\"{0}\")]", scriptFileName);

                string currentCode = "";

                string file = Path.Combine(CSExecutor.GetCacheDirectory(scriptFileName), Path.GetFileNameWithoutExtension(scriptFileName) + ".attr.g.cs");

                Exception lastError = null;

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        if (File.Exists(file))
                            using (StreamReader sr = new StreamReader(file))
                                currentCode = sr.ReadToEnd();

                        if (currentCode != code)
                        {
                            string dir = Path.GetDirectoryName(file);

                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);

                            using (StreamWriter sw = new StreamWriter(file)) //there were reports about the files being locked. Possibly by csc.exe so allow retry
                            {
                                sw.Write(code);
                            }
                        }
                        break;
                    }
                    catch (Exception e)
                    {
                        lastError = e;
                    }
                    Thread.Sleep(200);
                }

                if (!File.Exists(file))
                    throw new ApplicationException("Failed to create AttributeInjection file", lastError);

                return file;
            }
        }

        public static bool HaveSameTimestamp(string file1, string file2)
        {
            FileInfo info1 = new FileInfo(file1);

            FileInfo info2 = new FileInfo(file2);

            return (info2.LastWriteTime == info1.LastWriteTime &&
                    info2.LastWriteTimeUtc == info1.LastWriteTimeUtc);
        }

        public static void SetTimestamp(string fileDest, string fileSrc)
        {
            FileInfo info1 = new FileInfo(fileSrc);
            FileInfo info2 = new FileInfo(fileDest);

            try
            {
                info2.LastWriteTime = info1.LastWriteTime;
                info2.LastWriteTimeUtc = info1.LastWriteTimeUtc;
            }
            catch
            {
                //On Linux it may fail for no obvious reason
            }
        }

        /// <summary>
        /// Compiles ResX file into .resources
        /// </summary>
        public static string CompileResource(string file, string out_name)
        {
            var resgen_exe = "ResGen.exe";

            var input = file;
            var output = Path.ChangeExtension(file, ".resources");
            if (out_name != null)
                output = Path.Combine(Path.GetDirectoryName(file), out_name);

            string css_dir_res_gen = Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\lib\resgen.exe");
            string user_res_gen = Environment.GetEnvironmentVariable("CSS_RESGEN");
            if (File.Exists(user_res_gen))
                resgen_exe = user_res_gen;
            else if (File.Exists(css_dir_res_gen))
                resgen_exe = css_dir_res_gen;

            var error = new StringBuilder();

            try
            {
                var proc = new Process();
                proc.StartInfo.FileName = resgen_exe;
                proc.StartInfo.Arguments = "\"" + input + "\" \"" + output + "\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(input);
                proc.Start();

                string line = null;
                while (null != (line = proc.StandardError.ReadLine()))
                    error.AppendLine(line);

                proc.WaitForExit();
            }
            catch (Exception e)
            {
                if (!File.Exists(css_dir_res_gen))
                    throw new ApplicationException("Cannot invoke " + resgen_exe + ": " + e.Message +
                                                   "\nEnsure resgen.exe is in the %CSSCRIPT_DIR%\\lib or " +
                                                   "its location is in the system PATH. Alternatively you " +
                                                   "can specify the direct location of resgen.exe via " +
                                                   "CSS_RESGEN environment variable.");
            }

            if (error.Length > 0)
                throw new ApplicationException("Cannot compile resources: " + error);

            return output;
        }

        public delegate void ShowDocumentHandler();

        static public string[] GetDirectories(string workingDir, string rootDir)
        {
            if (!Path.IsPathRooted(rootDir))
                rootDir = Path.Combine(workingDir, rootDir); //cannot use Path.GetFullPath as it crashes if '*' or '?' are present

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
        {
            return new Regex(pattern.WildCardToRegExpPattern(), Runtime.IsWin ? RegexOptions.IgnoreCase : RegexOptions.None);
        }

        //Credit to MDbg team: https://github.com/SymbolSource/Microsoft.Samples.Debugging/blob/master/src/debugger/mdbg/mdbgCommands.cs
        public static string WildCardToRegExpPattern(this string simpleExp)
        {
            StringBuilder sb = new StringBuilder();
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

        internal class Args
        {
            static internal string Join(params string[] args)
            {
                StringBuilder sb = new StringBuilder();

                foreach (string arg in args)
                {
                    sb.Append(" ");
                    sb.Append("-");
                    sb.Append(arg);
                }
                return sb.ToString().Trim();
            }

            static internal string DefaultPrefix
            {
                get
                {
                    return Runtime.IsLinux ? "-" : "/";
                }
            }

            public static bool Same(string arg, params string[] patterns)
            {
                foreach (string pattern in patterns)
                {
                    if (arg.StartsWith("-"))
                        if (arg.Length == pattern.Length + 1 && arg.IndexOf(pattern) == 1)
                            return true;

                    if (Runtime.IsWin && arg[0] == '/')
                        if (arg.Length == pattern.Length + 1 && arg.IndexOf(pattern) == 1)
                            return true;
                }
                return false;
            }

            public static bool IsArg(string arg)
            {
                if (arg.StartsWith("-"))
                    return true;
                if (Runtime.IsWin)
                    return (arg[0] == '/');
                return false;
            }

            public static bool StartsWith(string arg, string pattern)
            {
                if (arg.StartsWith("-"))
                    return arg.IndexOf(pattern) == 1;
                if (Runtime.IsWin)
                    if (arg[0] == '/')
                        return arg.IndexOf(pattern) == 1;
                return false;
            }

            public static string ArgValue(string arg, string pattern)
            {
                return arg.Substring(pattern.Length + 1);
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

            public static bool ParseValuedArg(string arg, string pattern, string pattern2, out string value)
            {
                value = null;

                if (ParseValuedArg(arg, pattern, out value))
                    return true;

                if (ParseValuedArg(arg, pattern2, out value))
                    return true;

                return false;
            }

            // detects pseudo arguments - script files named as args (e.g. '-update')
            // disabled as currently every arg that starts with '-' is treated as scripted arg (script file)
            // public static bool IsScriptedArg(string arg)
            // {
            //     var rootDir = Path.GetFullPath(Assembly.GetExecutingAssembly().Location());
            //     if (!string.IsNullOrEmpty(rootDir))
            //         rootDir = Path.GetDirectoryName(rootDir);

            //     if (File.Exists(rootDir.PathCombine(arg)) || File.Exists(rootDir.PathCombine(arg + ".cs")))
            //         return true;
            //     else
            //         return false;
            // }
        }

        static internal int FirstScriptArg(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg[0] != '-' && File.Exists(arg))
                    return i; //on Linux '/' may indicate dir but not command
            }
            return -1;
        }

        /// <summary>
        /// Parses application (script engine) arguments.
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <param name="executor">Script executor instance</param>
        /// <returns>Index of the first script argument.</returns>
        static internal int ParseAppArgs(string[] args, IScriptExecutor executor)
        {
            // NOTE: it is expected that arguments are passed multiple times during the session.
            // E.g. first time from command line, second time for the DefaultArguments from the config file, that
            // has been specified from the command line args.
            // CLIExitRequest.Throw() is an exception based mechanism for unconditional application exit.

            ExecuteOptions options = executor.GetOptions();
            //Debug.Assert(false);
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string nextArg = null;
                if ((i + 1) < args.Length)
                    nextArg = args[i + 1];

                if (Args.IsArg(arg) && AppArgs.Supports(arg) && AppArgs.IsHelpRequest(nextArg))
                {
                    executor.ShowHelpFor(arg.Substring(1)); //skip prefix
                    CLIExitRequest.Throw();
                }

                if (arg[0] != '-' && File.Exists(arg))
                    return i; //on Linux '/' may indicate dir but not command

                string argValue = null;

                if (Args.IsArg(arg))
                {
                    if (Args.Same(arg, AppArgs.nl)) // -nl
                    {
                        // '-nl' has been made obsolete.
                        // Just continue to let the legacy setting/args pass through without affecting the execution.
                        // options.noLogo = true;
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.@out, out argValue)) // -out:
                    {
                        if (argValue != null)
                            options.forceOutputAssembly = Environment.ExpandEnvironmentVariables(argValue);
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.c, out argValue)) // -c:<value>
                    {
                        if (!options.suppressExecution) // do not change the value if compilation is the objective
                        {
                            if (argValue == "1" || argValue == null)
                                options.useCompiled = true;
                            else if (argValue == "0")
                                options.useCompiled = false;
                        }
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.inmem, out argValue)) // -inmem:<value>
                    {
                        if (!options.suppressExecution) // do not change the value if compilation is the objective
                        {
                            if (argValue == "1" || argValue == null)
                                options.inMemoryAsm = true;
                            else if (argValue == "0")
                                options.inMemoryAsm = false;
                        }
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.dbgprint, out argValue)) // -dbgprint:<value>
                    {
                        if (argValue == "1" || argValue == null)
                            options.enableDbgPrint = true;
                        else if (argValue == "0")
                            options.enableDbgPrint = false;
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.sconfig, out argValue)) // -sconfig:file
                    {
                        options.useScriptConfig = true;
                        if (argValue == "none")
                        {
                            options.useScriptConfig = false;
                        }
                        else if (argValue != null)
                        {
                            options.customConfigFileName = argValue;
                        }
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.provider, AppArgs.pvdr, out argValue)) // -provider:file
                    {
                        if (argValue != null)
                            options.altCompiler = Environment.ExpandEnvironmentVariables(argValue);
                    }
                    else if (Args.Same(arg, AppArgs.verbose))
                    {
                        options.verbose = true;
                    }
                    //else if (Args.Same(arg, AppArgs.preload)) // -preload
                    //else if (Args.Same(arg, "-preload")) // -preload
                    //{
                    //    options.useCompiled = false;
                    //    //CSScript.PreloadCompiler
                    //    //CLIExitRequest.Throw();
                    //}
                    else if (Args.ParseValuedArg(arg, AppArgs.dir, out argValue)) // -dir:path1,path2
                    {
                        if (argValue != null)
                        {
                            if (argValue == "show" && nextArg == null)
                            {
                                executor.ShowHelp(AppArgs.dir, options);
                                CLIExitRequest.Throw();
                            }
                            else
                            {
                                foreach (string dir in argValue.Split(','))
                                    options.AddSearchDir(dir.Trim(), Settings.cmd_dirs_section);
                            }
                        }
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.precompiler, AppArgs.pc, out argValue)) // -precompiler:file1,file2
                    {
                        if (argValue != null && argValue != "print")
                        {
                            options.preCompilers = argValue;
                        }
                        else
                        {
                            executor.ShowPrecompilerSample();
                            CLIExitRequest.Throw();
                        }
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.cache, out argValue)) // -cache[:<ls|trim|clear>]
                    {
                        executor.DoCacheOperations(argValue);
                        CLIExitRequest.Throw();
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.wait, out argValue)) // -wait
                    {
                        if (argValue != null)
                            executor.WaitForInputBeforeExit = argValue;
                        else
                            executor.WaitForInputBeforeExit = "Press any key to continue . . .";
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.config, out argValue)) // -config:<file>
                    {
                        if (!options.suppressExecution) // do not change the value if compilation is the objective
                        {
                            //-config:none             - ignore config file (use default settings)
                            //-config:create           - create config file with default settings
                            //-config:default          - print default config file
                            //-config:raw              - print current config file content
                            //-config:xml              - print current config file content
                            //-config:ls               - lists/prints current config values
                            //-config                  - lists/prints current config values
                            //-config:get:name         - print current config file value
                            //-config:set:name:value   - set current config file value
                            //-config:<file>           - use custom config file

                            if (argValue == null ||
                            argValue == "create" ||
                            argValue == "default" ||
                            argValue == "ls" ||
                            argValue == "raw" ||
                            argValue == "xml" ||
                            argValue.StartsWith("get:") ||
                            argValue.StartsWith("set:"))
                            {
                                // Debug.Assert(false);
                                executor.ProcessConfigCommand(argValue);
                                CLIExitRequest.Throw();
                            }
                            if (argValue == "none")
                            {
                                options.noConfig = true;
                            }
                            else
                            {
                                options.noConfig = true;
                                options.altConfig = argValue;
                            }
                        }
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.autoclass, AppArgs.ac, out argValue)) // -autoclass -ac
                    {
                        if (argValue == null)
                        {
                            options.autoClass = true;
                        }
                        else if (argValue == "out")
                        {
                            executor.PrintDecoratedAutoclass(nextArg);
                            CLIExitRequest.Throw();
                        }
                        else if (argValue == "0")
                        {
                            options.autoClass = false;
                        }
                        else
                        {
                            if (argValue == "1")
                            {
                                options.autoClass = true;
                            }
                            else if (argValue == "2")
                            {
                                options.autoClass = true;
                                options.autoClass_InjectBreakPoint = true;
                            }
                        }
                    }
                    else if (Args.Same(arg, AppArgs.nathash))
                    {
                        //-nathash //native hashing; by default it is deterministic but slower custom string hashing algorithm
                        //it is a hidden option for the cases when faster hashing is desired
                        options.customHashing = false;
                    }
                    else if (Args.Same(arg, AppArgs.check)) // -check
                    {
                        options.useCompiled = false;
                        options.forceCompile = true;
                        options.suppressExecution = true;
                        options.syntaxCheck = true;
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.proj, out argValue)) // -proj
                    {
                        // Requests for some no-dependency operations can be handled here
                        // e.g. ShowHelp
                        // But others like ShowProject need to be processed outside of this
                        // arg parser (at the caller) as the whole list of parsed arguments
                        // may be required for handling request.
                        options.nonExecuteOpRquest = AppArgs.proj;
                        if (argValue == "dbg")
                            options.nonExecuteOpRquest = AppArgs.proj_dbg;
                        options.processFile = false;
                        //options.processFile = false;
                        //options.forceOutputAssembly = ;
                    }
                    else if (Args.Same(arg, AppArgs.ca)) // -ca
                    {
                        options.useCompiled = true;
                        options.forceCompile = true;
                        options.suppressExecution = true;
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.co, out argValue)) // -co:<value>
                    {
                        if (argValue != null)
                        {
                            argValue.TunnelConditionalSymbolsToEnvironmentVariables();

                            //this one is accumulative
                            if (!options.compilerOptions.Contains(argValue))
                                options.compilerOptions += " " + argValue;
                        }
                    }
                    else if (Args.Same(arg, AppArgs.cd)) // -cd
                    {
                        options.suppressExecution = true;
                        options.DLLExtension = true;
                    }
                    else if (Args.Same(arg, AppArgs.tc)) // -tc
                    {
                        Environment.SetEnvironmentVariable("CSS_PROVIDER_TRACE", "true");
                    }
                    else if (Args.Same(arg, AppArgs.dbg, AppArgs.d)) // -dbg -d
                    {
                        Environment.SetEnvironmentVariable("DEBUG", "true");
                        options.DBG = true;
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.l, out argValue)) // -l:<1|0>
                    {
                        options.local = (argValue != "0");
                    }
                    else if (Args.Same(arg, AppArgs.ver, AppArgs.v, AppArgs.version, AppArgs.version2)) // -ver -v -version --version
                    {
                        executor.ShowVersion();
                        CLIExitRequest.Throw();
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.nuget, out argValue)) // -nuget[:<package>]
                    {
                        if (argValue.NotEmpty())
                            NuGet.InstallPackage(argValue);
                        else
                            NuGet.ListPackages();
                        CLIExitRequest.Throw();
                    }
                    else if (Args.Same(arg, AppArgs.stop)) // -stop
                    {
                        StopVBCSCompilers();
                        StopSyntaxer();
                        CLIExitRequest.Throw();
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.r, out argValue)) // -r:file1,file2
                    {
                        if (argValue != null)
                        {
                            string[] assemblies = argValue.Split(",;".ToCharArray());
                            options.refAssemblies = assemblies;
                        }
                    }
                    else if (Args.Same(arg, AppArgs.e, AppArgs.ew)) // -e -ew
                    {
                        options.buildExecutable = true;
                        options.suppressExecution = true;
                        if (Args.Same(arg, AppArgs.ew)) // -ew
                            options.buildWinExecutable = true;
                    }
                    else if (Args.Same(arg, AppArgs.question, AppArgs.help, AppArgs.help2)) // -? -help --help
                    {
                        executor.ShowHelpFor(nextArg);
                        CLIExitRequest.Throw();
                    }
                    else if (Args.Same(arg, AppArgs.syntax)) // -syntax
                    {
                        executor.ShowHelp(AppArgs.syntax);
                        CLIExitRequest.Throw();
                    }
                    else if (Args.Same(arg, AppArgs.cmd, AppArgs.commands)) // -cmd -commands
                    {
                        executor.ShowHelp(AppArgs.commands);
                        CLIExitRequest.Throw();
                    }
                    else if (Args.ParseValuedArg(arg, AppArgs.s, AppArgs.sample, out argValue)) // -s:<C# version>
                    {
                        executor.ShowSample(argValue);
                        CLIExitRequest.Throw();
                    }
                    else
                    {
                        // at this stage it's safe to assume that if arg starts with '-' but cannot be matched it is always
                        // a script file ("pseudo arg")
                        // if (Args.IsScriptedArg(arg))
                        return i;
                    }
                }
                else
                {
                    return i;
                }
            }

            return args.Length;
        }

        delegate bool CompileMethod(ref string content, string scriptFile, bool IsPrimaryScript, Hashtable context);

        delegate bool CompileMethodDynamic(object context);

        internal static PrecompilationContext Precompile(string scriptFile, string[] filesToCompile, ExecuteOptions options)
        {
            PrecompilationContext context = new PrecompilationContext();
            context.SearchDirs = options.searchDirs;

            Hashtable contextData = new Hashtable();
            contextData["NewDependencies"] = context.NewDependencies;
            contextData["NewSearchDirs"] = context.NewSearchDirs;
            contextData["NewReferences"] = context.NewReferences;
            contextData["NewIncludes"] = context.NewIncludes;
            contextData["NewCompilerOptions"] = "";
            contextData["SearchDirs"] = context.SearchDirs;
            contextData["ConsoleEncoding"] = options.consoleEncoding;
            contextData["CompilerOptions"] = options.compilerOptions;

            Dictionary<string, List<object>> precompilers = CSSUtils.LoadPrecompilers(options);

            if (precompilers.Count != 0)
            {
                for (int i = 0; i < filesToCompile.Length; i++)
                {
                    string content = File.ReadAllText(filesToCompile[i]);

                    context.Content = content;
                    context.scriptFile = filesToCompile[i];
                    context.IsPrimaryScript = (filesToCompile[i] == scriptFile);

                    bool modified = false;

                    foreach (string precompilerFile in precompilers.Keys)
                    {
                        foreach (object precompiler in precompilers[precompilerFile])
                        {
                            if (options.verbose && i == 0)
                            {
                                CSSUtils.VerbosePrint("  Precompilers: ", options);
                                int index = 0;

                                foreach (string file in filesToCompile)
                                    CSSUtils.VerbosePrint("   " + index++ + " - " + Path.GetFileName(file) + " -> " + precompiler.GetType() + "\n           from " + precompilerFile, options);
                                CSSUtils.VerbosePrint("", options);
                            }

                            MethodInfo method = precompiler.GetType().GetMethod("Compile");

                            CompileMethod compile = (CompileMethod)Delegate.CreateDelegate(typeof(CompileMethod), method);

                            bool result = compile(ref content,
                                                  filesToCompile[i],
                                                  filesToCompile[i] == scriptFile,
                                                  contextData);

                            if (result)
                            {
                                context.NewDependencies.Add(precompilerFile);
                                modified = true;
                            }
                        }
                    }

                    if (modified)
                    {
                        filesToCompile[i] = CSSUtils.SaveAsAutogeneratedScript(content, filesToCompile[i]);
                    }
                }
            }

            context.NewCompilerOptions = (string)contextData["NewCompilerOptions"];
            options.searchDirs = options.searchDirs.ConcatWith(context.NewSearchDirs);

            foreach (string asm in context.NewReferences)
                options.defaultRefAssemblies += "," + asm; //the easiest way to inject extra references is to merge them with the extra assemblies already specified by user

            return context;
        }

        internal const string noDefaultPrecompilerSwitch = "nodefault";

        internal static Dictionary<string, List<object>> LoadPrecompilers(ExecuteOptions options)
        {
            Dictionary<string, List<object>> retval = new Dictionary<string, List<object>>();

            if (!options.preCompilers.StartsWith(noDefaultPrecompilerSwitch)) //no defaults
                retval.Add(Assembly.GetExecutingAssembly().Location, new List<object>() { new DefaultPrecompiler() });

            if (options.autoClass)
            {
                bool canHandleCShar6 = (!string.IsNullOrEmpty(options.altCompiler) || Runtime.IsLinux);

                AutoclassPrecompiler.decorateAutoClassAsCS6 = (options.decorateAutoClassAsCS6 && options.enableDbgPrint && canHandleCShar6);

                AutoclassPrecompiler.injectBreakPoint = options.autoClass_InjectBreakPoint;
                if (retval.ContainsKey(Assembly.GetExecutingAssembly().Location))
                    retval[Assembly.GetExecutingAssembly().Location].Add(new AutoclassPrecompiler());
                else
                    retval.Add(Assembly.GetExecutingAssembly().Location, new List<object>() { new AutoclassPrecompiler() });
            }

            foreach (string precompiler in Utils.RemoveDuplicates((options.preCompilers).Split(new char[] { ',' })))
            {
                string precompilerFile = precompiler.Trim();

                if (precompilerFile != "" && precompilerFile != noDefaultPrecompilerSwitch)
                {
                    string sourceFile = FindImlementationFile(precompilerFile, options.searchDirs);

                    if (sourceFile == null)
                        throw new ApplicationException("Cannot find Precompiler file " + precompilerFile);

                    Assembly asm;

                    if (sourceFile.EndsWith(".dll", true, CultureInfo.InvariantCulture))
                        asm = Assembly.LoadFrom(sourceFile);
                    else
                        asm = CompilePrecompilerScript(sourceFile, options.searchDirs);

                    object precompilerObj = null;

                    foreach (Module m in asm.GetModules())
                    {
                        if (precompilerObj != null)
                            break;

                        foreach (Type t in m.GetTypes())
                        {
                            if (t.Name.EndsWith("Precompiler"))
                            {
                                precompilerObj = asm.CreateInstance(t.Name);
                                if (precompilerObj == null)
                                    throw new Exception("Precompiler " + sourceFile + " cannot be loaded. CreateInstance returned null.");
                                break;
                            }
                        }
                    }

                    if (precompilerObj != null)
                        retval.Add(sourceFile, new List<object>() { precompilerObj });
                }
            }

            return retval;
        }

        public static string FindFile(string file, string[] searchDirs)
        {
            if (File.Exists(file))
            {
                return Path.GetFullPath(file);
            }
            else if (!Path.IsPathRooted(file))
            {
                foreach (string dir in searchDirs)
                    if (File.Exists(Path.Combine(dir, file)))
                        return Path.Combine(dir, file);
            }

            return null;
        }

        public static string FindImlementationFile(string file, string[] searchDirs)
        {
            string retval = FindFile(file, searchDirs);

            if (retval == null && !Path.HasExtension(file))
            {
                retval = FindFile(file + ".cs", searchDirs);
                if (retval == null)
                    retval = FindFile(file + ".dll", searchDirs);
            }

            return retval;
        }

        internal static void StopVBCSCompilers()
        {
            kill("VBCSCompiler");
        }

        static void kill(string proc_name)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(proc_name))
                    try { p.Kill(); }
                    catch { } //cannot analyse main module as it may not be accessible for x86 vs. x64 reasons
            }
            catch { }
        }

        internal static void StopSyntaxer()
        {
            kill("syntaxer");
        }

        internal static string[] CollectPrecompillers(CSharpParser parser, ExecuteOptions options)
        {
            List<string> allPrecompillers = new List<string>();

            allPrecompillers.AddRange(options.preCompilers.Split(','));

            foreach (string item in parser.Precompilers)
                allPrecompillers.AddRange(item.Split(','));

            return Utils.RemoveDuplicates(allPrecompillers.ToArray());
        }

        internal static int GenerateCompilationContext(CSharpParser parser, ExecuteOptions options)
        {
            string[] allPrecompillers = CollectPrecompillers(parser, options);

            var sb = new StringBuilder();

            foreach (string file in allPrecompillers)
            {
                if (file != "")
                {
                    sb.Append(FindImlementationFile(file, options.searchDirs));
                    sb.Append(",");
                }
            }

            sb.Append(",");
            sb.Append(options.compilerOptions); // parser.CompilerOptions can be ignored as if they are changed the whole script timestamp is also changed
            sb.Append(string.Join("|", options.searchDirs)); // "Incorrect work of cache #86"

            return CSSUtils.GetHashCodeEx(sb.ToString());
        }

        public static string[] GetAppDomainAssemblies()
        {
            return (from a in AppDomain.CurrentDomain.GetAssemblies()
                    let location = a.Location()
                    where location != "" && !a.GlobalAssemblyCache
                    select location).ToArray();
        }

        public static bool IsDynamic(this Assembly asm)
        {
            //http://bloggingabout.net/blogs/vagif/archive/2010/07/02/net-4-0-and-notsupportedexception-complaining-about-dynamic-assemblies.aspx
            //Will cover both System.Reflection.Emit.AssemblyBuilder and System.Reflection.Emit.InternalAssemblyBuilder
            return asm.GetType().FullName.EndsWith("AssemblyBuilder") || asm.Location == null || asm.Location == "";
        }

        public static Assembly CompilePrecompilerScript(string sourceFile, string[] searchDirs)
        {
            try
            {
                var asmExtension = Runtime.IsMono ? ".dll" : ".compiled";
                string precompilerAsm = Path.Combine(CSExecutor.GetCacheDirectory(sourceFile), Path.GetFileName(sourceFile) + asmExtension);

                using (Mutex fileLock = new Mutex(false, "CSSPrecompiling." + CSSUtils.GetHashCodeEx(precompilerAsm))) //have to use hash code as path delimiters are illegal in the mutex name
                {
                    //let other thread/process (if any) to finish loading/compiling the same file; 3 seconds should be enough
                    //if not we will just fail to compile as precompilerAsm will still be locked.
                    //Infinite timeout is not good choice here as it may block forever but continuing while the file is still locked will
                    //throw a nice informative exception.
                    fileLock.WaitOne(3000, false);

                    if (File.Exists(precompilerAsm))
                    {
                        if (File.GetLastWriteTimeUtc(sourceFile) <= File.GetLastWriteTimeUtc(precompilerAsm))
                            return Assembly.LoadFrom(precompilerAsm);

                        Utils.FileDelete(precompilerAsm, true);
                    }

                    ScriptParser parser = new ScriptParser(sourceFile, searchDirs);

                    CompilerParameters compilerParams = new CompilerParameters();

                    compilerParams.IncludeDebugInformation = true;
                    compilerParams.GenerateExecutable = false;
                    compilerParams.GenerateInMemory = false;
                    compilerParams.OutputAssembly = precompilerAsm;

                    List<string> refAssemblies = new List<string>();

                    //add local and global assemblies (if found) that have the same assembly name as a namespace
                    foreach (string nmSpace in parser.ReferencedNamespaces)
                        foreach (string asm in AssemblyResolver.FindAssembly(nmSpace, searchDirs))
                        {
                            refAssemblies.Add(asm.EnsureAsmExtension());
                        }

                    //add assemblies referenced from code
                    foreach (string asmName in parser.ReferencedAssemblies)
                        if (asmName.StartsWith("\"") && asmName.EndsWith("\"")) //absolute path
                        {
                            //not-searchable assemblies
                            string asm = asmName.Replace("\"", "");
                            refAssemblies.Add(asm);
                        }
                        else
                        {
                            string nameSpace = asmName.RemoveAssemblyExtension();

                            string[] files = AssemblyResolver.FindAssembly(nameSpace, searchDirs);

                            if (files.Length > 0)
                            {
                                foreach (string asm in files)
                                {
                                    refAssemblies.Add(asm.EnsureAsmExtension());
                                }
                            }
                            else
                            {
                                refAssemblies.Add(nameSpace + ".dll");
                            }
                        }

                    try
                    {
                        compilerParams.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
                    }
                    catch { }

                    ////////////////////////////////////////
                    foreach (string asm in Utils.RemovePathDuplicates(refAssemblies.ToArray()))
                    {
                        compilerParams.ReferencedAssemblies.Add(asm);
                    }

#pragma warning disable 618
                    CompilerResults result = new CSharpCodeProvider().CreateCompiler().CompileAssemblyFromFile(compilerParams, sourceFile);
#pragma warning restore 618

                    if (result.Errors.HasErrors)
                        throw CompilerException.Create(result.Errors, true, false);

                    if (!File.Exists(precompilerAsm))
                        throw new Exception("Unknown building error");

                    File.SetLastWriteTimeUtc(precompilerAsm, File.GetLastWriteTimeUtc(sourceFile));

                    Assembly retval = Assembly.LoadFrom(precompilerAsm);

                    return retval;
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException("Cannot load precompiler " + sourceFile + ": " + e.Message);
            }
        }

        static public bool IsRuntimeErrorReportingSuppressed
        {
            get
            {
                // old silly typo
                return Environment.GetEnvironmentVariable("CSS_IsRuntimeErrorReportingSuppressed") != null ||
                       Environment.GetEnvironmentVariable("CSS_IsRuntimeErrorReportingSupressed") != null;
            }
        }

        public static int GetHashCodeEx(string s)
        {
            //during the script first compilation GetHashCodeEx is called ~10 times
            //during the cached execution ~5 times only
            //and for hosted scenarios it is twice less

            //The following profiling demonstrates that in the worst case scenario hashing would
            //only add ~2 microseconds to the execution time

            //Native executions cost (milliseconds)=> 100000: 7; 10 : 0.0007
            //Custom Safe executions cost (milliseconds)=> 100000: 40; 10: 0.004
            //Custom Unsafe executions cost (milliseconds)=> 100000: 13; 10: 0.0013

            if (ExecuteOptions.options.customHashing)
            {
                //deterministic GetHashCode; useful for integration with third party products (e.g. CS-Script.Npp)
                return GetHashCode32(s);
            }
            else
            {
                return s.GetHashCode();
            }
        }

        //needed to have reliable HASH as x64 and x32 have different algorithms; This leads to the inability of script clients calculate cache directory correctly
        static int GetHashCode32(string s)
        {
            char[] chars = s.ToCharArray();

            int lastCharInd = chars.Length - 1;

            int num1 = 0x15051505;

            int num2 = num1;

            int ind = 0;

            while (ind <= lastCharInd)
            {
                char ch = chars[ind];

                char nextCh = ++ind > lastCharInd ? '\0' : chars[ind];
                num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ (nextCh << 16 | ch);
                if (++ind > lastCharInd)
                    break;
                ch = chars[ind];
                nextCh = ++ind > lastCharInd ? '\0' : chars[ind++];
                num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ (nextCh << 16 | ch);
            }
            return num1 + num2 * 0x5d588b65;
        }

        // disabled just in case
        //public static unsafe int GetHashCode32Unsafe(string s)
        //{
        //    fixed (char* str = s.ToCharArray())
        //    {
        //        char* chPtr = str;
        //        int num = 0x15051505;
        //        int num2 = num;
        //        int* numPtr = (int*)chPtr;
        //        for (int i = s.Length; i > 0; i -= 4)
        //        {
        //            num = (((num << 5) + num) + (num >> 0x1b)) ^ numPtr[0];
        //            if (i <= 2)
        //            {
        //                break;
        //            }
        //            num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr[1];
        //            numPtr += 2;
        //        }
        //        return (num + (num2 * 0x5d588b65));
        //    }
        //}

        public static string SaveAsAutogeneratedScript(string content, string originalFileName)
        {
            string autogenFile = Path.Combine(CSExecutor.GetCacheDirectory(originalFileName), Path.GetFileNameWithoutExtension(originalFileName) + ".g" + Path.GetExtension(originalFileName));

            if (File.Exists(autogenFile))
                File.SetAttributes(autogenFile, FileAttributes.Normal);

            using (StreamWriter sw = new StreamWriter(autogenFile, false, Encoding.UTF8))
                sw.Write(content);

            File.SetAttributes(autogenFile, FileAttributes.ReadOnly);
            return autogenFile;
        }

        public static string GenerateAutoclass(string file)
        {
            StringBuilder code = new StringBuilder(4096);
            code.AppendLine("//Auto-generated file");

            bool headerProcessed = false;

            string line;

            using (StreamReader sr = new StreamReader(file, Encoding.UTF8))
                while ((line = sr.ReadLine()) != null)
                {
                    if (!headerProcessed && !line.TrimStart().StartsWith("using ")) //not using...; statement of the file header
                        if (!line.StartsWith("//") && line.Trim() != "") //not comments or empty line
                        {
                            headerProcessed = true;
                            code.AppendLine("   public class ScriptClass");
                            code.AppendLine("   {");
                            code.Append("   static public ");
                        }

                    code.AppendLine(line);
                }

            code.AppendLine("   }");

            string autogenFile = SaveAsAutogeneratedScript(code.ToString(), file);

            return autogenFile;
        }

        static public void NormaliseFileReference(ref string file, ref int line)
        {
            try
            {
                if (file.EndsWith(".g.csx") || file.EndsWith(".g.cs") && file.Contains(Path.Combine("CSSCRIPT", "Cache")))
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
    }

    #region MetaDataItems...

    /// <summary>
    /// The MetaDataItems class contains information about script dependencies (referenced local
    /// assemblies and imported scripts) and compiler options. This information is required when
    /// scripts are executed in a 'cached' mode (/c switch). On the base of this information the script
    /// engine will compile new version of .compiled assembly if any of it's dependencies is changed. This
    /// is required even for referenced local assemblies as it is possible that they are a strongly
    /// named assemblies (recompiling is required for any compiled client of the strongly named assembly
    /// in case this assembly is changed).
    ///
    /// The perfect place to store the dependencies info (custom meta data) is the assembly
    /// resources. However if we do so such assemblies would have to be loaded in order to read their
    /// resources. It is not acceptable as after loading assembly cannot be unloaded. Also assembly loading
    /// can significantly compromise performance.
    ///
    /// That is why custom meta data is just physically appended to the file. This is a valid
    /// approach because such assembly is not to be distributed anywhere but to stay always
    /// on the PC and play the role of the temporary data for the script engine.
    ///
    /// Note: A .dll assembly is always compiled and linked in a normal way without any custom meta data attached.
    /// </summary>
    internal class MetaDataItems
    {
        public class MetaDataItem
        {
            public MetaDataItem(string file, DateTime date, bool assembly)
            {
                this.file = file;
                this.date = date;
                this.assembly = assembly;
            }

            public string file;
            public DateTime date;
            public bool assembly;
        }

        public List<MetaDataItem> items = new List<MetaDataItem>();

        static public bool IsOutOfDate(string script, string assembly)
        {
            var depInfo = new MetaDataItems();

            if (depInfo.ReadFileStamp(assembly))
            {
                // Trace.WriteLine("Reading meta data...");
                //foreach (MetaDataItems.MetaDataItem item in depInfo.items)
                //    Trace.WriteLine(item.file + " : " + item.date);

                string dependencyFile = "";

                foreach (MetaDataItem item in depInfo.items)
                {
                    if (item.assembly && Path.IsPathRooted(item.file)) //is absolute path
                    {
                        dependencyFile = item.file;
                        CSExecutor.options.AddSearchDir(Path.GetDirectoryName(item.file), Settings.internal_dirs_section);
                    }
                    else
                        dependencyFile = FileParser.ResolveFile(item.file, CSExecutor.options.searchDirs, false);

                    if (!File.Exists(dependencyFile) || File.GetLastWriteTimeUtc(dependencyFile) != item.date)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public string[] AddItems(System.Collections.Specialized.StringCollection files, bool isAssembly, string[] searchDirs)
        {
            string[] referencedAssemblies = new string[files.Count];
            files.CopyTo(referencedAssemblies, 0);
            return AddItems(referencedAssemblies, isAssembly, searchDirs);
        }

        public string[] AddItems(string[] files, bool isAssembly, string[] searchDirs)
        {
            List<string> newProbingDirs = new List<string>();

            if (isAssembly)
            {
                foreach (string asmFile in files)
                {
                    //under some conditions assemblies do not have a location (e.g. dynamically built/emitted assemblies under ASP.NET)
                    if (!File.Exists(asmFile))
                        continue;

                    try
                    {
                        if (!IsGACAssembly(asmFile))
                        {
                            bool found = false;

                            foreach (string dir in searchDirs)
                                if (!IsGACAssembly(asmFile) && string.Compare(dir, Path.GetDirectoryName(asmFile), true) == 0)
                                {
                                    found = true;
                                    AddItem(Path.GetFileName(asmFile), File.GetLastWriteTimeUtc(asmFile), true);
                                    break;
                                }

                            if (!found) //the assembly was not in the search dirs
                            {
                                newProbingDirs.Add(Path.GetDirectoryName(asmFile));
                                AddItem(asmFile, File.GetLastWriteTimeUtc(asmFile), true); //assembly from the absolute path
                            }
                        }
                    }
                    catch (NotSupportedException)
                    {
                        //under ASP.NET some assemblies do not have location (e.g. dynamically built/emitted assemblies)
                    }
                    catch (ArgumentException)
                    {
                        //The asm.location parameter contains invalid characters, is empty, or contains only white spaces, or contains a wildcard character
                    }
                    catch (PathTooLongException)
                    {
                        //The asm.location parameter is longer than the system-defined maximum length
                    }
                    catch
                    {
                        //In fact ignore all exceptions at we should continue if for what ever reason assembly location cannot be obtained
                    }
                }
            }
            else
            {
                foreach (string file in files)
                {
                    string fullPath = Path.GetFullPath(file);

                    bool local = false;

                    foreach (string dir in searchDirs)
                        if ((local = (string.Compare(dir, Path.GetDirectoryName(fullPath), true) == 0)))
                            break;

                    if (local)
                        AddItem(Path.GetFileName(file), File.GetLastWriteTimeUtc(file), false);
                    else
                        AddItem(file, File.GetLastWriteTimeUtc(file), false);
                }
            }
            return newProbingDirs.ToArray();
        }

        public void AddItem(string file, DateTime date, bool assembly)
        {
            this.items.Add(new MetaDataItem(file, date, assembly));
        }

        public bool StampFile(string file)
        {
            //Trace.WriteLine("Writing mete data...");
            //foreach (MetaDataItem item in items)
            //    Trace.WriteLine(item.file + " : " + item.date);

            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open))
                {
                    fs.Seek(0, SeekOrigin.End);
                    using (var w = new BinaryWriter(fs))
                    {
                        char[] data = this.ToString().ToCharArray();
                        w.Write(data);
                        w.Write((Int32)data.Length);
                        w.Write((Int32)(CSExecutor.options.DBG ? 1 : 0));
                        w.Write((Int32)(CSExecutor.options.compilationContext));
                        w.Write((Int32)CSSUtils.GetHashCodeEx(Environment.Version.ToString()));

                        w.Write((Int32)stampID);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
            return true;
        }

        static int ReadIntBackwards(BinaryReader r, ref int offset)
        {
            var fs = (FileStream)r.BaseStream;
            offset += intSize;
            fs.Seek(-offset, SeekOrigin.End);
            return r.ReadInt32();
        }

        static long ReadLongBackwards(BinaryReader r, ref int offset)
        {
            var fs = (FileStream)r.BaseStream;
            offset += longSize;
            fs.Seek(-offset, SeekOrigin.End);
            return r.ReadInt64();
        }

        static int intSize = Marshal.SizeOf((Int32)0);
        static int longSize = Marshal.SizeOf((Int64)0);

        public bool ReadFileStamp(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader r = new BinaryReader(fs))
                    {
                        int offset = 0;
                        int stamp = ReadIntBackwards(r, ref offset);

                        if (stamp == stampID)
                        {
                            int value = ReadIntBackwards(r, ref offset);
                            if (value != CSSUtils.GetHashCodeEx(Environment.Version.ToString()))
                            {
                                return false;
                            }

                            value = ReadIntBackwards(r, ref offset);
                            if (value != CSExecutor.options.compilationContext)
                            {
                                return false;
                            }

                            value = ReadIntBackwards(r, ref offset);
                            if (value != (CSExecutor.options.DBG ? 1 : 0))
                            {
                                return false;
                            }

                            int dataSize = ReadIntBackwards(r, ref offset);
                            if (dataSize != 0)
                            {
                                fs.Seek(-(offset + dataSize), SeekOrigin.End);
                                var result = this.Parse(new string(r.ReadChars(dataSize)));
                                return result;
                            }
                            else
                                return true;
                        }
                        return false;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        new string ToString()
        {
            StringBuilder bs = new StringBuilder();

            foreach (MetaDataItem fileInfo in items)
            {
                bs.Append(fileInfo.file);
                bs.Append(";");
                bs.Append(fileInfo.date.ToFileTimeUtc().ToString());
                bs.Append(";");
                bs.Append(fileInfo.assembly ? "Y" : "N");
                bs.Append("|");
            }
            return bs.ToString();
        }

        bool Parse(string data)
        {
            foreach (string itemData in data.Split("|".ToCharArray()))
            {
                if (itemData.Length > 0)
                {
                    string[] parts = itemData.Split(";".ToCharArray());

                    if (parts.Length == 3)
                        this.items.Add(new MetaDataItem(parts[0], DateTime.FromFileTimeUtc(long.Parse(parts[1])), parts[2] == "Y"));
                    else
                        return false;
                }
            }
            return true;
        }

        int stampID = CSSUtils.GetHashCodeEx(Assembly.GetExecutingAssembly().FullName.Split(",".ToCharArray())[1]);

        bool IsGACAssembly(string file)
        {
            string s = file.ToLower();
            return s.Contains("microsoft.net\\framework") || s.Contains("microsoft.net/framework") || s.Contains("gac_msil") || s.Contains("gac_64") || s.Contains("gac_32");
        }
    }

    #endregion MetaDataItems...

    internal class Cache
    {
        static string cacheRootDir = Path.Combine(CSExecutor.GetScriptTempDir(), "Cache");

        static void deleteFile(string path)
        {
            try
            {
                File.SetAttributes(path, FileAttributes.Normal); //to remove possible read-only
                File.Delete(path);
            }
            catch { }
        }

        static void deleteDir(string path)
        {
            try
            {
                foreach (string file in Directory.GetFiles(path))
                    deleteFile(file);
                Directory.Delete(path);
            }
            catch { }
        }

        enum Op
        {
            List,
            Trim,
            Clear,
        }

        static public string List()
        {
            return Cache.Do(Op.List);
        }

        static public string Trim()
        {
            return Cache.Do(Op.Trim);
        }

        static public string Clear()
        {
            return Cache.Do(Op.Clear);
        }

        static string Do(Op operation)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("Cache root: " + cacheRootDir);
            if (operation == Op.List)
                result.AppendLine("Listing cache items:");
            else if (operation == Op.Trim)
                result.AppendLine("Purging abandoned cache items:");
            else if (operation == Op.Clear)
                result.AppendLine("Clearing all cache items:");

            result.AppendLine("");

            if (Directory.Exists(cacheRootDir))
                foreach (string cacheDir in Directory.GetDirectories(cacheRootDir))
                {
                    string infoFile = Path.Combine(cacheDir, "css_info.txt");

                    string cachName = Path.GetFileName(cacheDir);

                    if (!File.Exists(infoFile))
                    {
                        result.AppendLine(cachName + ":\tUNKNOWN");
                        if (operation == Op.Trim || operation == Op.Clear)
                            deleteDir(cacheDir);
                    }
                    else
                    {
                        string sourceDir = File.ReadAllLines(infoFile).Last();

                        if (operation == Op.List)
                        {
                            result.AppendLine(cachName + ":\t" + sourceDir);
                        }
                        else if (operation == Op.Clear)
                        {
                            result.AppendLine(cachName + ":\t" + sourceDir);
                            deleteDir(cacheDir);
                        }
                        else if (operation == Op.Trim)
                        {
                            if (!Directory.Exists(sourceDir))
                            {
                                result.AppendLine(cachName + ":\t" + sourceDir);
                                deleteDir(cacheDir);
                            }
                            else
                            {
                                // "path\script.cs.compiled"
                                foreach (string file in Directory.GetFiles(cacheDir, "*.compiled"))
                                {
                                    string name = Path.GetFileNameWithoutExtension(file);//script.cs

                                    string baseName = Path.GetFileNameWithoutExtension(name);//script

                                    string scriptFile = Path.Combine(sourceDir, name);

                                    if (!File.Exists(scriptFile))
                                    {
                                        result.AppendLine(cachName + ":\t" + scriptFile);
                                        foreach (string cacheFile in Directory.GetFiles(cacheDir, baseName + ".*"))
                                            deleteFile(cacheFile);

                                        string[] leftOvers = Directory.GetFiles(cacheDir);

                                        if (leftOvers.Length == 0 || (leftOvers.Length == 1 && leftOvers[0].EndsWith("css_info.txt")))
                                            deleteDir(cacheDir);
                                    }
                                }
                            }
                        }
                    }
                }

            return result.ToString().TrimEnd() + Environment.NewLine;
        }
    }

    /// <summary>
    /// Helper for retrieving currently installed .NET version
    /// </summary>
    public class DotNetVersion
    {
        /// <summary>
        /// Returns the exact full version of the installed .NET 4.5
        /// </summary>
        /// <returns></returns>
        public static string Get45PlusFromRegistry()
        {
#if net4
            var subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                    return CheckFor45PlusVersion((int)ndpKey.GetValue("Release"));
                else
                    return null;
            }
#else
                    return null;
#endif
        }

        // Checking the version using >= will enable forward compatibility.
        static string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 528040) return "4.8 or later";
            if (releaseKey >= 461808) return "4.7.2";
            if (releaseKey >= 461308) return "4.7.1";
            if (releaseKey >= 460798) return "4.7";
            if (releaseKey >= 394802) return "4.6.2";
            if (releaseKey >= 394254) return "4.6.1";
            if (releaseKey >= 393295) return "4.6";
            if (releaseKey >= 379893) return "4.5.2";
            if (releaseKey >= 378675) return "4.5.1";
            if (releaseKey >= 378389) return "4.5";
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
        }
    }
}