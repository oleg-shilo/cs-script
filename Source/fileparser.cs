#region Licence...

//-----------------------------------------------------------------------------
// Date:	31/01/05	Time: 17:15p
// Module:	fileparser.cs
// Classes:	ParsingParams
//			ScriptInfo
//			ScriptParser
//			FileParser
//			FileParserComparer
//
// This module contains the definition of the classes which implement
// parsing script code. The result of such processing is a collections of the names
// of the namespacs and assemblies used by the script code.
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
using System.IO;
using System.Text;

using System.Collections.Generic;
using System.Linq;

using csscript;

namespace CSScriptLibrary
{
    /// <summary>
    /// ParsingParams is an class that holds parsing parameters (parameters that controls how file is to be parsed).
    /// At this moment they are namespace renaming rules only.
    /// </summary>
    class ParsingParams
    {
        #region Public interface...

        public ParsingParams()
        {
            renameNamespaceMap = new List<string[]>();
        }

        public string[][] RenameNamespaceMap
        {
            get { return renameNamespaceMap.ToArray(); }
        }

        public void AddRenameNamespaceMap(string[][] names)
        {
            renameNamespaceMap.AddRange(names);
        }

        /// <summary>
        /// Compare() is to be used to help with implementation of IComparer for sorting operations.
        /// </summary>
        public static int Compare(ParsingParams xPrams, ParsingParams yPrams)
        {
            if (xPrams == null && yPrams == null)
                return 0;

            int retval = xPrams == null ? -1 : (yPrams == null ? 1 : 0);

            if (retval == 0)
            {
                string[][] xNames = xPrams.RenameNamespaceMap;
                string[][] yNames = yPrams.RenameNamespaceMap;
                retval = System.Collections.Comparer.Default.Compare(xNames.Length, yNames.Length);
                if (retval == 0)
                {
                    for (int i = 0; i < xNames.Length && retval == 0; i++)
                    {
                        retval = System.Collections.Comparer.Default.Compare(xNames[i].Length, yNames[i].Length);
                        if (retval == 0)
                        {
                            for (int j = 0; j < xNames[i].Length; j++)
                            {
                                retval = System.Collections.Comparer.Default.Compare(xNames[i][j], yNames[i][j]);
                            }
                        }
                    }
                }
            }
            return retval;
        }

        public bool preserveMain = false;
        public string importingErrorMessage;

        #endregion Public interface...

        List<string[]> renameNamespaceMap;
    }

    /// <summary>
    /// Class which is a placeholder for general information of the script file
    /// </summary>
    class ScriptInfo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">ImportInfo object containing the information how the script file should be parsed.</param>
        public ScriptInfo(CSharpParser.ImportInfo info)
        {
            this.fileName = info.file;
            parseParams.importingErrorMessage = "Cannot import \"" + info.rawStatement + "\" from the \"" + info.parentScript + "\" script.";
            parseParams.AddRenameNamespaceMap(info.renaming);
            parseParams.preserveMain = info.preserveMain;
        }

