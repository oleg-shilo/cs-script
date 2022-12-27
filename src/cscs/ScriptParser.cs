using csscript;
using CSScripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSScriptLib
{
    /// <summary>
    /// Class that manages parsing the main and all imported (if any) C# Script files
    /// </summary>
    public class ScriptParser
    {
        /// <summary>
        /// Gets the script parsing context. This object is effectively a parsing result.
        /// </summary>
        /// <returns>Parsing result</returns>
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
        /// Processes the imported script. Processing involves lookup for 'static Main' and renaming
        /// it so it does not interfere with the 'static Main' of the primary script. After renaming
        /// is done the new content is saved in the CS-Script cache and the new file location is
        /// returned. The saved file can be used late as an "included script". This technique can be
        /// from 'precompiler' scripts.
        /// <para>
        /// If the script file does not require renaming (static Main is not present) the method
        /// returns the original script file location.
        /// </para>
        /// </summary>
        /// <param name="scriptFile">The script file.</param>
        /// <returns>Path to the script file to be compiled and executed</returns>
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
        public string[] FilesToCompile => fileParsers.Select(x => x.FileToCompile).ToArray();

        /// <summary>
        /// Collection of the imported files (dependent scripts)
        /// </summary>
        public string[] ImportedFiles
            => fileParsers.Where(x => x.Imported).Select(x => x.FileToCompile).ToArray();

        /// <summary>
        /// Collection of resource files referenced from code
        /// </summary>
        public string[] ReferencedResources
            => referencedResources.ToArray();

        /// <summary>
        /// Collection of compiler options
        /// </summary>
        public string[] CompilerOptions => compilerOptions.ToArray();

        /// <summary>
        /// Precompilers specified in the primary script file.
        /// </summary>
        public string[] Precompilers => precompilers.ToArray();

        /// <summary>
        /// Collection of namespaces referenced from code (including those referenced in dependent scripts)
        /// </summary>
        public string[] ReferencedNamespaces => referencedNamespaces.ToArray();

        /// <summary>
        /// Collection of namespaces, which if found in code, should not be resolved into referenced assembly.
        /// </summary>
        public string[] IgnoreNamespaces => ignoreNamespaces.ToArray();

        /// <summary>
        /// Resolves the NuGet packages into assemblies to be referenced by the script.
        /// <para>
        /// If the package was never installed/downloaded yet CS-Script runtime will try to download it.
        /// </para>
        /// <para>
        /// CS-Script will also analyze the installed package structure in try to reference
        /// compatible assemblies from the package.
        /// </para>
        /// </summary>
        /// <param name="suppressDownloading">
        /// if set to <c>true</c> suppresses downloading the NuGet package. Suppressing can be
        /// useful for the quick 'referencing' assessment.
        /// </param>
        /// <returns>Collection of the referenced assembly files.</returns>
        public string[] ResolvePackages(bool suppressDownloading = false)
        {
#if !class_lib
            return NuGet.Resolve(Packages, suppressDownloading, this.ScriptPath);
#else
            return new string[0];
#endif
        }

        /// <summary>
        /// Collection of the NuGet packages
        /// </summary>
        public string[] Packages => packages.ToArray();

        /// <summary>
        /// Collection of referenced assemblies. All assemblies are referenced either from
        /// command-line, code or resolved from referenced namespaces.
        /// </summary>
        public string[] ReferencedAssemblies => referencedAssemblies.ToArray();

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
        /// <param name="searchDirs">Extra ScriptLibrary directory</param>
        public ScriptParser(string fileName, string[] searchDirs)
        {
            //if ((CSExecutor.ExecuteOptions.options != null && CSExecutor.options.useSmartCaching) && CSExecutor.ScriptCacheDir == "") //in case if ScriptParser is used outside of the script engine
            if (CSExecutor.ScriptCacheDir == "" && fileName.IsValidPath()) //in case if ScriptParser is used outside of the script engine
                CSExecutor.SetScriptCacheDir(fileName);
            Init(fileName, searchDirs);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileName">Script file name</param>
        /// <param name="searchDirs">Extra ScriptLibrary directory(s)</param>
        /// <param name="throwOnError">
        /// flag to indicate if the file parsing/processing error should raise an exception
        /// </param>
        public ScriptParser(string fileName, string[] searchDirs, bool throwOnError)
        {
            this.throwOnError = throwOnError;
            //if ((CSExecutor.ExecuteOptions.options != null && CSExecutor.ExecuteOptions.options.useSmartCaching) && CSExecutor.ScriptCacheDir == "") //in case if ScriptParser is used outside of the script engine
            if (CSExecutor.ScriptCacheDir == "" && fileName.IsValidPath()) //in case if ScriptParser is used outside of the script engine
                CSExecutor.SetScriptCacheDir(fileName);
            Init(fileName, searchDirs);
        }

        /// <summary>
        /// The path of the parsed script.
        /// </summary>
        public string ScriptPath { get; set; }

        /// <summary>
        /// Gets a value indicating whether the script being parsed is a web application script.
        /// </summary>
        /// <value><c>true</c> if the script is web application; otherwise, <c>false</c>.</value>
        public bool IsWebApp => this.fileParsers.FirstOrDefault()?.IsWebApp == true;

        /// <summary>
        /// Initialization of ScriptParser instance
        /// </summary>
        /// <param name="fileName">Script file name</param>
        /// <param name="searchDirs">Extra ScriptLibrary directory(s)</param>
        void Init(string fileName, string[] searchDirs)
        {
            if (fileName.IsValidPath())
                ScriptPath = fileName;

            //process main file
            var mainFile = new FileParser(fileName, null, true, false, searchDirs, throwOnError);

#if !class_lib
            this.apartmentState = mainFile.ThreadingModel;
#endif
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

            var dirs = new List<string>();
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

            this.SearchDirs = dirs.ToArray().RemovePathDuplicates();

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

                var importedFile = new FileParser(fileInfo.fileName, fileInfo.parseParams, true, true, this.SearchDirs, throwOnError); //do not parse it yet (the third param is false)

                if (fileParsers.BinarySearch(importedFile, fileComparer) < 0)
                {
                    if (File.Exists(importedFile.fileName))
                    {
                        importedFile.ProcessFile(); //parse now namespaces, ref. assemblies and scripts; also it will do namespace renaming


                        this.SearchDirs = this.SearchDirs.ToList()
                                              .AddIfNotThere(importedFile.fileName.GetDirName())
                                              .ToArray();


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
                throw e.ToNewException(fileInfo.parseParams.importingErrorMessage
#if !class_lib
                    , ExecuteOptions.options.reportDetailedErrorInfo
#endif
                                      );
            }
        }

        List<FileParser> fileParsers = new List<FileParser>();

        /// <summary>
        /// Saves all imported scripts in the temporary location.
        /// </summary>
        /// <returns>Collection of the saved imported scripts file names</returns>
        public string[] SaveImportedScripts()
        {
            List<string> retval = new List<string>();

            foreach (FileParser file in fileParsers)
            {
                if (file.Imported)
                {
                    if (file.fileNameImported.HasText() && file.fileNameImported != file.fileName) //script file was copied
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
            foreach (FileParser fileParser in fileParsers)
            {
                if (fileParser.Imported && fileParser.fileNameImported != fileParser.fileName) //the file was copied
                {
                    try
                    {
                        File.SetAttributes(fileParser.FileToCompile, FileAttributes.Normal);
                        fileParser.FileToCompile.FileDelete(rethrow: false);
                    }
                    catch { }
                }
            }
        }

        List<string> referencedNamespaces = new List<string>();
        List<string> ignoreNamespaces = new List<string>();
        List<string> referencedResources = new List<string>();
        List<string> compilerOptions = new List<string>();
        List<string> precompilers = new List<string>();
        List<string> referencedAssemblies = new List<string>();
        List<string> packages = new List<string>();

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
        /// Aggregates the references from the script and its imported scripts. It is a logical
        /// equivalent of CSExecutor.AggregateReferencedAssemblies but optimized for later .NET
        /// versions (e.g LINQ) and completely decoupled. Thus it has no dependencies on internal
        /// state (e.g. settings, options.shareHostAssemblies).
        /// <para>
        /// It is the method to call for generating list of ref asms as part of the project info.
        /// </para>
        /// </summary>
        /// <param name="searchDirs">The search dirs.</param>
        /// <param name="defaultRefAsms">The default ref asms.</param>
        /// <param name="defaultNamespacess">The default namespaces.</param>
        /// <returns>List of references</returns>
        public List<string> AgregateReferences(IEnumerable<string> searchDirs, IEnumerable<string> defaultRefAsms, IEnumerable<string> defaultNamespacess)
        {
            var probingDirs = searchDirs.ToArray();

            var refPkAsms = this.ResolvePackages(true); //suppressDownloading

            var refCodeAsms = this.ReferencedAssemblies
                                  .SelectMany(asm =>
                                  {
                                      var resolved = AssemblyResolver.FindAssembly(asm.Replace("\"", ""), probingDirs);
                                      if (resolved.Any())
                                          return resolved;
                                      else
                                          return new[] { asm.GetFullPath() }; // impotrtant to return not found assembly so it is reported by the compiler
                                  }).ToArray();

            // need to add default CLR assemblies as there will be no namespace->GAC assembly
            // resolving as it is .NET Core
            var clrDefaultAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.StartsWith("System.")).Select(x => x.Location).ToArray();

            var refAsms = refPkAsms.Union(refPkAsms)
                                   .Union(refCodeAsms)
                                   .Union(defaultRefAsms.SelectMany(name => AssemblyResolver.FindAssembly(name, probingDirs)))
                                   .Union(clrDefaultAssemblies)
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

            refAsms = FilterDuplicatedAssembliesByFileName(refAsms);
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
                    // need to ensure that item has extension in order to avoid interpreting complex
                    // file names as simple name + extension: System.Core -> System System.dll -> System
                    string name = item.GetFileNameWithoutExtension();
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