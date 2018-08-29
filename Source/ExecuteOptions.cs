#region Licence...

//-----------------------------------------------------------------------------
// Date:	17/10/04	Time: 2:33p
// Module:	csscript.cs
// Classes:	CSExecutor
//			ExecuteOptions
//
// This module contains the definition of the CSExecutor class. Which implements
// compiling C# code and executing 'Main' method of compiled assembly
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
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using CSScriptLibrary;
using System.Runtime.InteropServices;
using System.Threading;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Diagnostics;
using Microsoft.CSharp;
using System.Linq;

namespace csscript
{
    /// <summary>
    /// Indicates synchronization model used to for controlling concurrency when the same script file is executed by multiple processes.
    /// </summary>
    public enum ConcurrencyControl
    {
        /// <summary>
        /// Simple model. The script engine doesn't start the timestamp validation and the script compilation until another engine
        /// validating finishes its job.
        /// <para>
        /// Note: the compilation may be skipped if caching is enabled and the validation reviles that the previous compilation (cache)
        /// is still up to date. </para>
        /// <para>
        /// Due to the limited choices with the system wide named synchronization objects on Linux <c>Standard</c> is the only available
        /// synchronization model on Linux. And it is just happens to be a good default choice for Windows as well.</para>
        /// </summary>
        Standard,

        /// <summary>
        /// A legacy synchronization model available on Windows only. While it can be beneficial in the intense concurrent "border line" scenarios,
        /// its practical value very limited.
        /// </summary>
        HighResolution,

        /// <summary>
        /// No concurrency control is done by the script engine. All synchronization is the responsibility of the hosting environment.
        /// </summary>
        None
    }

    /// <summary>
    /// Application specific runtime settings
    /// </summary>
    internal class ExecuteOptions : ICloneable
    {
        public static ExecuteOptions options = new ExecuteOptions();

        public ExecuteOptions()
        {
            options = this;
        }

        public object Clone()
        {
            ExecuteOptions clone = new ExecuteOptions();
            clone.processFile = this.processFile;
            clone.scriptFileName = this.scriptFileName;
            // clone.noLogo = this.noLogo;
            clone.useCompiled = this.useCompiled;
            clone.suppressTimestampAltering = this.suppressTimestampAltering;
            clone.useSmartCaching = this.useSmartCaching;
            clone.DLLExtension = this.DLLExtension;
            clone.forceCompile = this.forceCompile;
            clone.suppressExecution = this.suppressExecution;
            clone.syntaxCheck = this.syntaxCheck;
            clone.DBG = this.DBG;
            clone.TargetFramework = this.TargetFramework;
            clone.verbose = this.verbose;
            clone.startDebugger = this.startDebugger;
            clone.local = this.local;
            clone.buildExecutable = this.buildExecutable;
            clone.refAssemblies = new List<string>(this.refAssemblies).ToArray();
            clone.searchDirs = new List<string>(this.searchDirs).ToArray();
            clone.buildWinExecutable = this.buildWinExecutable;
            clone.useSurrogateHostingProcess = this.useSurrogateHostingProcess;
            clone.altCompiler = this.altCompiler;
            clone.roslynDir = this.roslynDir;
            clone.consoleEncoding = this.consoleEncoding;
            clone.resolveAutogenFilesRefs = this.resolveAutogenFilesRefs;
            clone.preCompilers = this.preCompilers;
            clone.postProcessor = this.postProcessor;
            clone.compilerOptions = this.compilerOptions;
            clone.reportDetailedErrorInfo = this.reportDetailedErrorInfo;
            clone.hideCompilerWarnings = this.hideCompilerWarnings;
            clone.apartmentState = this.apartmentState;
            clone.openEndDirectiveSyntax = this.openEndDirectiveSyntax;
            clone.forceOutputAssembly = this.forceOutputAssembly;
            clone.cleanupShellCommand = this.cleanupShellCommand;
            clone.versionOnly = this.versionOnly;
            clone.noConfig = this.noConfig;
            //clone.suppressExternalHosting = this.suppressExternalHosting;
            clone.altConfig = this.altConfig;
            clone.defaultRefAssemblies = this.defaultRefAssemblies;
            clone.hideTemp = this.hideTemp;
            clone.autoClass = this.autoClass;
            clone.autoClassMode = this.autoClassMode;
            clone.autoClass_InjectBreakPoint = this.autoClass_InjectBreakPoint;
            clone.decorateAutoClassAsCS6 = this.decorateAutoClassAsCS6;
            clone.enableDbgPrint = this.enableDbgPrint;
            clone.initContext = this.initContext;
            clone.nonExecuteOpRquest = this.nonExecuteOpRquest;
            clone.customHashing = this.customHashing;
            clone.compilationContext = this.compilationContext;
            clone.useScriptConfig = this.useScriptConfig;
            clone.customConfigFileName = this.customConfigFileName;
            clone.scriptFileNamePrimary = this.scriptFileNamePrimary;
            clone.doCleanupAfterNumberOfRuns = this.doCleanupAfterNumberOfRuns;
            clone.inMemoryAsm = this.inMemoryAsm;
            clone.concurrencyControl = this.concurrencyControl;
            clone.shareHostRefAssemblies = this.shareHostRefAssemblies;
            return clone;
        }

