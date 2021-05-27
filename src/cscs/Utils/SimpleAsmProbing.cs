#region Licence...

//-----------------------------------------------------------------------------
// Date:	25/10/10
// Module:	Utils.cs
// Classes:	...
//
// This module contains the definition of the utility classes used by CS-Script modules
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CSScripting
{
    /// <summary>
    /// Class for automated assembly probing. It implements extremely simple ('optimistic')
    /// probing algorithm. At runtime it attempts to resolve the assemblies via AppDomain.Assembly resolve
    /// event by looking up the assembly files in the user defined list of probing directories.
    /// The algorithm relies on the simple relationship between assembly name and assembly file name:
    ///  &lt;assembly file&gt; = &lt;asm name&gt; + ".dll"
    /// </summary>
    ///<example>The following is an example of automated assembly probing.
    ///<code>
    /// using (SimpleAsmProbing.For(@"E:\Dev\Libs", @"E:\Dev\Packages"))
    /// {
    ///     dynamic script = CSScript.Evaluator
    ///                              .LoadFile(script_file);
    ///     script.Print();
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="System.IDisposable" />
    public class SimpleAsmProbing : IDisposable
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            Uninit();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SimpleAsmProbing"/> class.
        /// </summary>
        ~SimpleAsmProbing()
        {
            Dispose(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleAsmProbing"/> class.
        /// </summary>
        public SimpleAsmProbing() { }

        /// <summary>
        /// Creates and initializes a new instance of the <see cref="SimpleAsmProbing"/> class.
        /// </summary>
        /// <param name="probingDirs">The probing dirs.</param>
        /// <returns></returns>
        public static SimpleAsmProbing For(params string[] probingDirs)
        {
            return new SimpleAsmProbing(probingDirs);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleAsmProbing"/> class.
        /// </summary>
        /// <param name="probingDirs">The probing dirs.</param>
        public SimpleAsmProbing(params string[] probingDirs)
        {
            Init(probingDirs);
        }

        static bool initialized = false;
        static string[] probingDirs = new string[0];

        /// <summary>
        /// Sets probing dirs and subscribes to the <see cref="System.AppDomain.AssemblyResolve"/> event.
        /// </summary>
        /// <param name="probingDirs">The probing dirs.</param>
        public void Init(params string[] probingDirs)
        {
            SimpleAsmProbing.probingDirs = probingDirs;
            if (!initialized)
            {
                initialized = true;
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        /// <summary>
        /// Unsubscribes to the <see cref="System.AppDomain.AssemblyResolve"/> event.
        /// </summary>
        public void Uninit()
        {
            if (initialized)
            {
                initialized = false;
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        static Dictionary<string, Assembly> cache = new Dictionary<string, Assembly>();

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var shortName = args.Name.Split(',').First().Trim();

            if (cache.ContainsKey(shortName))
                return cache[shortName];

            cache[shortName] = null; // this will prevent reentrance an cercular calls

            foreach (string dir in probingDirs)
            {
                try
                {
                    string file = Path.Combine(dir, args.Name.Split(',').First().Trim() + ".dll");
                    if (File.Exists(file))
                        return (cache[shortName] = Assembly.LoadFile(file));
                }
                catch { }
            }

            try
            {
                return (cache[shortName] = Assembly.LoadFrom(shortName)); // will try to load by the asm file name without the path
            }
            catch
            {
            }
            return null;
        }
    }
}