// Ignore Spelling: Dirs

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

    /// <summary>
    /// Provides methods and properties for managing script execution and caching in CSSCRIPT.
    /// <p>This class is a functional CSScriptLib polyfill of the CLI CSExecutor.</p>
    /// </summary>
    /// <remarks>This class includes functionality to set and retrieve the cache directory for script files,
    /// ensuring that each script can have its own temporary cache.</remarks>
    class CSExecutor
    {
        internal static ExecuteOptions options = new ExecuteOptions();

        /// <summary>
        /// Contains the name of the temporary cache folder in the CSSCRIPT subfolder of Path.GetTempPath().
        /// The cache folder is specific for every script file.
        /// </summary>
        static public string ScriptCacheDir { get; set; } = "";

        /// <summary>
        /// Sets the script cache directory for the specified script file.
        /// </summary>
        /// <param name="scriptFile">The script file path.</param>
        static public void SetScriptCacheDir(string scriptFile)
        {
            ScriptCacheDir = CSScript.GetScriptCacheDir(scriptFile);
        }

        /// <summary>
        /// Generates the name of the cache directory for the specified script file.
        /// </summary>
        /// <param name="file">Script file name.</param>
        /// <returns>Cache directory name.</returns>
        /// <remarks>
        /// This method delegates to <see cref="CSScript.GetScriptCacheDir(string)"/>.
        /// </remarks>
        public static string GetCacheDirectory(string file)
            => CSScript.GetScriptCacheDir(file);
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
        [Obsolete($"This property is obsolete, use {nameof(CurrentConfigFile)} instead.")]
        public static string DefaultConfigFile => CurrentConfigFile;

        /// <summary>
        /// Gets the current configuration file path. It is a "css_config.json" file located either in the same directory
        /// where the assembly being executed.
        /// </summary>
        /// <value>
        /// The default configuration file location. Returns null if the file is not found.
        /// </value>
        public static string CurrentConfigFile
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
        /// Returns the name of the temporary folder in the `csscript.lib` subfolder of Path.GetTempPath().
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
                    tempDir = Path.Combine(Path.GetTempPath(), "csscript.lib");
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
                return Path.Combine(GetScriptTempDir(), string.Format("{0}.{1}.tmp.cs", Process.GetCurrentProcess().Id, Guid.NewGuid()));
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
        [Obsolete(message:
            "This property is no longer in use. It was serving CS-Script Native API, which is no longer supported due " +
            "to the breaking changes of .NET 5/Core. Use `CSScript.Evaluator.With(eval => eval.IsCachingEnabled = false)...` instead.")]
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
                        try
                        {
                            Runtime.CleanUnusedTmpFiles(CSScript.GetScriptTempDir(), "*????????-????-????-????-????????????.???*", ignoreCurrentProcessScripts);
                            Runtime.CleanAbandonedCache(CSScript.GetCacheDir());
                        }
                        catch { }
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
                    if (!file.IsParentProcessRunning())
                        file.FileDelete(rethrow: false);
                }
        }

        static Lazy<RoslynEvaluator> roslynEvaluator = new Lazy<RoslynEvaluator>();

        /// <summary>
        /// Wraps a fragment of method code into a complete, compilable class source.
        /// The method code is scanned for header using directives and the first non-using, non-comment, non-empty
        /// line is treated as the method signature/start of the body. Depending on the flags the generated
        /// source will be injected with a containing namespace, a class declaration (optionally inheriting from
        /// <paramref name="inheritFrom"/>), and a static/public modifier if missing.
        /// </summary>
        /// <param name="methodCode">The C# code fragment that contains a method or methods to be wrapped.</param>
        /// <param name="injectStatic">If true and the method declaration does not already contain the <c>static</c>
        /// modifier, a <c>static</c> token will be injected into the generated member declaration.</param>
        /// <param name="injectNamespace">If true the generated class will be placed into the <c>Scripting</c>
        /// namespace.</param>
        /// <param name="inheritFrom">Optional base type name to inherit from. If provided the generated class
        /// will have an inheritance clause using this value.</param>
        /// <param name="className">Optional name to use for the generated class. If not provided a default
        /// wrapper class name will be used.</param>
        /// <returns>A string containing the complete C# source for the auto-generated class that wraps the
        /// supplied method code.</returns>
        static public string WrapMethodToAutoClass(string methodCode, bool injectStatic, bool injectNamespace, string inheritFrom = null, string className = null)
        {
            var code = new StringBuilder(4096);
            code.AppendLine("//Auto-generated file")
                .AppendLine("using System;");

            bool headerProcessed = false;

            string classNameToUse = className.IsNotEmpty() ? className : Globals.DynamicWrapperClassName;

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
                                code.AppendLine($"   public class {classNameToUse} : " + inheritFrom);
                            else
                                code.AppendLine($"   public class {classNameToUse}");

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

        /// <summary>
        /// Returns a script-friendly type name for the provided <see cref="Type"/>.
        /// For non-generic types this is the type's full name. For generic types the
        /// method constructs a readable generic representation (e.g. "Namespace.TypeName&lt;Arg1, Arg2&gt;")
        /// by recursively formatting generic type arguments.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to get the script name for.</param>
        /// <returns>A string representing the type suitable for use in generated script code.</returns>
        static public string TypeNameForScript(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName;
            }
            string typeName = type.Name;
            //remove generic type suffix from name.
            typeName = typeName.Substring(0, typeName.IndexOf('`'));
            return $"{type.Namespace}.{typeName}<{string.Join(", ", type.GenericTypeArguments.Select(TypeNameForScript))}>";
        }

        /// <summary>
        /// Gets the full path to the cache directory used for temporary script-related files.
        /// </summary>
        /// <returns>A string containing the absolute path to the cache directory. The directory is located within the temporary
        /// script directory.</returns>
        public static string GetCacheDir() => Path.Combine(GetScriptTempDir(), "cache");

        /// <summary>
        /// Gets the directory path used for caching script-related files based on the specified script file path.
        /// </summary>
        /// <remarks>
        /// The cache directory is determined by hashing the directory path of the provided script file.
        /// On Windows systems, the hash is case-insensitive to ensure consistent results regardless of
        /// path casing. The cache directory is automatically created if it doesn't exist.
        /// </remarks>
        /// <param name="scriptFile">The path of the script file for which to retrieve the cache directory.</param>
        /// <returns>
        /// The full path to the cache directory associated with the specified script file.
        /// The directory is created if it does not already exist.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if the cache directory cannot be created due to insufficient write privileges.
        /// </exception>
        public static string GetScriptCacheDir(string scriptFile)
        {
            string commonCacheDir = GetCacheDir();
            string directoryPath = Path.GetDirectoryName(Path.GetFullPath(scriptFile));

            // Win is case-insensitive so ensure both lower and capital case paths yield the same hash
            string dirHash = Runtime.IsWin
                ? directoryPath.ToLower().GetHashCodeEx().ToString()
                : directoryPath.GetHashCodeEx().ToString();

            string cacheDir = Path.Combine(commonCacheDir, dirHash);

            // Create directory if it doesn't exist
            if (!Directory.Exists(cacheDir))
            {
                try
                {
                    Directory.CreateDirectory(cacheDir);
                }
                catch (UnauthorizedAccessException)
                {
                    var parentDir = Directory.Exists(commonCacheDir)
                        ? commonCacheDir
                        : Path.GetDirectoryName(commonCacheDir);

                    throw new Exception(
                        $"You do not have write privileges for the CS-Script cache directory ({parentDir}). " +
                        "Make sure you have sufficient privileges or use an alternative location as the CS-Script " +
                        "temporary directory (cscs -config:set=CustomTempDirectory=<new temp dir>)");
                }
            }

            // Create info file
            string infoFile = Path.Combine(cacheDir, "css_info.txt");
            if (!File.Exists(infoFile))
            {
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
                    // There can be many reasons for failure (e.g. file locked by another writer)
                    // In most cases this does not constitute an error
                }
            }

            return cacheDir;
        }
    }
}