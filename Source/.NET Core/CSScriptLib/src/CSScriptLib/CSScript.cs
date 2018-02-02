using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Scripting;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Text;

namespace CSScriptLib
{
    /// <summary>
    /// Settings is an class that holds CS-Script application settings.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// List of directories to be used to search (probing) for referenced assemblies and script files.
        /// This setting is similar to the system environment variable PATH.
        /// </summary>
        public string[] SearchDirs { get => searchDirs.ToArray(); }

        List<string> searchDirs { get; set; } = new List<string>();

        /// <summary>
        /// Clears the search directories.
        /// </summary>
        /// <returns></returns>
        public Settings ClearSearchDirs()
        {
            searchDirs.Clear();
            return this;
        }

        /// <summary>
        /// Adds the search directories aggregated from the unique locations of all assemblies referenced by the host application.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <returns></returns>
        public Settings AddSearchDir(string dir)
        {
            searchDirs.Add(Environment.ExpandEnvironmentVariables(dir));
            return this;
        }

        /// <summary>
        /// Adds the search dirs from host.
        /// </summary>
        /// <returns></returns>
        public Settings AddSearchDirsFromHost()
        {
            try
            {
                var dirs = new List<string>();
                foreach (var asm in Assembly.GetCallingAssembly().GetReferencedAssemblies())
                    try
                    {
                        var dir = Assembly.Load(asm).Directory(); // the asm is already loaded by the host anyway
                        if (dir.HasText())
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

        static List<string> tempFiles;

        internal static void NoteTempFile(string file)
        {
            if (tempFiles == null)
            {
                tempFiles = new List<string>();
                AssemblyLoadContext.Default.Unloading += x => CSScript.Cleanup();
            }
            tempFiles.Add(file);
        }

        static internal void Cleanup()
        {
            if (tempFiles != null)
                foreach (string file in tempFiles)
                {
                    file.FileDelete(rethrow: false);
                }

            // CleanupDynamicSources(); zos
        }

        static Lazy<RoslynEvaluator> roslynEvaluator = new Lazy<RoslynEvaluator>();

        static internal string WrapMethodToAutoClass(string methodCode, bool injectStatic, bool injectNamespace, string inheritFrom = null)
        {
            var code = new StringBuilder(4096);
            code.Append("//Auto-generated file\r\n"); //cannot use AppendLine as it is not available in StringBuilder v1.1
            code.Append("using System;\r\n");

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
                                code.Append("namespace Scripting\r\n");
                                code.Append("{\r\n");
                            }

                            if (inheritFrom != null)
                                code.Append("   public class DynamicClass : " + inheritFrom + "\r\n");
                            else
                                code.Append("   public class DynamicClass\r\n");

                            code.Append("   {\r\n");
                            string[] tokens = line.Split("\t ".ToCharArray(), 3, StringSplitOptions.RemoveEmptyEntries);

                            if (injectStatic)
                            {
                                if (tokens[0] != "static" && tokens[1] != "static" && tokens[2] != "static") //unsafe public static
                                    code.Append("   static\r\n");
                            }

                            if (tokens[0] != "public" && tokens[1] != "public" && tokens[2] != "public")
                                code.Append("   public\r\n");
                        }
                    }

                    code.Append(line);
                    code.Append("\r\n");
                }

            code.Append("   }\r\n");
            if (injectNamespace)
                code.Append("}\r\n");

            return code.ToString();
        }
    }
}