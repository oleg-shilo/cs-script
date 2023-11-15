using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using CSScripting;
using CSScripting.CodeDom;

namespace csscript
{
    /// <summary>
    /// The exception that is thrown when an invalid CS-Script directive is encountered.
    /// </summary>
    /// <seealso cref="csscript.CompilerException"/>
    class InvalidDirectiveException : CompilerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDirectiveException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public InvalidDirectiveException(string message) : base(message)
        {
        }
    }

    class CLIExitRequest : CLIException
    {
        static public void Throw(string message = null) => throw new CLIExitRequest(message);

        public CLIExitRequest(string message = null) : base(message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a the script CLI error occurs.
    /// </summary>
    class CLIException : ApplicationException
    {
        public int ExitCode = -1;

        public CLIException(string message = null) : base(message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a the script compiler error occurs.
    /// </summary>
    [Serializable]
    public class CompilerException : ApplicationException
    {
        ///// <summary>
        ///// Gets or sets the errors.
        ///// </summary>
        ///// <value>The errors.</value>
        //public CompilerErrorCollection Errors { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerException"/> class.
        /// </summary>
        public CompilerException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public CompilerException(string message) : base(message) { }

        /// <summary>
        /// Gets or sets the error count associated with the last script compilation.
        /// </summary>
        /// <value>The error count.</value>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Creates the CompilerException instance from the specified compiler errors.
        /// </summary>
        /// <param name="Errors">The compiler errors.</param>
        /// <param name="hideCompilerWarnings">if set to <c>true</c> hide compiler warnings.</param>
        /// <param name="resolveAutogenFilesRefs">
        /// if set to <c>true</c> all references to the path of the derived auto-generated files
        /// (e.g. errors in the decorated classless scripts) will be replaced with the path of the
        /// original files (e.g. classless script itself).
        /// </param>
        /// <returns></returns>
        public static CompilerException Create(IEnumerable<CompilerError> Errors, bool hideCompilerWarnings, bool resolveAutogenFilesRefs)
        {
            var compileErr = new StringBuilder();

            int errorCount = 0;

            foreach (CompilerError err in Errors)
            {
                if (!err.IsWarning)
                    errorCount++;

                if (err.IsWarning && hideCompilerWarnings)
                    continue;

                if (err.FileName.HasText())
                {
                    string file = err.FileName;
                    int line = err.Line;

                    if (resolveAutogenFilesRefs)
                        CoreExtensions.NormaliseFileReference(ref file, ref line);

                    compileErr.Append(file)
                              .Append("(")
                              .Append(line)
                              .Append(",")
                              .Append(err.Column)
                              .Append("): ");
                }
                else
                {
                    compileErr.Append("BUILD: ");
                }

                if (err.IsWarning)
                    compileErr.Append("warning ");
                else
                    compileErr.Append("error ");
                compileErr.Append(err.ErrorNumber)
                          .Append(": ")
                          .Append(err.ErrorText)
                          .Append(Environment.NewLine);
            }

            var retval = new CompilerException(compileErr.ToString());
            retval.Data.Add("Errors", Errors);
            retval.ErrorCount = errorCount;
            return retval;
        }

        internal static CompilerException Create(string errorText, string file, CompilerException parentException)
        {
            var error = new CompilerError
            {
                FileName = file,
                ErrorText = errorText
            };

            var errors = new List<CompilerError>();
            errors.Add(error);
            errors.AddRange((CompilerError[])parentException.Data["Errors"]);

            var retval = new CompilerException(error.ToString() + Environment.NewLine + parentException.Message);
            retval.Data.Add("Errors", errors.ToArray());
            retval.ErrorCount = errors.Count;

            return retval;
        }
    }
}