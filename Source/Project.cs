using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CSScriptLibrary;
using System.Runtime.InteropServices;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Globalization;
using System.Threading;
using System.Collections;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Xml;

namespace csscript
{
    /// <summary>
    /// Class that holds all information about the execution context 
    /// (probing directories and dependencies) of a script.
    /// </summary>
    public class Project
    {
        /// <summary>
        /// Primary script that defines the project.
        /// </summary>
        public string Script;
        /// <summary>
        /// List of all C# sources defined by the project. This 
        /// includes the primary script itself and all other scripts files the imported/included
        /// by the primary script.
        /// </summary>
        public string[] Files;
                     
        /// <summary>
        /// List of assemblies the script of the project is referencing.
        /// </summary>
        public string[] Refs;

        /// <summary>
        /// List of search folders where CS-Script does probing for 
        /// imported/included scripts and assemblies.
        /// </summary>
        public string[] SearchDirs;

        /// <summary>
        /// Generates the top level view project for a given script. 
        /// <para>
        /// Note this method uses the same algorithm as CS-Script executor but it deliberately doesn't
        /// include cached directories and auto-generated files. This method is to be used by IDs and tools. 
        /// </para>
        /// </summary>
        /// <param name="script">The script.</param>
        /// <returns></returns>
        static public Project GenerateProjectFor(string script)
        {
            return ProjectBuilder.GenerateProjectFor(script);
        }
    }

#pragma warning disable 649

    internal class ProjectBuilder
    {
        static public string DefaultSearchDirs;
        static public string DefaultRefAsms;
        static public string DefaultNamespaces;

        static public Project GenerateProjectFor(string script)
        {
            //Debug.Assert(false);
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

            ScriptParser parser = null;
            string currDir = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(script);
                parser = new ScriptParser(script, searchDirs.ToArray(), false);
            }
            finally
            {
                Environment.CurrentDirectory = currDir;
            }

            //search dirs could be also defined in the script
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

            project.Files = sources.Distinct().ToArray();
            project.Refs = parser.AgregateReferences(probingDirs, defaultRefAsms, defaultNamespaces).ToArray();
            project.SearchDirs = probingDirs;
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
                
                var configFile = GetCSSConfig();
                var settings = Settings.Load(configFile);

                items.dirs.AddRange(splitPathItems(settings.SearchDirs));
                if (configFile != null && File.Exists(configFile))
                    items.dirs.Add(Path.Combine(Path.GetDirectoryName(configFile), "lib"));
                items.asms.AddRange(splitPathItems(settings.DefaultRefAssemblies));
            }
            catch { }

            items.dirs = items.dirs.Distinct().ToList();
            return items;
        }

        public static Func<string> GetEngineExe = () => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cscs.exe");

        static public string GetCSSConfig()
        {
            try
            {
                string cscs_exe = GetEngineExe();
                if (cscs_exe != null)
                {
                    var file = Path.Combine(Path.GetDirectoryName(cscs_exe), "css_config.xml");
                    if (File.Exists(file))
                        return file;
                }
            }
            catch { }
            return null;

            //var csscriptDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");
            //if (csscriptDir != null)
            //    return Environment.ExpandEnvironmentVariables("%CSSCRIPT_DIR%\\css_config.xml");
            //else
            //    return null;
        }
    }
}