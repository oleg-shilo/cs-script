#region Licence...

//-----------------------------------------------------------------------------
// Date:	26/12/05	Time: 16:11p
// Module:	csparser.cs
// Classes:	CSharpParser
//
// This module contains the definition of the class CSharpParser, which implements
// parsing C# code. The result of such processing is a collections of the names
// of the namespacs and assemblies used by C# code and a 'code' (content of
// C# script file stripped out of comments, "using [namespace name];" and C# script engine directives.
//
// C# script engine directives:
//	[//css_import <file>[,rename_namespace(<oldName>, <newName>);]
//	[//css_reference <file>;]
//	[//css_prescript file([arg0][, arg1]..[,arg2])[,ignore];]
//	[//css_postscript file([arg0][, arg1]..[,arg2])[,ignore];]
//
// Written by Oleg Shilo (oshilo@gmail.com)
//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2017 Oleg Shilo
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
using System.Collections;

#if !net1

using System.Collections.Generic;

#endif

using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;
using System.Globalization;
using CSScriptLibrary;

namespace csscript
{
    #region CSharpParser...
    /// <summary>
    /// Very light parser for C# code. The main purpose of it is to be very fast and reliable.
    /// It only extracts code information relative to the CS-Script.
    /// </summary>
    public class CSharpParser
    {
        /// <summary>
        /// Class to hold the script information on what pre- or post-execution script needs to be executed.
        /// pre- and post-script CS-Script command format:
        /// //css_prescript file([arg0][, arg1]..[,arg2])[,ignore];
        /// //file - script file (extension is optional)
        /// //arg0..N - script string arguments;
        /// If $this is specified as arg0..N it will be replaced with the parent script full name at execution time.
        /// </summary>
        public class CmdScriptInfo
        {
            /// <summary>
            ///	Creates an instance of CmdScriptInfo.
            /// </summary>
            /// <param name="statement">CS-Script pre- or post-script directive</param>
            /// <param name="preScript">If set to true the 'statement' is a pre-script otherwise it is a post-script.</param>
            /// <param name="parentScript">The file name of the main script.</param>
            public CmdScriptInfo(string statement, bool preScript, string parentScript)
            {
                this.preScript = preScript;

                int rBracket = -1;
                int lBracket = statement.IndexOf("(");
                if (lBracket != -1)
                {
#if net1
                    ArrayList argList = new ArrayList();
#else
                    List<string> argList = new List<string>();
#endif
                    argList.Add(CSSUtils.Args.DefaultPrefix + "nl");
                    argList.Add(statement.Substring(0, lBracket).Trim());

                    rBracket = statement.LastIndexOf(")");
                    if (rBracket == -1)
                        throw new ApplicationException("Cannot parse statement (" + statement + ").");

                    string clearArg;
                    foreach (string arg in SplitByDelimiter(statement.Substring(lBracket + 1, rBracket - lBracket - 1), ','))
                    {
                        clearArg = arg.Trim();
                        if (clearArg != string.Empty)
                            argList.Add(clearArg.StartsWith("$this") ? (clearArg == "$this.name" ? Path.GetFileNameWithoutExtension(parentScript) : parentScript) : clearArg);
                    }
#if net1
                    args = (string[])argList.ToArray(typeof(string));
#else
                    args = argList.ToArray();
#endif
                    if (statement.Substring(rBracket + 1).Trim().Replace(",", "") == "ignore")
                        abortOnError = false;
                }
                else
                {
                    int pos = statement.LastIndexOfAny(new char[] { ' ', '\t', ';' });
                    if (pos != -1)
                        args = new string[] { CSSUtils.Args.DefaultPrefix + "nl", statement.Substring(0, pos) };
                    else
                        args = new string[] { CSSUtils.Args.DefaultPrefix + "nl", statement };
                }
            }

            /// <summary>
            /// Script file and it's arguments.
            /// </summary>
            public string[] args;
            /// <summary>
            /// If set to 'true' the CmdScriptInfo describes the pre-script, otherwise it is for the post-script.
            /// </summary>
            public bool preScript;
            /// <summary>
            /// If set to 'true' parent script will be aborted on pre/post-script error, otherwise the error will be ignored.
            /// </summary>
            public bool abortOnError = true;
        }

        /// <summary>
        /// Class to hold the script initialization information.
        /// </summary>
        public class InitInfo
        {
            /// <summary>
            /// The boolean flag indicating if CoInitializeSecurity (with default parameters) should be called at the start of the script execution.
            /// </summary>
            public bool CoInitializeSecurity = false;

            /// <summary>
            /// The RpcImpLevel of CoInitializeSecurity arguments
            /// </summary>
            public int RpcImpLevel = 3;  //RpcImpLevel.Impersonate

            /// <summary>
            /// The EoAuthnCap of CoInitializeSecurity arguments
            /// </summary>
            public int EoAuthnCap = 0x40; //EoAuthnCap.DynamicCloaking

            static char[] tokenDelimiters = new char[] { '(', ')' };
            static char[] argDelimiters = new char[] { ',' };
            /// <summary>
            /// Initializes a new instance of the <see cref="InitInfo"/> class.
            /// </summary>
            /// <param name="statement">The original argument statement of the <c>//css_init</c> directive.</param>
            public InitInfo(string statement)
            {
                //CoInitializeSecurity or
                //CoInitializeSecurity(3,0x40)

                bool error = !statement.StartsWith("CoInitializeSecurity");

                if (!error)
                {
                    CoInitializeSecurity = true;

                    string[] parts = statement.Split(tokenDelimiters);

                    if (parts.Length == 1 && parts[0].Trim() == "CoInitializeSecurity")
                        return;

                    error = parts.Length < 2;

                    if (!error) //[CoInitializeSecurity][3,0x40][]
                    {
                        string[] args = parts[1].Split(argDelimiters);

                        if (args.Length == 1 && args[0].Trim() == "")
                        {
                            //empty brackets CoInitializeSecurity()
                        }
                        else
                        {
                            error = args.Length != 2
                                    || (!TryParseInt(args[0], out RpcImpLevel))
                                    || (!TryParseInt(args[1], out EoAuthnCap));
                        }
                    }
                }

                if (error)
                    throw new ApplicationException("Cannot parse //css_init directive. '" + statement + "' is in unexpected format.");
            }

