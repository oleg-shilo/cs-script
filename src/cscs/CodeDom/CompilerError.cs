using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using CSScripting;
using CSScriptLib;

namespace CSScripting.CodeDom
{
    /// <summary>
    ///
    /// </summary>
    public class CompilerError
    {
        /// <summary>
        /// Gets or sets the line number of the syntax causing the error.
        /// </summary>
        /// <value>
        /// The line.
        /// </value>
        public int Line { get; set; }

        /// <summary>
        /// Gets or sets the column number of the syntax causing the error.
        /// </summary>
        /// <value>
        /// The column.
        /// </value>
        public int Column { get; set; }

        /// <summary>
        /// Gets or sets the error number.
        /// </summary>
        /// <value>
        /// The error number.
        /// </value>
        public string ErrorNumber { get; set; }

        /// <summary>
        /// Gets or sets the error text.
        /// </summary>
        /// <value>
        /// The error text.
        /// </value>
        public string ErrorText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance of the error is a warning.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is warning; otherwise, <c>false</c>.
        /// </value>
        public bool IsWarning { get; set; }

        /// <summary>
        /// Gets or sets the name of the file causing the error.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        public string FileName { get; set; }

        /// <summary>
        /// Parses the specified compiler output.
        /// </summary>
        /// <param name="compilerOutput">The compiler output.</param>
        /// <returns><c>Error</c> object</returns>
        public static CompilerError Parse(string compilerOutput)
        {
            // C:\Program Files\dotnet\sdk\2.1.300-preview1-008174\Sdks\Microsoft.NET.Sdk\build\Microsoft.NET.ConflictResolution.targets(59,5): error MSB4018: The "ResolvePackageFileConflicts" task failed unexpectedly. [C:\Users\%username%\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            // script.cs(11,8): error CS1029: #error: 'this is the error...' [C:\Users\%username%\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            // script.cs(10,10): warning CS1030: #warning: 'DEBUG is defined' [C:\Users\%username%\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            // MSBUILD : error MSB1001: Unknown switch.

            if (compilerOutput.StartsWith("error CS") ||
                compilerOutput.StartsWith("vbc : error BC"))
                compilerOutput = "(0,0): " + compilerOutput;

            bool isError = compilerOutput.Contains("): error ");

            bool isWarning = compilerOutput.Contains("): warning ");
            bool isBuildError = compilerOutput.Contains("MSBUILD : error", StringComparison.OrdinalIgnoreCase) ||
                                compilerOutput.Contains("vbc : error", StringComparison.OrdinalIgnoreCase) ||
                                compilerOutput.StartsWith("fatal error ", StringComparison.OrdinalIgnoreCase) ||
                                compilerOutput.Contains("CSC : error", StringComparison.OrdinalIgnoreCase);

            if (isBuildError)
            {
                var parts = compilerOutput.Replace("MSBUILD : error ", "", StringComparison.OrdinalIgnoreCase)
                                          .Replace("CSC : error ", "", StringComparison.OrdinalIgnoreCase)
                                          .Split(":".ToCharArray(), 2);
                return new CompilerError
                {
                    ErrorText = parts.Last().Trim(),        // MSBUILD error: Unknown switch.
                    ErrorNumber = parts.First()             // MSB1001
                };
            }
            else if (isWarning || isError)
            {
                var result = new CompilerError();

                var rx = new Regex(@".*\(\d+\,\d+\)\:");
                var match = rx.Match(compilerOutput, 0);
                if (match.Success)
                {
                    try
                    {
                        var m = Regex.Match(match.Value, @"\(\d+\,\d+\)\:");

                        var location_items = m.Value.Substring(1, m.Length - 3).Split(separator: ',').ToArray();
                        var description_items = compilerOutput.Substring(match.Value.Length).Split(":".ToArray(), 2);

                        result.ErrorText = description_items.Last();                            // #error: 'this is the error...'
                        result.ErrorNumber = description_items.First().Split(' ').Last();       // CS1029
                        result.IsWarning = isWarning;
                        result.FileName = match.Value.Substring(0, m.Index);                    // cript.cs
                        result.Line = int.Parse(location_items[0]);                             // 11
                        result.Column = int.Parse(location_items[1]);                           // 8

                        int desc_end = result.ErrorText.LastIndexOf("[");
                        if (desc_end != -1)
                            result.ErrorText = result.ErrorText.Substring(0, desc_end);

                        return result;
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e);
                    }
                }
            }
            return null;
        }
    }
}