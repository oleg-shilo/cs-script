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
// Copyright (c) 2016 Oleg Shilo
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

#if net1
using System.Collections;
#else

#endif

using System.Text;
using System.Runtime.Serialization;

namespace csscript
{
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

        private string scriptAssembly;

        public string ScriptAssembly
        {
            get { return scriptAssembly; }
            set { scriptAssembly = value; }
        }

        private bool startDebugger;

        public bool StartDebugger
        {
            get { return startDebugger; }
            set { startDebugger = value; }
        }

        private string[] scriptArgs;

        public string[] ScriptArgs
        {
            get { return scriptArgs; }
            set { scriptArgs = value; }
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

        /// <summary>
        /// Creates the CompilerException instance from the specified compiler errors.
        /// </summary>
        /// <param name="Errors">The compiler errors.</param>
        /// <param name="hideCompilerWarnings">if set to <c>true</c> hide compiler warnings.</param>
        /// <returns></returns>
        public static CompilerException Create(CompilerErrorCollection Errors, bool hideCompilerWarnings)
        {
            StringBuilder compileErr = new StringBuilder();
            foreach (CompilerError err in Errors)
            {
                if (err.IsWarning && hideCompilerWarnings)
                    continue;

                //compileErr.Append(err.ToString());
                compileErr.Append(err.FileName);
                compileErr.Append("(");
                compileErr.Append(err.Line);
                compileErr.Append(",");
                compileErr.Append(err.Column);
                compileErr.Append("): ");
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
            return retval;
        }
    }
}