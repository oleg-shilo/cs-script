using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSScripting;

namespace CSScriptLib
{
    class ExecuteOptions
    {
        public string compilerEngine = Directives.compiler_csc;
    }

    class CSExecutor
    {
        internal static ExecuteOptions options = new ExecuteOptions();

        ///<summary>
        /// Contains the name of the temporary cache folder in the CSSCRIPT subfolder of Path.GetTempPath(). The cache folder is specific for every script file.
        /// </summary>
        static public string ScriptCacheDir { get; set; } = "";

        static public void SetScriptCacheDir(string scriptFile)
        {
            string newCacheDir = GetCacheDirectory(scriptFile); //this will also create the directory if it does not exist
            ScriptCacheDir = newCacheDir;
        }

        /// <summary>
        /// Generates the name of the cache directory for the specified script file.
        /// </summary>
        /// <param name="file">Script file name.</param>
        /// <returns>Cache directory name.</returns>
        public static string GetCacheDirectory(string file)
        {
            string commonCacheDir = Path.Combine(CSScript.GetScriptTempDir(), "cache");

            string cacheDir;
            string directoryPath = Path.GetDirectoryName(Path.GetFullPath(file));
            string dirHash;
            if (Runtime.IsWin)
            {
                //Win is not case-sensitive so ensure, both lower and capital case path yield the same hash
                dirHash = directoryPath.ToLower().GetHashCodeEx().ToString();
            }
            else
            {
                dirHash = directoryPath.GetHashCodeEx().ToString();
            }

            cacheDir = Path.Combine(commonCacheDir, dirHash);

            if (!Directory.Exists(cacheDir))
                try
                {
                    Directory.CreateDirectory(cacheDir);
                }
                catch (UnauthorizedAccessException)
                {
                    var parentDir = commonCacheDir;

                    if (!Directory.Exists(commonCacheDir))
                        parentDir = Path.GetDirectoryName(commonCacheDir); // GetScriptTempDir()

                    throw new Exception("You do not have write privileges for the CS-Script cache directory (" + parentDir + "). " +
                                        "Make sure you have sufficient privileges or use an alternative location as the CS-Script " +
                                        "temporary  directory (cscs -config:set=CustomTempDirectory=<new temp dir>)");
                }

            string infoFile = Path.Combine(cacheDir, "css_info.txt");
            if (!File.Exists(infoFile))
                try
                {
                    using (var sw = new StreamWriter(infoFile))
                    {
                        sw.WriteLine(Environment.Version.ToString());
                        sw.WriteLine(directoryPath);
                    }
                }
                catch
                {
                    //there can be many reasons for the failure (e.g. file is already locked by another writer),
                    //which in most of the cases does not constitute the error but rather a runtime condition
                }

            return cacheDir;
        }
    }

    /// <summary>
    /// Settings is an class that holds CS-Script application settings.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Loads and returns the settings instance.
        /// </summary>
        public static Func<string, Settings> Load = (file) => new Settings();

        /// <summary>
        /// Gets the default configuration file path. It is a "css_config.xml" file located in the same directory where the assembly
        /// being executed is (e.g. cscs.exe).
        /// </summary>
        /// <value>
        /// The default configuration file location. Returns null if the file is not found.
        /// </value>
        public static string DefaultConfigFile
        {
            get
            {
                try
                {
                    string asm_path = Assembly.GetExecutingAssembly().Location;
                    if (asm_path.IsNotEmpty())
                        return asm_path.ChangeFileName("css_config.xml");
                }
                catch { }
                return null;
            }
        }

        /// <summary>
        /// List of directories to be used to search (probing) for referenced assemblies and script files.
        /// This setting is similar to the system environment variable PATH.
        /// </summary>
        public string[] SearchDirs { get => searchDirs.ToArray(); }

        /// <summary>
        /// Gets or sets the default reference assemblies.
        /// </summary>
        /// <value>
        /// The default reference assemblies.
        /// </value>
        public string DefaultRefAssemblies { get; set; } = "";

        List<string> searchDirs { get; set; } = new List<string>();

        /// <summary>
        /// Clears the search directories.
        /// </summary>
        /// <returns>The Settings instance.</returns>
        public Settings ClearSearchDirs()
        {
            searchDirs.Clear();
            return this;
        }

        /// <summary>
        /// Adds the search directories aggregated from the unique locations of all assemblies referenced by the host application.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <returns>The Settings instance</returns>
        public Settings AddSearchDir(string dir)
        {
            searchDirs.Add(Environment.ExpandEnvironmentVariables(dir));
            return this;
        }