            bool TryParseInt(string text, out int value)
            {
                text = text.TrimStart();
                if (text.StartsWith("0x", StringComparison.Ordinal))
                    return int.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                else
                    return int.TryParse(text, out value);
            }
        }

        /// <summary>
        /// Class to hold the script importing information, which actually controls how script is imported.
        /// </summary>
        public class ImportInfo
        {
            internal static ImportInfo[] ResolveStatement(string statement, string parentScript, string[] probinghDirs)
            {
                if (statement.Length > 1 && (statement[0] == '.' && statement[1] != '.')) //just a sinlgle-dot start dir
                    statement = Path.Combine(Path.GetDirectoryName(parentScript), statement);

                if (statement.Contains("*") || statement.Contains("?"))
                {
                    //e.g. resolve ..\subdir\*.cs into multiple concrete imports
                    string statementToParse = statement.Replace("($this.name)", Path.GetFileNameWithoutExtension(parentScript));
                    statementToParse = statementToParse.Replace("\t", "").Trim();

                    string[] parts = CSharpParser.SplitByDelimiter(statementToParse, DirectiveDelimiters);

                    string filePattern = parts[0];
#if net1
                    ArrayList result = new ArrayList();
                    foreach (string file in FileParser.ResolveFiles(filePattern, probinghDirs, false))
                    {
                        parts[0] = file; //substitute the file path pattern with the actual path
                        result.Add(new ImportInfo(parts));
                    }
                    return (ImportInfo[])result.ToArray(typeof(ImportInfo));
#else
                    List<ImportInfo> result = new List<ImportInfo>();

                    // To ensure that parent script dir is on top.
                    // Required because FileParser.ResolveFiles stops searching when it finds.
                    probinghDirs = Utils.RemoveDuplicates(Utils.Concat(new[] { Path.GetDirectoryName(parentScript) }, probinghDirs));

                    foreach (string file in FileParser.ResolveFiles(filePattern, probinghDirs, false))
                    {
                        parts[0] = file; //substitute the file path pattern with the actual path
                        result.Add(new ImportInfo(parts));
                    }

                    return result.ToArray();
#endif
                }
                else
                    return new ImportInfo[] { new ImportInfo(statement, parentScript) };
            }
            /// <summary>
            /// Creates an instance of ImportInfo.
            /// </summary>
            /// <param name="statement">CS-Script import directive (//css_import...) string.</param>
            /// <param name="parentScript">name of the parent (primary) script file.</param>
            public ImportInfo(string statement, string parentScript)
            {
                string statementToParse = statement.Replace("($this.name)", Path.GetFileNameWithoutExtension(parentScript));
                statementToParse = statementToParse.Replace("\t", "").Trim();

                string[] parts = CSharpParser.SplitByDelimiter(statementToParse, DirectiveDelimiters);
                this.file = parts[0];

                InternalInit(parts, 1);
            }

            private ImportInfo(string[] parts)
            {
                this.file = parts[0];
                InternalInit(parts, 1);
            }

            private void InternalInit(string[] statementParts, int startIndex)
            {
#if net1
                ArrayList renameingMap = new ArrayList();
#else
                List<string[]> renameingMap = new List<string[]>();
#endif

                for (int i = startIndex; i < statementParts.Length;)
                {
                    statementParts[i] = statementParts[i].Trim();
                    if (statementParts[i] == "rename_namespace" && i + 2 < statementParts.Length)
                    {
                        string[] names = new string[] { statementParts[i + 1], statementParts[i + 2].Replace(")", "") };
                        renameingMap.Add(names);
                        i += 3;
                    }
                    else if (statementParts[i] == "preserve_main")
                    {
                        preserveMain = true;
                        i += 1;
                    }
                    else
                        throw new ApplicationException("Cannot parse \"//css_import...\"");
                }
                if (renameingMap.Count == 0)
                    this.renaming = new string[0][];
                else
#if net1
                    this.renaming = (string[][])renameingMap.ToArray(typeof(string[]));
#else
                    this.renaming = renameingMap.ToArray();
#endif
            }

            /// <summary>
            /// The file to be imported.
            /// </summary>
            public string file;
            /// <summary>
            /// Renaming instructions (old_name vs. new_name)
            /// </summary>
            public string[][] renaming;
            /// <summary>
            /// If set to 'true' "static...Main" in the imported script is not renamed.
            /// </summary>
            public bool preserveMain = false;
        }

#if net1
        ArrayList stringRegions = new ArrayList();
        ArrayList commentRegions = new ArrayList();
#else
        List<int[]> stringRegions = new List<int[]>();
        List<int[]> commentRegions = new List<int[]>();
#endif

        #region Public interface
#if DEBUG
        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpParser"/> class.
        /// </summary>
        public CSharpParser() //Needed for testing only. Otherwise you will always call the constructor with the parameters
        {
            InitEnvironment();
        }
#endif
        static bool NeedInitEnvironment = true;
        static void InitEnvironment()
        {
            if (NeedInitEnvironment)
            {
                string css_nuget = Environment.GetEnvironmentVariable("css_nuget");
                if (css_nuget == null)
                {
                    try
                    {
                        //NuGet.NuGetCacheView will attempt to initialize (create) the cache directory and it is a problem if
                        //it happens that user has no rights to do so.
                        //Ignore the error and it will be reported when get.exe will try to download the package(s) into
                        //this cache directory.
                        Environment.SetEnvironmentVariable("css_nuget", NuGet.NuGetCacheView);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Trace.WriteLine("Cannot initialize NuGet cache folder.\n" + e.ToString());
                    }
                }
                NeedInitEnvironment = false;
            }
        }

        /// <summary>
        /// Creates an instance of CSharpParser.
        /// </summary>
        /// <param name="code">C# code string</param>
        public CSharpParser(string code)
        {
            InitEnvironment();
            Construct(code, false, null, null);
        }

