using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Scripting;
using CSScripting;
using CSScripting.CodeDom;

namespace csscript
{
    class PackageInfo
    {
        public string SpecFile;
        public string CompatibleLib;
        public string Version;
        public string PreferredRuntime;
        public string Name;
    }

    // Tempting to use "NuGet.Core" NuGet package to avoid deploying and using nuget.exe. However it
    // is not compatible with .NET Core runtime (at least as at 19.05.2018) Next candidate is the
    // REST API (e.g. https://api-v2v3search-0.nuget.org/query?q=cs-script&prerelease=false)
    class NuGet
    {
        // static NuGetCore nuget = new();

        internal static string RestoreMarker = "Restoring packages...";

        static public string NuGetCacheView => Directory.Exists(NuGetCache) ? NuGetCache : "<not found>";
        static public string NuGetCache => CSExecutor.options.legacyNugetSupport ? NuGetCore.NuGetCache : NuGetNewAlgorithm.NuGetCache;

        static public string NuGetExeView
            => (NuGetCore.NuGetExe.FileExists() || NuGetCore.NuGetExe == "dotnet") ? NuGetCore.NuGetExe : "<not found>";

        static public bool newPackageWasInstalled => NuGetCore.NewPackageWasInstalled;

        [Obsolete("Not to be used with new NuGet support algorithm", true)]
        static public void InstallPackage(string packageNameMask, string nugetConfig = null) => NuGetCore.InstallPackage(packageNameMask, nugetConfig);

        static public void ListPackages()
        {
            Console.WriteLine("Repository: " + NuGetCacheView);
            int i = 0;
            foreach (string name in NuGetCore.ListPackages())
                Console.WriteLine((++i) + ". " + name);
            NuGetCore.ListPackages();
        }

        static public string[] Resolve(string[] packages, bool suppressDownloading, string script)
            => CSExecutor.options.legacyNugetSupport ?
                NuGetCore.Resolve(packages, suppressDownloading, script) :
                NuGetNewAlgorithm.FindAssembliesOf(packages, suppressDownloading, script);
    }

    class NuGetCore
    {
        //https://docs.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders
        // .NET Mono, .NET Core
        static public string NuGetCache => Runtime.IsWin ?
                                           Environment.ExpandEnvironmentVariables(@"%userprofile%\.nuget\packages") :
                                           "~/.nuget/packages";

        static public string NuGetExe => "dotnet";

        static public bool NewPackageWasInstalled { get; set; }

        static public void InstallPackage(string packageNameMask, string version = null, string nugetArgs = null, string nugetConfig = null)
        {
            var packages = new string[0];
            //index is 1-based, exactly as it is printed with ListPackages
            if (int.TryParse(packageNameMask, out int index))
            {
                var all_packages = ListPackages();
                if (0 < index && index <= all_packages.Count())
                    packages = [all_packages[index - 1]];
                else
                    Console.WriteLine("There is no package with the specified index");
            }
            else
            {
                // Regex is too much at this stage string pattern =
                // CSSUtils.ConvertSimpleExpToRegExp(); Regex wildcard = new Regex(pattern, RegexOptions.IgnoreCase);

                if (packageNameMask.EndsWith('*'))
                    packages = ListPackages().Where(x => x.StartsWith(packageNameMask.Substring(0, packageNameMask.Length - 1))).ToArray();
                else
                    packages = [packageNameMask];
            }

            // C:\Users\user\AppData\Local\Temp\csscript.core\.nuget\333
            var nuget_dir = Runtime.GetScriptTempDir()
                                   .PathJoin(".nuget", Process.GetCurrentProcess().Id)
                                       .EnsureDir();

            try
            {
                var proj_template = nuget_dir.PathJoin("build.csproj");

                if (!File.Exists(proj_template))
                {
                    "dotnet".Run("new console", nuget_dir);

                    if (nugetConfig.FileExists())
                        File.Copy(nugetConfig, nuget_dir.PathJoin(nugetConfig.GetFileName()), true);

                    foreach (var name in packages)
                    {
                        var ver = "";
                        if (version != null)
                            ver = "-v " + version;

                        if (string.IsNullOrEmpty(nugetArgs))
                            nugetArgs = "";

                        // Syntax: dotnet add <PROJECT (optional)> package [options] <PACKAGE_NAME>
                        string commandLine = $"add package {ver} {nugetArgs} {name}";
                        "dotnet".Run(commandLine, nuget_dir, x => Console.WriteLine(x));
                    }

                    // intercept and report incompatible packages (maybe)
                }
            }
            finally
            {
                Task.Run(() =>
                {
                    try
                    {
                        nuget_dir.DeleteDir();
                        ClearAnabdonedNugetDirs(nuget_dir.GetDirName());
                    }
                    catch { }
                });
            }
        }

