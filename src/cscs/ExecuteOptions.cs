using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
            clone.isNetFx = this.isNetFx;
            clone.useCompiled = this.useCompiled;
            clone.suppressTimestampAltering = this.suppressTimestampAltering;
            clone.useSmartCaching = this.useSmartCaching;
            clone.compileDLL = this.compileDLL;
            clone.forceCompile = this.forceCompile;
            clone.suppressExecution = this.suppressExecution;
            clone.syntaxCheck = this.syntaxCheck;
            clone.DBG = this.DBG;
            clone.TargetFramework = this.TargetFramework;
            clone.verbose = this.verbose;
            clone.profile = this.profile;
            clone.startDebugger = this.startDebugger;
            clone.local = this.local;
            clone.buildExecutable = this.buildExecutable;
            clone.runExternal = this.runExternal;
            clone.refAssemblies = new List<string>(this.refAssemblies).ToArray();
            clone.searchDirs = new List<string>(this.searchDirs).ToArray();
            clone.buildWinExecutable = this.buildWinExecutable;
            clone.altCompiler = this.altCompiler;
            clone.consoleEncoding = this.consoleEncoding;
            clone.resolveAutogenFilesRefs = this.resolveAutogenFilesRefs;
            clone.legacyNugetSupport = this.legacyNugetSupport;
            clone.preCompilers = this.preCompilers;
            clone.compilerOptions = this.compilerOptions;
            clone.reportDetailedErrorInfo = this.reportDetailedErrorInfo;
            clone.hideCompilerWarnings = this.hideCompilerWarnings;
            clone.apartmentState = this.apartmentState;
            clone.openEndDirectiveSyntax = this.openEndDirectiveSyntax;
            clone.forceOutputAssembly = this.forceOutputAssembly;
            clone.versionOnly = this.versionOnly;
            clone.noConfig = this.noConfig;
            clone.altConfig = this.altConfig;
            clone.defaultRefAssemblies = this.defaultRefAssemblies;
            clone.hideTemp = this.hideTemp;
            clone.compilerEngine = this.compilerEngine;
            clone.autoClass = this.autoClass;
            clone.autoClass_InjectBreakPoint = this.autoClass_InjectBreakPoint;
            clone.defaultCompilerEngine = this.defaultCompilerEngine;
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
            //clone.scriptFileName
            //clone.noLogo
            //clone.useCompiled
            //clone.DLLExtension
            //clone.forceCompile
            clone.useSmartCaching = this.useSmartCaching;
            clone.suppressExecution = this.suppressExecution;
            clone.InjectScriptAssemblyAttribute = this.InjectScriptAssemblyAttribute;
            clone.legacyNugetSupport = this.legacyNugetSupport;
            clone.resolveAutogenFilesRefs = this.resolveAutogenFilesRefs;
            clone.DBG = this.DBG;
            clone.TargetFramework = this.TargetFramework;
            clone.verbose = this.verbose;
            clone.profile = this.profile;
            clone.local = this.local;
            clone.buildExecutable = this.buildExecutable;
            clone.refAssemblies = new List<string>(this.refAssemblies).ToArray();
            clone.searchDirs = new List<string>(this.searchDirs).ToArray();
            clone.buildWinExecutable = this.buildWinExecutable;
            clone.altCompiler = this.altCompiler;
            clone.preCompilers = this.preCompilers;
            clone.defaultRefAssemblies = this.defaultRefAssemblies;
            clone.compilerOptions = this.compilerOptions;
            clone.reportDetailedErrorInfo = this.reportDetailedErrorInfo;
            clone.hideCompilerWarnings = this.hideCompilerWarnings;
            clone.openEndDirectiveSyntax = this.openEndDirectiveSyntax;
            clone.apartmentState = this.apartmentState;
            clone.forceOutputAssembly = this.forceOutputAssembly;
            clone.versionOnly = this.versionOnly;
            clone.noConfig = this.noConfig;
            //clone.suppressExternalHosting
            clone.compilationContext = this.compilationContext;
            clone.compilerEngine = this.compilerEngine;
            clone.autoClass = this.autoClass;
            clone.autoClass_InjectBreakPoint = this.autoClass_InjectBreakPoint;
            clone.defaultCompilerEngine = this.defaultCompilerEngine;
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

        public bool inMemoryAsm = true;
        public bool processFile = true;
        public object nonExecuteOpRquest = null;
        public int compilationContext = 0;
        public string scriptFileName = "";
        public bool isNetFx = false;
        public object initContext = null;
        public string scriptFileNamePrimary = null;
        public bool useCompiled = false;
        public bool useScriptConfig = true;
        public bool useSmartCaching = true; //hard-coded true but can be set from config file in the future
        public bool suppressTimestampAltering = false; //hard-coded true but can be set from config file in the future
        public string customConfigFileName = "";
        public bool compileDLL = false;
        public bool forceCompile = false;
        public bool suppressExecution = false;
        public bool syntaxCheck = false;
        public bool DBG = false;
        public string TargetFramework = "v4.0";
        internal bool InjectScriptAssemblyAttribute = true;
        public bool verbose = false;
        public bool profile = false;
        public bool startDebugger = false;
        public bool local = true;
        public bool buildExecutable = false;
        public bool runExternal = false;
        public string[] refAssemblies = new string[0];
        public string[] searchDirs = new string[0];
        public bool shareHostRefAssemblies = false;
        public bool buildWinExecutable = false;
        public bool openEndDirectiveSyntax = true;
        public bool resolveAutogenFilesRefs = true;
        public bool legacyNugetSupport = true;
        public string altCompiler = "";
        public string consoleEncoding = "utf-8";
        public string defaultCompilerEngine = "dotnet";
        public bool enableDbgPrint = true;
        public string preCompilers = "";
        public string defaultRefAssemblies = "";
        public bool reportDetailedErrorInfo = false;
        public bool hideCompilerWarnings = false;
        public ApartmentState apartmentState = ApartmentState.STA;
        public ConcurrencyControl concurrencyControl = ConcurrencyControl.Standard;
        public string forceOutputAssembly = "";
        public bool noConfig = false;
        public bool customHashing = true;
        public bool autoClass = true;
        public string compilerEngine = null;
        public bool autoClass_InjectBreakPoint = false;
        public bool versionOnly = false;
        public string compilerOptions = "";
        public string altConfig = "";
        public Settings.HideOptions hideTemp = Settings.HideOptions.HideAll;
        public uint doCleanupAfterNumberOfRuns = 20;

        public void AddSearchDir(string dir, string section)
        {
            if (!this.searchDirs.Contains(dir))
            {
                this.searchDirs = this.searchDirs.ToList().AddPathIfNotThere(dir, section).ToArray();
            }
        }
    }
}