        /// <summary>
        /// Creates an instance of CSharpParser.
        /// </summary>
        /// <param name="script">C# script (code or file).</param>
        /// <param name="isFile">If set to 'true' the script is a file, otherwise it is a C# code.</param>
        public CSharpParser(string script, bool isFile)
        {
            InitEnvironment();
            Construct(script, isFile, null, null);
        }

        /// <summary>
        /// Loads and parces the file the file.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <returns>Parser contining parsing result.</returns>
        public static CSharpParser LoadFile(string script)
        {
            return new CSharpParser(script, true);
        }

        /// <summary>
        /// Creates an instance of CSharpParser.
        /// </summary>
        /// <param name="script">C# script (code or file).</param>
        /// <param name="isFile">If set to 'true' the script is a file, otherwise it is a C# code.</param>
        /// <param name="directivesToSearch">Additional C# script directives to search. The search result is stored in CSharpParser.CustomDirectives.</param>
        public CSharpParser(string script, bool isFile, string[] directivesToSearch)
        {
            InitEnvironment();
            Construct(script, isFile, directivesToSearch, null);
        }

        /// <summary>
        /// Creates an instance of CSharpParser.
        /// </summary>
        /// <param name="script">C# script (code or file).</param>
        /// <param name="isFile">If set to 'true' the script is a file, otherwise it is a C# code.</param>
        /// <param name="directivesToSearch">Additional C# script directives to search. The search result is stored in CSharpParser.CustomDirectives.</param>
        /// <param name="probingDirs">Search directories for resolving wild card paths in //css_inc and //css_imp</param>
        public CSharpParser(string script, bool isFile, string[] directivesToSearch, string[] probingDirs)
        {
            InitEnvironment();
            Construct(script, isFile, directivesToSearch, probingDirs);
        }

        void Construct(string script, bool isFile, string[] directivesToSearch, string[] probingDirs)
        {
            if (!isFile)
                Init(script, "", directivesToSearch, probingDirs);
            else
                using (StreamReader sr = new StreamReader(script))
                {
                    string code = sr.ReadToEnd();
                    Init(code, script, directivesToSearch, probingDirs);
                }
        }

        /// <summary>
        /// The result of search for additional C# script directives to search (directive vs. value).
        /// </summary>
        public Hashtable CustomDirectives = new Hashtable();

        ///// <summary>
        ///// Parses the C# code. Only one of the 'code' and 'file' parameters can be non empty.
        ///// </summary>
        ///// <param name="code">C# script code (empty string if code is in a file form).</param>
        ///// <param name="file">The script file name (empty if code is in the text form).</param>
        //public void Init(string code, string file)
        //{
        //    Init(code, file, null, null);
        //}

        ///// <summary>
        ///// Parses the C# code.
        ///// </summary>
        ///// <param name="code">C# script (code or file).</param>
        ///// <param name="file">If set to 'true' the script is a file, otherwise it is a C# code.</param>
        ///// <param name="directivesToSearch">Additional C# script directives to search. The search result is stored in CSharpParser.CustomDirectives.</param>
        //void Init(string code, string file, string[] directivesToSearch)
        //{
        //    Init(code, file, directivesToSearch, null);
        //}

        /// <summary>
        /// Global flag to forcefuly supress any C# code analysys. This flag efectively disables
        /// all CS-Script assembly and script probing and most likely some other functionality.
        /// <para>You may ever want to supress code analysys only for profiling perposes or during performance tuning.</para>
        /// </summary>
        public static bool SupressCodeAnalysis = false;

