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
// Copyright (c) 2016 Oleg Shilo
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
using System.IO;
using System.Reflection;
#if !net1

using System.Collections.Generic;
using System.Linq;

#endif

using System.Text;
using CSScriptLibrary;
using System.Runtime.InteropServices;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Globalization;
using System.Threading;
using System.Collections;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace csscript
{
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

    internal class Utils
    {
        //unfortunately LINQ is not available for .NET 1.1 compilations
        public static string[] Concat(string[] array1, string[] array2)
        {
            string[] retval = new string[array1.Length + array2.Length];
            Array.Copy(array1, 0, retval, 0, array1.Length);
            Array.Copy(array2, 0, retval, array1.Length, array2.Length);
            return retval;
        }

        public static string[] Concat(string[] array1, string item)
        {
            string[] retval = new string[array1.Length + 1];
            Array.Copy(array1, 0, retval, 0, array1.Length);
            retval[retval.Length - 1] = item;
            return retval;
        }

        public static string[] Except(string[] array1, string[] array2)
        {
            System.Collections.ArrayList retval = new System.Collections.ArrayList();
            foreach (string item1 in array1)
            {
                bool found = false;
                foreach (string item2 in array2)
                    if (item2 == item1)
                    {
                        found = true;
                        break;
                    }

                if (!found)
                    retval.Add(item1);
            }

            return (string[]) retval.ToArray(typeof(string));
        }

        public static string[] RemovePathDuplicates(string[] list)
        {
            System.Collections.ArrayList retval = new System.Collections.ArrayList();
            foreach (string item in list)
            {

                string path = item.Trim();
                if (path == "")
                    continue;

                path = Path.GetFullPath(path);

                bool found = false;
                foreach (string pathItem in retval)
                    if (Utils.IsSamePath(pathItem, path))
                    {
                        found = true;
                        break;
                    }

                if (!found)
                    retval.Add(path);


            }

            return (string[]) retval.ToArray(typeof(string));
        }

        public static string[] RemoveDuplicates(string[] list)
        {
            System.Collections.ArrayList retval = new System.Collections.ArrayList();
            foreach (string item in list)
            {
                if (item.Trim() != "")
                {
                    if (!retval.Contains(item))
                        retval.Add(item);
                }
            }

            return (string[]) retval.ToArray(typeof(string));
        }

        public static string[] RemoveEmptyStrings(string[] list)
        {
            System.Collections.ArrayList retval = new System.Collections.ArrayList();
            foreach (string item in list)
            {
                if (item.Trim() != "")
                    retval.Add(item);
            }

            return (string[]) retval.ToArray(typeof(string));
        }

        //to avoid throwing the exception
        public static string GetAssemblyDirectoryName(Assembly asm)
        {
            if (CSSUtils.IsDynamic(asm))
                return "";
            else
                return Path.GetDirectoryName(asm.Location);
        }

        //to avoid throwing the exception
        public static string GetAssemblyFileName(Assembly asm)
        {
            if (CSSUtils.IsDynamic(asm))
                return "";
            else
                return Path.GetFileName(asm.Location);
        }

        public static string RemoveAssemblyExtension(string asmName)
        {
#if net1
            if (asmName.ToLower().EndsWith(".dll") || asmName.ToLower().EndsWith(".exe"))
#else
            if (asmName.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) || asmName.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
#endif
                return asmName.Substring(0, asmName.Length - 4);
            else
                return asmName;
        }

        public static int PathCompare(string path1, string path2)
        {
            if (Utils.IsLinux())
                return string.Compare(path1, path2);
            else
                return string.Compare(path1, path2, true);
        }

        public static bool IsSamePath(string path1, string path2)
        {
            return PathCompare(path1, path2) == 0;
        }

        public static void FileDelete(string path)
        {
            FileDelete(path, false);
        }

        public static Mutex FileLock(string file, object context)
        {
            if (!IsLinux())
                file = file.ToLower(CultureInfo.InvariantCulture);

            string mutexName = context.ToString() + "." + CSSUtils.GetHashCodeEx(file).ToString();
            return new Mutex(false, mutexName);
        }

        public static void ReleaseFileLock(Mutex @lock)
        {
            if (@lock != null)
                try { @lock.ReleaseMutex(); }
                catch { }
        }

        public delegate string ProcessNewEncodingHandler(string requestedEncoding);
        public static ProcessNewEncodingHandler ProcessNewEncoding = DefaultProcessNewEncoding;
        public static bool IsDefaultConsoleEncoding = true;
        static string DefaultProcessNewEncoding(string requestedEncoding) { return requestedEncoding; }


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

        public static Mutex FileLock(string file)
        {
            return FileLock(file, "");
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

        public static bool IsLinux()
        {
            return (Environment.OSVersion.Platform == PlatformID.Unix);
        }

        public static bool ContainsPath(string path, string subPath)
        {
            return PathCompare(path.Substring(0, subPath.Length), subPath) == 0;
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

        ///// <summary>
        ///// More reliable version of the Path.GetTempFileName().
        ///// It is required because it was some reports about non unique names returned by Path.GetTempFileName()
        ///// when running in multi-threaded environment.
        ///// (it is not used yet as I did not give up on PInvoke GetTempFileName())
        ///// </summary>
        ///// <returns>Temporary file name.</returns>
        //string PathGetTempFileName()
        //{
        //    return Path.GetTempPath() + Guid.NewGuid().ToString() + ".tmp";
        //}
    }

    internal class CSSUtils
    {
        internal static void VerbosePrint(string message, ExecuteOptions options)
        {
            if (options.verbose)
                Console.WriteLine(message);
        }

        internal static string GetScriptedCodeAttributeInjectionCode(string scriptFileName)
        {
            using (Mutex fileLock = Utils.FileLock(scriptFileName, "GetScriptedCodeAttributeInjectionCode"))
            {
                //Infinite timeout is not good choice here as it may block forever but continuing while the file is still locked will 
                //throw a nice informative exception.
                fileLock.WaitOne(1000, false);

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

            info2.LastWriteTime = info1.LastWriteTime;
            info2.LastWriteTimeUtc = info1.LastWriteTimeUtc;
        }

        public delegate void ShowDocumentHandler();

        static internal string cmdFlagPrefix
        {
            get
            {
                if (Utils.IsLinux())
                    return "-";
                else
                    return "/";
            }
        }

        static public string[] GetDirectories(string workingDir, string rootDir)
        {
            if (!Path.IsPathRooted(rootDir))
                rootDir = Path.Combine(workingDir, rootDir); //cannot use Path.GetFullPath as it crashes if '*' or '?' are present
#if net1
            return new string [] { rootDir };
#else

            List<string> result = new List<string>();

            if (rootDir.Contains("*") || rootDir.Contains("?"))
            {
                bool useAllSubDirs = rootDir.EndsWith("**");

                string pattern = ConvertSimpleExpToRegExp(useAllSubDirs ? rootDir.Remove(rootDir.Length - 1) : rootDir);
                Regex wildcard = new Regex(pattern, RegexOptions.IgnoreCase);

                int pos = rootDir.IndexOfAny(new char[] { '*', '?' });

                string newRootDir = rootDir.Remove(pos);

                pos = newRootDir.LastIndexOf(Path.DirectorySeparatorChar);
                newRootDir = rootDir.Remove(pos);

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
            else
                result.Add(rootDir);

            return result.ToArray();
#endif
        }

        //Credit to MDbg team: https://github.com/SymbolSource/Microsoft.Samples.Debugging/blob/master/src/debugger/mdbg/mdbgCommands.cs
        public static string ConvertSimpleExpToRegExp(string simpleExp)
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


        /// <summary>
        /// Parses application (script engine) arguments.
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <param name="executor">Script executor instance</param>
        /// <returns>Index of the first script argument.</returns>
        static internal int ParseAppArgs(string[] args, IScriptExecutor executor)
        {
            ExecuteOptions options = executor.GetOptions();
            //Debug.Assert(false);
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (File.Exists(arg))
                    return i; //on Linux '/' may indicate dir but not command

                if (arg.StartsWith(cmdFlagPrefix))
                {
                    if (arg == cmdFlagPrefix + "nl") // -nl
                    {
                        options.noLogo = true;
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "out:")) // -out
                    {
                        string file = arg.Substring((cmdFlagPrefix + "out:").Length);
                        options.forceOutputAssembly = Environment.ExpandEnvironmentVariables(file);
                    }
                    else if ((arg == cmdFlagPrefix + "c" || arg.StartsWith(cmdFlagPrefix + "c:")) && !options.supressExecution) // -c
                    {
                        if (arg == cmdFlagPrefix + "c")
                            options.useCompiled = true;
                        else if (arg == cmdFlagPrefix + "c:0")
                            options.useCompiled = false;
                        else //if (arg == cmdFlagPrefix + "c:1")
                            options.useCompiled = true;
                    }
                    else if ((arg == cmdFlagPrefix + "inmem" || arg.StartsWith(cmdFlagPrefix + "inmem:")) && !options.supressExecution) // -inmem
                    {
                        if (arg == cmdFlagPrefix + "inmem")
                            options.inMemoryAsm = true;
                        else if (arg == cmdFlagPrefix + "inmem:0")
                            options.inMemoryAsm = false;
                        else //if (arg == cmdFlagPrefix + "inmem:1")
                            options.inMemoryAsm = true;
                    }
                    else if (arg == cmdFlagPrefix + "sconfig")// -sconfig
                    {
                        options.useScriptConfig = true;
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "sconfig:")) // -sconfig:file
                    {
                        options.useScriptConfig = true;
                        options.customConfigFileName = arg.Substring((cmdFlagPrefix + "sconfig:").Length);
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "provider:")) // -provider:file
                    {
                        try
                        {
                            options.altCompiler = Environment.ExpandEnvironmentVariables(arg).Substring((cmdFlagPrefix + "provider:").Length);
                        }
                        catch { }
                    }
                    else if (arg == cmdFlagPrefix + "verbose")
                    {
                        options.verbose = true;
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "dir:")) // -dir:path1,path2
                    {
                        foreach (string dir in arg.Substring((cmdFlagPrefix + "dir:").Length).Split(','))
                            options.AddSearchDir(dir.Trim());
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "precompiler"))
                    {
                        if (arg.StartsWith(cmdFlagPrefix + "precompiler:")) // -precompiler:file1,file2
                        {
                            options.preCompilers = arg.Substring((cmdFlagPrefix + "precompiler:").Length);
                        }
                        else
                        {
                            executor.ShowPrecompilerSample();
                            options.processFile = false;
                        }
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "pc:")) // -pc:
                    {
                        options.preCompilers = arg.Substring((cmdFlagPrefix + "pc:").Length);
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "noconfig"))// -noconfig:file
                    {
                        options.noConfig = true;
                        if (arg.StartsWith(cmdFlagPrefix + "noconfig:"))
                        {
                            if (arg == (cmdFlagPrefix + "noconfig:out"))
                            {
                                executor.CreateDefaultConfigFile();
                                options.processFile = false;
                            }
                            else
                                options.altConfig = arg.Substring((cmdFlagPrefix + "noconfig:").Length);
                        }
                    }
                    else if (arg == cmdFlagPrefix + "autoclass" || arg == cmdFlagPrefix + "ac") // -autoclass -ac
                    {
                        options.autoClass = true;
                    }
                    else if (arg == cmdFlagPrefix + "nathash")
                    {
                        //-nathash //native hashing; by default it is deterministic but slower custom string hashing algorithm
                        //it is a hidden option for the cases when faster hashing is desired
                        options.customHashing = false;
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "ca")) // -ca
                    {
                        options.useCompiled = true;
                        options.forceCompile = true;
                        options.supressExecution = true;
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "co:")) // -co
                    {
                        //this one is accumulative
                        string cOption = arg.Substring((cmdFlagPrefix + "co:").Length);

                        if (!options.compilerOptions.Contains(cOption))
                            options.compilerOptions += " " + cOption;
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "cd")) // -cd
                    {
                        options.supressExecution = true;
                        options.DLLExtension = true;
                    }
                    else if (arg == cmdFlagPrefix + "dbg" || arg == cmdFlagPrefix + "d") // -dbg -d
                    {
                        options.DBG = true;
                    }
                    else if (arg == cmdFlagPrefix + "l")
                    {
                        options.local = true;
                    }
                    else if (arg == cmdFlagPrefix + "v" || arg == cmdFlagPrefix + "V") // -v
                    {
                        executor.ShowVersion();
                        options.processFile = false;
                        options.versionOnly = true;
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "r:")) // -r:file1,file2
                    {
                        string[] assemblies = arg.Remove(0, 3).Split(",;".ToCharArray()); //important change
                        options.refAssemblies = assemblies;
                    }
                    else if (arg.StartsWith(cmdFlagPrefix + "e") && !options.buildExecutable) // -e
                    {
                        options.buildExecutable = true;
                        options.supressExecution = true;
                        options.buildWinExecutable = arg.StartsWith(cmdFlagPrefix + "ew"); // -ew
                    }
                    else if (args[0] == cmdFlagPrefix + "?" || args[0] == cmdFlagPrefix + "help") // -? -help
                    {
                        executor.ShowHelp();
                        options.processFile = false;
                        break;
                    }
                    else if (args[0] == cmdFlagPrefix + "s") // -s
                    {
                        executor.ShowSample();
                        options.processFile = false;
                        break;
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

        internal static PrecompilationContext Precompile(string scriptFile, string[] filesToCompile, ExecuteOptions options)
        {
            PrecompilationContext context = new PrecompilationContext();
            context.SearchDirs = options.searchDirs;

            Hashtable contextData = new Hashtable();
            contextData["NewDependencies"] = context.NewDependencies;
            contextData["NewSearchDirs"] = context.NewSearchDirs;
            contextData["NewReferences"] = context.NewReferences;
            contextData["NewIncludes"] = context.NewIncludes;
            contextData["SearchDirs"] = context.SearchDirs;
            contextData["ConsoleEncoding"] = options.consoleEncoding;

#if net1
            System.Collections.Hashtable precompilers = CSSUtils.LoadPrecompilers(options);
#else
            Dictionary<string, List<object>> precompilers = CSSUtils.LoadPrecompilers(options);
#endif
            if (precompilers.Count != 0)
            {
                for (int i = 0; i < filesToCompile.Length; i++)
                {
                    string content = File.ReadAllText(filesToCompile[i]);

                    bool modified = false;
                    foreach (string precompilerFile in precompilers.Keys)
                    {
#if net1
                        foreach (object precompiler in precompilers[precompilerFile] as ArrayList)
#else
                        foreach (object precompiler in precompilers[precompilerFile])
#endif
                        {
                            if (options.verbose && i == 0)
                            {
                                CSSUtils.VerbosePrint("  Precompilers: ", options);
                                int index = 0;
                                foreach (string file in filesToCompile)
                                    CSSUtils.VerbosePrint("   " + index++ + " - " + Path.GetFileName(file)+ " -> " + precompiler.GetType() + "\n           from " + precompilerFile, options);
                                CSSUtils.VerbosePrint("", options);
                            }

                            MethodInfo method = precompiler.GetType().GetMethod("Compile");
                            CompileMethod compile = (CompileMethod) Delegate.CreateDelegate(typeof(CompileMethod), method);
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

            options.searchDirs = Utils.Concat(options.searchDirs, context.NewSearchDirs.ToArray());

            foreach (string asm in context.NewReferences)
                options.defaultRefAssemblies += "," + asm; //the easiest way to inject extra references is to merge them with the extra assemblies already specified by user

            return context;
        }

        internal const string noDefaultPrecompilerSwitch = "nodefault";

#if net1
        public static System.Collections.Hashtable LoadPrecompilers(ExecuteOptions options)
        {
            System.Collections.Hashtable retval = new System.Collections.Hashtable();
            if (!options.preCompilers.StartsWith(noDefaultPrecompilerSwitch)) //no defaults
            {
                ArrayList compilers = new ArrayList();
                compilers.Add(new DefaultPrecompiler());
                retval.Add(Assembly.GetExecutingAssembly().Location, compilers);
            }

            if (options.autoClass)
            {
                if (retval.ContainsKey(Assembly.GetExecutingAssembly().Location))
                    (retval[Assembly.GetExecutingAssembly().Location] as ArrayList).Add(new AutoclassPrecompiler());
                else
                {
                    ArrayList compilers = new ArrayList();
                    compilers.Add(new AutoclassPrecompiler());
                    retval.Add(Assembly.GetExecutingAssembly().Location, compilers);
                }
            }

#else

        internal static Dictionary<string, List<object>> LoadPrecompilers(ExecuteOptions options)
        {
            Dictionary<string, List<object>> retval = new Dictionary<string, List<object>>();

            if (!options.preCompilers.StartsWith(noDefaultPrecompilerSwitch)) //no defaults
                retval.Add(Assembly.GetExecutingAssembly().Location, new List<object>() { new DefaultPrecompiler() });

            if (options.autoClass)
            {
                if (retval.ContainsKey(Assembly.GetExecutingAssembly().Location))
                    retval[Assembly.GetExecutingAssembly().Location].Add(new AutoclassPrecompiler());
                else
                    retval.Add(Assembly.GetExecutingAssembly().Location, new List<object>() { new AutoclassPrecompiler() });
            }

#endif

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

                    //string typeName = typeof(IPrecompiler).Name;

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

#if net1
                    if (precompilerObj != null)
                        {
                            ArrayList compilers = new ArrayList();
                            compilers.Add(precompilerObj);
                            retval.Add(sourceFile, compilers);
                        }

#else
                    if (precompilerObj != null)
                        retval.Add(sourceFile, new List<object>() { precompilerObj });
#endif
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

        internal static string[] CollectPrecompillers(CSharpParser parser, ExecuteOptions options)
        {
#if net1
            ArrayList allPrecompillers = new ArrayList();
#else
            List<string> allPrecompillers = new List<string>();
#endif
            allPrecompillers.AddRange(options.preCompilers.Split(','));

            foreach (string item in parser.Precompilers)
                allPrecompillers.AddRange(item.Split(','));

#if net1
            return Utils.RemoveDuplicates((string[])allPrecompillers.ToArray(typeof(string)));
#else
            return Utils.RemoveDuplicates(allPrecompillers.ToArray());
#endif
        }

        internal static int GenerateCompilationContext(CSharpParser parser, ExecuteOptions options)
        {
            string[] allPrecompillers = CollectPrecompillers(parser, options);

            StringBuilder sb = new StringBuilder();

            foreach (string file in allPrecompillers)
            {
                if (file != "")
                {
                    sb.Append(FindImlementationFile(file, options.searchDirs));
                    sb.Append(",");
                }
            }

            return CSSUtils.GetHashCodeEx(sb.ToString());
        }

#if !net1
        public static string[] GetAppDomainAssemblies()
        {
            return (from a in AppDomain.CurrentDomain.GetAssemblies()
                    where !CSSUtils.IsDynamic(a) && !a.GlobalAssemblyCache
                    select a.Location).ToArray();
        }

#endif
        public static bool IsDynamic(Assembly asm)
        {
            //http://bloggingabout.net/blogs/vagif/archive/2010/07/02/net-4-0-and-notsupportedexception-complaining-about-dynamic-assemblies.aspx
            //Will cover both System.Reflection.Emit.AssemblyBuilder and System.Reflection.Emit.InternalAssemblyBuilder
            return asm.GetType().FullName.EndsWith("AssemblyBuilder") || asm.Location == null || asm.Location == "";
        }

        public static Assembly CompilePrecompilerScript(string sourceFile, string[] searchDirs)
        {
            try
            {
                string precompilerAsm = Path.Combine(CSExecutor.GetCacheDirectory(sourceFile), Path.GetFileName(sourceFile) + ".compiled");

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
#if net1
                    ArrayList refAssemblies = new ArrayList();
#else
                    List<string> refAssemblies = new List<string>();
#endif

                    //add local and global assemblies (if found) that have the same assembly name as a namespace
                    foreach (string nmSpace in parser.ReferencedNamespaces)
                        foreach (string asm in AssemblyResolver.FindAssembly(nmSpace, searchDirs))
                            refAssemblies.Add(asm);

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
                            string nameSpace = Utils.RemoveAssemblyExtension(asmName);

                            string[] files = AssemblyResolver.FindAssembly(nameSpace, searchDirs);
                            if (files.Length > 0)
                                foreach (string asm in files)
                                    refAssemblies.Add(asm);
                            else
                                refAssemblies.Add(nameSpace + ".dll");
                        }

                    ////////////////////////////////////////
#if net1
                foreach (string asm in Utils.RemovePathDuplicates((string[])refAssemblies.ToArray(typeof(string))))
#else
                    foreach (string asm in Utils.RemovePathDuplicates(refAssemblies.ToArray()))
#endif
                    {
                        compilerParams.ReferencedAssemblies.Add(asm);
                    }

#pragma warning disable 618
                    CompilerResults result = new CSharpCodeProvider().CreateCompiler().CompileAssemblyFromFile(compilerParams, sourceFile);
#pragma warning restore 618

                    if (result.Errors.Count != 0)
                        throw CompilerException.Create(result.Errors, true);

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

        static public bool IsRuntimeErrorReportingSupressed
        {
            get
            {
                return Environment.GetEnvironmentVariable("CSS_IsRuntimeErrorReportingSupressed") != null;
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
            code.Append("//Auto-generated file\r\n"); //cannot use AppendLine as it is not available in StringBuilder v1.1
            //code.Append("using System;\r\n");

            bool headerProcessed = false;
            string line;
            using (StreamReader sr = new StreamReader(file, Encoding.UTF8))
                while ((line = sr.ReadLine()) != null)
                {
                    if (!headerProcessed && !line.TrimStart().StartsWith("using ")) //not using...; statement of the file header
                        if (!line.StartsWith("//") && line.Trim() != "") //not comments or empty line
                        {
                            headerProcessed = true;
                            //code.Append("namespace Scripting\r\n");
                            //code.Append("{\r\n");
                            code.Append("   public class ScriptClass\r\n");
                            code.Append("   {\r\n");
                            code.Append("   static public ");
                        }

                    code.Append(line);
                    code.Append("\r\n");
                }

            code.Append("   }\r\n");

            string autogenFile = SaveAsAutogeneratedScript(code.ToString(), file);

            return autogenFile;
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

#if net1
        public ArrayList items = new ArrayList();
#else
        public List<MetaDataItem> items = new List<MetaDataItem>();
#endif

        static public bool IsOutOfDate(string script, string assembly)
        {
            MetaDataItems depInfo = new MetaDataItems();

            if (depInfo.ReadFileStamp(assembly))
            {
                //Trace.WriteLine("Reading mete data...");
                //foreach (MetaDataItems.MetaDataItem item in depInfo.items)
                //    Trace.WriteLine(item.file + " : " + item.date);

                string dependencyFile = "";
                foreach (MetaDataItem item in depInfo.items)
                {
                    if (item.assembly)
                    {
                        if (Path.IsPathRooted(item.file)) //is absolute path
                        {
                            dependencyFile = item.file;
                            CSExecutor.options.AddSearchDir(Path.GetDirectoryName(item.file));
                        }
                        else
                        {
                            foreach (string dir in CSExecutor.options.searchDirs)
                            {
                                dependencyFile = Path.Combine(dir, item.file); //assembly should be in the same directory with the script
                                if (File.Exists(dependencyFile))
                                    break;
                            }
                        }
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
                return true;
        }

        public string[] AddItems(System.Collections.Specialized.StringCollection files, bool isAssembly, string[] searchDirs)
        {
            string[] referencedAssemblies = new string[files.Count];
            files.CopyTo(referencedAssemblies, 0);
            return AddItems(referencedAssemblies, isAssembly, searchDirs);
        }

        public string[] AddItems(string[] files, bool isAssembly, string[] searchDirs)
        {
#if net1
            ArrayList newProbingDirs = new ArrayList();
#else
            List<string> newProbingDirs = new List<string>();
#endif
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
#if net1
            return (string[])newProbingDirs.ToArray(typeof(string));
#else
            return newProbingDirs.ToArray();
#endif
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
                    using (BinaryWriter w = new BinaryWriter(fs))
                    {
                        char[] data = this.ToString().ToCharArray();
                        w.Write(data);
                        w.Write((Int32) data.Length);
                        w.Write((Int32) (CSExecutor.options.DBG ? 1 : 0));
                        w.Write((Int32) (CSExecutor.options.compilationContext));
                        w.Write((Int32) CSSUtils.GetHashCodeEx(Environment.Version.ToString()));
                        w.Write((Int32) stampID);
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

        public bool ReadFileStamp(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader r = new BinaryReader(fs))
                    {
                        fs.Seek(-intSize, SeekOrigin.End);
                        int stamp = r.ReadInt32();
                        if (stamp == stampID)
                        {
                            fs.Seek(-(intSize * 2), SeekOrigin.End);
                            if (r.ReadInt32() != CSSUtils.GetHashCodeEx(Environment.Version.ToString()))
                            {
                                //Console.WriteLine("Environment.Version");
                                return false;
                            }

                            fs.Seek(-(intSize * 3), SeekOrigin.End);

                            //int yyy = r.ReadInt32();
                            //if (yyy != CSExecutor.options.compilationContext)
                            if (r.ReadInt32() != CSExecutor.options.compilationContext)
                            {
                                //Console.WriteLine("CSExecutor.options.compilationContext");
                                return false;
                            }

                            fs.Seek(-(intSize * 4), SeekOrigin.End);
                            if (r.ReadInt32() != (CSExecutor.options.DBG ? 1 : 0))
                            {
                                //Console.WriteLine("CSExecutor.options.DBG");
                                return false;
                            }

                            fs.Seek(-(intSize * 5), SeekOrigin.End);
                            int dataSize = r.ReadInt32();

                            if (dataSize != 0)
                            {
                                fs.Seek(-(intSize * 5 + dataSize), SeekOrigin.End);
                                return this.Parse(new string(r.ReadChars(dataSize)));
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
        int intSize = Marshal.SizeOf((Int32) 0);

        //#pragma warning disable 414
        //int executionFlag = Marshal.SizeOf((Int32)0);
        //#pragma warning restore 414

        bool IsGACAssembly(string file)
        {
            string s = file.ToLower();
#if net1
            return s.IndexOf("microsoft.net\\framework") != -1 || s.IndexOf("microsoft.net/framework") != -1 || s.IndexOf("gac_msil") != -1 || s.IndexOf("gac_64") != -1 || s.IndexOf("gac_32") != -1;
#else
            return s.Contains("microsoft.net\\framework") || s.Contains("microsoft.net/framework") || s.Contains("gac_msil") || s.Contains("gac_64") || s.Contains("gac_32");
#endif
        }
    }

    #endregion MetaDataItems...

    internal class HelpProvider
    {
        public static string BuildCommandInterfaceHelp()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(AppInfo.appLogo);
            builder.Append("\nUsage: " + AppInfo.appName + " <switch 1> <switch 2> <file> [params] [//x]\n");
            builder.Append("\n");
            builder.Append("<switch 1>\n");
            builder.Append(" {0}?    - Display help info.\n");
            builder.Append(" {0}e    - Compile script into console application executable.\n");
            builder.Append(" {0}ew   - Compile script into Windows application executable.\n");
            builder.Append(" {0}c[:<0|1>]\n");
            builder.Append("         - Use compiled file (cache file .compiled) if found (to improve performance).\n");
            builder.Append("         {0}c:1|{0}c - enable caching\n");
            builder.Append("         {0}c:0 - disable caching (which might be enabled globally);\n");
            builder.Append(" {0}ca   - Compile script file into assembly (cache file .compiled) without execution.\n");
            builder.Append(" {0}cd   - Compile script file into assembly (.dll) without execution.\n\n");
            builder.Append(" {0}co:<options>\n");
            builder.Append("       - Pass compiler options directly to the language compiler\n");
            builder.Append("       (e.g.  {0}co:/d:TRACE pass /d:TRACE option to C# compiler\n");
            builder.Append("        or  {0}co:/platform:x86 to produce Win32 executable)\n\n");
            builder.Append(" {0}s    - Print content of sample script file (e.g. " + AppInfo.appName + " /s > sample.cs).\n");
            builder.Append(" {0}ac | {0}autoclass\n");
            builder.Append("       - Automatically generates wrapper class if the script does not define any class of its own:\n");
            builder.Append("\n");
            builder.Append("         using System;\n");
            builder.Append("                      \n");
            builder.Append("         void Main()\n");
            builder.Append("         {\n");
            builder.Append("             Console.WriteLine(\"Hello World!\");\n");
            builder.Append("         }\n");
            builder.Append("\n");
            builder.Append("\n");
            builder.Append("<switch 2>\n");
            if (AppInfo.appParamsHelp != "")
                builder.Append(" {0}" + AppInfo.appParamsHelp);	//application specific usage info
            builder.Append(" {0}dbg|" + CSSUtils.cmdFlagPrefix + "d\n");
            builder.Append("         - Force compiler to include debug information.\n");
            builder.Append(" {0}l    - 'local'(makes the script directory a 'current directory')\n");
            builder.Append(" {0}v    - Prints CS-Script version information\n");
            builder.Append("         - Use compiled file (cache file .compiled) if found (to improve performance).\n");
            builder.Append(" {0}inmem[:<0|1>]\n");
            builder.Append("         Loads compiled script in memory before execution. This mode allows preventing locking the \n");
            builder.Append("         compiled script file. Can be beneficial for fine concurrency control as it allows changing \n");
            builder.Append("         and executing the scripts that are already loaded (being executed). This mode is incompatible \n");
            builder.Append("         with the scripting scenarios that require scriptassembly to be file based (e.g. advanced Reflection).\n");
            builder.Append("         {0}inmem:1|inmem - disable caching (which might be enabled globally);\n");
            builder.Append("         {0}inmem:0 - disable caching (which might be enabled globally);\n");
            builder.Append(" {0}verbose \n");
            builder.Append("       - prints runtime information during the script execution (applicable for console clients only)\n");
            builder.Append(" {0}noconfig[:<file>]\n       - Do not use default CS-Script config file or use alternative one.\n");
            builder.Append("         Value \"out\" of the <file> is reserved for creating the config file (css_config.xml) with the default settings.\n");
            builder.Append("         (e.g. " + AppInfo.appName + " {0}noconfig sample.cs\n");
            builder.Append("         " + AppInfo.appName + " {0}noconfig:c:\\cs-script\\css_VB.dat sample.vb)\n");
            builder.Append(" {0}out[:<file>]\n       - Forces the script to be compiled into a specific location. Used only for very fine hosting tuning.\n");
            builder.Append("         (e.g. " + AppInfo.appName + " {0}out:%temp%\\%pid%\\sample.dll sample.cs\n");
            builder.Append(" {0}sconfig[:<file>]\n       - Use script config file or custom config file as a .NET application configuration file.\n");
            builder.Append("  This option might be useful for running scripts, which usually cannot be executed without configuration file (e.g. WCF, Remoting).\n\n");
            builder.Append("          (e.g. if {0}sconfig is used the expected config file name is <script_name>.cs.config or <script_name>.exe.config\n");
            builder.Append("           if {0}sconfig:myApp.config is used the expected config file name is myApp.config)\n");
            builder.Append(" {0}r:<assembly 1>:<assembly N>\n");
            builder.Append("       - Use explicitly referenced assembly. It is required only for\n");
            builder.Append("         rare cases when namespace cannot be resolved into assembly.\n");
            builder.Append("         (e.g. " + AppInfo.appName + " /r:myLib.dll myScript.cs).\n");
            builder.Append(" {0}dir:<directory 1>,<directory N>\n");
            builder.Append("       - Add path(s) to the assembly probing directory list.\n");
            builder.Append("         (e.g. " + AppInfo.appName + " /dir:C:\\MyLibraries myScript.cs).\n");
            builder.Append(" {0}co:<options>\n");
            builder.Append("       -  Passes compiler options directy to the language compiler.\n");
            builder.Append("         (e.g. /co:/d:TRACE pass /d:TRACE option to C# compiler).\n");
            builder.Append(" {0}precompiler[:<file 1>,<file N>]\n");
            builder.Append("       - specifies custom precompiler file(s). This can be either script or assembly file.\n");
            builder.Append("         If no file(s) specified prints the code template for the custom precompiler.\n");
            builder.Append("         There is a special reserved word '" + CSSUtils.noDefaultPrecompilerSwitch + "' to be used as a file name.\n");
            builder.Append("         It instructs script engine to prevent loading any built-in precompilers \n");
            builder.Append("         like the one for removing shebang\n");
            builder.Append("         before the execution.\n");
            builder.Append("         (see Precompilers chapter in the documentation)\n");
            builder.Append(" {0}provider:<file>\n");
            builder.Append("       - Location of alternative code provider assembly. If set it forces script engine to use an alternative code compiler.\n");
            builder.Append("         (see \"Alternative compilers\" chapter in the documentation)\n");
            builder.Append("\n");
            builder.Append("file   - Specifies name of a script file to be run.\n");
            builder.Append("params - Specifies optional parameters for a script file to be run.\n");
            builder.Append(" //x   - Launch debugger just before starting the script.\n");
            builder.Append("\n");
            if (AppInfo.appConsole) // a temporary hack to prevent showing a huge message box when not in console mode
            {
                builder.Append("\n");
                builder.Append("**************************************\n");
                builder.Append("Script specific syntax\n");
                builder.Append("**************************************\n");
                builder.Append("\n");
                builder.Append("Engine directives:\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_include <file>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_inc\n");
                builder.Append("\n");
                builder.Append("file - name of a script file to be included at compile-time.\n");
                builder.Append("\n");
                builder.Append("This directive is used to include one script into another one.It is a logical equivalent of '#include' in C++.\n");
                builder.Append("This directive is a simplified version of //css_import.\n");
                builder.Append("Note if you use wildcard in the imported script name (e.g. *_build.cs) the directive will only import from the first\n");
                builder.Append("probing directory where the match ing file(s) is found.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_import <file>[, preserve_main][, rename_namespace(<oldName>, <newName>)];\n");
                builder.Append("\n");
                builder.Append("Alias - //css_imp\n");
                builder.Append("There are also another two aliases //css_include and //css_inc. They are equivalents of //css_import <file>, preserve_main\n");
                builder.Append("If $this (or $this.name) is specified as part of <file> it will be replaced at execution time with the main script full name (or file name only).\n");
                builder.Append("\n");
                builder.Append("file            - name of a script file to be imported at compile-time.\n");
                builder.Append("<preserve_main> - do not rename 'static Main'\n");
                builder.Append("oldName         - name of a namespace to be renamed during importing\n");
                builder.Append("newName         - new name of a namespace to be renamed during importing\n");
                builder.Append("\n");
                builder.Append("This directive is used to inject one script into another at compile time. Thus code from one script can be exercised in another one.\n");
                builder.Append("'Rename' clause can appear in the directive multiple times.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_nuget [-noref] [-force[:delay]] [-ver:<version>] [-ng:<nuget arguments>] package0[,package1]..[,packageN];\n");
                builder.Append("\n");
                builder.Append("Downloads/Installs the NuGet package. It also automatically references the downloaded package assemblies.\n");
                builder.Append("Note:\n");
                builder.Append("  The directive switches need to be in the order as above.\n");
                builder.Append("  By default the package is not downloaded again if it was already downloaded.\n");
                builder.Append("  If no version is specified then the highest downloaded version (if any) will be used.\n");
                builder.Append("  Referencing the downloaded packages can only handle simple dependency scenarios when all downloaded assemblies are to be referenced.\n");
                builder.Append("  You should use '-noref' switch and reference assemblies manually for all other cases. For example multiple assemblies with the same file name that \n");
                builder.Append("  target different CLRs (e.g. v3.5 vs v4.0) in the same package.\n");
                builder.Append("Switches:\n");
                builder.Append(" -noref - switch for individual packages if automatic referencing isn't desired. You can use 'css_nuget' environment variable for\n");
                builder.Append("          further referencing package content (e.g. //css_dir %css_nuget%\\WixSharp\\**)\n");
                builder.Append(" -force[:delay] - switch to force individual packages downloading even when they were already downloaded.\n");
                builder.Append("                  You can optionally specify delay for the next forced downloading by number of seconds since last download.\n");
                builder.Append("                  '-force:3600' will delay it for one hour. This option is useful for preventing frequent download interruptions\n");
                builder.Append("                  during active script development.\n");
                builder.Append(" -ver: - switch to download/reference a specific package version.\n");
                builder.Append(" -ng: - switch to pass NuGet arguments for every individual package.\n");
                builder.Append("Example: //css_nuget cs-script;\n");
                builder.Append("         //css_nuget -ver:4.1.2 NLog\n");
                builder.Append("         //css_nuget -ver:\"4.1.1-rc1\" -ng:\"-Pre -NoCache\" NLog\n");
                builder.Append("This directive will install CS-Script NuGet package.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_args arg0[,arg1]..[,argN];\n");
                builder.Append("\n");
                builder.Append("Embedded script arguments. The both script and engine arguments are allowed except \"/noconfig\" engine command switch.\n");
                builder.Append(" Example: //css_args {0}dbg, {0}inmem;\n This directive will always force script engine to execute the script in debug mode.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_reference <file>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_ref\n");
                builder.Append("\n");
                builder.Append("file - name of the assembly file to be loaded at run-time.\n");
                builder.Append("\n");
                builder.Append("This directive is used to reference assemblies required at run time.\n");
                builder.Append("The assembly must be in GAC, the same folder with the script file or in the 'Script Library' folders (see 'CS-Script settings').\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_precompiler <file 1>,<file 2>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_pc\n");
                builder.Append("\n");
                builder.Append("file - name of the script or assembly file implementing precompiler.\n");
                builder.Append("\n");
                builder.Append("This directive is used to specify the CS-Script precompilers to be loaded and exercised against script at run time.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_searchdir <directory>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_dir\n");
                builder.Append("\n");
                builder.Append("directory - name of the directory to be used for script and assembly probing at run-time.\n");
                builder.Append("\n");
                builder.Append("This directive is used to extend set of search directories (script and assembly probing).\n");
#if !net1
                builder.Append("The directory name can be a wild card based expression.In such a case all directories matching the pattern will be this \n");
                builder.Append("case all directories will be probed.\n");
                builder.Append("The special case when the path ends with '**' is reserved to indicate 'sub directories' case. Examples:\n");
                builder.Append("    //css_dir packages\\ServiceStack*.1.0.21\\lib\\net40\n");
                builder.Append("    //css_dir packages\\**\n");
#endif
                builder.Append("------------------------------------\n");
                builder.Append("//css_resource <file>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_res\n");
                builder.Append("\n");
                builder.Append("file - name of the resource file (.resources) to be used with the script.\n");
                builder.Append("\n");
                builder.Append("This directive is used to reference resource file for script.\n");
                builder.Append(" Example: //css_res Scripting.Form1.resources;\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_co <options>;\n");
                builder.Append("\n");
                builder.Append("options - options string.\n");
                builder.Append("\n");
                builder.Append("This directive is used to pass compiler options string directly to the language specific CLR compiler.\n");
                builder.Append(" Example: //css_co /d:TRACE pass /d:TRACE option to C# compiler\n");
                builder.Append("          //css_co /platform:x86 to produce Win32 executable\n\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_ignore_namespace <namespace>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_ignore_ns\n");
                builder.Append("\n");
                builder.Append("namespace - name of the namespace. Use '*' to completely disable namespace resolution\n");
                builder.Append("\n");
                builder.Append("This directive is used to prevent CS-Script from resolving the referenced namespace into assembly.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_prescript file([arg0][,arg1]..[,argN])[ignore];\n");
                builder.Append("//css_postscript file([arg0][,arg1]..[,argN])[ignore];\n");
                builder.Append("\n");
                builder.Append("Aliases - //css_pre and //css_post\n");
                builder.Append("\n");
                builder.Append("file    - script file (extension is optional)\n");
                builder.Append("arg0..N - script string arguments\n");
                builder.Append("ignore  - continue execution of the main script in case of error\n");
                builder.Append("\n");
                builder.Append("These directives are used to execute secondary pre- and post-action scripts.\n");
                builder.Append("If $this (or $this.name) is specified as arg0..N it will be replaced at execution time with the main script full name (or file name only).\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_host [/version:<CLR_Version>] [/platform:<CPU>]\n");
                builder.Append("\n");
                builder.Append("CLR_Version - version of CLR the script should be execute on (e.g. //css_host /version:v3.5)\n");
                builder.Append("CPU - indicates which platforms the script should be run on: x86, Itanium, x64, or anycpu.\n");
                builder.Append("Sample: //css_host /version:v2.0 /platform:x86;");
                builder.Append("\n");
                builder.Append("These directive is used to execute script from a surrogate host process. The script engine application (cscs.exe or csws.exe) launches the script\n");
                builder.Append("execution as a separate process of the specified CLR version and CPU architecture.\n");
                builder.Append("------------------------------------\n");
                builder.Append("Note the script engine always sets the following environment variables:\n");
                builder.Append(" 'pid' - host processId (e.g. Environment.GetEnvironmentVariable(\"pid\")\n");
                builder.Append(" 'CSScriptRuntime' - script engine version\n");
                builder.Append(" 'CSScriptRuntimeLocation' - script engine location\n");
                builder.Append(" 'css_nuget' - location of the NuGet packages scripts can load/reference\n");
                builder.Append(" 'EntryScript' - location of the entry script\n");
                builder.Append(" 'location:<assm_hash>' - location of the compiled script assembly.\n");
                builder.Append("                          This variable is particularly useful as it allows finding the compiled assembly file from the inside of the script code.\n");
                builder.Append("                          Even when the script loaded in-memory (InMemoryAssembly setting) but not from the original file.\n");
                builder.Append("                          (e.g. var location = Environment.GetEnvironmentVariable(\"location:\" + Assembly.GetExecutingAssembly().GetHashCode());\n");
                builder.Append("------------------------------------\n");
                builder.Append("\n");
                builder.Append("Any directive has to be written as a single line in order to have no impact on compiling by CLI compliant compiler.\n");
                builder.Append("It also must be placed before any namespace or class declaration.\n");
                builder.Append("\n");
                builder.Append("------------------------------------\n");
                builder.Append("Example:\n");
                builder.Append("\n");
                builder.Append(" using System;\n");
                builder.Append(" //css_prescript com(WScript.Shell, swshell.dll);\n");
                builder.Append(" //css_import tick, rename_namespace(CSScript, TickScript);\n");
                builder.Append(" //css_reference teechart.lite.dll;\n");
                builder.Append(" \n");
                builder.Append(" namespace CSScript\n");
                builder.Append(" {\n");
                builder.Append("   class TickImporter\n");
                builder.Append("   {\n");
                builder.Append("      static public void Main(string[] args)\n");
                builder.Append("      {\n");
                builder.Append("         TickScript.Ticker.i_Main(args);\n");
                builder.Append("      }\n");
                builder.Append("   }\n");
                builder.Append(" }\n");
                builder.Append("\n");
            }

            //return string.Format(builder.ToString(), CSSUtils.cmdFlagPrefix); //for some reason Format(..) fails
            return builder.ToString().Replace("{0}", CSSUtils.cmdFlagPrefix);
        }

        public static string BuildSampleCode()
        {
            StringBuilder builder = new StringBuilder();
            if (Utils.IsLinux())
            {
                builder.Append("#!<cscs.exe path> " + CSSUtils.cmdFlagPrefix + "nl " + Environment.NewLine);
                builder.Append("//css_reference System.Windows.Forms;" + Environment.NewLine);
            }

            builder.Append("using System;" + Environment.NewLine);
            builder.Append("using System.Windows.Forms;" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("class Script" + Environment.NewLine);
            builder.Append("{" + Environment.NewLine);
            if (!Utils.IsLinux())
                builder.Append("    [STAThread]" + Environment.NewLine);
            builder.Append("    static public void Main(string[] args)" + Environment.NewLine);
            builder.Append("    {" + Environment.NewLine);
            builder.Append("        for (int i = 0; i < args.Length; i++)" + Environment.NewLine);
            builder.Append("            Console.WriteLine(args[i]);" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("        MessageBox.Show(\"Just a test!\");" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("    }" + Environment.NewLine);
            builder.Append("}" + Environment.NewLine);

            return builder.ToString();
        }

        public static string BuildPrecompilerSampleCode()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("using System;" + Environment.NewLine);
            builder.Append("using System.Collections;" + Environment.NewLine);
            builder.Append("using System.Collections.Generic;" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("public class Sample_Precompiler //precompiler class name must end with 'Precompiler'" + Environment.NewLine);
            builder.Append("{" + Environment.NewLine);
            builder.Append("    public static bool Compile(ref string scriptCode, string scriptFile, bool isPrimaryScript, Hashtable context)" + Environment.NewLine);
            builder.Append("    {" + Environment.NewLine);
            builder.Append("        //if new assemblies are to be referenced add them (see 'Precompilers' in the documentation)" + Environment.NewLine);
            builder.Append("        //var newReferences = (List<string>)context[\"NewReferences\"];" + Environment.NewLine);
            builder.Append("        //newReferences.Add(\"System.Xml.dll\");" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("        //if scriptCode needs to be altered assign scriptCode the new value and return true. Otherwise return false" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("        //scriptCode = \"code after precompilation\";" + Environment.NewLine);
            builder.Append("        //return true;" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("        return false;" + Environment.NewLine);
            builder.Append("    }" + Environment.NewLine);
            builder.Append("}" + Environment.NewLine);

            return builder.ToString();
        }

        public static string BuildVersionInfo()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(AppInfo.appLogo.TrimEnd() + " www.csscript.net\n");
            builder.Append("\n");
            builder.Append("   CLR:            " + Environment.Version + "\n");
            builder.Append("   System:         " + Environment.OSVersion + "\n");
#if net4
            builder.Append("   Architecture:   " + (Environment.Is64BitProcess ? "x64" : "x86") + "\n");
#endif
            builder.Append("   Home dir:       " + (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") ?? "<not integrated>") + "\n");
            return builder.ToString();
        }
    }

    class NuGet
    {
        static public string NuGetCacheView
        {
            get { return Directory.Exists(NuGetCache) ? NuGetCache : "<not found>"; }
        }
        static public string NuGetExeView
        {
            get { return File.Exists(NuGetExe) ? NuGetExe : "<not found>"; }
        }

        static string nuGetCache = null;
        static string NuGetCache
        {
            get
            {
                if (nuGetCache == null)
                {
                    nuGetCache = Environment.GetEnvironmentVariable("css_nuget") ??
                                 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CS-Script" + Path.DirectorySeparatorChar + "nuget");

                    if (!Directory.Exists(nuGetCache))
                        Directory.CreateDirectory(nuGetCache);
                }
                return nuGetCache;
            }
        }

        static string nuGetExe = null;
        static string NuGetExe
        {
            get
            {
                if (nuGetExe == null)
                {
                    string localDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); //N++ case

                    nuGetExe = Path.Combine(localDir, "nuget.exe");
                    if (!File.Exists(nuGetExe))
                    {
                        string libDir = Path.Combine(Environment.ExpandEnvironmentVariables("%CSSCRIPT_DIR%"), "lib"); //CS-S installed
                        nuGetExe = Path.Combine(libDir, "nuget.exe");
                        if (!File.Exists(nuGetExe))
                        {
                            try
                            {
                                Console.WriteLine("Warning: Cannot find 'nuget.exe'. Ensure it is in the application directory or in the %CSSCRIPT_DIR%/lib");
                            }
                            catch { }
                            nuGetExe = null;
                        }
                    }
                }
                return nuGetExe;
            }
        }

        static bool IsPackageDownloaded(string packageDir, string packageVersion)
        {
            if (!Directory.Exists(packageDir))
                return false;

            if (!string.IsNullOrEmpty(packageVersion))
            {
                string packageVersionDir = Path.Combine(packageDir, Path.GetFileName(packageDir) + "." + packageVersion);
                return Directory.Exists(packageVersionDir);
            }
            else
            {
                return Directory.Exists(packageDir) && Directory.GetDirectories(packageDir).Length > 0;
            }
        }

        static public string[] Resolve(string[] packages, bool supressDownloading, string script)
        {
#if net1
            return new string[0];
        }
#else
            if (!Utils.IsLinux())
            {
                List<string> assemblies = new List<string>();

                bool promptPrinted = false;
                foreach (string item in packages)
                {
                    // //css_nuget -noref -ng:"-IncludePrerelease –version 1.0beta" cs-script
                    // //css_nuget -noref -ver:"4.1.0-alpha1" -ng:"-Pre" NLog

                    string package = item;
                    string nugetArgs = "";
                    string packageVersion = "";

                    bool supressReferencing = item.StartsWith("-noref");
                    if (supressReferencing)
                        package = item.Replace("-noref", "").Trim();

                    bool forceDownloading = item.StartsWith("-force");
                    uint forceTimeout = 0;

                    if (forceDownloading)
                    {
                        if (item.StartsWith("-force:")) //'-force:<seconds>'
                        {
                            var pos = item.IndexOf(" ");
                            if (pos != -1)
                            {
                                var forceStatement = item.Substring(0, pos);
                                package = item.Replace(forceStatement, "").Trim();

                                string timeout = forceStatement.Replace("-force:", "");
                                forceTimeout = 0;
                                uint.TryParse(timeout, out forceTimeout);
                            }
                            else
                            {
                                //the syntax is wrong; let it fail
                            }
                        }
                        else
                        {
                            package = item.Replace("-force", "").Trim();
                        }
                    }

                    if (package.StartsWith("-ver:"))
                    {
                        package = package.Replace("-ver:", "");
                        int argEnd = package.IndexOf(" ");

                        if (package.StartsWith("\""))
                        {
                            package = package.Substring(1, package.Length - 1);
                            argEnd = package.IndexOf("\"");
                        }
                        if (argEnd != -1)
                        {
                            packageVersion = package.Substring(0, argEnd).Trim();

                            if (package[argEnd] == '"')
                                package = package.Substring(argEnd + 1).Trim();
                            else
                                package = package.Substring(argEnd).Trim();
                        }
                    }

                    int nameStart = package.LastIndexOf(" ");
                    if (nameStart != -1)
                    {
                        if (package.StartsWith("-ng:"))
                        {
                            nugetArgs = package.Substring(0, nameStart).Replace("-ng:", "").Trim();
                            if (nugetArgs.StartsWith("\"") && nugetArgs.EndsWith("\""))
                                nugetArgs = nugetArgs.Substring(1, nugetArgs.Length - 2);
                        }
                        package = package.Substring(nameStart).Trim();
                    }

                    string packageDir = Path.Combine(NuGetCache, package);

                    if (Directory.Exists(packageDir) && forceDownloading)
                    {
                        var age = DateTime.Now.ToUniversalTime() - Directory.GetLastWriteTimeUtc(packageDir);
                        if (age.TotalSeconds < forceTimeout)
                            forceDownloading = false;
                    }


                    if (supressDownloading)
                    {
                        //it is OK if the package is not downloaded (e.g. N++ intellisense)
                        if (!supressReferencing && IsPackageDownloaded(packageDir, packageVersion))
                            assemblies.AddRange(GetPackageLibDlls(package, packageVersion));
                    }
                    else
                    {
                        if (forceDownloading || !IsPackageDownloaded(packageDir, packageVersion))
                        {
                            if (!promptPrinted)
                                Console.WriteLine("NuGet> Processing NuGet packages...");

                            promptPrinted = true;

                            try
                            {
                                if (packageVersion != "")
                                    nugetArgs = "-version \"" + packageVersion + "\" " + nugetArgs;
                                var sw = new Stopwatch();
                                sw.Start();
                                Run(NuGetExe, "install " + package + " " + nugetArgs + " -OutputDirectory " + packageDir);
                                sw.Stop();
                            }
                            catch { }

                            try
                            {
                                Directory.SetLastWriteTimeUtc(packageDir, DateTime.Now.ToUniversalTime());
                            }
                            catch { }
                        }

                        if (!IsPackageDownloaded(packageDir, packageVersion))
                            throw new ApplicationException("Cannot process NuGet package '" + package + "'");

                        if (!supressReferencing)
                            assemblies.AddRange(GetPackageLibDlls(package, packageVersion));
                    }
                }

                return Utils.RemovePathDuplicates(assemblies.ToArray());
            }
            return new string[0];
        }

        static string GetPackageName(string path)
        {
            var result = Path.GetFileName(path);

            //WixSharp.bin.1.0.30.4-HotFix
            int i = 0;
            char? prev = null;
            for (; i < result.Length; i++)
            {
                char current = result[i];
                if ((prev.HasValue && prev == '.') && char.IsDigit(current))
                {
                    i = i - 2; //-currPos-prevPos
                    break;
                }
                prev = current;
            }

            result = result.Substring(0, i + 1); //i-inclusive
            return result;
        }

        static bool IsPackageDir(string dirPath, string packageName)
        {
            var dirName = Path.GetFileName(dirPath);

            if (Utils.PathCompare(dirName, packageName) == 0)
            {
                return true;
            }
            else
            {
                if (dirName.StartsWith(packageName + ".", StringComparison.OrdinalIgnoreCase))
                {
                    var version = dirName.Substring(packageName.Length + 1);
#if net4
                    Version ver;
                    return Version.TryParse(version, out ver);
#else 
                    try
                    {
                        new Version(version);
                        return true;
                    }
                    catch { }
                    return false;
#endif
                }
            }
            return false;
        }

        static public string[] GetPackageDependencies(string rootDir, string package)
        {
            var packages = Directory.GetDirectories(rootDir)
                                    .Select(x => GetPackageName(x))
                                    .Where(x => x != package)
                                    .Distinct()
                                    .ToArray();

            return packages;
        }

        static public string[] GetPackageLibDirs(string package, string version)
        {
            List<string> result = new List<string>();

            //cs-script will always store dependency packages in the package root directory:
            //
            //C:\ProgramData\CS-Script\nuget\WixSharp\WixSharp.1.0.30.4
            //C:\ProgramData\CS-Script\nuget\WixSharp\WixSharp.bin.1.0.30.4

            string packageDir = Path.Combine(NuGetCache, package);

            result.AddRange(GetSinglePackageLibDirs(package, version));

            foreach (string dependency in GetPackageDependencies(packageDir, package))
                result.AddRange(GetSinglePackageLibDirs(dependency, "", packageDir)); //do not assume the dependency has the same version as the major package; Get the latest instead 

            return result.ToArray();
        }

        static public string[] GetSinglePackageLibDirs(string package, string version)
        {
            return GetSinglePackageLibDirs(package, version, null);
        }

        static public string[] GetSinglePackageLibDirs(string package, string version, string rootDir)
        {
            List<string> result = new List<string>();

            string packageDir = rootDir ?? Path.Combine(NuGetCache, package);

            string requiredVersion;

            if (!string.IsNullOrEmpty(version))
                requiredVersion = Path.Combine(packageDir, Path.GetFileName(package) + "." + version);
            else
                requiredVersion = Directory.GetDirectories(packageDir)
                                           .Where(x => IsPackageDir(x, package))
                                           .OrderByDescending(x => x)
                                           .FirstOrDefault();

            string lib = Path.Combine(requiredVersion, "lib");

            if (!Directory.Exists(lib))
                return result.ToArray();

            string compatibleVersion = null;
            if (Directory.GetFiles(lib, "*.dll").Any())
                result.Add(lib);

            var libVersions = Directory.GetDirectories(lib, "net*");

            if (libVersions.Length != 0)
            {
                if (Utils.IsNet45Plus())
                    compatibleVersion = libVersions.FirstOrDefault(x => Path.GetFileName(x).StartsWith("net45", StringComparison.OrdinalIgnoreCase));

                if (compatibleVersion == null && Utils.IsNet40Plus())
                    compatibleVersion = libVersions.FirstOrDefault(x => Path.GetFileName(x).StartsWith("net40", StringComparison.OrdinalIgnoreCase));

                if (compatibleVersion == null && Utils.IsNet20Plus())
                {
                    compatibleVersion = libVersions.FirstOrDefault(x => Path.GetFileName(x).StartsWith("net35", StringComparison.OrdinalIgnoreCase));

                    if (compatibleVersion == null)
                        compatibleVersion = libVersions.FirstOrDefault(x => Path.GetFileName(x).StartsWith("net30", StringComparison.OrdinalIgnoreCase));

                    if (compatibleVersion == null)
                        compatibleVersion = libVersions.FirstOrDefault(x => Path.GetFileName(x).StartsWith("net20", StringComparison.OrdinalIgnoreCase));
                }

                if (compatibleVersion != null)
                    result.Add(compatibleVersion);
            }

            return result.ToArray();
        }

        static string[] GetPackageLibDlls(string package, string version)
        {
            List<string> dlls = new List<string>();
            foreach (string dir in GetPackageLibDirs(package, version))
                dlls.AddRange(Directory.GetFiles(dir, "*.dll"));

            List<string> assemblies = new List<string>();

            foreach (var item in dlls)
            {
                //official NuGet documentation states that .resources.dll is not references so we do the same
                if (!item.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                {
                    if (Utils.IsRuntimeCompatibleAsm(item))
                        assemblies.Add(item);
                }
            }

            return assemblies.ToArray();
        }

        static Thread StartMonitor(StreamReader stream)
        {
            Thread retval = new Thread(x =>
            {
                try
                {
                    string line = null;
                    while (null != (line = stream.ReadLine()))
                    {
                        Console.WriteLine(line);
                    }
                }
                catch { }
            });
            retval.Start();
            return retval;
        }

        static void Run(string exe, string args)
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();

                var error = StartMonitor(p.StandardError);
                var output = StartMonitor(p.StandardOutput);

                p.WaitForExit();

                error.Abort();
                output.Abort();
            }
        }
#endif

    }

    //internal class ComInit
    //{
    //    public enum RpcAuthnLevel
    //    {
    //        Default = 0,
    //        None,
    //        Connect,
    //        Call,
    //        Pkt,
    //        PktIntegrity,
    //        PktPrivacy
    //    }

    //    public enum RpcImpLevel
    //    {
    //        Default = 0,
    //        Anonymous,
    //        Identify,
    //        Impersonate,
    //        Delegate
    //    }

    //    public enum EoAuthnCap
    //    {
    //        None = 0x00,
    //        MutualAuth = 0x01,
    //        StaticCloaking = 0x20,
    //        DynamicCloaking = 0x40,
    //        AnyAuthority = 0x80,
    //        MakeFullSIC = 0x100,
    //        Default = 0x800,
    //        SecureRefs = 0x02,
    //        AccessControl = 0x04,
    //        AppID = 0x08,
    //        Dynamic = 0x10,
    //        RequireFullSIC = 0x200,
    //        AutoImpersonate = 0x400,
    //        NoCustomMarshal = 0x2000,
    //        DisableAAA = 0x1000
    //    }

    //    [DllImport("ole32.dll")]
    //    public static extern int CoInitializeSecurity(IntPtr pVoid,
    //                                                       int cAuthSvc,
    //                                                       IntPtr asAuthSvc,
    //                                                       IntPtr pReserved1,
    //                                                       RpcAuthnLevel level,
    //                                                       RpcImpLevel impers,
    //                                                       IntPtr pAuthList,
    //                                                       EoAuthnCap capabilities,
    //                                                       IntPtr pReserved3);

    //    public ComInit()
    //        : this(EoAuthnCap.DynamicCloaking)
    //    {
    //    }

    //    public ComInit(EoAuthnCap capabilities)
    //    {
    //        int hr = CoInitializeSecurity(
    //                                IntPtr.Zero,
    //                                -1,
    //                                IntPtr.Zero,
    //                                IntPtr.Zero,
    //                                RpcAuthnLevel.Default,
    //                                RpcImpLevel.Impersonate,
    //                                IntPtr.Zero,
    //                                capabilities,
    //                                IntPtr.Zero);
    //        if (hr != 0)
    //        {
    //            throw new ApplicationException("CoInitializeSecurity failed. [" + hr.ToString("0x8") + "]");
    //        }
    //    }
    //}

}