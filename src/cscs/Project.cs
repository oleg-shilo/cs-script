using CSScripting;
using CSScriptLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
        /// by IDs and tools.
        /// </para>
        /// </summary>
        /// <param name="script">The script.</param>
        /// <returns>The project instance</returns>
        static public Project GenerateProjectFor(string script)
        {
            return ProjectBuilder.GenerateProjectFor(script);
        }
    }

#pragma warning disable 649

    internal class ProjectBuilder
    {
        static ProjectBuilder()
        {
            if (CSharpParser.NeedInitEnvironment)
                Environment.SetEnvironmentVariable("css_nuget", null);
        }

        static public string DefaultSearchDirs;
        static public string DefaultRefAsms;
        static public string DefaultNamespaces;

        static public Project GenerateProjectFor(string script)
        {
            // ********************************************************************************************
            // * Extremely important to keep the project building algorithm in sync with CSExecutor.Compile
            // ********************************************************************************************
            var project = new Project { Script = script };

            var searchDirs = new List<string>();
            searchDirs.Add(Path.GetDirectoryName(script));

            var globalConfig = GetGlobalConfigItems();
            var defaultSearchDirs = globalConfig.dirs;
            var defaultRefAsms = globalConfig.asms;
            var defaultNamespaces = globalConfig.namespaces;

            searchDirs.AddRange(defaultSearchDirs);

#if !class_lib

            foreach (var item in defaultSearchDirs)
            {
                Console.WriteLine("searchdir: " + item);
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

            project.Refs = parser.AgregateReferences(probingDirs, defaultRefAsms, defaultNamespaces)
                                 .Select(PathExtensions.PathNormaliseSeparators)
                                 .ToArray();

#if !class_lib
            project.Refs = project.Refs.ConcatWith(precompiling.NewReferences);
            project.Files = project.Files.ConcatWith(precompiling.NewIncludes);
#endif

            project.SearchDirs = probingDirs.Select<string, string>(PathExtensions.PathNormaliseSeparators).ToArray();

            return project;
        }

        public class ConfigItems
        {
            public List<string> dirs = new List<string>();
            public List<string> asms = new List<string>();
            public List<string> namespaces = new List<string>();
        }

        static public ConfigItems GetGlobalConfigItems()
        {
            var items = new ConfigItems();

            Func<string, string[]> splitPathItems = text => text.Split(';', ',')
                                                                .Where(x => !string.IsNullOrEmpty(x))
                                                                .Select(x => Environment.ExpandEnvironmentVariables(x.Trim()))
                                                                .ToArray();
            try
            {
                items.dirs.AddRange(splitPathItems(DefaultSearchDirs ?? ""));
                items.asms.AddRange(splitPathItems(DefaultRefAsms ?? ""));
                items.namespaces.AddRange(splitPathItems(DefaultNamespaces ?? ""));

                var configFile = Settings.DefaultConfigFile;
                var settings = Settings.Load(configFile);
#if !class_lib
                items.dirs.AddRange(splitPathItems(settings.SearchDirs));
#else
                items.dirs.AddRange(settings.SearchDirs);
                items.dirs.AddRange(CSScript.GlobalSettings.SearchDirs);
#endif

                //if (configFile != null && File.Exists(configFile))
                if (Assembly.GetExecutingAssembly().Location().HasText())
                    items.dirs.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location()), "lib"));
                else if (Environment.GetEnvironmentVariable("CSS_ENTRY_ASM") != null)
                    items.dirs.Add(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("CSS_ENTRY_ASM")), "lib"));

                items.asms.AddRange(splitPathItems(settings.DefaultRefAssemblies));
            }
            catch { }

            items.dirs = items.dirs.Distinct().ToList();
            return items;
        }

        public static Func<string> GetEngineExe = () => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cscs.exe");

        /// <summary>
        /// Gets the CSS configuration. Used by ST3 Syntaxer
        /// </summary>
        /// <returns>Default config file location</returns>
        static public string GetCSSConfig()
            => Settings.DefaultConfigFile;
    }
}