        /// <summary>
        /// Parses the C# code.
        /// </summary>
        /// <param name="code">C# script (code or file).</param>
        /// <param name="file">If set to 'true' the script is a file, otherwise it is a C# code.</param>
        /// <param name="directivesToSearch">Additional C# script directives to search. The search result is stored in CSharpParser.CustomDirectives.</param>
        /// <param name="probingDirs">Search directories for resolving wild card paths in //css_inc and //css_imp</param>
        void Init(string code, string file, string[] directivesToSearch, string[] probingDirs)
        {
            string workingDir = Environment.CurrentDirectory;
            if (file != "")
                workingDir = Path.GetDirectoryName(file);

            this.code = code;

            if (SupressCodeAnalysis)
                return;

            //analyse comments and strings
            NoteCommentsAndStrings();

            //note the end of the header area (from the text start to the first class declaration)
            int pos = code.IndexOf("class");
            int endCodePos = code.Length - 1;
            while (pos != -1)
            {
                if (IsToken(pos, "class".Length) && !IsComment(pos))
                {
                    endCodePos = pos;
                    break;
                }
                pos = code.IndexOf("class", pos + 1);
            }

            //analyse script arguments
            foreach (string statement in GetRawStatements("//css_args", endCodePos))
            {
                foreach (string arg in SplitByDelimiter(statement, ','))
                {
                    string newArg = arg.Trim();
                    if (newArg.StartsWith("\""))
                        newArg = newArg.Substring(1);
                    if (newArg.EndsWith("\""))
                        newArg = newArg.Remove(newArg.Length - 1, 1);
                    args.Add(newArg);
                }
            }

            //analyse 'pre' and 'post' script commands
            foreach (string statement in GetRawStatements("//css_pre", endCodePos))
                cmdScripts.Add(new CmdScriptInfo(statement.Trim(), true, file));
            foreach (string statement in GetRawStatements("//css_prescript", endCodePos))
                cmdScripts.Add(new CmdScriptInfo(statement.Trim(), true, file));
            foreach (string statement in GetRawStatements("//css_post", endCodePos))
                cmdScripts.Add(new CmdScriptInfo(statement.Trim(), false, file));
            foreach (string statement in GetRawStatements("//css_postscript", endCodePos))
                cmdScripts.Add(new CmdScriptInfo(statement.Trim(), false, file));

            //analyze script initialization directives
            foreach (string statement in GetRawStatements("//css_init", endCodePos))
                inits.Add(new InitInfo(statement.Trim()));

            //analyze script initialization directives
            foreach (string statement in GetRawStatements("//css_nuget", endCodePos))
                foreach (string package in SplitByDelimiter(statement, ','))
                    nugets.Add(package.Trim());

            //analyse script imports/includes
            foreach (string statement in GetRawStatements("//css_import", endCodePos))
                imports.AddRange(ImportInfo.ResolveStatement(Environment.ExpandEnvironmentVariables(statement).Trim(), file, probingDirs));
            foreach (string statement in GetRawStatements("//css_imp", endCodePos))
                imports.AddRange(ImportInfo.ResolveStatement(Environment.ExpandEnvironmentVariables(statement).Trim(), file, probingDirs));
            foreach (string statement in GetRawStatements("//css_include", endCodePos))
                imports.AddRange(ImportInfo.ResolveStatement(Environment.ExpandEnvironmentVariables(statement).Trim() + ",preserve_main", file, probingDirs));
            foreach (string statement in GetRawStatements("//css_inc", endCodePos))
                imports.AddRange(ImportInfo.ResolveStatement(Environment.ExpandEnvironmentVariables(statement).Trim() + ",preserve_main", file, probingDirs));

            //analyse assembly references
            foreach (string statement in GetRawStatements("//css_reference", endCodePos))
                refAssemblies.Add(Environment.ExpandEnvironmentVariables(UnescapeDirectiveDelimiters(statement)).Trim());
            foreach (string statement in GetRawStatements("//css_ref", endCodePos))
                refAssemblies.Add(Environment.ExpandEnvironmentVariables(UnescapeDirectiveDelimiters(statement)).Trim());

            //analyse precompilers
            foreach (string statement in GetRawStatements("//css_precompiler", endCodePos))
                precompilers.Add(Environment.ExpandEnvironmentVariables(UnescapeDirectiveDelimiters(statement)).Trim());

            foreach (string statement in GetRawStatements("//css_pc", endCodePos))
                precompilers.Add(Environment.ExpandEnvironmentVariables(UnescapeDirectiveDelimiters(statement)).Trim());

            //analyse compiler options
            foreach (string statement in GetRawStatements("//css_co", endCodePos))
                compilerOptions.Add(Environment.ExpandEnvironmentVariables(UnescapeDirectiveDelimiters(statement)).Trim());

            if(!Utils.IsLinux())
                foreach (string statement in GetRawStatements("//css_host", endCodePos))
                    hostOptions.Add(Environment.ExpandEnvironmentVariables(UnescapeDirectiveDelimiters(statement)).Trim());

            //analyse assembly references
            foreach (string statement in GetRawStatements("//css_ignore_namespace", endCodePos))
                ignoreNamespaces.Add(statement.Trim());
            foreach (string statement in GetRawStatements("//css_ignore_ns", endCodePos))
                ignoreNamespaces.Add(statement.Trim());

            //analyse resource references
            foreach (string statement in GetRawStatements("//css_resource", endCodePos))
                resFiles.Add(UnescapeDirectiveDelimiters(statement).Trim());
            foreach (string statement in GetRawStatements("//css_res", endCodePos))
                resFiles.Add(UnescapeDirectiveDelimiters(statement).Trim());

            //analyse resource references
            foreach (string statement in GetRawStatements("//css_searchdir", endCodePos))
                searchDirs.AddRange(CSSUtils.GetDirectories(workingDir, Environment.ExpandEnvironmentVariables(UnescapeDirectiveDelimiters(statement)).Trim()));
            foreach (string statement in GetRawStatements("//css_dir", endCodePos))
                searchDirs.AddRange(CSSUtils.GetDirectories(workingDir, Environment.ExpandEnvironmentVariables(UnescapeDirectiveDelimiters(statement)).Trim()));

            //analyse namespace references
            foreach (string statement in GetRawStatements(code, "using", endCodePos, true))
                if (!statement.StartsWith("(")) //just to cut off "using statements" as we are interested in "using directives" only
                    refNamespaces.Add(statement.Trim().Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace(" ", ""));

            //analyse threading model
            pos = code.IndexOf("TAThread]");
            while (pos != -1 && pos > 0 && threadingModel == ApartmentState.Unknown)
            {
                if (!IsComment(pos - 1) && !IsString(pos - 1) && IsToken(pos - 2, "??TAThread]".Length))
                {
                    if (code[pos - 1] == 'S')
                        threadingModel = ApartmentState.STA;
                    else if (code[pos - 1] == 'M')
                        threadingModel = ApartmentState.MTA;
                }
                pos = code.IndexOf("TAThread]", pos + 1);
            }

            this.CustomDirectives.Clear();
            if (directivesToSearch != null)
            {
                foreach (string directive in directivesToSearch)
                {
#if net1
                    this.CustomDirectives[directive] = new ArrayList();
                    foreach (string statement in GetRawStatements(directive, endCodePos))
                        (this.CustomDirectives[directive] as ArrayList).Add(statement.Trim());
#else
                    this.CustomDirectives[directive] = new List<string>();
                    foreach (string statement in GetRawStatements(directive, endCodePos))
                        (this.CustomDirectives[directive] as List<string>).Add(statement.Trim());
#endif
                }
            }
        }

        class RenamingInfo
        {
            public RenamingInfo(int stratPos, int endPos, string newValue)
            {
                this.stratPos = stratPos;
                this.endPos = endPos;
                this.newValue = newValue;
            }

            public int stratPos;
            public int endPos;
            public string newValue;
        }

#if net1
        class RenamingInfoComparer : IComparer
        {
            int IComparer.Compare(object x, object y)
            {
                int retval = (x == null) ? -1 : (y == null ? 1 : 0);

                if (retval == 0)
                    return Comparer.Default.Compare(((RenamingInfo)x).stratPos, ((RenamingInfo)y).stratPos);
                else
                    return retval;
            }
        }
#else

        class RenamingInfoComparer : System.Collections.Generic.IComparer<RenamingInfo>
        {
            public int Compare(RenamingInfo x, RenamingInfo y)
            {
                int retval = (x == null) ? -1 : (y == null ? 1 : 0);

                if (retval == 0)
                    return Comparer.Default.Compare(x.stratPos, y.stratPos);
                else
                    return retval;
            }
        }

#endif

