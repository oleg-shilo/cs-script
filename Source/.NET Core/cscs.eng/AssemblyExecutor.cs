#region Licence...

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
using System.IO;
using System.Linq;
using System.Reflection;
using CSScriptLibrary;

namespace csscript
{
    /// <summary>
    /// Executes "public static void Main(..)" of assembly in a separate domain.
    /// </summary>
    class AssemblyExecutor
    {
        LocalExecutor remoteExecutor;
        string assemblyFileName;

        public AssemblyExecutor(string fileName, string domainName)
        {
            assemblyFileName = fileName;
            remoteExecutor = new LocalExecutor(ExecuteOptions.options.searchDirs);
        }

        public void Execute(string[] args)
        {
            remoteExecutor.ExecuteAssembly(assemblyFileName, args);
        }

    }

    /// <summary>
    /// Invokes static method 'Main' from the assembly.
    /// </summary>
    class LocalExecutor
    {

        public string[] searchDirs = new string[0];

        public LocalExecutor(string[] searchDirs)
        {
            this.searchDirs = searchDirs;
        }

        public LocalExecutor()
        {
        }

        /// <summary>
        /// AppDomain event handler. This handler will be called if CLR cannot resolve
        /// referenced local assemblies
        /// </summary>
        public Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly retval = null;

            foreach (string dir in searchDirs)
            {
                //it is tempting to throw but should not as there can be other (e.g. host) ResolveEventHandler(s)
                //and throwing will prevent them from being invoked
                bool throwExceptions = false;

                retval = CSScriptLibrary.AssemblyResolver.ResolveAssembly(args.Name, dir, throwExceptions);
                if (retval != null)
                    break;
            }
            return retval;
        }

        public Assembly ResolveResEventHandler(object sender, ResolveEventArgs args)
        {
            return Assembly.LoadFrom(this.asmFile);
        }

        string asmFile = "";

        public void ExecuteAssembly(string filename, string[] args)
        {
            ExecuteAssembly(filename, args, null);
        }

        public void ExecuteAssembly(string filename, string[] args, SystemWideLock asmLock)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveEventHandler);
            AppDomain.CurrentDomain.ResourceResolve += new ResolveEventHandler(ResolveResEventHandler);

            asmFile = filename;

            Assembly assembly;

            if (!ExecuteOptions.options.inMemoryAsm)
            {
                assembly = Assembly.LoadFrom(filename);
            }
            else
            {
                //Load(byte[]) does not lock the assembly file as LoadFrom(filename) does
                byte[] data = File.ReadAllBytes(filename);
                string dbg = Utils.DbgFileOf(filename);

                if (ExecuteOptions.options.DBG && File.Exists(dbg))
                {
                    byte[] dbgData = File.ReadAllBytes(dbg);
                    assembly = Assembly.Load(data, dbgData);
                }
                else
                    assembly = Assembly.Load(data);

                asmLock?.Release();
            }

            SetScriptReflection(assembly, Path.GetFullPath(filename), true);

            InvokeStaticMain(assembly, args);
        }

        internal static void SetScriptReflection(Assembly assembly, string location, bool setScriptLocationReflection)
        {
            if (setScriptLocationReflection)
                Environment.SetEnvironmentVariable("location:" + assembly.GetHashCode(), location);

            string source = null;
            //Note assembly can contain only single AssemblyDescriptionAttribute
            foreach (AssemblyDescriptionAttribute attribute in assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true))
                source = attribute.Description;

            //check if executing the primary script and not hosted execution ("CSScriptRuntime" == null)
            if (Environment.GetEnvironmentVariable("CSScriptRuntime") != null && source == Environment.GetEnvironmentVariable("EntryScript"))
                Environment.SetEnvironmentVariable("EntryScriptAssembly", location);
        }

        public void InvokeStaticMain(Assembly compiledAssembly, string[] scriptArgs)
        {
            var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Static;

            MethodInfo[] methods = compiledAssembly.GetModules()
                                                   .SelectMany(m => m.GetTypes())
                                                   .SelectMany(t => t.GetMembers(bf)
                                                   .OfType<MethodInfo>())
                                                   .Where(x => x.Name == "Main" && x.IsStatic)
                                                   .ToArray();

            if (methods.Any())
            {
                if (methods.Count() > 1)
                    throw new ApplicationException("Multiple entry points are defined in the script.");

                var method = methods.First();

                object retval = null;

                if (method.GetParameters().Length != 0)
                    retval = method.Invoke(new object(), new object[] { (Object)scriptArgs });
                else
                    retval = method.Invoke(new object(), null);

                if (retval != null)
                {
                    try
                    {
                        Environment.ExitCode = int.Parse(retval.ToString());
                    }
                    catch { }
                }
            }
            else
            {
                throw new ApplicationException("Cannot find entry point. Make sure script file contains method: 'public static Main(...)'");
            }
        }
    }
}