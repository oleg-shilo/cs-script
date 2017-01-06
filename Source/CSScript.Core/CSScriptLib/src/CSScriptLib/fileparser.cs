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
using System.IO;
using System.Text;

#if net1
using System.Collections;
#else

using System.Collections.Generic;
using System.Linq;

#endif

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
#if net1
            renameNamespaceMap = new ArrayList();
#else
            renameNamespaceMap = new List<string[]>();
#endif
        }

        public string[][] RenameNamespaceMap
        {
#if net1
            get { return (string[][])renameNamespaceMap.ToArray(typeof(string[])); }
#else
            get { return renameNamespaceMap.ToArray(); }
#endif
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
                retval = xNames.Length - yNames.Length;
                if (retval == 0)
                {
                    for (int i = 0; i < xNames.Length && retval == 0; i++)
                    {
                        retval = xNames[i].Length - yNames[i].Length;
                        if (retval == 0)
                        {
                            for (int j = 0; j < xNames[i].Length; j++)
                            {
                                retval = string.Compare(xNames[i][j], yNames[i][j]);
                            }
                        }
                    }
                }
            }
            return retval;
        }

        public bool preserveMain = false;

        #endregion Public interface...

#if net1
        ArrayList renameNamespaceMap;
#else
        List<string[]> renameNamespaceMap;
#endif
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

        public FileParser()
        {
        }

        public FileParser(string fileName, ParsingParams prams, bool process, bool imported, string[] searchDirs, bool throwOnError)
        {
            FileParser._throwOnError = throwOnError;
            this.imported = imported;
            this.prams = prams;
            this.fileName = ResolveFile(fileName, searchDirs);
            this.searchDirs = searchDirs.Concat(new string[] { Path.GetDirectoryName(this.fileName) }).Distinct().ToArray();
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
#if net1
            get { return (ScriptInfo[])referencedScripts.ToArray(typeof(ScriptInfo)); }
#else
            get { return referencedScripts.ToArray(); }
#endif
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
                    throw new FileNotFoundException(string.Format("Could not find file \"{0}\".\nEnsure it is in one of the CS-Script search/probing directories.", file));

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
#if net1
                ArrayList result = new ArrayList();
#else
                List<string> result = new List<string>();
#endif
                if (Directory.Exists(dir))
                    foreach (string item in Directory.GetFiles(dir, name))
                        result.Add(Path.GetFullPath(item));

#if net1
                return (string[])result.ToArray(typeof(string));
#else
                return result.ToArray();
#endif

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

            string[] files = LocateFiles(Path.Combine(Directory.GetCurrentDirectory(), fileName));
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
            return string.Format(headerTemplate, "CS-Script", path, fileName, DateTime.Now);
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
#if net1
    class FileParserComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            if (x == null && y == null)
                return 0;

            int retval = x == null ? -1 : (y == null ? 1 : 0);

            if (retval == 0)
            {
                FileParser xParser = (FileParser)x;
                FileParser yParser = (FileParser)y;
                retval = string.Compare(xParser.fileName, yParser.fileName, true);
                if (retval == 0)
                {
                    retval = ParsingParams.Compare(xParser.prams, yParser.prams);
                }
            }

            return retval;
        }
    }
#else

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

#endif

}