        /// <summary>
        /// Renames namespaces according renaming instructions.
        /// </summary>
        /// <param name="renamingMap">Renaming instructions (old_name vs. new_name).</param>
        /// <param name="preserveMain">/// If set to 'true' "static...Main" in the imported script is not renamed.</param>
        public void DoRenaming(string[][] renamingMap, bool preserveMain)
        {
            int renamingPos = -1;
#if net1
            ArrayList renamingPositions = new ArrayList();
#else
            List<RenamingInfo> renamingPositions = new List<RenamingInfo>();
#endif
            int pos = FindStatement("Main", 0);
            while (!preserveMain && pos != -1 && renamingPos == -1)
            {
                int declarationStart = code.LastIndexOfAny("{};".ToCharArray(), pos, pos);
                do
                {
                    if (!IsComment(declarationStart) || !IsString(declarationStart))
                    {
                        //test if it is "static void" Main
                        string statement = StripNonStringStatementComments(code.Substring(declarationStart + 1, pos - declarationStart - 1));
                        string[] tokens = statement.Trim().Split("\n\r\t ".ToCharArray());

                        foreach (string token in tokens)
                        {
                            if (token.Trim() == "static")
                                renamingPos = pos;
                        }
                        break;
                    }
                    else
                        declarationStart = code.LastIndexOfAny("{};".ToCharArray(), declarationStart - 1, declarationStart - 1);
                }
                while (declarationStart != -1 && renamingPos == -1);

                pos = FindStatement("Main", pos + 1);
            }
            if (renamingPos != -1)
                renamingPositions.Add(new RenamingInfo(renamingPos, renamingPos + "Main".Length, "i_Main"));

            foreach (string[] names in renamingMap)
            {
                renamingPos = -1;
                pos = FindStatement(names[0], 0);
                while (pos != -1 && renamingPos == -1)
                {
                    int declarationStart = code.LastIndexOfAny("{};".ToCharArray(), pos, pos);
                    do
                    {
                        if (!IsComment(declarationStart) || !IsString(declarationStart))
                        {
                            //test if it is "namespace" <name>
                            string test = code.Substring(declarationStart + 1, pos - declarationStart - 1);
                            string statement = StripNonStringStatementComments(code.Substring(declarationStart + 1, pos - declarationStart - 1));
                            string[] tokens = statement.Trim().Split("\n\r\t ".ToCharArray());

                            foreach (string token in tokens)
                            {
                                if (token.Trim() == "namespace")
                                {
                                    renamingPos = pos;
                                    break;
                                }
                            }
                            break;
                        }
                        else
                            declarationStart = code.LastIndexOfAny("{};".ToCharArray(), declarationStart - 1, declarationStart - 1);
                    }
                    while (declarationStart != -1 && renamingPos == -1);

                    pos = FindStatement(names[0], pos + 1);
                }
                if (renamingPos != -1)
                    renamingPositions.Add(new RenamingInfo(renamingPos, renamingPos + names[0].Length, names[1]));
            }

            renamingPositions.Sort(new RenamingInfoComparer());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < renamingPositions.Count; i++)
            {
                //RenamingInfo info = (RenamingInfo)renamingPositions[i];
                //int prevEnd = ((i - 1) >= 0) ? ((RenamingInfo)renamingPositions[i - 1]).endPos : 0;
#if net1
                RenamingInfo info = (RenamingInfo)renamingPositions[i];
                int prevEnd = ((i - 1) >= 0) ? ((RenamingInfo)renamingPositions[i - 1]).endPos : 0;
#else
                RenamingInfo info = renamingPositions[i];
                int prevEnd = ((i - 1) >= 0) ? renamingPositions[i - 1].endPos : 0;
#endif

                sb.Append(code.Substring(prevEnd, info.stratPos - prevEnd));
                sb.Append(info.newValue);
                if (i == renamingPositions.Count - 1) // the last renaming
                    sb.Append(code.Substring(info.endPos, code.Length - info.endPos));
            }
            this.modifiedCode = sb.ToString();
        }

        /// <summary>
        /// Embedded script arguments. The both script and engine arguments are allowed except "/noconfig" engine command line switch.
        /// </summary>
        public string[] Args
        {
            get
            {
#if net1
                return (string[])args.ToArray(typeof(string));
#else
                return args.ToArray();
#endif
            }
        }

        /// <summary>
        /// Embedded compiler options.
        /// </summary>
        public string[] CompilerOptions
        {
            get
            {
#if net1
                return (string[])compilerOptions.ToArray(typeof(string));
#else
                return compilerOptions.ToArray();
#endif
            }
        }

        /// <summary>
        /// Embedded compiler options.
        /// </summary>
        public string[] HostOptions
        {
            get
            {
#if net1
                return (string[])hostOptions.ToArray(typeof(string));
#else
                return hostOptions.ToArray();
#endif
            }
        }

        /// <summary>
        /// Precompilers.
        /// </summary>
        public string[] Precompilers
        {
            get
            {
#if net1
                return (string[])precompilers.ToArray(typeof(string));
#else
                return precompilers.ToArray();
#endif
            }
        }
        /// <summary>
        /// References to the external assemblies and namespaces.
        /// </summary>
        public string[] References
        {
            get
            {
#if net1
                ArrayList retval = new ArrayList();
                retval.AddRange(refAssemblies);
                retval.AddRange(refNamespaces);
                return (string[])retval.ToArray(typeof(string));
#else
                List<string> retval = new List<string>();
                retval.AddRange(refAssemblies);
                retval.AddRange(refNamespaces);
                return retval.ToArray();
#endif
            }
        }

        /// <summary>
        /// References to the external assemblies.
        /// </summary>
        public string[] RefAssemblies
        {
#if net1
            get { return (string[])refAssemblies.ToArray(typeof(string)); }
#else
            get { return refAssemblies.ToArray(); }
#endif
        }