        static void ClearAnabdonedNugetDirs(string nuget_root)
        {
            // not implemented yet
            foreach (var item in Directory.GetDirectories(nuget_root))
            {
                if (int.TryParse(item.GetFileName(), out int proc_id))
                {
                    try
                    {
                        if (Process.GetProcessById(proc_id) == null)
                            item.DeleteDir();
                    }
                    catch { }
                }
            }
        }

        static IEnumerable<PackageInfo> ResolveDependenciesFor(IEnumerable<PackageInfo> packages)
        {
            var result = new List<PackageInfo>(packages);
            var queue = new Queue<PackageInfo>(packages);
            while (queue.Any())
            {
                PackageInfo item = queue.Dequeue();

                IEnumerable<XElement> dependencyPackages;

                var dependenciesSection = XElement.Parse(File.ReadAllText(item.SpecFile))
                                                  .FindDescendants("dependencies")
                                                  .FirstOrDefault();
                if (dependenciesSection == null)
                    continue;

                // <dependencies> <group targetFramework=".NETStandard2.0"> <dependency
                // id="Microsoft.Extensions.Logging.Abstractions" version="2.1.0"
                // exclude="Build,Analyzers" />
                var frameworks = dependenciesSection.FindDescendants("group");
                if (frameworks.Any())
                {
                    IEnumerable<XElement> frameworkGroups = dependenciesSection.FindDescendants("group");

                    dependencyPackages = GetCompatibleTargetFramework(frameworkGroups, item)
                                             ?.FindDescendants("dependency")
                                             ?? [];
                }
                else
                    dependencyPackages = dependenciesSection.FindDescendants("dependency");

                foreach (var element in dependencyPackages)
                {
                    var newPackage = new PackageInfo
                    {
                        Name = element.Attribute("id").Value,
                        Version = element.Attribute("version").Value,
                        PreferredRuntime = item.PreferredRuntime
                    };

                    newPackage.SpecFile = NuGetCache.PathJoin(newPackage.Name, newPackage.Version, newPackage.Name + ".nuspec");

                    if (!result.Any(x => x.Name == newPackage.Name) && File.Exists(newPackage.SpecFile))
                    {
                        queue.Enqueue(newPackage);
                        result.Add(newPackage);
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Gets the compatible target framework. Similar to `GetPackageCompatibleLib` but relies on
        /// NuGet spec file
        /// </summary>
        /// <param name="freameworks">The frameworks.</param>
        /// <param name="package">The package.</param>
        /// <returns></returns>
        static XElement GetCompatibleTargetFramework(IEnumerable<XElement> freameworks, PackageInfo package)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/frameworks netstandard?.?
            // netcoreapp?.? net?? | net??? "" (no framework element, meaning "any framework")
            // Though packages use Upper case with '.' preffix: '<group targetFramework=".NETStandard2.0">'

            XElement findMatch(Predicate<string> matchTest)
            {
                var items = freameworks.Select(x => new { Name = x.Attribute("targetFramework")?.Value, Element = x })
                            .OrderByDescending(x => x.Name)
                            .ToArray();

                var match = items.FirstOrDefault(x => matchTest(x.Name ?? ""))?.Element ??   // exact match
                            items.FirstOrDefault(x => x.Name == null)?.Element;               // universal dependency specified by not supplying targetFramework element

                return match;
            }

            if (package.PreferredRuntime != null)
            {
                // by requested runtime
                return findMatch(x => x.Contains(package.PreferredRuntime));
            }
            else
            {
                if (CSharpCompiler.DefaultCompilerRuntime == DefaultCompilerRuntime.Standard)
                {
                    // by configured runtime
                    return findMatch(x => x.Contains("netstandard", ignoreCase: true));
                }
                else
                {
                    if (Runtime.IsCore)
                        // by runtime of the host
                        return findMatch(x => x.Contains("netcore", ignoreCase: true))
                            ?? findMatch(x => x.Contains("netstandard", ignoreCase: true));
                    else
                        // by .NET full as tehre is no other options
                        return findMatch(x => (x.StartsWith("net", ignoreCase: true)
                                               || x.StartsWith(".net", ignoreCase: true))
                                         && !x.Contains("netcore", ignoreCase: true)
                                         && !x.Contains("netstandard", ignoreCase: true))
                               ?? findMatch(x => x.Contains("netstandard", ignoreCase: true));
                }
            }
        }

        /// <summary>
        /// Gets the package compatible library. Similar to `GetCompatibleTargetFramework` but
        /// relies on file structure
        /// </summary>
        /// <param name="package">The package.</param>
        /// <returns></returns>
        static string GetPackageCompatibleLib(PackageInfo package)
        {
            var libDir = package.SpecFile.GetDirName().PathJoin("lib");

            if (!Directory.Exists(libDir))
                return null;

            var frameworks = Directory.GetDirectories(package.SpecFile.GetDirName().PathJoin("lib"))
                                      .OrderByDescending(x => x)
                                      .Select(x => new { Runtime = x.GetFileName(), Path = x });

            if (package.PreferredRuntime != null)
            {
                return frameworks.FirstOrDefault(x => x.Runtime.EndsWith(package.PreferredRuntime))?.Path;
            }
            else
            {
                if (CSharpCompiler.DefaultCompilerRuntime == DefaultCompilerRuntime.Standard)
                {
                    return frameworks.FirstOrDefault(x => x.Runtime.StartsWith("netstandard", ignoreCase: true))?.Path;
                }
                else // host runtime
                {
                    if (Runtime.IsCore)
                        return (frameworks.FirstOrDefault(x => x.Runtime.StartsWith("netcore", ignoreCase: true))
                                ?? frameworks.FirstOrDefault(x => x.Runtime.StartsWith("netstandard", ignoreCase: true)))?.Path;
                    else
                        return frameworks.FirstOrDefault(x => x.Runtime.StartsWith("net", ignoreCase: true)
                                                              && !x.Runtime.StartsWith("netcore", ignoreCase: true)
                                                              && !x.Runtime.StartsWith("netstandard", ignoreCase: true))?.Path;
                }
            }
        }

        static string[] GetCompatibleAssemblies(PackageInfo package)
        {
            var lib = GetPackageCompatibleLib(package);
            if (lib != null)
                return Directory.GetFiles(GetPackageCompatibleLib(package), "*.dll")
                                .Where(item => !item.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                                .Where(x => Utils.IsRuntimeCompatibleAsm(x))
                                .ToArray();
            else
                return [];
        }

        static public string[] ListPackages()
        {
            return Directory.GetDirectories(NuGetCache)
                            .Select(x =>
                            {
                                var spec = Directory.GetFiles(x, "*.nuspec", SearchOption.AllDirectories).FirstOrDefault();
                                if (spec != null)
                                {
                                    return XDocument.Load(spec)
                                                    .SelectFirst("package/metadata/id")?.Value;
                                }
                                return null;
                            })
                            .Where(x => !x.IsEmpty())
                            .ToArray();
        }

        static PackageInfo FindPackage(string name, string version)
        {
            // Create nuget cache directory if we are on a blank system
            if (!Directory.Exists(NuGetCache))
                Directory.CreateDirectory(NuGetCache);

            var packages = Directory.GetDirectories(NuGetCache, name, SearchOption.TopDirectoryOnly)
                           .SelectMany(x =>
                            {
                                return Directory.GetFiles(x, "*.nuspec", SearchOption.AllDirectories)
                                                .Select(spec =>
                                                        {
                                                            if (spec != null)
                                                            {
                                                                var doc = XDocument.Load(spec);
                                                                return new PackageInfo
                                                                {
                                                                    SpecFile = spec,
                                                                    Version = doc.SelectFirst("package/metadata/version")?.Value,
                                                                    Name = doc.SelectFirst("package/metadata/id")?.Value
                                                                };
                                                            }
                                                            return null;
                                                        });
                            })
                           .OrderByDescending(x => x.Version)
                           .Where(x => x != null)
                           .ToArray();

            return packages.FirstOrDefault(x => string.Compare(x.Name, name, StringComparison.OrdinalIgnoreCase) == 0 &&
                                                               (version.IsEmpty() || version == "*" || version == x.Version));
        }

        static public string[] Resolve(string[] packages, bool suppressDownloading, string script)
        {
            var assemblies = new List<string>();
            var all_packages = new List<PackageInfo>();

            var allPackages = packages.ToList();

            string packagesConfig = script.ChangeFileName("packages.config");
            if (packagesConfig.FileExists())
                allPackages.AddRange(XDocument.Load(packagesConfig)
                                              .Descendants("package")
                                              .Select(n =>
                                                      {
                                                          var package = n.Attribute("id").Value;
                                                          if (n.Attribute("version") != null)
                                                              package += $" -ver:\"{n.Attribute("version").Value}\"";
                                                          return package;
                                                      }));

            bool promptPrinted = false;
            foreach (string item in allPackages)
            {
                // //css_nuget -noref -ng:"-IncludePrerelease version 1.0beta" cs-script
                // //css_nuget -noref -ver:"4.1.0-alpha1" -ng:"-Pre" NLog

                string[] packageArgs = item.SplitCommandLine();

                string package = packageArgs.FirstOrDefault(x => !x.StartsWith('-'));

                bool suppressReferencing = packageArgs.Contains("-noref");
                string nugetArgs = packageArgs.ArgValue("-ng");
                string packageVersion = packageArgs.ArgValue("-ver");

                string forceTimeoutString = packageArgs.ArgValue("-force");

                bool forceDownloading = (forceTimeoutString != null);
                uint.TryParse(forceTimeoutString, out uint forceTimeout); //'-force:<seconds>'

                var package_info = FindPackage(package, packageVersion);

                if (package_info != null && forceDownloading)
                {
                    var age = DateTime.Now.ToUniversalTime() - File.GetLastWriteTimeUtc(package_info.SpecFile);
                    if (age.TotalSeconds < forceTimeout)
                        forceDownloading = false;
                }

                if (suppressDownloading)
                {
                    // it is OK if the package is not downloaded (e.g. N++ Intellisense)
                    if (!suppressReferencing && package_info != null)
                    {
                        package_info.PreferredRuntime = packageArgs.ArgValue("-rt");
                        //assemblies.AddRange(GetPackageAssemblies(package_info));
                        all_packages.Add(package_info);
                    }
                }
                else
                {
                    if (forceDownloading || package_info == null)
                    {
                        if (!promptPrinted)
                            Console.WriteLine("NuGet> Processing NuGet packages...");

                        promptPrinted = true;

                        try
                        {
                            InstallPackage(package, packageVersion, nugetArgs, script.ChangeFileName("NuGet.config"));
                            package_info = FindPackage(package, packageVersion);
                            NewPackageWasInstalled = true;
                        }
                        catch { }

                        try
                        {
                            if (package_info != null)
                                File.SetLastWriteTimeUtc(package_info.SpecFile, DateTime.Now.ToUniversalTime());
                        }
                        catch { }
                    }

                    if (package_info == null)
                        throw new ApplicationException("Cannot process NuGet package '" + package + "'");

                    if (!suppressReferencing)
                    {
                        package_info.PreferredRuntime = packageArgs.ArgValue("-rt");
                        all_packages.Add(package_info);
                    }
                }
            }

            foreach (PackageInfo package in ResolveDependenciesFor(all_packages))
            {
                assemblies.AddRange(GetCompatibleAssemblies(package));
            }

            return assemblies.ToArray().RemovePathDuplicates();
        }
    }

    class NuGetNewAlgorithm
    {
        static string[] GetPackagesFromConfigFileOfScript(string scriptFile)
        {
            var packages = new List<string>();

            string packagesConfig = scriptFile.ChangeFileName("packages.config");
            if (packagesConfig.FileExists())
                packages.AddRange(XDocument.Load(packagesConfig)
                                                 .Descendants("package")
                                                 .Select(n =>
                                            {
                                                var package = n.Attribute("id").Value;
                                                if (n.Attribute("version") != null)
                                                    package += $" -ver:\"{n.Attribute("version").Value}\"";
                                                return package;
                                            }));
            return [.. packages];
        }

        public static string[] FindAssembliesOf(string[] packages, bool suppressDownloading, string script)
        {
            if (packages.IsEmpty())
                return [];

            var allPackages = packages.Concat(GetPackagesFromConfigFileOfScript(script));

            var forceRestore = Environment.GetEnvironmentVariable("CSS_RESTORE_NUGET_PACKAGES") != null;
            if (packages.Any(x => x.SplitCommandLine().Any(arg => arg.StartsWith("-force"))))
                forceRestore = true;

            var packagesId = $"// packages: {allPackages.OrderBy(x => x).JoinBy(", ")}";

            var assembliesList = CSExecutor.GetCacheDirectory(script).PathJoin(script.GetFileName() + ".nuget.cs");

            string[] assemblies;
            if (!forceRestore && File.Exists(assembliesList))
            {
                var lines = File.ReadAllLines(assembliesList);
                if (lines.FirstOrDefault() == packagesId)
                {
                    // assembliesList file is only ever used here and it is not used for anything else
                    // //css_dir is only used as a convenient prefix
                    var pathFolders = lines
                        .Where(x => x.StartsWith("//css_dir "))
                        .Select(x => x.Replace("//css_dir ", ""));

                    foreach (string dir in pathFolders)
                        CSExecutor.options.AddSearchDir(dir.Trim(), Settings.code_dirs_section);

                    var refs = lines
                        .Where(x => x.StartsWith("//css_ref "))
                        .Select(x => x.Replace("//css_ref ", ""))
                        .ToArray();

                    if (refs.All(File.Exists))
                        return refs;
                }
            }

            (assemblies, var nativeAssetsFolders) = FindAssembliesOf(allPackages);

            File.WriteAllText(assembliesList, packagesId + NewLine);
            File.AppendAllLines(assembliesList, assemblies.Select(x => "//css_ref " + x));
            if (nativeAssetsFolders.Any())
            {
                foreach (string dir in nativeAssetsFolders)
                    CSExecutor.options.AddSearchDir(dir.Trim(), Settings.code_dirs_section);

                File.AppendAllLines(assembliesList, nativeAssetsFolders.Select(x => "//css_dir " + x));
            }
            return assemblies;
        }

        public static string NuGetCache => GetEnvironmentVariable("CSSCRIPT_NUGET_PACKAGES") ??
                                           SpecialFolder.UserProfile.GetPath(".nuget", "packages");

        internal static string[] GetPackageNativeDllsFolders(string packageDir)
        {
            //
            // C:\Users\user\AppData\Local\Temp\csscript.core\nuget\16b5531e-8d0d-43fc-aaed-af5eac79ca04
            // C:\Users\user\AppData\Local\Temp\csscript.core\nuget\16b5531e-8d0d-43fc-aaed-af5eac79ca04\publish\runtimes\win-x64\native\libSkiaSharp.dll

            var runtimeFolder = packageDir.PathJoin("runtimes", Runtime.RID, "native");
            var sw = Stopwatch.StartNew();
            if (runtimeFolder.DirExists())
            {
                var result = new List<string>();
                foreach (var file in Directory.GetFiles(runtimeFolder))
                {
                    var expectedSize = new FileInfo(file).Length;

                    var pattern = file.GetFileName();
                    var matchingFileNames = Directory.GetFiles(NuGetCache, pattern, SearchOption.AllDirectories);
                    foreach (var matchingFileName in matchingFileNames.Where(x => x.Contains(Runtime.RID)))
                    {
                        if (expectedSize == new FileInfo(matchingFileName).Length)
                        {
                            // if(isSameData(refAssemblyBytes, File.ReadAllBytes(f)))
                            {
                                result.Add(matchingFileName.GetDirName());
                                break;
                            }
                        }
                    }
                }
                sw.Stop();
                return result.ToArray();
            }
            return [];
        }

        public static (string[] assemblies, string[] nativeAssetsDirs) FindAssembliesOf(IEnumerable<string> packages)
        {
            // TODO
            // - create a project template based on "dotnet new classlib" runResult
            // - analyse only directories that are listed in `*.nuget.cache` file (e.g. test\obj\project.nuget.cache)

            var result = new List<string>();
            var nativeAssetsDirs = new string[0];

            var projId = Guid.NewGuid().ToString();
            var projectDir = SpecialFolder.LocalApplicationData.GetPath("Temp", "csscript.core", "nuget", projId);
            Directory.CreateDirectory(projectDir);

            var restoreArgs = "";

            restoreArgs += $" --packages \"{NuGetCache}\"";

            (int exitCode, string output) dotnet_run(string args, int timeout = -1)
            {
                var p = new Process();
                p.StartInfo.FileName = "dotnet";
                p.StartInfo.Arguments = args;
                p.StartInfo.WorkingDirectory = projectDir;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();

                var sb = new StringBuilder();
                p.OutputDataReceived += (_, e) => sb.AppendLine(e.Data);
                p.BeginOutputReadLine();

                if (!p.WaitForExit(timeout))
                {
                    p.KillSafe(true);
                    return (-1, $"Process timed out after {timeout}ms");
                }

                return (p.ExitCode, sb.ToString());
            }

            try
            {
                var projectFile = projectDir.PathJoin($"{projId}.csproj");

                dotnet_run("new classlib");

                foreach (var item in packages)
                {
                    string[] packageArgs = item.SplitCommandLine();

                    // accumulate restore args
                    restoreArgs += packageArgs.ArgValue("-ng") + " "; // temp, until `-ng` is dropped
                    restoreArgs += packageArgs.ArgValue("-restore") + " ";

                    var package = packageArgs.FirstOrDefault(x => !x.StartsWith('-'));
                    var prerelease = packageArgs.ContainsAny("-pre", "--prerelease") ? "--prerelease" : "";
                    var packageVersion = packageArgs.ArgValue("-ver") ?? packageArgs.ArgValue("-v");
                    packageVersion = packageVersion.IsNotEmpty() ? $"-v {packageVersion} " : "";

                    var runResult = dotnet_run($"add package {package} {prerelease} {packageVersion} -n");// `-n` is to prevent restoring at this stage as it will be called for the whole project

                    if (runResult.exitCode != 0)
                        Console.WriteLine($"Cannot add package {package}: {runResult.output}");
                }

                packages = packages.OrderBy(x => x).ToArray();

                var sw = Stopwatch.StartNew();
                Console.WriteLine(NuGet.RestoreMarker);
                Console.WriteLine("   " + packages.JoinBy(NewLine + "   "));

                var nugetRestoreTimeout = Environment.GetEnvironmentVariable("CSS_NUGET_RESTORE_TIMEOUT")?.ToInt() ?? (int)TimeSpan.FromMinutes(3).TotalMilliseconds;
                var restore = dotnet_run("restore " + restoreArgs.Trim(), nugetRestoreTimeout);

                if (restore.exitCode != 0)
                {
                    Console.WriteLine(restore.output);
                    throw new ApplicationException($"Package restoring failed.{NewLine}If problem persist you may try to download the package before executing the script:{NewLine}" +
                                                   $"   dotnet nuget download <package-name> --include-dependencies");
                }

                var publish = dotnet_run("publish --no-restore -o ./publish");
                if (publish.exitCode != 0)
                {
                    Console.WriteLine(publish.output);
                    throw new ApplicationException($"Package restoring failed.");
                }

                string[] locateAssemblies(string dir)
                {
                    if (Directory.Exists(dir))
                        return Directory.GetFiles(dir, "*.dll")
                                        .Concat(Directory.GetFiles(dir, "*.exe"))
                                        .Where(x => !x.EndsWith($"{projId}.dll"))
                                        .OrderBy(x => x)
                                        .ToArray();
                    else
                        return [];
                }

                var allRefAssemblies = new List<string>();

                if ("CSS_FEATURE_DISABLE_NUGET_RUNTIMES_SUPPORT".GetEnvar() == null)
                {
                    allRefAssemblies.AddRange(locateAssemblies(projectDir.PathJoin("publish", "runtimes", Runtime.RID_cpu_nutral, "lib", Runtime.TFM_Any)));
                    allRefAssemblies.AddRange(locateAssemblies(projectDir.PathJoin("publish", "runtimes", Runtime.RID_cpu_nutral, "lib", Runtime.TFM)));
                    allRefAssemblies.AddRange(locateAssemblies(projectDir.PathJoin("publish", "runtimes", Runtime.RID, "lib", Runtime.TFM_Any)));
                    allRefAssemblies.AddRange(locateAssemblies(projectDir.PathJoin("publish", "runtimes", Runtime.RID, "lib", Runtime.TFM)));
                }

                allRefAssemblies.AddRange(locateAssemblies(projectDir.PathJoin("publish")));

                // filter out the same dlls found in lower compatibility folders or duplicates
                allRefAssemblies = allRefAssemblies.GroupBy(x => x.GetFileName(), StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();

                bool isSameData(byte[] a, byte[] b)
                {
                    if (a.Length == b.Length)
                    {
                        for (int i = 0; i < a.Length; i++)
                            if (a[i] != b[i])
                                return false;
                        return true;
                    }
                    else
                        return false;
                }

                // Console.WriteLine("    " + sw.Elapsed.ToString());
                sw.Restart();
                Console.WriteLine("Mapping packages to assemblies...");
                // Debug.Assert(false);

                foreach (var x in allRefAssemblies)
                {
                    var packageName = Path.GetFileNameWithoutExtension(x);

                    var refAssemblyVersion = FileVersionInfo.GetVersionInfo(x).FileVersion;
                    var refAssemblySize = new FileInfo(x).Length;
                    byte[] refAssemblyBytes = File.ReadAllBytes(x);

                    var matchingAssemblies = Directory
                            // .GetDirectories(nugetRepo, $"{packageName}*") // quicker but less reliable as the package name may not be the same as the assembly name. IE `ICSharpCode.SharpZipLib.dll` vs `SharpZipLib.dll`
                            .GetDirectories(NuGetCache, $"*")
                            .SelectMany(d => Directory
                                                 .GetFiles(d, Path.GetFileName(x), SearchOption.AllDirectories)
                                                 .Where(f =>
                                                        refAssemblyVersion == FileVersionInfo.GetVersionInfo(f).FileVersion
                                                        && refAssemblySize == new FileInfo(f).Length
                                                        && isSameData(refAssemblyBytes, File.ReadAllBytes(f))
                                                       ));

                    var assembly = matchingAssemblies.FirstOrDefault() ?? $"{packageName} - not found";
                    result.Add(assembly);
                }

                // Console.WriteLine("    " + sw.Elapsed.ToString());
                nativeAssetsDirs = GetPackageNativeDllsFolders(Path.Combine(projectDir, "publish"));
            }
            finally
            {
                if (GetEnvironmentVariable("CSS_RESTORE_DONOT_CLEAN") == null)
                    try { Directory.Delete(projectDir, true); }
                    catch { }
            }

            return (result.ToArray(), nativeAssetsDirs);
        }
    }
}