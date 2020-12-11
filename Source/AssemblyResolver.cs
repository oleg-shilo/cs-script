#region Licence...

//-----------------------------------------------------------------------------
// Date:	17/10/04	Time: 2:33p
// Module:	AssemblyResolver.cs
// Classes:	AssemblyResolver
//
// This module contains the definition of the AssemblyResolver class. Which implements
// some methods for simplified Assembly navigation
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
using csscript;

////////////////////////////////////////////////////
//
//  shfusion.dll is no longer supported by .NET 4.0
//
////////////////////////////////////////////////////
namespace CSScriptLibrary
{
    /// <summary>
    /// Class for resolving assembly name to assembly file
    /// </summary>
    public class AssemblyResolver
    {
        #region Class public data...

        /// <summary>
        /// File to be excluded from assembly search
        /// </summary>
        static public string ignoreFileName = "";

        #endregion Class public data...

        #region Class public methods...

        static bool cacheProbingResults;

        /// <summary>
        /// Gets or sets a value indicating whether the assembly probing results should be cached. Default value is <c>false</c>;
        /// <para>
        /// Caching means that during the probing if the assembly is not found in one of the probing directories this directory will not
        /// be checked again if the same assembly is to be resolved in the future.
        /// </para>
        /// <para>
        /// This setting is to be used with the caution. While it can bring some performance benefits when the list of probing directories
        /// is large it also may be wrong to assume that if the assembly in not found in a particular directory it still will not be there if the probing is repeated.
        /// </para>
        /// </summary>
        /// <value><c>true</c> if probing results should be cached; otherwise, <c>false</c>.</value>
        public static bool CacheProbingResults
        {
            get
            {
                return cacheProbingResults;
            }
            set
            {
                cacheProbingResults = value;
                if (!value)
                    lock (NotFoundAssemblies)
                    {
                        NotFoundAssemblies.Clear();
                    }
            }
        }

        static readonly HashSet<int> NotFoundAssemblies = new HashSet<int>();

        static int BuildHashSetValue(string assemblyName, string directory)
        {
            return CSSUtils.GetHashCodeEx((assemblyName ?? "") + (directory ?? ""));
        }

#if net4

        static Assembly LoadAssemblyFrom(string assemblyName, string asmFile, bool throwException = false)
#else
        static Assembly LoadAssemblyFrom(string assemblyName, string asmFile, bool throwException)
#endif
        {
            try
            {
                if (asmFile.EndsWith(".cs"))
                    return null;

                AssemblyName asmName = AssemblyName.GetAssemblyName(asmFile);
                if (asmName != null && asmName.FullName == assemblyName)
                    return Assembly.LoadFrom(asmFile);
                else if (assemblyName.IndexOf(",") == -1 && asmName.FullName.StartsWith(assemblyName + ",")) //short name requested
                    return Assembly.LoadFrom(asmFile);
            }
            catch
            {
                if (throwException)
                    throw;
            }
            return null;
        }

        /// <summary>
        /// Resolves assembly name to assembly file. Loads assembly file to the current AppDomain.
        /// </summary>
        /// <param name="assemblyName">The name of assembly</param>
        /// <param name="dir">The name of directory where local assemblies are expected to be</param>
        /// <param name="throwExceptions">if set to <c>true</c> [throw exceptions].</param>
        /// <returns>loaded assembly</returns>
#if net4

        static public Assembly ResolveAssembly(string assemblyName, string dir, bool throwExceptions = false)
#else
        static public Assembly ResolveAssembly(string assemblyName, string dir, bool throwExceptions)
#endif
        {
            if (dir.IsDirSectionSeparator())
                return null;

            int hashSetValue = -1;

            if (CacheProbingResults)
            {
                hashSetValue = BuildHashSetValue(assemblyName, dir);

                lock (NotFoundAssemblies)
                {
                    if (NotFoundAssemblies.Contains(hashSetValue))
                        return null;
                }
            }

            try
            {
                if (Directory.Exists(dir))
                {
                    Assembly retval = null;
                    string[] asmFileNameTokens = assemblyName.Split(",".ToCharArray(), 5);

                    string asmFile = Path.Combine(dir, asmFileNameTokens[0]);
                    if (ignoreFileName != Path.GetFileName(asmFile) && File.Exists(asmFile))
                        if (null != (retval = LoadAssemblyFrom(assemblyName, asmFile, throwExceptions)))
                            return retval;

                    //try file with name AssemblyDisplayName + .dll
                    asmFile = Path.Combine(dir, asmFileNameTokens[0]) + ".dll";

                    if (ignoreFileName != Path.GetFileName(asmFile) && File.Exists(asmFile))
                        if (null != (retval = LoadAssemblyFrom(assemblyName, asmFile, throwExceptions)))
                            return retval;

                    //try file with extension
                    foreach (string file in Directory.GetFiles(dir, asmFileNameTokens[0] + "*"))
                        if (null != (retval = LoadAssemblyFrom(assemblyName, file, throwExceptions)))
                            return retval;
                }
            }
            catch
            {
                if (throwExceptions)
                    throw;
            }

            if (CacheProbingResults)
            {
                lock (NotFoundAssemblies)
                {
                    NotFoundAssemblies.Add(hashSetValue);
                }
            }
            return null;
        }

        static readonly char[] illegalChars = ":*?<>|\"".ToCharArray();

        /// <summary>
        /// Determines whether the string is a legal path token.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        /// 	<c>true</c> if the string is a legal path token; otherwise, <c>false</c>.
        /// </returns>
        static public bool IsLegalPathToken(string name)
        {
            return name.IndexOfAny(illegalChars) != -1;
        }

        static internal ResolveAssemblyHandler FindAssemblyAlgorithm = DefaultFindAssemblyAlgorithm;

