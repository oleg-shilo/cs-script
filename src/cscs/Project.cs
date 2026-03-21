using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CSScripting;
using CSScriptLib;

#if !class_lib

namespace csscript
#else

namespace CSScriptLib
#endif
{
    /// <summary>
    /// Class that holds all information about the execution context (probing directories and
    /// dependencies) of a script.
    /// </summary>
    [Serializable]
    public class Project
    {
        /// <summary>
        /// Primary script that defines the project.
        /// </summary>
        public string Script;

        /// <summary>
        /// List of all C# sources defined by the project. This includes the primary script itself
        /// and all other scripts files the imported/included by the primary script.
        /// </summary>
        public string[] Files;

        /// <summary>
        /// List of NuGet packages the script is referencing. Both directly and via child scripts.
        /// </summary>
        public string[] Packages;

        /// <summary>
        /// List of assemblies the script of the project is referencing.
        /// </summary>
        public string[] Refs;

        /// <summary>
        /// List of search folders where CS-Script does probing for imported/included scripts and assemblies.
        /// </summary>
        public string[] SearchDirs;

        /// <summary>
        /// Generates the top level view project for a given script.
        /// <para>
        /// Note this method uses the same algorithm as CS-Script executor but it deliberately
        /// doesn't include cached directories and auto-generated files. This method is to be used
        /// by IDEs and tools.
        /// </para>
        /// </summary>
        /// <param name="script">The script.</param>
        /// <returns>The project instance</returns>
        static public Project GenerateProjectFor(string script)
        {
            return ProjectBuilder.GenerateProjectFor(script);
        }

        static internal Project GenerateProjectInCliOutput(string script)
        {
            return ProjectBuilder.GenerateProjectFor(script, false);
        }
    }

#pragma warning disable 649

    /// <summary>
    /// Provides functionality for building and managing CS-Script projects by analyzing scripts and their dependencies,
    /// resolving references, and managing configuration settings.
    /// </summary>
    public class ProjectBuilder
    {
        /// <summary>
        /// Initializes the <see cref="ProjectBuilder"/> class by configuring the environment for NuGet package management.
        /// </summary>
        static ProjectBuilder()
        {
            if (CSharpParser.NeedInitEnvironment)
                Environment.SetEnvironmentVariable("css_nuget", null);
        }

        /// <summary>
        /// Gets or sets the default search directories used for resolving script dependencies and assemblies.
        /// The value is a comma or semicolon separated string of directory paths.
        /// </summary>
        /// <value>
        /// A string containing comma or semicolon separated directory paths that will be used as default search locations.
        /// </value>
        static public string DefaultSearchDirs;

        /// <summary>
        /// Gets or sets the default reference assembly paths that are automatically included in script compilation.
        /// The value is a comma or semicolon separated string of assembly file paths.
        /// </summary>
        /// <value>
        /// A string containing comma or semicolon separated assembly file paths that will be referenced by default.
        /// </value>
        static public string DefaultRefAsms;

        /// <summary>
        /// Gets or sets the default namespaces that are automatically imported in scripts.
        /// The value is a comma or semicolon separated string of namespace names.
        /// </summary>
        /// <value>
        /// A string containing comma or semicolon separated namespace names that will be imported by default.
        /// </value>
        static public string DefaultNamespaces;

        /// <summary>
        /// Generates a complete project definition for the specified script by analyzing its dependencies,
        /// references, and configuration settings.
        /// </summary>
        /// <param name="script">The path to the primary script file for which to generate the project.</param>
        /// <param name="printSearchDirs">
        /// If set to <c>true</c>, prints the search directories to the console during project generation.
        /// Default is <c>true</c>.
        /// </param>
        /// <returns>
        /// A <see cref="Project"/> instance containing all resolved files, references, packages, and search directories
        /// for the specified script.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method performs comprehensive script analysis including:
        /// - Parsing the primary script and all imported/included scripts
        /// - Resolving NuGet package dependencies
        /// - Determining assembly references
        /// - Establishing search directories for dependency resolution
        /// - Processing precompilation directives
        /// </para>
        /// <para>
        /// The project building algorithm is kept in sync with CSExecutor.Compile to ensure consistency
        /// between project generation and actual script execution.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="script"/> is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified script file does not exist.</exception>
        static public Project GenerateProjectFor(string script, bool printSearchDirs = true)
        {
            // ********************************************************************************************
            // * Extremely important to keep the project building algorithm in sync with CSExecutor.Compile
            // ********************************************************************************************
            var project = new Project { Script = script };

            var searchDirs = new List<string>
            {
                Path.GetDirectoryName(script)
            };

            var globalConfig = GetGlobalConfigItems();
            var defaultSearchDirs = globalConfig.dirs;
            var defaultRefAsms = globalConfig.asms;
            var defaultNamespaces = globalConfig.namespaces;

            searchDirs.AddRange(defaultSearchDirs);

#if !class_lib

            if (printSearchDirs)
            {
                foreach (var item in defaultSearchDirs)
                {
                    Console.WriteLine("searchdir: " + item);
                }
            }
#endif

            ScriptParser parser;
            using (new CurrentDirGuard())
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(script);
                parser = new ScriptParser(script, searchDirs.ToArray(), false);
            }

#if !class_lib

            CSExecutor.options.preCompilers = parser.Precompilers
                                                    .Select(x => FileParser.ResolveFile(x, CSExecutor.options.searchDirs))
                                                    .AddItem(CSExecutor.options.preCompilers)
                                                    .JoinBy(",");
            PrecompilationContext precompiling = CSSUtils.Precompile(script,
                                                                     parser.FilesToCompile.Distinct(),
                                                                         CSExecutor.options);
#endif
            // search dirs could be also defined in the script
            var probingDirs = searchDirs.Concat(parser.SearchDirs)
                                        .Where(x => !string.IsNullOrEmpty(x))
                                        .Distinct()
                                        .ToArray();

            var sources = parser.SaveImportedScripts().ToList(); //this will also generate auto-scripts and save them
            sources.Insert(0, script);

            //if (parser.Packages.Any() && NotifyClient != null)
            //{
            //    NotifyClient("Processing NuGet packages...");
            //}

            project.Files = sources.Distinct().Select<string, string>(PathExtensions.PathNormaliseSeparators).ToArray();

            project.Packages = parser.Packages.ToArray();

            var allRefs = defaultRefAsms;
#if !class_lib
            if (CSExecutor.options.enableDbgPrint)
                allRefs.Add(Assembly.GetExecutingAssembly().Location());
#endif
            project.Refs = parser.AgregateReferences(probingDirs, allRefs, defaultNamespaces)
                                 .Select(PathExtensions.PathNormaliseSeparators)
                                 .ToArray();

#if !class_lib
            project.Refs = project.Refs.ConcatWith(precompiling.NewReferences);
            project.Files = project.Files.ConcatWith(precompiling.NewIncludes);
#endif

            project.SearchDirs = probingDirs.Select<string, string>(PathExtensions.PathNormaliseSeparators).ToArray();

            return project;
        }

        /// <summary>
        /// Represents a collection of configuration items including directories, assemblies, and namespaces
        /// used for CS-Script project configuration.
        /// </summary>
        public class ConfigItems
        {
            /// <summary>
            /// Gets the list of directory paths used for searching scripts and assemblies.
            /// </summary>
            /// <value>
            /// A list of directory paths that will be searched for dependencies.
            /// </value>
            public List<string> dirs = new List<string>();

            /// <summary>
            /// Gets the list of assembly file paths that are referenced by default.
            /// </summary>
            /// <value>
            /// A list of assembly file paths that will be included as references.
            /// </value>
            public List<string> asms = new List<string>();

            /// <summary>
            /// Gets the list of namespace names that are imported by default.
            /// </summary>
            /// <value>
            /// A list of namespace names that will be automatically imported.
            /// </value>
            public List<string> namespaces = new List<string>();
        }

        /// <summary>
        /// Retrieves the global configuration items from CS-Script settings including default search directories,
        /// reference assemblies, and imported namespaces.
        /// </summary>
        /// <returns>
        /// A <see cref="ConfigItems"/> instance containing the consolidated configuration from all sources including
        /// static defaults, environment variables, configuration files, and runtime settings.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method aggregates configuration from multiple sources in the following order:
        /// - Static default values (DefaultSearchDirs, DefaultRefAsms, DefaultNamespaces)
        /// - CS-Script configuration file settings
        /// - Environment variables
        /// - Entry assembly location
        /// - Global CS-Script settings
        /// </para>
        /// <para>
        /// The method handles environment variable expansion and ensures all paths are properly resolved.
        /// Duplicate entries are automatically removed from the final configuration.
        /// </para>
        /// </remarks>
        static public ConfigItems GetGlobalConfigItems()
        {
            var items = new ConfigItems();

            // note: string.Split(params string[] args) will fail in some cases (e.g. hosted by syntaxer .NET8 vs .NET9) so using the old way
            string[] splitPathItems(string text) => text.Split(";,".ToCharArray())
                                                        .Where(x => !string.IsNullOrEmpty(x))
                                                        .Select(x => Environment.ExpandEnvironmentVariables(x.Trim()))
                                                        .ToArray();
            try
            {
                items.dirs.AddRange(splitPathItems(DefaultSearchDirs ?? ""));
                items.asms.AddRange(splitPathItems(DefaultRefAsms ?? ""));
                items.namespaces.AddRange(splitPathItems(DefaultNamespaces ?? ""));

                var configFile = Settings.CurrentConfigFile;
                var settings = Settings.Load(configFile);

#if !class_lib
                var addCssAsm = settings.EnableDbgPrint; // requires referencing CS-Script engine assembly
                items.dirs.AddRange(splitPathItems(settings.SearchDirs));

                if (Assembly.GetExecutingAssembly().Location().HasText())
                    items.dirs.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location()), "lib"));
