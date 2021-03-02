using CSScripting.CodeDom;
using CSScriptLib;

namespace csscript
{
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
}