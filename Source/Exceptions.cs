#region Licence...

//-----------------------------------------------------------------------------
// Date:	25/10/10
// Module:	Exceptions.cs
// Classes:	Exceptions
//
//
// This module contains the definition of the CS-Script exceptions
//
// Written by Oleg Shilo (oshilo@gmail.com)
//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2004-2018 Oleg Shilo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------

#endregion Licence...

using System;
using System.CodeDom.Compiler;

using System.Text;
using System.Runtime.Serialization;

namespace csscript
{
    /// <summary>
    /// The exception that is thrown when an incvalid CS-Script directive is encountered.
    /// </summary>
    /// <seealso cref="csscript.CompilerException" />
    public class InvalidDirectiveException : CompilerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDirectiveException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public InvalidDirectiveException(string message) : base(message)
        {
        }
    }

    internal class Surrogate86ProcessRequiredException : ApplicationException
    {
    }

    internal class SurrogateHostProcessRequiredException : ApplicationException
    {
        public SurrogateHostProcessRequiredException(string scriptAssembly, string[] scriptArgs, bool startDebugger)
        {
            ScriptAssembly = scriptAssembly;
            StartDebugger = startDebugger;
            ScriptArgs = scriptArgs;
        }

        string scriptAssembly;

        public string ScriptAssembly
        {
            get { return scriptAssembly; }
            set { scriptAssembly = value; }
        }

        bool startDebugger;

        public bool StartDebugger
        {
            get { return startDebugger; }
            set { startDebugger = value; }
        }

        string[] scriptArgs;

        public string[] ScriptArgs
        {
            get { return scriptArgs; }
            set { scriptArgs = value; }
        }
    }

    internal class CLIExitRequest : CLIException
    {
        static public void Throw()
        {
            throw new CLIExitRequest();
        }
    }

    /// <summary>
    /// The exception that is thrown when a the script CLI error occurs.
    /// </summary>
    internal class CLIException : ApplicationException
    {
        public int ExitCode = -1;

        public CLIException()
        {
        }

        public CLIException(string message) : base(message)
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
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        public CompilerException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public CompilerException(string message)
            : base(message)
        {
        }

        int errorCount;

        /// <summary>
        /// Gets or sets the error count associated with the last script compilation.
        /// </summary>
        /// <value>The error count.</value>
        public int ErrorCount
        {
            get { return errorCount; }
            set { errorCount = value; }
        }

        /// <summary>
        /// Creates the CompilerException instance from the specified compiler errors.
        /// </summary>
        /// <param name="Errors">The compiler errors.</param>
        /// <param name="hideCompilerWarnings">if set to <c>true</c> hide compiler warnings.</param>
        /// <param name="resolveAutogenFilesRefs">if set to <c>true</c> all references to the path of the derived auto-generated files
        /// (e.g. errors in the decorated classless scripts) will be replaced with the path of the original files (e.g. classless script itself).</param>
        /// <returns></returns>
        public static CompilerException Create(CompilerErrorCollection Errors, bool hideCompilerWarnings, bool resolveAutogenFilesRefs)
        {
            StringBuilder compileErr = new StringBuilder();

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
                        CSSUtils.NormaliseFileReference(ref file, ref line);

                    compileErr.Append(file);
                    compileErr.Append("(");
                    compileErr.Append(line);
                    compileErr.Append(",");
                    compileErr.Append(err.Column);
                    compileErr.Append("): ");
                }
                else
                {
                    compileErr.Append("BUILD: ");
                }

                if (err.IsWarning)
                    compileErr.Append("warning ");
                else
                    compileErr.Append("error ");
                compileErr.Append(err.ErrorNumber);
                compileErr.Append(": ");
                compileErr.Append(err.ErrorText);
                compileErr.Append(Environment.NewLine);
            }
            CompilerException retval = new CompilerException(compileErr.ToString());
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

            var errors = (CompilerErrorCollection)parentException.Data["Errors"];
            errors.Insert(0, error);

            CompilerException retval = new CompilerException(error.ToString() + Environment.NewLine + parentException.Message);
            retval.Data.Add("Errors", errors);
            retval.ErrorCount = errors.Count;

            return retval;
        }
    }
}