        /// <summary>
        /// Names of namespaces to be ignored by namespace-to-assembly resolver.
        /// </summary>
        public string[] IgnoreNamespaces
        {
#if net1
            get { return (string[])ignoreNamespaces.ToArray(typeof(string)); }
#else
            get { return ignoreNamespaces.ToArray(); }
#endif
        }

        /// <summary>
        /// Additional search directories (for script and assembly probing).
        /// </summary>
        public string[] ExtraSearchDirs
        {
#if net1
            get { return (string[])searchDirs.ToArray(typeof(string)); }
#else
            get { return searchDirs.ToArray(); }
#endif
        }

        /// <summary>
        /// References to the resource files.
        /// </summary>
        public string[] ResFiles
        {
#if net1
            get { return (string[])resFiles.ToArray(typeof(string)); }
#else
            get { return resFiles.ToArray(); }
#endif
        }

        /// <summary>
        /// References to the namespaces.
        /// </summary>
        public string[] RefNamespaces
        {
#if net1
            get { return (string[])refNamespaces.ToArray(typeof(string)); }
#else
            get { return refNamespaces.ToArray(); }
#endif
        }

        /// <summary>
        /// References to the NuGet packages.
        /// </summary>
        public string[] NuGets
        {
#if net1
            get { return (string[])nugets.ToArray(typeof(string)); }
#else
            get { return nugets.ToArray(); }
#endif
        }

        /// <summary>
        /// C# scripts to be imported.
        /// </summary>
        public ImportInfo[] Imports
        {
#if net1
            get { return (ImportInfo[])imports.ToArray(typeof(ImportInfo)); }
#else
            get { return imports.ToArray(); }
#endif
        }

        /// <summary>
        /// Pre- and post-execution scripts.
        /// </summary>
        public CmdScriptInfo[] CmdScripts
        {
#if net1
            get { return (CmdScriptInfo[])cmdScripts.ToArray(typeof(CmdScriptInfo)); }
#else
            get { return cmdScripts.ToArray(); }
#endif
        }

        /// <summary>
        /// Script initialization directives.
        /// </summary>
        public InitInfo[] Inits
        {
#if net1
            get { return (InitInfo[])inits.ToArray(typeof(InitInfo)); }
#else
            get { return inits.ToArray(); }
#endif
        }

        /// <summary>
        /// Apartment state of the script.
        /// </summary>
        public ApartmentState ThreadingModel
        {
            get { return threadingModel; }
        }

        /// <summary>
        /// Script C# raw code.
        /// </summary>
        public string Code
        {
            get { return code; }
        }

        /// <summary>
        /// Script C# code after namespace renaming.
        /// </summary>
        public string ModifiedCode
        {
            get { return modifiedCode; }
        }

        #endregion Public interface

#if net1
        ArrayList searchDirs = new ArrayList();
        ArrayList resFiles = new ArrayList();
        ArrayList refAssemblies = new ArrayList();
        ArrayList compilerOptions = new ArrayList();
        ArrayList hostOptions = new ArrayList();
        ArrayList cmdScripts = new ArrayList();
        ArrayList inits = new ArrayList();
        ArrayList nugets = new ArrayList();
        ArrayList refNamespaces = new ArrayList();
        ArrayList ignoreNamespaces = new ArrayList();
        ArrayList imports = new ArrayList();
        ArrayList precompilers = new ArrayList();
        ArrayList args = new ArrayList();
#else
        List<string> searchDirs = new List<string>();
        List<string> resFiles = new List<string>();
        List<string> refAssemblies = new List<string>();
        List<string> compilerOptions = new List<string>();
        List<string> hostOptions = new List<string>();
        List<CmdScriptInfo> cmdScripts = new List<CmdScriptInfo>();
        List<InitInfo> inits = new List<InitInfo>();
        List<string> nugets = new List<string>();
        List<string> refNamespaces = new List<string>();
        List<string> ignoreNamespaces = new List<string>();
        List<ImportInfo> imports = new List<ImportInfo>();
        List<string> precompilers = new List<string>();
        List<string> args = new List<string>();
#endif

        ApartmentState threadingModel = ApartmentState.Unknown;
        string code = "";
        string modifiedCode = "";

        /// <summary>
        /// Enables omitting closing character (";") for CS-Script directives (e.g. "//css_ref System.Xml.dll" instead of "//css_ref System.Xml.dll;").
        /// </summary>
        public static bool OpenEndDirectiveSyntax = true;

        string[] GetRawStatements(string pattern, int endIndex)
        {
            return GetRawStatements(this.code, pattern, endIndex, false);
        }

        string[] GetRawStatements(string codeToAnalyse, string pattern, int endIndex, bool ignoreComments)
        {
#if net1
            ArrayList retval = new ArrayList();
#else
            List<string> retval = new List<string>();
#endif

            int pos = codeToAnalyse.IndexOf(pattern);
            int endPos = -1;
            while (pos != -1 && pos <= endIndex)
            {
                if (IsDirectiveToken(pos, pattern.Length))
                {
                    if (!ignoreComments || (ignoreComments && !IsComment(pos)))
                    {
                        pos += pattern.Length;

                        if (OpenEndDirectiveSyntax)
                            endPos = IndexOfDelimiter(pos, codeToAnalyse.Length - 1, '\n', ';');
                        else
                            endPos = IndexOfDelimiter(pos, codeToAnalyse.Length - 1, ';');

                        if (endPos != -1)
                            retval.Add(codeToAnalyse.Substring(pos, endPos - pos).Trim());
                    }
                }
                pos = codeToAnalyse.IndexOf(pattern, pos + 1);
            }
#if net1
            return (string[])retval.ToArray(typeof(string));
#else
            return retval.ToArray();
#endif
        }

        int[] AllRawIndexOf(string pattern, int startIndex, int endIndex) //all raw matches
        {
#if net1
            ArrayList retval = new ArrayList();
#else
            List<int> retval = new List<int>();
#endif

            int pos = code.IndexOf(pattern, startIndex, endIndex - startIndex);
            while (pos != -1)
            {
                retval.Add(pos);
                pos = code.IndexOf(pattern, pos + 1, endIndex - (pos + 1));
            }
#if net1
            return (int[])retval.ToArray(typeof(int));
#else
            return retval.ToArray();
#endif
        }