#else
                items.dirs.AddRange(settings.SearchDirs);
                items.dirs.AddRange(CSScript.GlobalSettings.SearchDirs);
#endif

                // `Assembly.GetExecutingAssembly().Location` may not be resolved properly so adding the entry assembly location as well
                if (Environment.GetEnvironmentVariable("CSS_ENTRY_ASM") != null)
                {
                    var entryAsm = Environment.GetEnvironmentVariable("CSS_ENTRY_ASM");
#if !class_lib
                    items.dirs.Add(Path.Combine(Path.GetDirectoryName(entryAsm), "lib"));
                    if (addCssAsm)
                    {
                        items.asms.Add(entryAsm);
                    }
#endif
                }

                items.dirs.Add(Assembly.GetEntryAssembly().Location?.GetDirName() ?? "");

                items.asms.AddRange(splitPathItems(settings.DefaultRefAssemblies));
            }
            catch { }

            items.dirs = items.dirs.Where(x => x.HasText()).Distinct().ToList();
            return items;
        }

        /// <summary>
        /// Gets a function that returns the path to the CS-Script engine executable (cscs.exe).
        /// </summary>
        /// <value>
        /// A function that returns the full path to the cscs.exe file located in the same directory
        /// as the currently executing assembly.
        /// </value>
        /// <remarks>
        /// This delegate is used to dynamically resolve the CS-Script engine executable location,
        /// which is typically needed for launching script compilation and execution processes.
        /// </remarks>
        public static Func<string> GetEngineExe = () => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cscs.exe");

        /// <summary>
        /// Gets the path to the current CS-Script configuration file.
        /// </summary>
        /// <returns>
        /// A string containing the full path to the CS-Script configuration file currently in use.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is primarily used by external tools and IDEs (such as the Sublime Text 3 Syntaxer)
        /// to locate and read the CS-Script configuration settings.
        /// </para>
        /// <para>
        /// The configuration file contains settings such as default search directories, reference assemblies,
        /// and other CS-Script runtime options.
        /// </para>
        /// </remarks>
        static public string GetCSSConfig()
            => Settings.CurrentConfigFile;
    }
}