#region Licence...

//-----------------------------------------------------------------------------
// Date:	25/10/10	Time: 2:33p
// Module:	settings.cs
// Classes:	Settings
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
using System.Collections.Generic;

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace csscript
{
    /// <summary>
    /// Settings is an class that holds CS-Script application settings.
    /// </summary>
    public class Settings
    {
        internal const string dirs_section_prefix = "------- (";
        internal const string dirs_section_suffix = ") -------";
        internal const string local_dirs_section = dirs_section_prefix + "local dirs" + dirs_section_suffix;
        internal const string cmd_dirs_section = dirs_section_prefix + "dirs from cmd args" + dirs_section_suffix;
        internal const string code_dirs_section = dirs_section_prefix + "dirs from code" + dirs_section_suffix;
        internal const string config_dirs_section = dirs_section_prefix + "dirs from config" + dirs_section_suffix;
        internal const string internal_dirs_section = dirs_section_prefix + "cs-script special dirs" + dirs_section_suffix;
        internal const string nuget_dirs_section = dirs_section_prefix + "dirs from nuget store" + dirs_section_suffix;

        internal static bool ProbingLegacyOrder
        {
            get { return Environment.GetEnvironmentVariable("CSS_ProbingLegacyOrder") != null; }
        }

        /// <summary>
        /// Command to be executed to perform custom cleanup.
        /// If this value is empty automatic cleanup of all
        /// temporary files will occurs after the script execution.
        /// This implies that the script has to be executed in the
        /// separate AppDomain and some performance penalty will be incurred.
        ///
        /// Setting this value to the command for custom cleanup application
        /// (e.g. csc.exe cleanTemp.cs) will force the script engine to execute
        /// script in the 'current' AppDomain what will improve performance.
        /// </summary>
        [Category("CustomCleanup"), Description("Command to be executed to perform custom cleanup.")]
        public string CleanupShellCommand
        {
            get { return cleanupShellCommand; }
            set { cleanupShellCommand = value; }
        }

        /// <summary>
        /// Returns value of the CleanupShellCommand (with expanding environment variables).
        /// </summary>
        /// <returns>shell command string</returns>
        public string ExpandCleanupShellCommand() { return Environment.ExpandEnvironmentVariables(cleanupShellCommand); }

        string cleanupShellCommand = "";

        /// <summary>
        /// This value indicates frequency of the custom cleanup
        /// operation. It has affect only if CleanupShellCommand is not empty.
        /// </summary>
        [Category("CustomCleanup"), Description("This value indicates frequency of the custom cleanup operation.")]
        public uint DoCleanupAfterNumberOfRuns
        {
            get { return doCleanupAfterNumberOfRuns; }
            set { doCleanupAfterNumberOfRuns = value; }
        }

        uint doCleanupAfterNumberOfRuns = 30;

        /// <summary>
        /// Location of alternative code provider assembly. If set it forces script engine to use an alternative code compiler.
        /// </summary>
        [Category("Extensibility"), Description("Location of alternative code provider assembly. If set it forces script engine to use an alternative code compiler.")]
#if !InterfaceAssembly
        [Editor(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(UITypeEditor))]
#endif
        public string UseAlternativeCompiler
        {
            get { return useAlternativeCompiler; }
            set { useAlternativeCompiler = value; }
        }

        /// <summary>
        /// Gets or sets the path to the Roslyn directory. This setting is used to redirect Microsoft.CodeDom.Providers.DotNetCompilerPlatform.dll to the
        /// custom location of the Roslyn compilers (e.g. /usr/lib/mono/4.5).
        /// </summary>
        /// <value>
        /// The Roslyn directory.
        /// </value>
        public string RoslynDir
        {
            get { return _RoslynDir; }
            set { _RoslynDir = value; }
        }

        static internal string _RoslynDir
        {
            get { return Environment.GetEnvironmentVariable("CSSCRIPT_ROSLYN") ?? ""; }
            set { Environment.SetEnvironmentVariable("CSSCRIPT_ROSLYN", value); }
        }

        /// <summary>
        /// <para> When importing dependency scripts with '//css_include' or '//css_import' you can use relative path.
        /// Resolving relative path is typically done with respect to the <c>current directory</c>.
        /// </para>
        /// <para>
        /// The only exception is any path that starts with a single dot ('.') prefix, which triggers the conversion of the path into the absolute path
        /// with respect to the location of the file containing the import directive.
        /// </para>
        /// <para>Set <see cref="ResolveRelativeFromParentScriptLocation"/> to <c>true</c> if you want to convert all relative paths
        /// into the absolute path, not only the ones with a single dot ('.') prefix.</para>
        /// </summary>
        [Browsable(false)]
        public bool ResolveRelativeFromParentScriptLocation
        {
            get { return CSharpParser.ImportInfo.ResolveRelativeFromParentScriptLocation; }
            set { CSharpParser.ImportInfo.ResolveRelativeFromParentScriptLocation = value; }
        }

        /// <summary>
        /// Returns value of the UseAlternativeCompiler (with expanding environment variables).
        /// </summary>
        /// <returns>Path string</returns>
        public string ExpandUseAlternativeCompiler() { return (useAlternativeCompiler == null) ? "" : Environment.ExpandEnvironmentVariables(useAlternativeCompiler); }

        string useAlternativeCompiler = "";

        /// <summary>
        /// Location of PostProcessor assembly. If set it forces script engine to pass compiled script through PostProcessor before the execution.
        /// </summary>
        [Category("Extensibility"), Description("Location of PostProcessor assembly. If set it forces script engine to pass compiled script through PostProcessor before the execution.")]
#if !InterfaceAssembly
        [Editor(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(UITypeEditor))]
#endif
        public string UsePostProcessor
        {
            get { return usePostProcessor; }
            set { usePostProcessor = value; }
        }

        /// <summary>
        /// Returns value of the UsePostProcessor (with expanding environment variables).
        /// </summary>
        /// <returns>Path string</returns>
        public string ExpandUsePostProcessor() { return Environment.ExpandEnvironmentVariables(usePostProcessor); }

        string usePostProcessor = "";

        /// <summary>
        /// DefaultApartmentState is an ApartmemntState, which will be used
        /// at run-time if none specified in the code with COM threading model attributes.
        /// </summary>
        [Category("RuntimeSettings"), Description("DefaultApartmentState is an ApartmemntState, which will be used at run-time if none specified in the code with COM threading model attributes.")]
        public ApartmentState DefaultApartmentState
        {
            get { return defaultApartmentState; }
            set { defaultApartmentState = value; }
        }

        ApartmentState defaultApartmentState = ApartmentState.STA;

        /// <summary>
        /// Default command-line arguments. For example if "/dbg" is specified all scripts will be compiled in debug mode
        /// regardless if the user specified "/dbg" when a particular script is launched.
        /// </summary>
        [Category("RuntimeSettings"), Description("Default command-line arguments (e.g.-dbg) for all scripts.")]
        public string DefaultArguments
        {
            get { return defaultArguments; }
            set { defaultArguments = value; }
        }

        bool injectScriptAssemblyAttribute = true;

        /// <summary>
        /// Gets or sets a value indicating whether script assembly attribute should be injected. The AssemblyDecription attribute
        /// contains the original location of the script file the assembly being compiled from./
        /// </summary>
        /// <value>
        /// <c>true</c> if the attribute should be injected; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool InjectScriptAssemblyAttribute
        {
            get { return injectScriptAssemblyAttribute; }
            set { injectScriptAssemblyAttribute = value; }
        }

        bool resolveAutogenFilesRefs = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable Python-like print methods (e.g. dbg.print(DateTime.Now)).
        /// </summary>
        /// <value>
        ///   <c>true</c> if print methods are enabled; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool EnableDbgPrint
        {
            get { return enableDbgPrint; }
            set { enableDbgPrint = value; }
        }

        bool enableDbgPrint = true;

        /// <summary>
        /// Gets or sets a value indicating whether references to the auto-generated files should be resolved.
        /// <para>
        /// If this flag is set the all references in the compile errors text to the path of the derived auto-generated files
        /// (e.g. errors in the decorated classless scripts) will be replaced with the path of the original file(s) (e.g. classless script itself).
        /// </para>
        /// </summary>
        /// <value>
        /// <c>true</c> if preferences needs to be resolved; otherwise, <c>false</c>.
        /// </value>
        public bool ResolveAutogenFilesRefs
        {
            get { return resolveAutogenFilesRefs; }
            set { resolveAutogenFilesRefs = value; }
        }

        string defaultArguments = CSSUtils.Args.Join(
            "c",
            "co:" + CSSUtils.Args.DefaultPrefix + "warn:0",
            "co:" + CSSUtils.Args.DefaultPrefix + "d:TRACE");

        ///// <summary>
        ///// Enables using a surrogate process to host the script engine at runtime. This may be a useful option for fine control over the hosting process
        ///// (e.g. ensuring "CPU type" of the process, CLR version to be loaded).
        ///// </summary>
        //[Category("RuntimeSettings")]
        //internal bool UseSurrogateHostingProcess  //do not expose it to the user just yet
        //{
        //    get { return useSurrogatepHostingProcess; }
        //    set { useSurrogatepHostingProcess = value; }
        //}

        bool useSurrogatepHostingProcess = false;

        bool openEndDirectiveSyntax = true;

        /// <summary>
        /// Enables omitting closing character (";") for CS-Script directives (e.g. "//css_ref System.Xml.dll" instead of "//css_ref System.Xml.dll;").
        /// </summary>
        [Browsable(false)]
        public bool OpenEndDirectiveSyntax
        {
            get { return openEndDirectiveSyntax; }
            set { openEndDirectiveSyntax = value; }
        }

        string consoleEncoding = DefaultEncodingName;

        /// <summary>
        /// Encoding of he Console Output. Applicable for console applications script engine only.
        /// </summary>
        [Category("RuntimeSettings"), Description("Console output encoding. Use 'default' value if you want to use system default encoding. " +
                                                  "Otherwise specify the name of the encoding (e.g. utf-8).")]
        [TypeConverter(typeof(EncodingConverter))]
        public string ConsoleEncoding
        {
            get { return consoleEncoding; }

            set
            {
                //consider: https://social.msdn.microsoft.com/Forums/vstudio/en-US/e448b241-e250-4dcb-8ecd-361e00920dde/consoleoutputencoding-breaks-batch-files?forum=netfxbcl
                if (consoleEncoding != value)
                {
                    consoleEncoding = Utils.ProcessNewEncoding(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to enforce timestamping the compiled script assembly.
        /// <para>
        /// Some reports indicated that changing assembly timestamp can affect CLR caching (shadow copying)
        /// due to some unknown/undocumented CLR feature(s).
        /// </para>
        /// <para>
        /// Thus script caching algorithm has been changed to avoid using the compiled assembly
        /// LastModifiedTime file-attribute for storing the script timestamp. Currently caching algorithm
        /// uses injected metadata instead.
        /// </para>
        /// <para>
        /// If for whatever reason the old behaver is preferred you can always enable it by either setting
        /// <see cref="Settings.LegacyTimestampCaching"/> to <c>true</c> or by setting CSS_LEGACY_TIMESTAMP_CAHING
        /// environment variable to <c>"true"</c>.
        /// </para>
        /// <para>
        /// Triggered by issue #61: https://github.com/oleg-shilo/cs-script/issues/61
        /// </para>
        /// </summary>
        /// <value>
        ///   <c>true</c> if to suppress timestamping; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool LegacyTimestampCaching
        {
            get { return legacyTimestampCaching; }
            set { legacyTimestampCaching = value; }
        }

        /// <summary>
        /// Gets or sets the custom CS-Script temporary directory.
        /// </summary>
        /// <value>
        /// The custom temporary directory.
        /// </value>
        [Browsable(false)]
        public string CustomTempDirectory
        {
            get { return Environment.GetEnvironmentVariable("CSS_CUSTOM_TEMPDIR"); }
            set { Environment.SetEnvironmentVariable("CSS_CUSTOM_TEMPDIR", value); }
        }

        static internal bool legacyTimestampCaching
        {
            get { return Environment.GetEnvironmentVariable("CSS_LEGACY_TIMESTAMP_CACHING") == true.ToString(); }
            set { Environment.SetEnvironmentVariable("CSS_LEGACY_TIMESTAMP_CACHING", value.ToString()); }
        }

        internal const string DefaultEncodingName = "default";

        /// <summary>
        /// Specifies the .NET Framework version that the script is compiled against. This option can have the following values:
        ///   v2.0
        ///   v3.0
        ///   v3.5
        ///   v4.0
        /// </summary>
        [Browsable(false)]
        public string TargetFramework
        {
            get { return targetFramework; }
            set { targetFramework = value; }
        }

#if net35
        string targetFramework = "v3.5";
#else
        string targetFramework = "v4.0";
#endif

        /// <summary>
        /// Specifies the .NET Framework version that the script is compiled against. This option can have the following values:
        ///   v2.0
        ///   v3.0
        ///   v3.5
        ///   v4.0
        /// </summary>
        [Category("RuntimeSettings")]
        [Description("Specifies the .NET Framework version that the script is compiled against (used by CSharpCodeProvider.CreateCompiler as the 'CompilerVersion' parameter).\nThis option is for the script compilation only.\nFor changing the script execution CLR use //css_host directive from the script.\nYou are discouraged from modifying this value thus if the change is required you need to edit css_config.xml file directly.")]
        public string CompilerFramework
        {
            get { return targetFramework; }
        }

        /// <summary>
        /// List of assembly names to be automatically referenced by the script. The items must be separated by coma or semicolon. Specifying .dll extension (e.g. System.Core.dll) is optional.
        /// Assembly can contain expandable environment variables.
        /// </summary>
        [Category("Extensibility"), Description("List of assembly names to be automatically referenced by the scripts (e.g. System.dll, System.Core.dll). Assembly extension is optional.")]
        public string DefaultRefAssemblies
        {
            get
            {
                if (defaultRefAssemblies == null)
                    defaultRefAssemblies = InitDefaultRefAssemblies();

                return defaultRefAssemblies;
            }

            set { defaultRefAssemblies = value; }
        }

        string defaultRefAssemblies;

        string InitDefaultRefAssemblies()
        {
#if net4
            if (Runtime.IsMono)
            {
                return "System; System.Core;";
            }
            else
            {
                if (Runtime.IsNet45Plus())
                    return "System; System.Core; System.Linq;";
                else
                    return "System; System.Core;";
            }
#else
            return "";
#endif
        }

        /// <summary>
        /// Returns value of the DefaultRefAssemblies (with expanding environment variables).
        /// </summary>
        /// <returns>List of assembly names</returns>
        public string ExpandDefaultRefAssemblies() { return Environment.ExpandEnvironmentVariables(DefaultRefAssemblies); }

        /// <summary>
        /// List of directories to be used to search (probing) for referenced assemblies and script files.
        /// This setting is similar to the system environment variable PATH.
        /// </summary>
        [Category("Extensibility"), Description("List of directories to be used to search (probing) for referenced assemblies and script files.\nThis setting is similar to the system environment variable PATH.")]
        public string SearchDirs
        {
            get { return searchDirs; }
            set { searchDirs = value; }
        }

        string searchDirs = "%CSSCRIPT_DIR%" + Path.DirectorySeparatorChar + "lib;%CSSCRIPT_INC%;";

        /// <summary>
        /// Add search directory to the search (probing) path Settings.SearchDirs.
        /// For example if Settings.SearchDirs = "c:\scripts" then after call Settings.AddSearchDir("c:\temp") Settings.SearchDirs is "c:\scripts;c:\temp"
        /// </summary>
        /// <param name="dir">Directory path.</param>
        public void AddSearchDir(string dir)
        {
            if (dir != "")
            {
                foreach (string searchDir in searchDirs.Split(';'))
                    if (searchDir != "" && Utils.IsSamePath(Path.GetFullPath(searchDir), Path.GetFullPath(dir)))
                        return; //already there

                searchDirs += ";" + dir;
            }
        }

        /// <summary>
        /// The value, which indicates if auto-generated files (if any) should be hidden in the temporary directory.
        /// </summary>
        [Category("RuntimeSettings"), Description("The value, which indicates if auto-generated files (if any) should be hidden in the temporary directory.")]
        public HideOptions HideAutoGeneratedFiles
        {
            get { return hideOptions; }
            set { hideOptions = value; }
        }

        string precompiler = "";

        /// <summary>
        /// Path to the precompiler script/assembly (see documentation for details). You can specify multiple recompiles separating them by semicolon.
        /// </summary>
        [Category("RuntimeSettings"), Description("Path to the precompiler script/assembly (see documentation for details). You can specify multiple recompiles separating them by semicolon.")]
        public string Precompiler
        {
            get { return precompiler; }
            set { precompiler = value; }
        }

        bool customHashing = true;

        /// <summary>
        /// Gets or sets a value indicating whether custom string hashing algorithm should be used.
        /// <para>
        /// String hashing is used by the script engine for allocating temporary and cached paths.
        /// However default string hashing is platform dependent (x32 vs. x64) what makes impossible
        /// truly deterministic string hashing. This in turns complicates the integration of the
        /// CS-Script infrastructure with the third-party applications (e.g. Notepad++ CS-Script plugin).
        /// </para>
        /// <para>
        /// To overcome this problem CS-Script uses custom string hashing algorithm (default setting).
        /// Though the native .NET hashing can be enabled if desired by setting <c>CustomHashing</c>
        /// to <c>false</c>.</para>
        /// </summary>
        /// <value>
        ///   <c>true</c> if custom hashing is in use; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool CustomHashing
        {
            get { return customHashing; }
            set { customHashing = value; }
        }

        HideOptions hideOptions = HideOptions.HideAll;
        ///// <summary>
        ///// The value, which indicates which version of CLR compiler should be used to compile script.
        ///// For example CLR 2.0 can use the following compiler versions:
        ///// default - .NET 2.0
        ///// 3.5 - .NET 3.5
        ///// Use empty string for default compiler.
        ///// </summary>string compilerVersion = "";
        //[Category("RuntimeSettings")]
        //public string CompilerVersion
        //{
        //    get { return compilerVersion; }
        //    set { compilerVersion = value; }
        //}
        //string compilerVersion = "";

        /// <summary>
        /// Enum for possible hide auto-generated files scenarios
        /// Note: when HideAll is used it is responsibility of the pre/post script to implement actual hiding.
        /// </summary>
        public enum HideOptions
        {
            /// <summary>
            /// Do not hide auto-generated files.
            /// </summary>
            DoNotHide,

            /// <summary>
            /// Hide the most of the auto-generated (cache and "imported") files.
            /// </summary>
            HideMostFiles,

            /// <summary>
            /// Hide all auto-generated files including the files generated by pre/post scripts.
            /// </summary>
            HideAll
        }

        /// <summary>
        /// Boolean flag that indicates how much error details to be reported should error occur.
        /// false - Top level exception will be reported
        /// true - Whole exception stack will be reported
        /// </summary>
        [Category("RuntimeSettings"), Description("Indicates how much error details to be reported should error occur.")]
        public bool ReportDetailedErrorInfo
        {
            get { return reportDetailedErrorInfo; }
            set { reportDetailedErrorInfo = value; }
        }

        bool reportDetailedErrorInfo = true;

        /// <summary>
        /// Gets or sets a value indicating whether Optimistic Concurrency model should be used when executing scripts from the host application.
        /// If set to <c>false</c> the script loading (not the execution) is globally thread-safe. If set to <c>true</c> the script loading is
        /// thread-safe only among loading operations for the same script file.
        /// <para>The default value is <c>true</c>.</para>
        /// </summary>
        /// <value>
        /// 	<c>true</c> if Optimistic Concurrency model otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        internal bool OptimisticConcurrencyModel
        {
            get { return optimisticConcurrencyModel; }
            set { optimisticConcurrencyModel = value; }
        }

        bool optimisticConcurrencyModel = true;

        /// <summary>
        /// Gets or sets a value indicating whether auto-class decoration should allow C# 6 specific syntax.
        /// If it does the statement "using static dbg;" will be injected at the start of the auto-class definition thus the
        /// entry script may invoke static methods for object inspection with <c>dbg</c> class without specifying the
        /// class name (e.g. "print(DateTime.Now);").
        /// </summary>
        /// <value>
        /// <c>true</c> if decorate auto-class as C# 6; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool AutoClass_DecorateAsCS6
        {
            get { return autoClass_DecorateAsCS6; }
            set { autoClass_DecorateAsCS6 = value; }
        }

        bool autoClass_DecorateAsCS6 = true;

        //Not used yet.
        [Browsable(false)]
        internal bool AutoClass_DecorateAlways
        {
            get { return autoClass_DecorateAlways; }
            set { autoClass_DecorateAlways = value; }
        }

        bool autoClass_DecorateAlways = false;

        /// <summary>
        /// Boolean flag that indicates if compiler warnings should be included in script compilation output.
        /// false - warnings will be displayed
        /// true - warnings will not be displayed
        /// </summary>
        [Category("RuntimeSettings"), Description("Indicates if compiler warnings should be included in script compilation output.")]
        public bool HideCompilerWarnings
        {
            get { return hideCompilerWarnings; }
            set { hideCompilerWarnings = value; }
        }

        bool hideCompilerWarnings = false;

        /// <summary>
        /// Boolean flag that indicates the script assembly is to be loaded by CLR as an in-memory byte stream instead of the file.
        /// This setting can be useful when you need to prevent script assembly (compiled script) from locking by CLR during the execution.
        /// false - script assembly will be loaded as a file. It is an equivalent of Assembly.LoadFrom(string assemblyFile).
        /// true - script assembly will be loaded as a file. It is an equivalent of Assembly.Load(byte[] rawAssembly)
        /// <para>Note: some undesired side effects can be triggered by having assemblies with <c>Assembly.Location</c> being empty.
        /// For example <c>Interface Alignment</c> any not work with such assemblies as it relies on CLR compiling services that
        /// typically require assembly <c>Location</c> member being populated with the valid path.</para>
        /// </summary>
        [Category("RuntimeSettings"),
        Description("Indicates the script assembly is to be loaded by CLR as an in-memory byte stream instead of the file. " +
            "Note this settings can affect the use cases requiring the loaded assemblies to have non empty Assembly.Location.")]
        public bool InMemoryAssembly
        {
            get { return inMemoryAsm; }
            set { inMemoryAsm = value; }
        }

        bool inMemoryAsm = true;

        /// <summary>
        /// Gets or sets the concurrency control model.
        /// </summary>
        /// <value>The concurrency control.</value>
        [Browsable(false)]
        public ConcurrencyControl ConcurrencyControl
        {
            get
            {
                if (concurrencyControl == ConcurrencyControl.HighResolution && Runtime.IsMono)
                    concurrencyControl = ConcurrencyControl.Standard;
                return concurrencyControl;
            }

            set
            {
                concurrencyControl = value;
            }
        }

        ConcurrencyControl concurrencyControl = ConcurrencyControl.Standard;

        /// <summary>
        /// Serializes instance of Settings.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var props = this.GetType()
                            .GetProperties()
                            .Where(p => p.CanRead && p.CanWrite)
                            .Where(p => p.Name != "SuppressTimestamping")
                            .Select(p => " " + p.Name + ": " + (p.PropertyType == typeof(string) ? "\"" + p.GetValue(this, dummy) + "\"" : p.GetValue(this, dummy)))
                            .ToArray();

            return string.Join(Environment.NewLine, props);
        }

        internal string ToStringRaw()
        {
            string file = Path.GetTempFileName();
            try
            {
                Save(file);
                return File.ReadAllText(file);
            }
            finally
            {
                File.Delete(file);
            }
        }

        internal void Set(string name, string value)
        {
            string lookupName = name.Replace("_", ""); // user is allowed to use '_' for very long named properties

            PropertyInfo prop = typeof(Settings).GetProperties()
                                                .Where(x => 0 == string.Compare(x.Name.Replace("_", ""), lookupName, StringComparison.OrdinalIgnoreCase))
                                                .FirstOrDefault();

            if (prop == null)
            {
                throw new CLIException("Invalid config property name '" + name + "'.");
            }
            else
            {
                value = (value ?? "").Trim('"').Trim('\'');
                var add = value.StartsWith("add:");
                var del = value.StartsWith("del:");

                if (prop.PropertyType == typeof(string))
                {
                    var old_value = (string)prop.GetValue(this, dummy);

                    if (value.StartsWith("del:"))
                    {
                        var val = value.Substring(4);

                        if (old_value.StartsWith(val + " "))
                            old_value = old_value.Substring(val.Length);
                        if (old_value.EndsWith(" " + val))
                            old_value = old_value.Substring(0, old_value.Length - val.Length);

                        value = old_value.Replace(" " + value.Substring(4) + " ", "").Trim();
                    }

                    if (value.StartsWith("add:"))
                        value = old_value + " " + value.Substring(4);

                    prop.SetValue(this, value, dummy);
                }
                else if (prop.PropertyType.IsEnum)
                {
                    object value_obj = Enum.Parse(prop.PropertyType, value, true);
                    prop.SetValue(this, value_obj, dummy);
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(this, bool.Parse(value), dummy);
                }
            }
        }

        internal static object[] dummy = new object[0];

        internal string Get(ref string name)
        {
            string lookupName = name.Replace("_", ""); // user is allowed to use '_' for very long named properties
            PropertyInfo prop = typeof(Settings).GetProperties()
                                                .Where(x => 0 == string.Compare(x.Name.Replace("_", ""), lookupName, StringComparison.OrdinalIgnoreCase))
                                                .FirstOrDefault();

            if (prop == null)
            {
                throw new CLIException("Invalid config property name '" + name + "'.");
            }
            else
            {
                name = prop.Name;
                if (prop.PropertyType == typeof(string))
                    return "\"" + prop.GetValue(this, dummy) + "\"";
                else
                    return "" + prop.GetValue(this, dummy);
            }
        }

        /// <summary>
        /// Saves CS-Script application settings to a file.
        /// </summary>
        /// <param name="fileName">File name of the settings file</param>
        public void Save(string fileName)
        {
            Save(fileName, false);
        }

        internal void Save(string fileName, bool throwOnError)
        {
            //It is very tempting to use XmlSerializer but it adds 200 ms to the
            //application startup time. Whereas current startup delay for cscs.exe is just a 100 ms.
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<CSSConfig/>");

                //write the all most important elements and less important ones only if they have non-default values.
                doc.DocumentElement.AppendChild(doc.CreateElement("defaultArguments")).AppendChild(doc.CreateTextNode(DefaultArguments));
                doc.DocumentElement.AppendChild(doc.CreateElement("defaultRefAssemblies")).AppendChild(doc.CreateTextNode(DefaultRefAssemblies));
                doc.DocumentElement.AppendChild(doc.CreateElement("searchDirs")).AppendChild(doc.CreateTextNode(SearchDirs));
                doc.DocumentElement.AppendChild(doc.CreateElement("useAlternativeCompiler")).AppendChild(doc.CreateTextNode(UseAlternativeCompiler));
                doc.DocumentElement.AppendChild(doc.CreateElement("roslynDir")).AppendChild(doc.CreateTextNode(RoslynDir));
                doc.DocumentElement.AppendChild(doc.CreateElement("consoleEncoding")).AppendChild(doc.CreateTextNode(ConsoleEncoding));
                doc.DocumentElement.AppendChild(doc.CreateElement("autoclass.decorateAsCS6")).AppendChild(doc.CreateTextNode(autoClass_DecorateAsCS6.ToString()));
                doc.DocumentElement.AppendChild(doc.CreateElement("inMemoryAsm")).AppendChild(doc.CreateTextNode(InMemoryAssembly.ToString()));
                doc.DocumentElement.AppendChild(doc.CreateElement("hideCompilerWarnings")).AppendChild(doc.CreateTextNode(HideCompilerWarnings.ToString()));
                doc.DocumentElement.AppendChild(doc.CreateElement("reportDetailedErrorInfo")).AppendChild(doc.CreateTextNode(ReportDetailedErrorInfo.ToString()));
                doc.DocumentElement.AppendChild(doc.CreateElement("enableDbgPrint")).AppendChild(doc.CreateTextNode(enableDbgPrint.ToString()));

                if (ResolveRelativeFromParentScriptLocation != false)
                    doc.DocumentElement.AppendChild(doc.CreateElement("resolveRelativeFromParentScriptLocation")).AppendChild(doc.CreateTextNode(ResolveRelativeFromParentScriptLocation.ToString()));

                if (hideOptions != HideOptions.HideAll)
                    doc.DocumentElement.AppendChild(doc.CreateElement("hideOptions")).AppendChild(doc.CreateTextNode(hideOptions.ToString()));

                if (autoClass_DecorateAlways)
                    doc.DocumentElement.AppendChild(doc.CreateElement("autoclass.decorateAlways")).AppendChild(doc.CreateTextNode(autoClass_DecorateAlways.ToString()));

                if (DefaultApartmentState != ApartmentState.STA)
                    doc.DocumentElement.AppendChild(doc.CreateElement("defaultApartmentState")).AppendChild(doc.CreateTextNode(DefaultApartmentState.ToString()));

                if (!string.IsNullOrEmpty(CustomTempDirectory))
                    doc.DocumentElement.AppendChild(doc.CreateElement("customTempDirectory")).AppendChild(doc.CreateTextNode(CustomTempDirectory));

                if (!string.IsNullOrEmpty(precompiler))
                    doc.DocumentElement.AppendChild(doc.CreateElement("precompiler")).AppendChild(doc.CreateTextNode(Precompiler));

                if (!string.IsNullOrEmpty(UsePostProcessor))
                    doc.DocumentElement.AppendChild(doc.CreateElement("usePostProcessor")).AppendChild(doc.CreateTextNode(UsePostProcessor));

                if (!string.IsNullOrEmpty(CleanupShellCommand))
                {
                    doc.DocumentElement.AppendChild(doc.CreateElement("cleanupShellCommand")).AppendChild(doc.CreateTextNode(CleanupShellCommand));
                    doc.DocumentElement.AppendChild(doc.CreateElement("doCleanupAfterNumberOfRuns")).AppendChild(doc.CreateTextNode(DoCleanupAfterNumberOfRuns.ToString()));
                }

                if (ConcurrencyControl != ConcurrencyControl.Standard)
                    doc.DocumentElement.AppendChild(doc.CreateElement("concurrencyControl")).AppendChild(doc.CreateTextNode(ConcurrencyControl.ToString()));
#if net35
                if (TargetFramework != "v3.5")
#else
                if (TargetFramework != "v4.0")
#endif
                    doc.DocumentElement.AppendChild(doc.CreateElement("targetFramework")).AppendChild(doc.CreateTextNode(TargetFramework));

                if (useSurrogatepHostingProcess)
                    doc.DocumentElement.AppendChild(doc.CreateElement("useSurrogatepHostingProcess")).AppendChild(doc.CreateTextNode(useSurrogatepHostingProcess.ToString()));

                if (!openEndDirectiveSyntax)
                    doc.DocumentElement.AppendChild(doc.CreateElement("openEndDirectiveSyntax")).AppendChild(doc.CreateTextNode(openEndDirectiveSyntax.ToString()));

                if (!CustomHashing)
                    doc.DocumentElement.AppendChild(doc.CreateElement("customHashing")).AppendChild(doc.CreateTextNode(CustomHashing.ToString()));

                //note node.ParentNode.InsertAfter(doc.CreateComment("") injects int node inner text and it is not what we want
                //very simplistic formatting
                var xml = doc.InnerXml.Replace("><", ">" + Environment.NewLine + "  <")
                                      .Replace(">\n  </", "></")
                                      .Replace("></CSSConfig>", ">" + Environment.NewLine + "</CSSConfig>");

                xml = CommentElement(xml, "consoleEncoding", "if 'default' then system default is used; otherwise specify the name of the encoding (e.g. 'utf-8')");
                xml = CommentElement(xml, "autoclass.decorateAsCS6", "if 'true' auto-class decoration will inject C# 6 specific syntax expressions (e.g. 'using static dbg;')");
                xml = CommentElement(xml, "autoclass.decorateAlways", "if 'true' decorate classless scripts unconditionally; otherwise only if a top level class-less 'main' detected. Not used yet.");
                xml = CommentElement(xml, "useAlternativeCompiler", "Custom script compiler. For example C# 7 (Roslyn): '%CSSCRIPT_DIR%!lib!CSSRoslynProvider.dll'".Replace('!', Path.DirectorySeparatorChar));
                xml = CommentElement(xml, "roslynDir", "Location of Roslyn compilers to be used by custom script compilers. For example C# 7 (Roslyn): /usr/lib/mono/4.5");
                xml = CommentElement(xml, "enableDbgPrint", "Gets or sets a value indicating whether to enable Python-like print methods (e.g. dbg.print(DateTime.Now))");

                File.WriteAllText(fileName, xml);
            }
            catch
            {
                if (throwOnError)
                    throw;
            }
        }

        static string CommentElement(string xml, string name, string comment)
        {
            return xml.Replace("<" + name + ">", "<!-- " + comment + " -->" + Environment.NewLine + "  <" + name + ">");
        }

        /// <summary>
        /// Loads CS-Script application settings from a file. Default settings object is returned if it cannot be loaded from the file.
        /// </summary>
        /// <param name="fileName">File name of the XML file</param>
        /// <returns>Setting object deserialized from the XML file</returns>
        public static Settings Load(string fileName)
        {
            return Load(fileName, true);
        }

        internal static Settings LoadDefault()
        {
            return Load(DefaultConfigFile, true);
        }

        /// <summary>
        /// Gets the default configuration file path. It is a "css_config.xml" file located in the same directory where the assembly
        /// being executed is (e.g. cscs.exe).
        /// <para>Note, when running under Mono the "css_config.mono.xml" file will have higher precedence than "css_config.xml".
        /// This way you can have Mono specific settings without affecting the settings for non-Mono runtimes.</para>
        /// </summary>
        /// <value>
        /// The default configuration file location. Returns null if the file is not found.
        /// </value>
        public static string DefaultConfigFile
        {
            get
            {
                try
                {
                    string asm_path = Assembly.GetExecutingAssembly().Location;
                    if (!string.IsNullOrEmpty(asm_path))
                    {
                        if (Runtime.IsMono)
                        {
                            var monoFileName = Path.Combine(Path.GetDirectoryName(asm_path), "css_config.mono.xml");
                            if (File.Exists(monoFileName))
                                return monoFileName;
                        }

                        return Path.Combine(Path.GetDirectoryName(asm_path), "css_config.xml");
                    }
                }
                catch { }
                return null;
            }
        }

        /// <summary>
        /// Loads CS-Script application settings from the default config file (css_config.xml in the cscs.exe/csws.exe folder).
        /// </summary>
        /// <param name="createAlways">Create and return default settings object if it cannot be loaded from the file.</param>
        /// <returns>Setting object deserialized from the XML file</returns>
        public static Settings Load(bool createAlways)
        {
            return Load(DefaultConfigFile, true);
        }

        internal void Save()
        {
            Save(DefaultConfigFile, true);
        }

        /// <summary>
        /// Loads CS-Script application settings from a file.
        /// </summary>
        /// <param name="fileName">File name of the XML file</param>
        /// <param name="createAlways">Create and return default settings object if it cannot be loaded from the file.</param>
        /// <returns>Setting object deserialized from the XML file</returns>
        public static Settings Load(string fileName, bool createAlways)
        {
            //System.Diagnostics.Debug.Assert(false);
            Settings settings = new Settings();

            var filePath = fileName;

            if (filePath != null)
            {
                var config_root = Path.GetDirectoryName(fileName);

                filePath = Path.GetFullPath(filePath);

                if (!File.Exists(filePath) && config_root == "")
                {
                    try
                    {
                        var candidate = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location()), fileName);
                        if (File.Exists(candidate))
                            filePath = candidate;
                    }
                    catch { }
                }
            }

            if (filePath != null && File.Exists(filePath))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filePath);
                    XmlNode data = doc.SelectSingleNode("CSSConfig");
                    XmlNode node;
                    node = data.SelectSingleNode("defaultArguments"); if (node != null) settings.defaultArguments = node.InnerText;
                    node = data.SelectSingleNode("defaultApartmentState"); if (node != null) settings.defaultApartmentState = (ApartmentState)Enum.Parse(typeof(ApartmentState), node.InnerText, false);
                    node = data.SelectSingleNode("reportDetailedErrorInfo"); if (node != null) settings.reportDetailedErrorInfo = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("useAlternativeCompiler"); if (node != null) settings.UseAlternativeCompiler = node.InnerText;
                    node = data.SelectSingleNode("roslynDir"); if (node != null) settings.RoslynDir = Environment.ExpandEnvironmentVariables(node.InnerText);
                    node = data.SelectSingleNode("usePostProcessor"); if (node != null) settings.UsePostProcessor = node.InnerText;
                    node = data.SelectSingleNode("searchDirs"); if (node != null) settings.SearchDirs = node.InnerText;
                    node = data.SelectSingleNode("cleanupShellCommand"); if (node != null) settings.cleanupShellCommand = node.InnerText;
                    node = data.SelectSingleNode("doCleanupAfterNumberOfRuns"); if (node != null) settings.doCleanupAfterNumberOfRuns = uint.Parse(node.InnerText);
                    node = data.SelectSingleNode("hideOptions"); if (node != null) settings.hideOptions = (HideOptions)Enum.Parse(typeof(HideOptions), node.InnerText, true);
                    node = data.SelectSingleNode("autoclass.decorateAsCS6"); if (node != null) settings.autoClass_DecorateAsCS6 = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("enableDbgPrint"); if (node != null) settings.enableDbgPrint = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("autoclass.decorateAlways"); if (node != null) settings.autoClass_DecorateAlways = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("hideCompilerWarnings"); if (node != null) settings.hideCompilerWarnings = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("inMemoryAsm"); if (node != null) settings.inMemoryAsm = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("resolveRelativeFromParentScriptLocation"); if (node != null) settings.ResolveRelativeFromParentScriptLocation = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("concurrencyControl"); if (node != null) settings.concurrencyControl = (ConcurrencyControl)Enum.Parse(typeof(ConcurrencyControl), node.InnerText, false);
                    node = data.SelectSingleNode("TargetFramework"); if (node != null) settings.TargetFramework = node.InnerText;
                    node = data.SelectSingleNode("defaultRefAssemblies"); if (node != null) settings.defaultRefAssemblies = node.InnerText;
                    node = data.SelectSingleNode("useSurrogatepHostingProcess"); if (node != null) settings.useSurrogatepHostingProcess = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("openEndDirectiveSyntax"); if (node != null) settings.OpenEndDirectiveSyntax = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("precompiler"); if (node != null) settings.Precompiler = node.InnerText;
                    node = data.SelectSingleNode("customHashing"); if (node != null) settings.CustomHashing = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("consoleEncoding"); if (node != null) settings.ConsoleEncoding = node.InnerText;
                    node = data.SelectSingleNode("customTempDirectory"); if (node != null) settings.CustomTempDirectory = node.InnerText;

                    //Read old Camel-case naming as well to accommodate older versions
                    node = data.SelectSingleNode("Precompiler"); if (node != null) settings.Precompiler = node.InnerText;
                    node = data.SelectSingleNode("CustomHashing"); if (node != null) settings.CustomHashing = node.InnerText.ToLower() == "true";
                    node = data.SelectSingleNode("ConsoleEncoding"); if (node != null) settings.ConsoleEncoding = node.InnerText;
                    node = data.SelectSingleNode("ConcurrencyControl"); if (node != null) settings.concurrencyControl = (ConcurrencyControl)Enum.Parse(typeof(ConcurrencyControl), node.InnerText, false);
                }
                catch
                {
                    if (!createAlways)
                        settings = null;
                    else
                        settings.Save(filePath);
                }
                if (settings != null)
                    CSharpParser.OpenEndDirectiveSyntax = settings.OpenEndDirectiveSyntax;
            }
            return settings;
        }
    }

    internal class EncodingConverter : TypeConverter
    {
        public EncodingConverter()
        {
            encodings.Add("default");
            foreach (EncodingInfo item in Encoding.GetEncodings())
                encodings.Add(item.Name);
        }

        List<string> encodings = new List<string>();

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(encodings);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return true;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            return value;
        }
    }
}