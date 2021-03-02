using Microsoft.CodeAnalysis;

// using Microsoft.CodeAnalysis.Emit;
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

    public class BuildResult
    {
        // using XML or JSON serializer is the best approach, though the first one  is heavy
        // and the second one requires an additional NuGet package, which even does not exist
        // for .NET Standard 2.0.
        // Thus for the temp solution like this go with a simple line serialization
        public class Diagnostic
        {
            /* Microsoft.CodeAnalysis.Diagnostic is coupled to the source tree and other CodeDOM objects
             * so need to use an adapter.
             */

            // public static BuildResult.Diagnostic From(Microsoft.CodeAnalysis.Diagnostic data)
            // {
            //     return new BuildResult.Diagnostic
            //     {
            //         IsWarningAsError = data.IsWarningAsError,
            //         // Severity = data.Severity,
            //         Location_IsInSource = data.Location.IsInSource,
            //         Location_StartLinePosition_Line = data.Location.GetLineSpan().StartLinePosition.Line,
            //         Location_StartLinePosition_Character = data.Location.GetLineSpan().StartLinePosition.Character,
            //         Location_FilePath = data.Location.SourceTree.FilePath,
            //         Id = data.Id,
            //         Message = data.GetMessage()
            //     };
            // }

            public bool IsWarningAsError;

            // public DiagnosticSeverity Severity;
            public bool Location_IsInSource;

            public int Location_StartLinePosition_Line;
            public int Location_StartLinePosition_Character;
            public string Location_FilePath;
            public string Id;
            public string Message;
        }

        public bool Success;
        // public List<BuildResult.Diagnostic> Diagnostics = new List<BuildResult.Diagnostic>();

        // public static BuildResult From(EmitResult data)
        // {
        //     return new BuildResult
        //     {
        //         Success = data.Success,
        //         Diagnostics = data.Diagnostics.Select(x => Diagnostic.From(x)).ToList()
        //     };
        // }
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