        public object Derive()
        {
            ExecuteOptions clone = new ExecuteOptions();
            clone.processFile = this.processFile;
            //some props will be further set by the caller
            //clone.scriptFileName = this.scriptFileName;
            //clone.noLogo = this.noLogo;
            //clone.useCompiled = this.useCompiled;
            //clone.DLLExtension = this.DLLExtension;
            //clone.forceCompile = this.forceCompile;
            clone.useSmartCaching = this.useSmartCaching;
            clone.suppressExecution = this.suppressExecution;
            clone.InjectScriptAssemblyAttribute = this.InjectScriptAssemblyAttribute;
            clone.resolveAutogenFilesRefs = this.resolveAutogenFilesRefs;
            clone.DBG = this.DBG;
            clone.TargetFramework = this.TargetFramework;
            clone.verbose = this.verbose;
            clone.local = this.local;
            clone.buildExecutable = this.buildExecutable;
            clone.refAssemblies = new List<string>(this.refAssemblies).ToArray();
            clone.searchDirs = new List<string>(this.searchDirs).ToArray();
            clone.buildWinExecutable = this.buildWinExecutable;
            clone.altCompiler = this.altCompiler;
            clone.roslynDir = this.roslynDir;
            clone.preCompilers = this.preCompilers;
            clone.defaultRefAssemblies = this.defaultRefAssemblies;
            clone.postProcessor = this.postProcessor;
            clone.compilerOptions = this.compilerOptions;
            clone.reportDetailedErrorInfo = this.reportDetailedErrorInfo;
            clone.hideCompilerWarnings = this.hideCompilerWarnings;
            clone.openEndDirectiveSyntax = this.openEndDirectiveSyntax;
            clone.apartmentState = this.apartmentState;
            clone.forceOutputAssembly = this.forceOutputAssembly;
            clone.versionOnly = this.versionOnly;
            clone.cleanupShellCommand = this.cleanupShellCommand;
            clone.noConfig = this.noConfig;
            //clone.suppressExternalHosting = this.suppressExternalHosting;
            clone.compilationContext = this.compilationContext;
            clone.autoClass = this.autoClass;
            clone.autoClass_InjectBreakPoint = this.autoClass_InjectBreakPoint;
            clone.decorateAutoClassAsCS6 = this.decorateAutoClassAsCS6;
            clone.enableDbgPrint = this.enableDbgPrint;
            clone.customHashing = this.customHashing;
            clone.altConfig = this.altConfig;
            clone.hideTemp = this.hideTemp;
            clone.nonExecuteOpRquest = this.nonExecuteOpRquest;
            clone.initContext = this.initContext;
            clone.scriptFileNamePrimary = this.scriptFileNamePrimary;
            clone.doCleanupAfterNumberOfRuns = this.doCleanupAfterNumberOfRuns;
            clone.shareHostRefAssemblies = this.shareHostRefAssemblies;
            clone.concurrencyControl = this.concurrencyControl;
            clone.inMemoryAsm = this.inMemoryAsm;

