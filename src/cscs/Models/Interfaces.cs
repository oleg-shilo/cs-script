using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace csscript
{
    /// <summary>
    /// Delegate implementing source file probing algorithm.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="searchDirs">The extra dirs.</param>
    /// <param name="throwOnError">if set to <c>true</c> [throw on error].</param>
    /// <returns></returns>
    public delegate string[] ResolveSourceFileAlgorithm(string file, string[] searchDirs, bool throwOnError);

    /// <summary>
    /// Delegate implementing assembly file probing algorithm.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="searchDirs">The extra dirs.</param>
    /// <returns></returns>
    public delegate string[] ResolveAssemblyHandler(string file, string[] searchDirs);

    internal interface IScriptExecutor
    {
        void ShowHelpFor(string arg);

        void ShowProjectFor(string arg);

        void EnableWpf(string arg);

        void ShowHelp(string helpType, params object[] context);

        void DoCacheOperations(string command);

        void ShowVersion(string arg = null);

        void ShowPrecompilerSample();

        void CreateDefaultConfigFile();

        void PrintDefaultConfig();

        void PrintDecoratedAutoclass(string script);

        void ProcessConfigCommand(string command);

        void Sample(string version, string file = null);

        ExecuteOptions GetOptions();

        string WaitForInputBeforeExit { get; set; }
    }

    public class BuildRequest
    {
        public string Source;
        public string Assembly;
        public bool IsDebug;
        public string[] References;
    }

    class UniqueAssemblyLocations
    {
        public static explicit operator string[](UniqueAssemblyLocations obj)
        {
            string[] retval = new string[obj.locations.Count];
            obj.locations.Values.CopyTo(retval, 0);
            return retval;
        }

        public void AddAssembly(string location)
        {
            string assemblyID = Path.GetFileName(location).ToUpperInvariant();
            if (!locations.ContainsKey(assemblyID))
                locations[assemblyID] = location;
        }

        public bool ContainsAssembly(string name)
        {
            string assemblyID = name.ToUpperInvariant();
            foreach (string key in locations.Keys)
            {
                if (Path.GetFileNameWithoutExtension(key) == assemblyID)
                    return true;
            }
            return false;
        }

        System.Collections.Hashtable locations = new System.Collections.Hashtable();
    }
}