        int IndexOf(string pattern, int startIndex, int endIndex) //non-comment match
        {
            int pos = code.IndexOf(pattern, startIndex, endIndex - startIndex);
            while (pos != -1)
            {
                if (!IsComment(pos) && IsToken(pos, pattern.Length))
                    return pos;

                pos = code.IndexOf(pattern, pos + 1, endIndex - (pos + 1));
            }
            return -1;
        }

        internal static bool IsOneOf(char c, char[] items)
        {
            foreach (char delimiter in items)
            {
                if (c == delimiter)
                    return true;
            }
            return false;
        }

        internal static string[] SplitByDelimiter(string text, params char[] delimiters)
        {
            StringBuilder builder = new StringBuilder();
#if net1
            ArrayList retval = new ArrayList();
#else
            List<string> retval = new List<string>();
#endif
            char lastDelimiter = char.MinValue;

            foreach (char c in text)
            {
                if (lastDelimiter != char.MinValue)
                {
                    if (c != lastDelimiter)
                    {
                        string entry = builder.ToString();
                        if (entry != null && entry.Trim() != "")
                            retval.Add(entry.Trim());

                        builder.Length = 0;

                        if (IsOneOf(c, delimiters))
                            lastDelimiter = c;
                        else
                            lastDelimiter = char.MinValue;
                    }
                    else
                    {
                        lastDelimiter = char.MinValue;
                    }
                }
                else
                {
                    if (IsOneOf(c, delimiters))
                        lastDelimiter = c;
                }

                if (lastDelimiter == char.MinValue)
                    builder.Append(c);
            }

            if (builder.Length > 0)
                retval.Add(builder.ToString());

#if net1
            return (string[])retval.ToArray(typeof(string));
#else
            return retval.ToArray();
#endif
        }

        int IndexOfDelimiter(int startIndex, int endIndex, params char[] delimiters)
        {
            char lastDelimiter = char.MinValue;

            for (int i = startIndex; i <= endIndex; i++)
            {
                char c = code[i];
                if (lastDelimiter != char.MinValue)
                {
                    if (lastDelimiter == c) //delimiter was escaped
                        lastDelimiter = char.MinValue;
                    else
                        return i - 1;
                }
                else
                {
                    foreach (char delimiter in delimiters)
                        if (code[i] == delimiter)
                        {
                            lastDelimiter = delimiter;
                            if (i == endIndex)
                                return i;
                            break;
                        }
                }
            }
            return -1;
        }

        bool IsComment(int charPos)
        {
            foreach (int[] region in commentRegions)
            {
                if (charPos < region[0])
                    return false;
                else if (region[0] <= charPos && charPos <= region[1])
                    return true;
            }
            return false;
        }

        bool IsString(int charPos)
        {
            foreach (int[] region in stringRegions)
            {
                if (charPos < region[0])
                    return false;
                else if (region[0] <= charPos && charPos <= region[1])
                    return true;
            }
            return false;
        }

        //bool IsTokenOld(int startPos, int length)
        //{
        //    if (code.Length < startPos + length)
        //        return false;

        //    int probeStart = (startPos != 0) ? startPos - 1 : 0;
        //    int endPos = (code.Length == startPos + length) ? startPos + length : startPos + length + 1;

        //    string original = code.Substring(startPos, length);
        //    string probeStr = code.Substring(probeStart, endPos - probeStart);

        //    probeStr = probeStr.Replace(";", "").Replace("(", "").Replace(")", "").Replace("{", "");
        //    probeStr = probeStr.Trim();

        //    return probeStr.Length == original.Length;
        //}

        static char[] codeDelimiters = new char[] { ';', '(', ')', '{', };

        bool IsToken(int startPos, int length)
        {
            if (code.Length < startPos + length) //the rest of the text is too short
                return false;

            if (startPos != 0 && !(char.IsWhiteSpace(code[startPos - 1]) || IsOneOf(code[startPos - 1], codeDelimiters))) //position is not at the start of the token
                return false;

            if (code.Length > startPos + length && !(char.IsWhiteSpace(code[startPos + length]) || IsOneOf(code[startPos + length], codeDelimiters))) //position is not at the end of the token
                return false;


            return true;
        }