            return clone;
        }

        public bool inMemoryAsm = false;
        public bool processFile = true;
        public object nonExecuteOpRquest = null;
        public int compilationContext = 0;
        public string scriptFileName = "";
        public object initContext = null;
        public string scriptFileNamePrimary = null;
        public bool useCompiled = false;
        public bool useScriptConfig = true;
        public bool useSmartCaching = true; //hardcoded true but can be set from config file in the future
        public bool suppressTimestampAltering = false; //hardcoded true but can be set from config file in the future
        public string customConfigFileName = "";
        public bool DLLExtension = false;
        public bool forceCompile = false;
        public bool suppressExecution = false;
        public bool syntaxCheck = false;
        public bool DBG = false;

#if net35
        public string TargetFramework = "v3.5";
#else
        public string TargetFramework = "v4.0";
#endif
        internal bool InjectScriptAssemblyAttribute = true;
        public bool verbose = false;
        public bool startDebugger = false;
        public bool local = true;
        public bool buildExecutable = false;
        public string[] refAssemblies = new string[0];
        public string[] searchDirs = new string[0];
        public bool shareHostRefAssemblies = false;
        public bool buildWinExecutable = false;
        public bool openEndDirectiveSyntax = true;
        public bool resolveAutogenFilesRefs = true;
        public bool useSurrogateHostingProcess = false;
        public string altCompiler = "";
        public string roslynDir = "";
        public string consoleEncoding = "utf-8";
        public bool decorateAutoClassAsCS6 = false;
        public bool enableDbgPrint = true;
        public string preCompilers = "";
        public string defaultRefAssemblies = "";
        public string postProcessor = "";
        public bool reportDetailedErrorInfo = false;
        public bool hideCompilerWarnings = false;
        public ApartmentState apartmentState = ApartmentState.STA;
        public ConcurrencyControl concurrencyControl = ConcurrencyControl.Standard;
        public string forceOutputAssembly = "";
        public string cleanupShellCommand = "";
        public bool noConfig = false;
        public bool customHashing = true;
        public bool autoClass = false;
        public string autoClassMode = "";
        public bool autoClass_InjectBreakPoint = false;
        public bool versionOnly = false;
        public string compilerOptions = "";
        public string altConfig = "";
        public Settings.HideOptions hideTemp = Settings.HideOptions.HideMostFiles;
        public uint doCleanupAfterNumberOfRuns = 20;

        public void AddSearchDir(string dir, string section)
        {
            if (!this.searchDirs.Contains(dir))
            {
                this.searchDirs = this.searchDirs.ToList().AddIfNotThere(dir, section).ToArray();
            }
        }

        // public void AddSearchDir1(string dir)
        // {
        //     if (Array.Find(this.searchDirs, (x) => x == dir) != null)
        //         return;

        //     string[] newSearchDirs = new string[this.searchDirs.Length + 1];
        //     this.searchDirs.CopyTo(newSearchDirs, 0);
        //     newSearchDirs[newSearchDirs.Length - 1] = dir;
        //     this.searchDirs = newSearchDirs;
        // }

        public string[] ExtractShellCommand(string command)
        {
            int pos = command.IndexOf("\"");
            string endToken = "\"";
            if (pos == -1 || pos != 0) //no quotation marks
                endToken = " ";

            pos = command.IndexOf(endToken, pos + 1);
            if (pos == -1)
                return new string[] { command };
            else
                return new string[] { command.Substring(0, pos).Replace("\"", ""), command.Substring(pos + 1).Trim() };
        }
    }
}