        public ParsingParams parseParams = new ParsingParams();
        public string fileName;
    }

    /// <summary>
    /// Class that implements parsing the single C# script file
    /// </summary>
    class FileParser
    {
        static bool _throwOnError = true;

        public System.Threading.ApartmentState ThreadingModel
        {
            get
            {
                if (this.parser == null)
                    return System.Threading.ApartmentState.Unknown;
                else
                    return this.parser.ThreadingModel;
            }
        }

        public FileParser()
        {
        }

        public FileParser(string fileName, ParsingParams prams, bool process, bool imported, string[] searchDirs, bool throwOnError)
        {
            if (searchDirs == null)
                searchDirs = new string[0];

            FileParser._throwOnError = throwOnError;
            this.imported = imported;
            this.prams = prams;
            this.fileName = ResolveFile(fileName, searchDirs);
            this.searchDirs = searchDirs.ConcatWith(Path.GetDirectoryName(this.fileName))
                                        .RemovePathDuplicates();
            if (process)
                ProcessFile();
        }

        public string fileNameImported = "";
        public ParsingParams prams = null;

        public string FileToCompile
        {
            get { return imported ? fileNameImported : fileName; }
        }

        public string[] SearchDirs
        {
            get { return searchDirs; }
        }

        public bool Imported
        {
            get { return imported; }
        }

        public string[] ReferencedNamespaces
        {
            get { return parser.RefNamespaces.Except(parser.IgnoreNamespaces).ToArray(); }
        }

        public string[] IgnoreNamespaces
        {
            get { return parser.IgnoreNamespaces; }
        }

        public string[] Precompilers
        {
            get { return parser.Precompilers; }
        }

        public string[] ReferencedAssemblies
        {
            get { return parser.RefAssemblies; }
        }

        public string[] Packages
        {
            get { return parser.NuGets; }
        }

        public string[] ExtraSearchDirs
        {
            get { return parser.ExtraSearchDirs; }
        }

        public string[] ReferencedResources
        {
            get { return parser.ResFiles; }
        }

        public string[] CompilerOptions
        {
            get { return parser.CompilerOptions; }
        }

        public ScriptInfo[] ReferencedScripts
        {
            get { return referencedScripts.ToArray(); }
        }

        public void ProcessFile()
        {
            packages.Clear();
            referencedAssemblies.Clear();
            referencedScripts.Clear();
            referencedNamespaces.Clear();
            referencedResources.Clear();

            this.parser = new CSharpParser(fileName, true, null, this.searchDirs);

            foreach (CSharpParser.ImportInfo info in parser.Imports)
            {
                referencedScripts.Add(new ScriptInfo(info));
            }

            referencedAssemblies.AddRange(parser.RefAssemblies);
            referencedNamespaces.AddRange(parser.RefNamespaces.Except(parser.IgnoreNamespaces));
            referencedResources.AddRange(parser.ResFiles);

            if (imported)
            {
                if (prams != null)
                {
                    parser.DoRenaming(prams.RenameNamespaceMap, prams.preserveMain);
                }
                if (parser.ModifiedCode == "")
                {
                    fileNameImported = fileName; //importing does not require any modification of the original code
                }
                else
                {
                    fileNameImported = Path.Combine(CSExecutor.ScriptCacheDir, string.Format("i_{0}_{1}{2}", Path.GetFileNameWithoutExtension(fileName), CSSUtils.GetHashCodeEx(Path.GetDirectoryName(fileName)), Path.GetExtension(fileName)));
                    if (!Directory.Exists(Path.GetDirectoryName(fileNameImported)))
                        Directory.CreateDirectory(Path.GetDirectoryName(fileNameImported));
                    if (File.Exists(fileNameImported))
                    {
                        File.SetAttributes(fileNameImported, FileAttributes.Normal);
                        Utils.FileDelete(fileNameImported, true);
                    }

                    using (StreamWriter scriptWriter = new StreamWriter(fileNameImported, false, Encoding.UTF8))
                    {
                        //scriptWriter.Write(ComposeHeader(fileNameImported)); //using a big header at start is overkill (it also shifts line numbers so they do not match with the original script file)
                        //but might be required in future
                        scriptWriter.WriteLine(parser.ModifiedCode);
                        scriptWriter.WriteLine("///////////////////////////////////////////");
                        scriptWriter.WriteLine("// Compiler-generated file - DO NOT EDIT!");
                        scriptWriter.WriteLine("///////////////////////////////////////////");
                    }
                    File.SetAttributes(fileNameImported, FileAttributes.ReadOnly);
                }
            }
        }

        List<ScriptInfo> referencedScripts = new List<ScriptInfo>();
        List<string> referencedNamespaces = new List<string>();
        List<string> referencedAssemblies = new List<string>();
        List<string> packages = new List<string>();
        List<string> referencedResources = new List<string>();

        string[] searchDirs;
        bool imported = false;

        /// <summary>
        /// Searches for script file by given script name. Calls ResolveFile(string fileName, string[] extraDirs, bool throwOnError)
        /// with throwOnError flag set to true.
        /// </summary>
        public static string ResolveFile(string fileName, string[] extraDirs)
        {
            return ResolveFile(fileName, extraDirs, _throwOnError);
        }

        internal static string[] ResolveFiles(string fileName, string[] extraDirs)
        {
            return ResolveFiles(fileName, extraDirs, _throwOnError);
        }

        /// <summary>
        /// The resolve file algorithm,
        /// <para>
        /// The default algorithm searches for script file by given script name. Search order:
        /// 1. Current directory
        /// 2. extraDirs (usually %CSSCRIPT_DIR%\Lib and ExtraLibDirectory)
        /// 3. PATH
        /// Also fixes file name if user did not provide extension for script file (assuming .cs extension)
        /// </para>
        /// </summary>
        internal static ResolveSourceFileAlgorithm ResolveFilesAlgorithm = ResolveFilesDefault;

        //internal static ResolveSourceFileHandler ResolveFileAlgorithm = ResolveFileDefault;

        /// <summary>
        /// Searches for script file by given script name. Search order:
        /// 1. Current directory
        /// 2. extraDirs (usually %CSSCRIPT_DIR%\Lib and ExtraLibDirectory)
        /// 3. PATH
        /// Also fixes file name if user did not provide extension for script file (assuming .cs extension)
        /// <para>If the default implementation isn't suitable then you can set <c>FileParser.ResolveFilesAlgorithm</c>
        /// to the alternative implementation of the probing algorithm.</para>
        /// </summary>
        public static string ResolveFile(string file, string[] extraDirs, bool throwOnError)
        {
            string[] files = ResolveFilesAlgorithm(file, extraDirs, throwOnError);
            return files.Length > 0 ? files[0] : null;
        }

        internal static string[] ResolveFiles(string file, string[] extraDirs, bool throwOnError)
        {
            return ResolveFilesAlgorithm(file, extraDirs, throwOnError);
        }

        internal static string[] ResolveFilesDefault(string file, string[] extraDirs, bool throwOnError)
        {
            string[] retval = _ResolveFiles(file, extraDirs, "");
            if (retval.Length == 0)
                retval = _ResolveFiles(file, extraDirs, ".cs");
            if (retval.Length == 0)
                retval = _ResolveFiles(file, extraDirs, ".csl"); //script link file

            if (retval.Length == 0)
            {
                if (throwOnError)
                    throw new FileNotFoundException(string.Format("Could not find file \"{0}\".{1}Ensure it is in one of the CS-Script search/probing directories.", file, Environment.NewLine));

                if (!file.EndsWith(".cs"))
                    retval = new string[] { file + ".cs" };
                else
                    retval = new string[] { file };
            }

            return retval;
        }

        static string[] LocateFiles(string filePath)
        {
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                string name = Path.GetFileName(filePath);

                List<string> result = new List<string>();

                if (Directory.Exists(dir))
                    foreach (string item in Directory.GetFiles(dir, name))
                        result.Add(Path.GetFullPath(item));

                return result.ToArray();
            }
            catch { }
            return new string[0];
        }

        static string[] _ResolveFiles(string file, string[] extraDirs, string extension)
        {
            string fileName = file;

            //current directory
            if (Path.GetExtension(fileName) == "")
                fileName += extension;

            string[] files = LocateFiles(Path.Combine(Environment.CurrentDirectory, fileName));
            if (files.Length > 0)
                return files;

            //arbitrary directories
            if (extraDirs != null)
            {
                foreach (string dir in extraDirs)
                {
                    files = LocateFiles(Path.Combine(dir, fileName));
                    if (files.Length > 0)
                        return files;
                }
            }

            //PATH
            string[] pathDirs = Environment.GetEnvironmentVariable("PATH").Replace("\"", "").Split(';');
            foreach (string dir in pathDirs)
            {
                files = LocateFiles(Path.Combine(dir, fileName));
                if (files.Length > 0)
                    return files;
            }

            return new string[0];
        }

        static public string headerTemplate =
                @"/*" + Environment.NewLine +
                @" Created by {0}" +
                @" Original location: {1}" + Environment.NewLine +
                @" C# source equivalent of {2}" + Environment.NewLine +
                @" compiler-generated file created {3} - DO NOT EDIT!" + Environment.NewLine +
                @"*/" + Environment.NewLine;

        public string ComposeHeader(string path)
        {
            return string.Format(headerTemplate, csscript.AppInfo.appLogoShort, path, fileName, DateTime.Now);
        }

        public string fileName = "";
        public CSharpParser parser;
    }

    /// <summary>
    /// Class that implements parsing the single C# Script file
    /// </summary>
    /// <summary>
    /// Implementation of the IComparer for sorting operations of collections of FileParser instances
    /// </summary>
    ///
    class FileParserComparer : IComparer<FileParser>
    {
        public int Compare(FileParser x, FileParser y)
        {
            if (x == null && y == null)
                return 0;

            int retval = x == null ? -1 : (y == null ? 1 : 0);

            if (retval == 0)
            {
                retval = string.Compare(x.fileName, y.fileName, true);
                if (retval == 0)
                {
                    retval = ParsingParams.Compare(x.prams, y.prams);
                }
            }

            return retval;
        }
    }

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
        /// The compiler options sepcified with `//css_co` directive
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

    /// <summary>
    /// Class that manages parsing the main and all imported (if any) C# Script files
    /// </summary>
    public class ScriptParser
    {
        /// <summary>
        /// Gets the script parsing context. This object is effectively a parsing result.
        /// </summary>
        /// <returns></returns>
        public ScriptParsingResult GetContext()
        {
            return new ScriptParsingResult
            {
                Packages = this.Packages,
                ReferencedResources = this.ReferencedResources,
                ReferencedAssemblies = this.ReferencedAssemblies,
                ReferencedNamespaces = this.ReferencedNamespaces,
                IgnoreNamespaces = this.IgnoreNamespaces,
                SearchDirs = this.SearchDirs,
                CompilerOptions = this.CompilerOptions,
                Precompilers = this.Precompilers,
                FilesToCompile = this.FilesToCompile,
            };
        }

        /// <summary>
        /// Processes the imported script. Processing involves lookup for 'static Main' and renaming it so it does not
        /// interfere with the 'static Main' of the primary script. After renaming is done the new content is saved in the
        /// CS-Script cache and the new file location is returned. The saved file can be used late as an "included script".
        /// This technique can be from 'precompiler' scripts.
        /// <para>If the script file does not require renaming (static Main is not present) the method returns the
        /// original script file location.</para>
        /// </summary>
        /// <param name="scriptFile">The script file.</param>
        /// <returns></returns>
        public static string ProcessImportedScript(string scriptFile)
        {
            var parser = new FileParser(scriptFile, new ParsingParams(), true, true, new string[0], true);
            return parser.FileToCompile;
        }

        bool throwOnError = true;

        /// <summary>
        /// ApartmentState of a script during the execution (default: ApartmentState.Unknown)
        /// </summary>
        public System.Threading.ApartmentState apartmentState = System.Threading.ApartmentState.Unknown;

        /// <summary>
        /// Collection of the files to be compiled (including dependent scripts)
        /// </summary>
        public string[] FilesToCompile
        {
            get
            {
                List<string> retval = new List<string>();
                foreach (FileParser file in fileParsers)
                    retval.Add(file.FileToCompile);
                return retval.ToArray();
            }
        }

        /// <summary>
        /// Collection of the imported files (dependent scripts)
        /// </summary>
        public string[] ImportedFiles
        {
            get
            {
                List<string> retval = new List<string>();
                foreach (FileParser file in fileParsers)
                {
                    if (file.Imported)
                        retval.Add(file.fileName);
                }
                return retval.ToArray();
            }
        }

        /// <summary>
        /// Collection of resource files referenced from code
        /// </summary>
        public string[] ReferencedResources
        {
            get { return referencedResources.ToArray(); }
        }

        /// <summary>
        /// Collection of compiler options
        /// </summary>
        public string[] CompilerOptions
        {
            get { return compilerOptions.ToArray(); }
        }

        /// <summary>
        /// Precompilers specified in the primary script file.
        /// </summary>
        public string[] Precompilers
        {
            get { return precompilers.ToArray(); }
        }

        /// <summary>
        /// Collection of namespaces referenced from code (including those referenced in dependand scripts)
        /// </summary>
        public string[] ReferencedNamespaces
        {
            get { return referencedNamespaces.ToArray(); }
        }

        /// <summary>
        /// Collection of namespaces, which if found in code, should not be resolved into referenced assembly.
        /// </summary>
        public string[] IgnoreNamespaces
        {
            get { return ignoreNamespaces.ToArray(); }
        }

#if !net4
        /// <summary>
        /// Resolves the NuGet packages into assemblies to be referenced by the script.
        /// <para>If the package was never installed/downloaded yet CS-Script runtime will try to download it.</para>
        /// <para>CS-Script will also analyze the installed package structure in try to reference compatible assemblies
        /// from the package.</para>
        /// </summary>
        /// <returns>Collection of the referenced assembly files.</returns>
        public string[] ResolvePackages()
        {
            return ResolvePackages(false);
        }

        /// <summary>
        /// Resolves the NuGet packages into assemblies to be referenced by the script.
        /// <para>If the package was never installed/downloaded yet CS-Script runtime will try to download it.</para>
        /// <para>CS-Script will also analyze the installed package structure in try to reference compatible assemblies
        /// from the package.</para>
        /// </summary>
        /// <param name="suppressDownloading">if set to <c>true</c> suppresses downloading the NuGet package.
        /// Suppressing can be useful for the quick 'referencing' assessment.</param>
        /// <returns>Collection of the referenced assembly files.</returns>
        public string[] ResolvePackages(bool suppressDownloading)
#else

        /// <summary>
        /// Resolves the NuGet packages into assemblies to be referenced by the script.
        /// <para>If the package was never installed/downloaded yet CS-Script runtime will try to download it.</para>
        /// <para>CS-Script will also analyze the installed package structure in try to reference compatible assemblies
        /// from the package.</para>
        /// </summary>
        /// <param name="suppressDownloading">if set to <c>true</c> suppresses downloading the NuGet package.
        /// Suppressing can be useful for the quick 'referencing' assessment.</param>
        /// <returns>Collection of the referenced assembly files.</returns>
        public string[] ResolvePackages(bool suppressDownloading = false)
#endif
        {
            return NuGet.Resolve(Packages, suppressDownloading, this.ScriptPath);
        }

        /// <summary>
        /// Collection of the NuGet packages
        /// </summary>
        public string[] Packages
        {
            get { return packages.ToArray(); }
        }

        /// <summary>
        /// Collection of referenced assemblies. All assemblies are referenced either from command-line, code or resolved from referenced namespaces.
        /// </summary>
        public string[] ReferencedAssemblies
        {
            get { return referencedAssemblies.ToArray(); }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileName">Script file name</param>
        public ScriptParser(string fileName)
        {
            Init(fileName, null);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileName">Script file name</param>
        /// <param name="searchDirs">Extra ScriptLibrary directory </param>
        public ScriptParser(string fileName, string[] searchDirs)
        {
            //if ((CSExecutor.ExecuteOptions.options != null && CSExecutor.options.useSmartCaching) && CSExecutor.ScriptCacheDir == "") //in case if ScriptParser is used outside of the script engine
            if (CSExecutor.ScriptCacheDir == "") //in case if ScriptParser is used outside of the script engine
                CSExecutor.SetScriptCacheDir(fileName);
            Init(fileName, searchDirs);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileName">Script file name</param>
        /// <param name="searchDirs">Extra ScriptLibrary directory(ies) </param>
        /// <param name="throwOnError">flag to indicate if the file parsing/processing error should raise an exception</param>
        public ScriptParser(string fileName, string[] searchDirs, bool throwOnError)
        {
            this.throwOnError = throwOnError;
            //if ((CSExecutor.ExecuteOptions.options != null && CSExecutor.ExecuteOptions.options.useSmartCaching) && CSExecutor.ScriptCacheDir == "") //in case if ScriptParser is used outside of the script engine
            if (CSExecutor.ScriptCacheDir == "") //in case if ScriptParser is used outside of the script engine
                CSExecutor.SetScriptCacheDir(fileName);
            Init(fileName, searchDirs);
        }

        /// <summary>
        /// The path of the parsed script.
        /// </summary>
        public string ScriptPath
        {
            get { return scriptPath; }
            set { scriptPath = value; }
        }

        string scriptPath;

        /// <summary>
        /// Initialization of ScriptParser instance
        /// </summary>
        /// <param name="fileName">Script file name</param>
        /// <param name="searchDirs">Extra ScriptLibrary directory(ies) </param>
        void Init(string fileName, string[] searchDirs)
        {
            ScriptPath = fileName;

            packages = new List<string>();
            referencedNamespaces = new List<string>();
            referencedAssemblies = new List<string>();
            referencedResources = new List<string>();
            ignoreNamespaces = new List<string>();
            precompilers = new List<string>();
            compilerOptions = new List<string>();

            //process main file
            FileParser mainFile = new FileParser(fileName, null, true, false, searchDirs, throwOnError);
            this.apartmentState = mainFile.ThreadingModel;

            foreach (string file in mainFile.Precompilers)
                PushPrecompiler(file);

            foreach (string namespaceName in mainFile.IgnoreNamespaces)
                PushIgnoreNamespace(namespaceName);

            foreach (string namespaceName in mainFile.ReferencedNamespaces)
                PushNamespace(namespaceName);

            foreach (string asmName in mainFile.ReferencedAssemblies)
                PushAssembly(asmName);

            foreach (string name in mainFile.Packages)
                PushPackage(name);

            foreach (string resFile in mainFile.ReferencedResources)
                PushResource(resFile);

            foreach (string opt in mainFile.CompilerOptions)
                PushCompilerOptions(opt);

            List<string> dirs = new List<string>();
            dirs.Add(Path.GetDirectoryName(mainFile.fileName));//note: mainFile.fileName is warrantied to be a full name but fileName is not
            if (searchDirs != null)
                dirs.AddRange(searchDirs);

            foreach (string dir in mainFile.ExtraSearchDirs)
            {
                if (Path.IsPathRooted(dir))
                    dirs.Add(Path.GetFullPath(dir));
                else
                    dirs.Add(Path.Combine(Path.GetDirectoryName(mainFile.fileName), dir));
            }

            this.SearchDirs = Utils.RemovePathDuplicates(dirs.ToArray());

            //process imported files if any
            foreach (ScriptInfo fileInfo in mainFile.ReferencedScripts)
                ProcessFile(fileInfo);

            //Main script file shall always be the first. Add it now as previously array was sorted a few times
            this.fileParsers.Insert(0, mainFile);
        }

        void ProcessFile(ScriptInfo fileInfo)
        {
            try
            {
                var fileComparer = new FileParserComparer();

                var importedFile = new FileParser(fileInfo.fileName, fileInfo.parseParams, false, true, this.SearchDirs, throwOnError); //do not parse it yet (the third param is false)

                if (fileParsers.BinarySearch(importedFile, fileComparer) < 0)
                {
                    if (File.Exists(importedFile.fileName))
                    {
                        importedFile.ProcessFile(); //parse now namespaces, ref. assemblies and scripts; also it will do namespace renaming

                        this.fileParsers.Add(importedFile);
                        this.fileParsers.Sort(fileComparer);

                        foreach (string namespaceName in importedFile.ReferencedNamespaces)
                            PushNamespace(namespaceName);

                        foreach (string asmName in importedFile.ReferencedAssemblies)
                            PushAssembly(asmName);

                        foreach (string packageName in importedFile.Packages)
                            PushPackage(packageName);

                        foreach (string file in importedFile.Precompilers)
                            PushPrecompiler(file);

                        foreach (ScriptInfo scriptFile in importedFile.ReferencedScripts)
                            ProcessFile(scriptFile);

                        foreach (string resFile in importedFile.ReferencedResources)
                            PushResource(resFile);

                        foreach (string file in importedFile.IgnoreNamespaces)
                            PushIgnoreNamespace(file);

                        List<string> dirs = new List<string>(this.SearchDirs);
                        foreach (string dir in importedFile.ExtraSearchDirs)
                            if (Path.IsPathRooted(dir))
                                dirs.Add(Path.GetFullPath(dir));
                            else
                                dirs.Add(Path.Combine(Path.GetDirectoryName(importedFile.fileName), dir));
                        this.SearchDirs = dirs.ToArray();
                    }
                    else
                    {
                        importedFile.fileNameImported = importedFile.fileName;
                        this.fileParsers.Add(importedFile);
                        this.fileParsers.Sort(fileComparer);
                    }
                }
            }
            catch (Exception e)
            {
                throw e.ToNewException(
                    fileInfo.parseParams.importingErrorMessage,
                    ExecuteOptions.options.reportDetailedErrorInfo); // encapsulate: ExecuteOptions.options.reportDetailedErrorInfo
            }
        }

        List<FileParser> fileParsers = new List<FileParser>();

        /// <summary>
        /// Saves all imported scripts int temporary location.
        /// </summary>
        /// <returns>Collection of the saved imported scrips file names</returns>
        public string[] SaveImportedScripts()
        {
            string workingDir = Path.GetDirectoryName(((FileParser)fileParsers[0]).fileName);
            List<string> retval = new List<string>();

            foreach (FileParser file in fileParsers)
            {
                if (file.Imported)
                {
                    if (file.fileNameImported != file.fileName) //script file was copied
                        retval.Add(file.fileNameImported);
                    else
                        retval.Add(file.fileName);
                }
            }
            return retval.ToArray();
        }

        /// <summary>
        /// Deletes imported scripts as a cleanup operation
        /// </summary>
        public void DeleteImportedFiles()
        {
            foreach (FileParser file in fileParsers)
            {
                if (file.Imported && file.fileNameImported != file.fileName) //the file was copied
                {
                    try
                    {
                        File.SetAttributes(file.FileToCompile, FileAttributes.Normal);
                        Utils.FileDelete(file.FileToCompile);
                    }
                    catch { }
                }
            }
        }

        List<string> referencedNamespaces;
        List<string> ignoreNamespaces;
        List<string> referencedResources;
        List<string> compilerOptions;
        List<string> precompilers;
        List<string> referencedAssemblies;
        List<string> packages;

        /// <summary>
        /// CS-Script SearchDirectories specified in the parsed script or its dependent scripts.
        /// </summary>
        public string[] SearchDirs;

        void PushItem(List<string> collection, string item)
        {
            if (collection.Count > 1)
                collection.Sort();

            AddIfNotThere(collection, item);
        }

        void PushNamespace(string nameSpace)
        {
            PushItem(referencedNamespaces, nameSpace);
        }

        void PushPrecompiler(string file)
        {
            PushItem(precompilers, file);
        }

        void PushIgnoreNamespace(string nameSpace)
        {
            PushItem(ignoreNamespaces, nameSpace);
        }

        void PushAssembly(string asmName)
        {
            PushItem(referencedAssemblies, asmName);
        }

        void PushPackage(string name)
        {
            PushItem(packages, name);
        }

        void PushResource(string resName)
        {
            PushItem(referencedResources, resName);
        }

        void PushCompilerOptions(string option)
        {
            AddIfNotThere(compilerOptions, option);
        }

        class StringComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return string.Compare(x, y, true);
            }
        }

        void AddIfNotThere(List<string> list, string item)
        {
            if (list.BinarySearch(item, new StringComparer()) < 0)
                list.Add(item);
        }

        /// <summary>
        /// Aggregates the references from the script and its imported scripts. It is a logical equivalent od CSExecutor.AggregateReferencedAssemblies
        /// but optimized for later .NET versions (e.g LINQ) and completely decoupled. Thus it has no dependencies on internal state
        /// (e.g. settings, options.shareHostAssemblies).
        /// <para>It is the method to call for generating list of ref asms as part of the project info.</para>
        ///
        /// </summary>
        /// <param name="searchDirs">The search dirs.</param>
        /// <param name="defaultRefAsms">The default ref asms.</param>
        /// <param name="defaultNamespacess">The default namespacess.</param>
        /// <returns></returns>
        public List<string> AgregateReferences(IEnumerable<string> searchDirs, IEnumerable<string> defaultRefAsms, IEnumerable<string> defaultNamespacess)
        {
            var probingDirs = searchDirs.ToArray();

            var refPkAsms = this.ResolvePackages(true); //suppressDownloading

            var refCodeAsms = this.ReferencedAssemblies
                                  .SelectMany(asm => AssemblyResolver.FindAssembly(asm.Replace("\"", ""), probingDirs));

            var refAsms = refPkAsms.Union(refPkAsms)
                                   .Union(refCodeAsms)
                                   .Union(defaultRefAsms.SelectMany(name => AssemblyResolver.FindAssembly(name, probingDirs)))
                                   .Distinct()
                                   .ToArray();

            //some assemblies are referenced from code and some will need to be resolved from the namespaces
            bool disableNamespaceResolving = (this.IgnoreNamespaces.Count() == 1 && this.IgnoreNamespaces[0] == "*");

            if (!disableNamespaceResolving)
            {
                var asmNames = refAsms.Select(x => Path.GetFileNameWithoutExtension(x).ToUpper()).ToArray();

                var refNsAsms = this.ReferencedNamespaces
                                      .Union(defaultNamespacess)
                                      .Where(name => !string.IsNullOrEmpty(name))
                                      .Where(name => !this.IgnoreNamespaces.Contains(name))
                                      .Where(name => !asmNames.Contains(name.ToUpper()))
                                      .Distinct()
                                      .SelectMany(name =>
                                      {
                                          var asms = AssemblyResolver.FindAssembly(name, probingDirs);
                                          return asms;
                                      })
                                      .ToArray();

                refAsms = refAsms.Union(refNsAsms).ToArray();
            }

            // foreach (var item in refAsms) Console.WriteLine("> " + item);
            refAsms = FilterDuplicatedAssembliesByFileName(refAsms);
            // foreach (var item in refAsms) Console.WriteLine("< " + item);
            return refAsms.ToList();
        }

        static string[] FilterDuplicatedAssembliesByFileName(string[] assemblies)
        {
            var uniqueAsms = new List<string>();
            var asmNames = new List<string>();
            foreach (var item in assemblies)
            {
                try
                {
                    // need to ensure that item has extension in order to avoid interpreting
                    // complex file names as simple name + extension:
                    // System.Core -> System
                    // System.dll  -> System
                    string name = Path.GetFileNameWithoutExtension(item.EnsureAsmExtension());
                    if (!asmNames.Contains(name))
                    {
                        uniqueAsms.Add(item);
                        asmNames.Add(name);
                    }
                }
                catch { }
            }
            return uniqueAsms.ToArray();
        }
    }
}