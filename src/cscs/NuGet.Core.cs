using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        static NuGetCore nuget = new NuGetCore();

        static public string NuGetCacheView => Directory.Exists(NuGetCore.NuGetCache) ? NuGetCore.NuGetCache : "<not found>";
        static public string NuGetCache => NuGetCore.NuGetCache;

        static public string NuGetExeView
            => (NuGetCore.NuGetExe.FileExists() || NuGetCore.NuGetExe == "dotnet") ? NuGetCore.NuGetExe : "<not found>";

        static public bool newPackageWasInstalled => NuGetCore.NewPackageWasInstalled;

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
            => NuGetCore.Resolve(packages, suppressDownloading, script);
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
                    packages = new string[] { all_packages[index - 1] };
                else
                    Console.WriteLine("There is no package with the specified index");
            }
            else
            {
                // Regex is too much at this stage string pattern =
                // CSSUtils.ConvertSimpleExpToRegExp(); Regex wildcard = new Regex(pattern, RegexOptions.IgnoreCase);

                if (packageNameMask.EndsWith("*"))
                    packages = ListPackages().Where(x => x.StartsWith(packageNameMask.Substring(0, packageNameMask.Length - 1))).ToArray();
                else
                    packages = new[] { packageNameMask };
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
                    nuget_dir.DeleteDir();
                    ClearAnabdonedNugetDirs(nuget_dir.GetDirName());
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
                    if (Process.GetProcessById(proc_id) == null)
                        try { item.DeleteDir(); } catch { }
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
                                             ?? new XElement[0];
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
                        return (frameworks.FirstOrDefault(x => x.Runtime.StartsWith("net", ignoreCase: true))
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
                return new string[0];
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
                                                               (version.IsEmpty() || version == x.Version));
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
            foreach (string item in packages.Concat(allPackages))
            {
                // //css_nuget -noref -ng:"-IncludePrerelease version 1.0beta" cs-script
                // //css_nuget -noref -ver:"4.1.0-alpha1" -ng:"-Pre" NLog

                string[] packageArgs = item.SplitCommandLine();

                string package = packageArgs.FirstOrDefault(x => !x.StartsWith("-"));

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
}