        /// <summary>
        /// Resolves namespace/assembly(file) name into array of assembly locations (local and GAC ones).
        /// </summary>
        /// <param name="name">'namespace'/assembly(file) name</param>
        /// <param name="searchDirs">Assembly search directories</param>
        /// <para>If the default implementation isn't suitable then you can set <c>CSScript.FindAssemblyAlgorithm</c>
        /// to the alternative implementation of the probing algorithm.</para>
        /// <returns>collection of assembly file names where namespace is implemented</returns>
        static public string[] FindAssembly(string name, string[] searchDirs)
        {
            return FindAssemblyAlgorithm(name, searchDirs);
        }

        static string[] DefaultFindAssemblyAlgorithm(string name, string[] searchDirs)
        {
            var retval = new List<string>();

            if (!IsLegalPathToken(name))
            {
                foreach (string dir in searchDirs)
                {
                    foreach (string asmLocation in FindLocalAssembly(name, dir))	//local assemblies alternative locations
                        retval.Add(asmLocation);

                    if (retval.Count != 0)
                        break;
                }

                if (retval.Count == 0)
                {
                    string nameSpace = name.RemoveAssemblyExtension();
                    foreach (string asmGACLocation in FindGlobalAssembly(nameSpace))
                        retval.Add(asmGACLocation);
                }
            }
            else
            {
                try
                {
                    if (Path.IsPathRooted(name) && File.Exists(name)) //note relative path will return IsLegalPathToken(name)==true
                        retval.Add(name);
                }
                catch { } //does not matter why...
            }
            return retval.ToArray();
        }

        /// <summary>
        /// Resolves namespace into array of local assembly locations.
        /// (Currently it returns only one assembly location but in future
        /// it can be extended to collect all assemblies with the same namespace)
        /// </summary>
        /// <param name="name">namespace/assembly name</param>
        /// <param name="dir">directory</param>
        /// <returns>collection of assembly file names where namespace is implemented</returns>
        public static string[] FindLocalAssembly(string name, string dir)
        {
            //We are returning and array because name may represent assembly name or namespace
            //and as such can consist of more than one assembly file (multiple assembly file is not supported at this stage).
            if (!dir.IsDirSectionSeparator())
                try
                {
                    string asmFile = Path.Combine(dir, name);

                    //cannot just check Directory.Exists(dir) as "name" can contain some subDir parts
                    if (Directory.Exists(Path.GetDirectoryName(asmFile)))
                    {
                        //test well-known assembly extensions first
                        foreach (string ext in new string[] { "", ".dll", ".exe", ".compiled" })
                        {
                            string file = asmFile + ext; //just in case if user did not specify the extension
                            if (ignoreFileName != Path.GetFileName(file) && File.Exists(file))
                                return new string[] { file };
                        }

                        if (asmFile != Path.GetFileName(asmFile) && File.Exists(asmFile))
                            return new string[] { asmFile };
                    }
                }
                catch { } //name may not be a valid path name
            return new string[0];
        }

        /// <summary>
        /// Resolves namespace into array of global assembly (GAC) locations.
        /// </summary>
        /// <param name="namespaceStr">'namespace' name</param>
        /// <returns>collection of assembly file names where namespace is implemented</returns>
        public static string[] FindGlobalAssembly(String namespaceStr)
        {
            var retval = new List<string>();

            if (Runtime.IsMono)
            {
                if (Runtime.MonoGAC.Contains(namespaceStr))
                {
                    retval.Add(namespaceStr);
                }
            }

            if (!retval.Any() && Runtime.IsWin)
            {
                try
                {
                    AssemblyEnum asmEnum = new csscript.AssemblyEnum(namespaceStr);

                    string highestVersion = "";
                    string asmName = "";
                    do
                    {
                        asmName = asmEnum.GetNextAssembly();
                        if (string.Compare(asmName, highestVersion) > 0)
                            highestVersion = asmName;

                        if (namespaceStr.Contains(", Version=")) //the assembly was specified by its full name
                            break; //stop searching for the higher version
                    }
                    while (asmName != null);

                    if (highestVersion != "")
                    {
                        string asmLocation = AssemblyCache.QueryAssemblyInfo(highestVersion);
                        retval.Add(asmLocation);
                    }
                }
                catch (Exception)
                {
                    // fuslion.dll cannot be found under Mono even if it is running on Windows
                    //If exception is thrown it is very likely it is because where fusion.dll does not exist/unavailable/broken.
                    //We might be running under the MONO run-time.
                }
            }

            if (retval.Count == 0 && namespaceStr.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase))
                retval.Add(namespaceStr); //in case of if the namespaceStr is a dll name

            return retval.ToArray();
        }

        #endregion Class public methods...

        /// <summary>
        /// Search for namespace into local assembly file.
        /// </summary>
        static public bool IsNamespaceDefinedInAssembly(string asmFileName, string namespaceStr)
        {
            if (File.Exists(asmFileName))
            {
                try
                {
                    //non reflection base assembly inspection can be found here: http://ccimetadata.codeplex.com/
                    //also there are some indications that Reflector uses ILReader without reflection: http://blogs.msdn.com/haibo_luo/default.aspx?p=3
                    //Potential solutions: AsmReader in this file or Assembly.Load(byte[]);

                    Assembly assembly = Assembly.ReflectionOnlyLoadFrom(asmFileName);

                    if (assembly != null)
                    {
                        foreach (Module m in assembly.GetModules())
                        {
                            foreach (Type t in m.GetTypes())
                            {
                                if (namespaceStr == t.Namespace)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            return false;
        }

        bool ProbeAssembly(string file)
        {
            try
            {
                Assembly.ReflectionOnlyLoadFrom(Path.GetFullPath(file));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}