        /// <summary>
        /// Adds the search dirs from host application.
        /// <para>The dirs are the list of locations the all currently loaded assemblies are loaded from.</para>
        /// </summary>
        /// <returns>The Settings instance</returns>
        public Settings AddSearchDirsFromHost()
        {
            try
            {
                var dirs = new List<string>();
                foreach (var asm in Assembly.GetCallingAssembly().GetReferencedAssemblies())
                    try
                    {
                        var dir = Assembly.Load(asm).Directory(); // the asm is already loaded by the host anyway
                        if (dir.IsNotEmpty())
                            dirs.Add(dir);
                    }
                    catch { }
                searchDirs.AddRange(dirs.Distinct());
            }
            catch { }
            return this;
        }
    }

    /// <summary>
    /// Class which is implements CS-Script class library interface.
    /// </summary>
    public partial class CSScript
    {
        /// <summary>
        /// Starts the build server.
        /// </summary>
        static public void StartBuildServer()
            => Globals.StartBuildServer();

        /// <summary>
        /// Stops the build server.
        /// </summary>
        static public void StopBuildServer()
            => Globals.StopBuildServer();

        static EvaluatorConfig evaluatorConfig = new EvaluatorConfig();

        /// <summary>
        /// Gets the CSScript.<see cref="CSScriptLib.EvaluatorConfig"/>, which controls the way code evaluation is conducted at runtime.
        /// </summary>
        /// <value>The evaluator CSScript.<see cref="CSScriptLib.EvaluatorConfig"/>.</value>
        public static EvaluatorConfig EvaluatorConfig
        {
            get { return evaluatorConfig; }
        }

        /// <summary>
        /// Global instance of the generic <see cref="CSScriptLib.IEvaluator"/>. This object is to be used for
        /// dynamic loading of the  C# code by "compiler as service" based on the
        /// <see cref="P:CSScriptLib.CSScript.EvaluatorConfig.Engine"/> value.
        /// <para>Generic <see cref="CSScriptLib.IEvaluator"/> interface provides a convenient way of accessing
        /// compilers without 'committing' to a specific compiler technology (e.g. Mono, Roslyn, CodeDOM). This may be
        /// required during troubleshooting or performance tuning.</para>
        /// <para>Switching between compilers can be done via global
        /// CSScript.<see cref="P:CSScriptLib.CSScript.EvaluatorConfig.Engine"/>.</para>
        /// <remarks>
        /// By default CSScript.<see cref="CSScriptLib.CSScript.Evaluator"/> always returns a new instance of
        /// <see cref="CSScriptLib.IEvaluator"/>. If this behavior is undesired change the evaluator access
        /// policy by setting <see cref="CSScriptLib.CSScript.EvaluatorConfig"/>.Access value.
        /// </remarks>
        /// </summary>
        /// <value>The <see cref="CSScriptLib.IEvaluator"/> instance.</value>
        /// <example>
        ///<code>
        /// if(testingWithMono)
        ///     CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
        /// else
        ///     CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn;
        ///
        /// var sub = CSScript.Evaluator
        ///                   .LoadDelegate&lt;Func&lt;int, int, int&gt;&gt;(
        ///                               @"int Sub(int a, int b) {
        ///                                     return a - b;
        ///                                 }");
        /// </code>
        /// </example>
        static public IEvaluator Evaluator
        {
            get
            {
                switch (CSScript.EvaluatorConfig.Engine)
                {
                    case EvaluatorEngine.Roslyn: return RoslynEvaluator;
                    case EvaluatorEngine.CodeDom: return CodeDomEvaluator;
                    default: return null;
                }
            }
        }

        static string tempDir = null;

        /// <summary>
        /// Returns the name of the temporary folder in the CSSCRIPT subfolder of Path.GetTempPath().
        /// <para>Under certain circumstances it may be desirable to the use the alternative location for the CS-Script temporary files.
        /// In such cases use SetScriptTempDir() to set the alternative location.
        /// </para>
        /// </summary>
        /// <returns>Temporary directory name.</returns>
        static public string GetScriptTempDir()
        {
            if (tempDir == null)
            {
                tempDir = Environment.GetEnvironmentVariable("CSS_CUSTOM_TEMPDIR");
                if (tempDir == null)
                {
                    tempDir = Path.Combine(Path.GetTempPath(), "CSSCRIPT");
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }
                }
            }
            return tempDir;
        }

        /// <summary>
        /// Returns the name of the temporary file in the CSSCRIPT subfolder of Path.GetTempPath().
        /// </summary>
        /// <returns>Temporary file name.</returns>
        static public string GetScriptTempFile()
        {
            lock (typeof(CSScript))
            {
                return Path.Combine(GetScriptTempDir(), string.Format("{0}.{1}.tmp", Process.GetCurrentProcess().Id, Guid.NewGuid()));
            }
        }

        static internal string GetScriptTempFile(string subDir)
        {
            lock (typeof(CSScript))
            {
                string tempDir = Path.Combine(GetScriptTempDir(), subDir);
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                return Path.Combine(tempDir, string.Format("{0}.{1}.tmp", Process.GetCurrentProcess().Id, Guid.NewGuid()));
            }
        }

        /// <summary>
        /// Settings object containing runtime settings, which controls script compilation/execution.
        /// This is Settings class essentially is a deserialized content of the CS-Script configuration file (css_config.xml).
        /// </summary>
        static public Settings GlobalSettings = new Settings();

        /// <summary>
        /// Global instance of <see cref="CSScriptLib.RoslynEvaluator"/>. This object is to be used for
        /// dynamic loading of the  C# code by using Roslyn "compiler as service".
        /// <para>If you need to use multiple instances of th evaluator then you will need to call
        /// <see cref="CSScriptLib.IEvaluator"/>.Clone().
        /// </para>
        /// </summary>
        /// <value> The <see cref="CSScriptLib.RoslynEvaluator"/> instance.</value>
        static public RoslynEvaluator RoslynEvaluator
        {
            get
            {
                if (EvaluatorConfig.Access == EvaluatorAccess.AlwaysCreate)
                    return (RoslynEvaluator)roslynEvaluator.Value.Clone();
                else
                    return roslynEvaluator.Value;
            }
        }

        /// <summary>
        /// Global instance of <see cref="CSScriptLib.CodeDomEvaluator"/>. This object is to be used for
        /// dynamic loading of the  C# code by using CodeDom "compiler as service".
        /// <para>If you need to use multiple instances of th evaluator then you will need to call
        /// <see cref="CSScriptLib.IEvaluator"/>.Clone().
        /// </para>
        /// </summary>
        /// <value> The <see cref="CSScriptLib.CodeDomEvaluator"/> instance.</value>
        static public CodeDomEvaluator CodeDomEvaluator
        {
            get
            {
                if (EvaluatorConfig.Access == EvaluatorAccess.AlwaysCreate)
                    return (CodeDomEvaluator)codeDomEvaluator.Value.Clone();
                else
                    return codeDomEvaluator.Value;
            }
        }

        static Lazy<CodeDomEvaluator> codeDomEvaluator = new Lazy<CodeDomEvaluator>();

        /// <summary>
        /// Controls if ScriptCache should be used when script file loading is requested (CSScript.Load(...)). If set to true and the script file was previously compiled and already loaded
        /// the script engine will use that compiled script from the cache instead of compiling it again.
        /// Note the script cache is always maintained by the script engine. The CacheEnabled property only indicates if the cached script should be used or not when CSScript.Load(...) method is called.
        /// </summary>
        public static bool CacheEnabled { get; set; } = true;

        static List<string> tempFiles;

        /// <summary>
        /// Notes the temporary file to be removed on application exit.
        /// </summary>
        /// <param name="file">The file.</param>
        internal static void NoteTempFile(string file)
        {
            if (file.IsNotEmpty())
            {
                if (tempFiles == null)
                {
                    tempFiles = new List<string>();
                    AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnApplicationExit);
                }
                tempFiles.Add(file);
            }
        }

        static bool purging = false;

        /// <summary>
        /// Starts the purging old temporary files.
        /// </summary>
        /// <param name="ignoreCurrentProcessScripts">if set to <c>true</c> [ignore current process scripts].</param>
        public static void StartPurgingOldTempFiles(bool ignoreCurrentProcessScripts)
        {
            if (!purging)
            {
                purging = true;
                Task.Run(() =>
                    {
                        Runtime.CleanUnusedTmpFiles(CSScript.GetScriptTempDir(), "*????????-????-????-????-????????????.???", ignoreCurrentProcessScripts);
                        // don't do cscs related cleaning, save time.
                        // Runtime.CleanSnippets();
                        // Runtime.CleanAbandonedCache();
                        purging = false;
                    });
            }
        }

        static void OnApplicationExit(object sender, EventArgs e)
        {
            Cleanup();
        }

        static internal void Cleanup()
        {
            if (tempFiles != null)
                foreach (string file in tempFiles)
                {
                    file.FileDelete(rethrow: false);
                }
        }

        static Lazy<RoslynEvaluator> roslynEvaluator = new Lazy<RoslynEvaluator>();

        static internal string WrapMethodToAutoClass(string methodCode, bool injectStatic, bool injectNamespace, string inheritFrom = null)
        {
            var code = new StringBuilder(4096);
            code.AppendLine("//Auto-generated file")
                .AppendLine("using System;");

            bool headerProcessed = false;

            string line;

            using (StringReader sr = new StringReader(methodCode))
                while ((line = sr.ReadLine()) != null)
                {
                    if (!headerProcessed && !line.TrimStart().StartsWith("using ")) //not using...; statement of the file header
                    {
                        string trimmed = line.Trim();
                        if (!trimmed.StartsWith("//") && trimmed != "") //not comments or empty line
                        {
                            headerProcessed = true;

                            if (injectNamespace)
                            {
                                code.AppendLine("namespace Scripting")
                                    .AppendLine("{");
                            }

                            if (inheritFrom != null)
                                code.AppendLine($"   public class {Globals.DynamicWrapperClassName} : " + inheritFrom);
                            else
                                code.AppendLine($"   public class {Globals.DynamicWrapperClassName}");

                            code.AppendLine("   {");
                            string[] tokens = line.Split("\t ".ToCharArray(), 3, StringSplitOptions.RemoveEmptyEntries);

                            if (injectStatic)
                            {
                                //IE "unsafe public static"
                                if (!tokens.Contains("static"))
                                    code.AppendLine("   static");
                            }

                            if (!tokens.Contains("public"))
                                code.AppendLine("   public");
                        }
                    }

                    code.AppendLine(line);
                }

            code.AppendLine("   }");
            if (injectNamespace)
                code.AppendLine("}");

            return code.ToString();
        }
    }
}