        bool IsDirectiveToken(int startPos, int length)
        {
            if (code.Length < startPos + length) //the rest of the text is too short
                return false;

            if (startPos != 0 && !char.IsWhiteSpace(code[startPos - 1])) //position is not at the start of the token
                return false;

            //if (code.Length > startPos + length && !(char.IsWhiteSpace(code[startPos + length]) || IsDelimiter(code[startPos + length], DirectiveDelimiters))) //position is not at the end of the token
            if (code.Length > startPos + length && !char.IsWhiteSpace(code[startPos + length])) //position is not at the end of the token
                return false;

            int endPos = startPos + length;

            char lastDelimiter = char.MinValue;

            for (int i = startPos; i <= endPos; i++)
            {
                char c = code[i];
                if (lastDelimiter != char.MinValue)
                {
                    if (lastDelimiter == c) //delimiter was escaped
                        lastDelimiter = char.MinValue;
                    else
                        return false;
                }
                else
                {
                    if (IsOneOf(c, DirectiveDelimiters))
                    {
                        lastDelimiter = c;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Escapes the CS-Script directive (e.g. //css_*) delimiters.
        /// <para>All //css_* directives should escape any internal CS-Script delimiters by doubling the delimiter character.
        /// For example //css_include for 'script(today).cs' should escape brackets as they are the directive delimiters.
        /// The correct syntax would be as follows '//css_include script((today)).cs;'</para>
        /// <remarks>The delimiters characters are ';,(){}'.
        /// <para>However you should check <see cref="csscript.CSharpParser.DirectiveDelimiters"/> for the accurate list of all delimiters.
        /// </para>
        /// </remarks>
        /// </summary>
        /// <param name="text">The text to be processed.</param>
        /// <returns></returns>
        public static string EscapeDirectiveDelimiters(string text)
        {
            foreach (char c in DirectiveDelimiters)
                text = text.Replace(c.ToString(), new string(c, 2)); //very unoptimized but it is intended only for troubleshooting.
            return text;
        }

        /// <summary>
        /// Unescapes the CS-Script directive (e.g. //css_*) delimiters.
        /// <para>All //css_* directives should escape any internal CS-Script delimiters by doubling the delimiter character.
        /// For example //css_include for 'script(today).cs' should escape brackets as they are the directive delimiters.
        /// The correct syntax would be as follows '//css_include script((today)).cs;'</para>
        /// <remarks>The delimiters characters are ';,(){}'.
        /// <para>However you should check <see cref="csscript.CSharpParser.DirectiveDelimiters"/> for the accurate list of all delimiters.
        /// </para>
        /// </remarks>
        /// </summary>
        /// <param name="text">The text to be processed.</param>
        /// <returns></returns>
        public static string UnescapeDirectiveDelimiters(string text)
        {
            foreach (char c in DirectiveDelimiters)
                text = text.Replace(new string(c, 2), c.ToString()); //very unoptimized but it is intended only for troubleshooting.
            return text;
        }

        /// <summary>
        /// The //css_* directive delimiters.
        /// <remarks>All //css_* directives should escape any internal CS-Script delimiters by doubling the delimiter character.
        /// For example //css_include for 'script(today).cs' should escape brackets as they are the directive delimiters.
        /// The correct syntax would be as follows '//css_include script((today)).cs;'
        /// </remarks>
        /// </summary>
        public static char[] DirectiveDelimiters = new char[] { ';', '(', ')', '{', '}', ',' };

        void NoteCommentsAndStrings()
        {
#if net1
            ArrayList quotationChars = new ArrayList();
#else
            List<int> quotationChars = new List<int>();
#endif
            int startPos = -1;
            int startSLC = -1; //single line comment
            int startMLC = -1; //multiple line comment
            int searchOffset = 0;
            string endToken = "";
            string startToken = "";
            int endPos = -1;
            int lastEndPos = -1;
            do
            {
                startSLC = code.IndexOf("//", searchOffset);
                startMLC = code.IndexOf("/*", searchOffset);

                if (startSLC == Math.Min(startSLC != -1 ? startSLC : Int16.MaxValue,
                                         startMLC != -1 ? startMLC : Int16.MaxValue))
                {
                    startPos = startSLC;
                    startToken = "//";
                    endToken = "\n";
                }
                else
                {
                    startPos = startMLC;
                    startToken = "/*";
                    endToken = "*/";
                }

                if (startPos != -1)
                    endPos = code.IndexOf(endToken, startPos + startToken.Length);

                if (startPos != -1 && endPos != -1)
                {
                    int startCode = commentRegions.Count == 0 ? 0 : ((int[]) commentRegions[commentRegions.Count - 1])[1] + 1;

                    int[] quotationIndexes = AllRawIndexOf("\"", startCode, startPos);
                    if ((quotationIndexes.Length % 2) != 0)
                    {
                        searchOffset = startPos + startToken.Length;
                        continue;
                    }

                    //string comment = code.Substring(startPos, endPos - startPos);
                    commentRegions.Add(new int[2] { startPos, endPos });
                    quotationChars.AddRange(quotationIndexes);

                    searchOffset = endPos + endToken.Length;
                }
            }
            while (startPos != -1 && endPos != -1);

            if (lastEndPos != 0 && searchOffset < code.Length)
            {
                quotationChars.AddRange(AllRawIndexOf("\"", searchOffset, code.Length));
            }

            for (int i = 0; i < quotationChars.Count; i++)
            {
#if net1
                if (i + 1 < stringRegions.Count)
                    stringRegions.Add(new int[] { (int)quotationChars[i], (int)quotationChars[i + 1] });
                else
                    stringRegions.Add(new int[] { (int)quotationChars[i], -1 });
#else
                if (i + 1 < stringRegions.Count)
                    stringRegions.Add(new int[] { quotationChars[i], quotationChars[i + 1] });
                else
                    stringRegions.Add(new int[] { quotationChars[i], -1 });
#endif
                i++;
            }
        }

        int FindStatement(string pattern, int start)
        {
            if (code.Length == 0)
                return -1;

            int pos = IndexOf(pattern, start, code.Length - 1);
            while (pos != -1)
            {
                if (!IsString(pos))
                    return pos;
                else
                    pos = IndexOf(pattern, pos + 1, code.Length - 1);
            }
            return -1;
        }

        string StripNonStringStatementComments(string text)
        {
            StringBuilder sb = new StringBuilder();
            int startPos = -1;
            int startSLC = -1; //single line comment
            int startMLC = -1; //multiple line comment
            int searchOffset = 0;
            string endToken = "";
            string startToken = "";
            int endPos = -1;
            int lastEndPos = -1;
            do
            {
                startSLC = text.IndexOf("//", searchOffset);
                startMLC = text.IndexOf("/*", searchOffset);

                if (startSLC == Math.Min(startSLC != -1 ? startSLC : Int16.MaxValue,
                                         startMLC != -1 ? startMLC : Int16.MaxValue))
                {
                    startPos = startSLC;
                    startToken = "//";
                    endToken = "\n";
                }
                else
                {
                    startPos = startMLC;
                    startToken = "/*";
                    endToken = "*/";
                }

                if (startPos != -1)
                    endPos = text.IndexOf(endToken, startPos + startToken.Length);

                if (startPos != -1 && endPos != -1)
                {
                    string codeFragment = text.Substring(searchOffset, startPos - searchOffset);
                    sb.Append(codeFragment);

                    searchOffset = endPos + endToken.Length;
                }
            }
            while (startPos != -1 && endPos != -1);

            if (lastEndPos != 0 && searchOffset < code.Length)
            {
                string codeFragment = text.Substring(searchOffset, text.Length - searchOffset);
                sb.Append(codeFragment);
            }
            return sb.ToString();
        }
    }

    #endregion CSharpParser...
}