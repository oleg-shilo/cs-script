using System;

namespace CSScriptLib
{
    /// <summary>
    /// Information about the script parsing result.
    /// </summary>
    public class ScriptParsingResult
    {
        /// <summary>
        /// The packages referenced from the script with `//css_nuget` directive
        /// </summary>
        public string[] Packages;

        /// <summary>
        /// The referenced resources referenced from the script with `//css_res` directive
        /// </summary>
        public string[] ReferencedResources;

        /// <summary>
        /// The referenced assemblies referenced from the script with `//css_ref` directive
        /// </summary>
        public string[] ReferencedAssemblies;

        /// <summary>
        /// The namespaces imported with C# `using` directive
        /// </summary>
        public string[] ReferencedNamespaces;

        /// <summary>
        /// The namespaces that are marked as "to ignore" with `//css_ignore_namespace` directive
        /// </summary>
        public string[] IgnoreNamespaces;

        /// <summary>
        /// The compiler options specified with `//css_co` directive
        /// </summary>
        public string[] CompilerOptions;

        /// <summary>
        /// The directories specified with `//css_dir` directive
        /// </summary>
        public string[] SearchDirs;

        /// <summary>
        /// The precompilers specified with `//css_pc` directive
        /// </summary>
        public string[] Precompilers;

        /// <summary>
        /// All files that need to be compiled as part of the script execution.
        /// </summary>
        public string[] FilesToCompile;

        /// <summary>
        /// The time of parsing.
        /// </summary>
        public DateTime Timestamp = DateTime.Now;
    }
}