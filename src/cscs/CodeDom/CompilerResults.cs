using System;
using System.Collections.Generic;
using static System.Environment;
using System.Linq;
using System.Reflection;
using csscript;
using CSScriptLib;

namespace CSScripting.CodeDom
{
    /// <summary>
    ///
    /// </summary>
    public class CompilerResults
    {
        /// <summary>
        /// Gets or sets the collection of the temporary files that the runtime will delete on the application exit.
        /// </summary>
        /// <value>
        /// The temporary files.
        /// </value>
        public TempFileCollection TempFiles { get; set; } = new TempFileCollection();

        /// <summary>
        /// Gets or sets the probing directories to be used for assembly probings.
        /// </summary>
        /// <value>
        /// The probing directories.
        /// </value>
        public List<string> ProbingDirs { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the compiled assembly.
        /// </summary>
        /// <value>
        /// The compiled assembly.
        /// </value>
        public Assembly CompiledAssembly { get; set; }

        /// <summary>
        /// Gets or sets the errors.
        /// </summary>
        /// <value>
        /// The errors.
        /// </value>
        public List<CompilerError> Errors { get; set; } = new List<CompilerError>();

        /// <summary>
        /// Gets or sets the output.
        /// </summary>
        /// <value>
        /// The output.
        /// </value>
        public List<string> Output { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the path to assembly.
        /// </summary>
        /// <value>
        /// The path to assembly.
        /// </value>
        public string PathToAssembly { get; set; }

        /// <summary>
        /// Gets or sets the native compiler return value, which is a process exit code of the compiler executable (e.g. dotnet.exe).
        /// </summary>
        /// <value>
        /// The native compiler return value.
        /// </value>
        public int NativeCompilerReturnValue { get; set; }

        internal void ProcessErrors()
        {
            var isErrroSection = true;

            // only dotnet has a distinctive error message that separates "info" and "error" section.
            // It is particularly important to process only the "error" section as dotnet compiler prints
            // the same errors in both of these sections.

            if (CSExecutor.options.compilerEngine == null || CSExecutor.options.compilerEngine == Directives.compiler_dotnet)
                isErrroSection = false;

            // Console.WriteLine("-----------");
            // Console.WriteLine(">>>" + Output.JoinBy("\n"));
            // Console.WriteLine("-----------");
            // Build succeeded.
            foreach (var line in Output)
            {
                if (!isErrroSection)
                {
                    // MSBUILD : error MSB1001: Unknown switch.
                    if (line.StartsWith("Build FAILED.") || line.StartsWith("Build succeeded."))
                        isErrroSection = true;

                    if (line.Contains("CSC : error ") || line.Contains("): error ") || line.StartsWith("error CS") || line.StartsWith("vbc : error BC") || line.Contains("MSBUILD : error "))
                    {
                        var error = CompilerError.Parse(line);
                        if (error != null)
                            Errors.Add(error);
                    }
                }
                else
                {
                    if (line.IsNotEmpty())
                    {
                        var error = CompilerError.Parse(line);
                        if (error != null)
                            Errors.Add(error);
                    }
                }
            }

            if (this.NativeCompilerReturnValue != 0 && Errors.IsEmpty() && !Output.IsEmpty())
                Errors.Add(new CompilerError
                {
                    ErrorText = NewLine + Output.Where(x => x.IsNotEmpty()).JoinBy(NewLine),
                    ErrorNumber = "CS0000"
                });
        }
    }
}