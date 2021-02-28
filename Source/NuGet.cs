using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace csscript
{
    class NuGet
    {
        static public string NuGetCacheView
        {
            get { return Directory.Exists(NuGetCache) ? NuGetCache : "<not found>"; }
        }

        static public string NuGetExeView
        {
            get { return File.Exists(NuGetExe) ? NuGetExe : "<not found>"; }
        }

        static string nuGetCache = null;

        static string NuGetCache
        {
            get
            {
                if (nuGetCache == null)
                {
                    var folder = Environment.SpecialFolder.CommonApplicationData;

                    if (Runtime.IsLinux)
                        folder = Environment.SpecialFolder.ApplicationData;

                    nuGetCache = Environment.GetEnvironmentVariable("css_nuget") ??
                                 Path.Combine(Environment.GetFolderPath(folder), "CS-Script" + Path.DirectorySeparatorChar + "nuget");

                    if (!Directory.Exists(nuGetCache))
                        Directory.CreateDirectory(nuGetCache);
                }
                return nuGetCache;
            }
        }

        static string nuGetExe = null;

        internal static string NuGetExe
        {
            get
            {
                if (nuGetExe == null)
                {
                    string localDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); //N++ case

                    nuGetExe = Path.Combine(localDir, "nuget.exe");
                    if (!File.Exists(nuGetExe))
                    {
                        string libDir = Path.Combine(Environment.ExpandEnvironmentVariables("%CSSCRIPT_DIR%"), "lib"); //CS-S installed
                        nuGetExe = Path.Combine(libDir, "nuget.exe");
                        if (!File.Exists(nuGetExe))
                        {
                            nuGetExe = GetSystemWideNugetApp();
                            if (nuGetExe == null)
                                try
                                {
                                    Console.WriteLine("Warning: Cannot find 'nuget.exe'. Ensure it is in the application directory or in the %CSSCRIPT_DIR%/lib");
                                }
                                catch { }
                        }
                    }
                }
                return nuGetExe;
            }
        }

        static string GetSystemWideNugetApp()
        {
            try
            {
                if (Environment.GetEnvironmentVariable("NUGET_INCOMPATIBLE_HOST") == null)
                {
                    var candidates = Environment.GetEnvironmentVariable("PATH")
                                                .Split(Runtime.IsLinux ? ':' : ';')
                                                .SelectMany(dir => new[]
                                                                   {
                                                                       Path.Combine(dir, "nuget"),
                                                                       Path.Combine(dir, "nuget.exe")
                                                                   });

                    foreach (string file in candidates)
                        if (File.Exists(file))
                            return file;
                }
                return "nuget";
            }
            catch { }
            return null;
        }

        static bool IsPackageDownloaded(string packageDir, string packageVersion)
        {
            if (!Directory.Exists(packageDir))
                return false;

            if (!string.IsNullOrEmpty(packageVersion))
            {
                string packageVersionDir = Path.Combine(packageDir, Path.GetFileName(packageDir) + "." + packageVersion);
                return Directory.Exists(packageVersionDir);
            }
            else
            {
                return Directory.Exists(packageDir) && Directory.GetDirectories(packageDir).Length > 0;
            }
        }

        public static bool newPackageWasInstalled = false;

        static IEnumerable<PackageInfo> ResolveDependenciesFor(IEnumerable<PackageInfo> packages)
        {
            var result = new List<PackageInfo>();
            var map = new Dictionary<string, List<PackageInfo>>();

            void add(PackageInfo info)
            {
                if (!map.ContainsKey(info.Name))
                    map[info.Name] = new List<PackageInfo>();
                map[info.Name].Add(info);
            }

            foreach (var parentPackage in packages)
            {
                add(parentPackage);

                foreach (var nupkg in Directory.GetFiles(parentPackage.SpecFile.GetDirName().GetDirName(), "*.nupkg", SearchOption.AllDirectories))
                {
                    var info = ExtractPackageInfo(nupkg);

                    // if user requested a specific version, then do not interfere and ignore other packages with the same
                    // name but potentially different version
                    if (info.Name == parentPackage.Name && parentPackage.Version.HasText())
                        continue;

                    info.PreferredRuntime = parentPackage.PreferredRuntime;
                    add(info);
                }
            }

            foreach (var key in map.Keys)
            {
                result.Add(map[key].OrderByDescending(x => new Version(x.Version)).FirstOrDefault());
            }

            return result.Where(x => x != null).ToArray();
        }

        static IEnumerable<PackageInfo> ResolveDependenciesFor_NUSPEC(IEnumerable<PackageInfo> packages)
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

                // <dependencies>
                //   <group targetFramework=".NETStandard2.0">
                //     <dependency id="Microsoft.Extensions.Logging.Abstractions" version="2.1.0" exclude="Build,Analyzers" />
                var frameworks = dependenciesSection.FindDescendants("group");

                if (frameworks.Any())
                {
                    IEnumerable<XElement> frameworkGroups = dependenciesSection.FindDescendants("group");

                    var match = GetCompatibleTargetFramework(frameworkGroups, item);
                    dependencyPackages = match != null
                                         ? match.FindDescendants("dependency")
                                         : new XElement[0];
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

        static PackageInfo ExtractPackageInfo(string specPpath)
        {
            // Discord.Net.WebSocket.2.0.1.nupkg

            var parts = Path.GetFileNameWithoutExtension(specPpath).Split('.');

            var pkgVer = string.Join(".", parts.Where(y => y.All(char.IsDigit)).ToArray());
            var pkgName = string.Join(".", parts.Where(y => !y.All(char.IsDigit)).ToArray());

            return new PackageInfo
            {
                SpecFile = specPpath,
                Version = pkgVer,
                Name = pkgName
            };
        }

        static PackageInfo FindPackage(string name, string version)
        {
            var packageInfoFile = "*.nuspec";
            packageInfoFile = "*.nupkg"; // raw nuget.exe does not extract  nuspec (dotnet does)

            return Directory.GetDirectories(NuGetCache)
                            .SelectMany(x => Directory.GetFiles(x, packageInfoFile, SearchOption.AllDirectories)
                                                      .Select(y => ExtractPackageInfo(y)))

                            .OrderByDescending(x => x.Version)
                            .Where(x => x != null)
                            .FirstOrDefault(x => x.Name.IsSameAs(name, true) && (version.IsEmpty() || version == x.Version));

            // nuspec based algorithm from cs-script.core
            // return Directory.GetDirectories(NuGetCache)
            //                 .SelectMany(x => Directory.GetFiles(x, "*.nuspec", SearchOption.AllDirectories)
            //                                           .Select(spec =>
            //                                            {
            //                                                if (spec != null)
            //                                                {
            //                                                    var doc = XDocument.Load(spec);
            //                                                    return new PackageInfo
            //                                                    {
            //                                                        SpecFile = spec,
            //                                                        Version = doc.SelectFirst("package/metadata/version")?.Value,
            //                                                        Name = doc.SelectFirst("package/metadata/id")?.Value
            //                                                    };
            //                                                }
            //                                                return null;
            //                                            }))

            //                 .OrderByDescending(x => x.Version)
            //                 .Where(x => x != null)
            //                 .FirstOrDefault(x => x.Name == name && (version.IsEmpty() || version == x.Version));
        }

        static public string[] Resolve(string[] packages, bool suppressDownloading, string script)
        {
            // Debug.Assert(false);

            // check if custom sources are specified
            // `//css_nuget -source source1;`
            var source = packages.Where(x => x.StartsWith("-source"))
                                 .Select(x => x.Substring("-source".Length).Trim())
                                 .LastOrDefault();

            packages = packages.Where(x => !x.StartsWith("-source")).ToArray();

            List<string> assemblies = new List<string>();
            var all_packages = new List<PackageInfo>();

            bool promptPrinted = false;

            foreach (string item in packages)
            {
                // //css_nuget -noref -ng:"-IncludePrerelease –version 1.0beta" cs-script
                // //css_nuget -noref -ver:"4.1.0-alpha1" -ng:"-Pre" NLog
                string[] packageArgs = item.SplitCommandLine();

                string packageName = packageArgs.FirstOrDefault(x => !x.StartsWith("-"));

                bool suppressReferencing = packageArgs.Contains("-noref");
                string nugetArgs = packageArgs.ArgValue("-ng");
                string packageVersion = packageArgs.ArgValue("-ver");
                string preferredRuntime = packageArgs.ArgValue("-rt");
                string forceTimeoutString = packageArgs.ArgValue("-force");

                bool forceDownloading = (forceTimeoutString != null);
                uint forceTimeout = 0;
                uint.TryParse(forceTimeoutString, out forceTimeout); //'-force:<seconds>'

                var packageInfo = FindPackage(packageName, packageVersion);

                string packageDir = Path.Combine(NuGetCache, packageName);

                if (packageInfo != null && forceDownloading)
                {
                    var age = DateTime.Now.ToUniversalTime() - File.GetLastWriteTimeUtc(packageInfo.SpecFile);

                    if (age.TotalSeconds < forceTimeout)
                        forceDownloading = false;
                }

                if (suppressDownloading)
                {
                    //it is OK if the package is not downloaded (e.g. N++ Intellisense)
                    if (!suppressReferencing && packageInfo != null)
                    {
                        packageInfo.PreferredRuntime = preferredRuntime;
                        all_packages.Add(packageInfo);
                    }
                }
                else
                {
                    if (forceDownloading || packageInfo == null)
                    {
                        bool abort_downloading = Environment.GetEnvironmentVariable("NUGET_INCOMPATIBLE_HOST") != null;

                        if (abort_downloading)
                        {
                            Console.WriteLine("Warning: Resolving (installing) NuGet package has been aborted due to the incompatibility of the CS-Script host with the nuget stdout redirection.");
                            Console.WriteLine("Run the script from the terminal (e.g. Ctrl+F5 in ST3) at least once to resolve all missing NuGet packages.");
                            Console.WriteLine();
                        }
                        else
                        {
                            if (!promptPrinted)
                                Console.WriteLine("NuGet> Processing NuGet packages...");

                            promptPrinted = true;

                            try
                            {
                                if (!string.IsNullOrEmpty(packageVersion))
                                    nugetArgs = "-version \"" + packageVersion + "\" " + nugetArgs;

                                if (!string.IsNullOrEmpty(source))
                                    nugetArgs = "-source \"" + source + "\" " + nugetArgs;

                                var sw = new Stopwatch();
                                sw.Start();

                                Run(NuGetExe, string.Format("install {0} {1} -OutputDirectory \"{2}\"", packageName, nugetArgs, packageDir));
                                packageInfo = FindPackage(packageName, packageVersion);
                                newPackageWasInstalled = true;
                                sw.Stop();
                            }
                            catch
                            {
                                // the failed package willl be reported as missing reference anyway
                            }

                            try
                            {
                                Directory.SetLastWriteTimeUtc(packageDir, DateTime.Now.ToUniversalTime());
                            }
                            catch { }
                        }
                    }

                    if (packageInfo == null)
                        throw new ApplicationException("Cannot process NuGet package '" + packageName + "'");

                    if (!suppressReferencing)
                        all_packages.Add(packageInfo);
                }
            }

            foreach (PackageInfo package in ResolveDependenciesFor(all_packages))
            {
                assemblies.AddRange(GetCompatibleAssemblies(package));
            }

            return Utils.RemovePathDuplicates(assemblies.ToArray());
        }

        static XElement GetCompatibleTargetFramework(IEnumerable<XElement> freameworks, PackageInfo package)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/frameworks
            // netstandard?.?
            // net?? | net???
            // Though packages use Upper case with '.' preffix: '<group targetFramework=".NETStandard2.0">'

            Func<Predicate<string>, XElement> findMatch = (Predicate<string> matchTest) =>
                {
                    var items = freameworks.Select(x => new { Name = x.Attribute("targetFramework").Value, Element = x })
                                .OrderByDescending(x => x.Name)
                                .ToArray();

                    var match = items.FirstOrDefault(x => matchTest(x.Name ?? ""));
                    return match != null ? match.Element : null; ;
                };

            if (package.PreferredRuntime != null)
            {
                // by requested runtime
                return findMatch(x => x.Contains(package.PreferredRuntime));
            }
            else
            {
                // by .NET full as there is no other options
                return findMatch(x => (x.StartsWith("net") || x.StartsWith(".net"))
                                       && !x.Contains("netcore")
                                       && !x.Contains("netstandard"))
                       ?? findMatch(x => x.Contains("netstandard"));
            }
        }

        /// <summary>
        /// Gets the package compatible library. Similar to `GetCompatibleTargetFramework` but relies on file structure
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
                                      .Select(x => new { Runtime = Path.GetFileName(x), Path = x });

            if (package.PreferredRuntime != null)
            {
                var match = frameworks.FirstOrDefault(x => x.Runtime.EndsWith(package.PreferredRuntime));

                return match != null ? match.Path : null;
            }
            else
            {
                var match = frameworks.FirstOrDefault(x => x.Runtime.StartsWith("net")
                                                      && !x.Runtime.StartsWith("netcore")
                                                      && !x.Runtime.StartsWith("netstandard"))
                        ?? frameworks.FirstOrDefault(x => x.Runtime.StartsWith("netstandard"));

                return match != null ? match.Path : null;
            }
        }

        static string[] GetCompatibleAssemblies(PackageInfo package)
        {
            var lib = GetPackageCompatibleLib(package);

            if (lib != null)
                return Directory.GetFiles(GetPackageCompatibleLib(package), "*.dll")
                    .Where(item => !item.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                    .Where(x => Runtime.IsRuntimeCompatibleAsm(x))
                    .ToArray();
            else
                return new string[0];
        }

        public static void InstallPackage(string packageNameMask)
        {
            var packages = new string[0];
            int index = 0;
            //index is 1-based, exactly as it is printed with ListPackages
            if (int.TryParse(packageNameMask, out index))
            {
                var all_packages = GetLocalPackages();

                if (0 < index && index <= all_packages.Count())
                    packages = new string[] { all_packages[index - 1] };
                else
                    Console.WriteLine("There is no package with the specified index");
            }
            else
                packages = Directory.GetDirectories(NuGetCache, packageNameMask);

            foreach (string dir in packages)
            {
                string name = Path.GetFileName(dir);
                Console.WriteLine("Installing " + name + " package...");
                Run(NuGetExe, "install " + name + " -OutputDirectory " + Path.Combine(NuGetCache, name));
                Console.WriteLine("");
            }
        }

        public static void ListPackages()
        {
            Console.WriteLine("Repository: " + NuGetCache);

            int i = 0;

            foreach (string name in GetLocalPackages())
                Console.WriteLine((++i) + ". " + name);
        }

        static string[] GetLocalPackages()
        {
            return Directory.GetDirectories(NuGetCache)
                            .Select(x => Path.GetFileName(x))
                            .ToArray();
        }

        static string GetPackageName(string path)
        {
            var result = Path.GetFileName(path);

            //WixSharp.bin.1.0.30.4-HotFix
            int i = 0;
            char? prev = null;

            for (; i < result.Length; i++)
            {
                char current = result[i];

                if ((prev.HasValue && prev == '.') && char.IsDigit(current))
                {
                    i = i - 2; //-currPos-prevPos
                    break;
                }
                prev = current;
            }

            result = result.Substring(0, i + 1); //i-inclusive
            return result;
        }

        static bool IsPackageDir(string dirPath, string packageName)
        {
            var dirName = Path.GetFileName(dirPath);

            if (dirName.IsSamePath(packageName))
            {
                return true;
            }
            else
            {
                if (dirName.StartsWith(packageName + ".", StringComparison.OrdinalIgnoreCase))
                {
                    var version = dirName.Substring(packageName.Length + 1);
#if net4
                    Version ver;
                    return Version.TryParse(version, out ver);
#else
                    try
                    {
                        new Version(version);
                        return true;
                    }
                    catch { }
                    return false;
#endif
                }
            }
            return false;
        }

        static public string[] GetPackageDependencies(string rootDir, string package)
        {
            var packages = Directory.GetDirectories(rootDir)
                                    .Select(x => GetPackageName(x))
                                    .Where(x => x != package)
                                    .Distinct()
                                    .ToArray();

            return packages;
        }

        public class PackageInfo
        {
            public string Version;
            public string PreferredRuntime;
            public string Name;
            public string SpecFile;
        }

        static public string[] GetPackageLibDirs(PackageInfo package)
        {
            List<string> result = new List<string>();

            //cs-script will always store dependency packages in the package root directory:
            //
            //C:\ProgramData\CS-Script\nuget\WixSharp\WixSharp.1.0.30.4
            //C:\ProgramData\CS-Script\nuget\WixSharp\WixSharp.bin.1.0.30.4

            string packageDir = Path.Combine(NuGetCache, package.Name);

            result.AddRange(GetSinglePackageLibDirs(package));

            foreach (string dependency in GetPackageDependencies(packageDir, package.Name))
                result.AddRange(GetSinglePackageLibDirs(new PackageInfo { Name = dependency }, packageDir)); //do not assume the dependency has the same version as the major package; Get the latest instead

            return result.ToArray();
        }

        static public string[] GetSinglePackageLibDirs(PackageInfo package)
        {
            return GetSinglePackageLibDirs(package, null);
        }

        /// <summary>
        /// Gets the single package library dirs.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="rootDir">The root dir.</param>
        /// <returns></returns>
        static public string[] GetSinglePackageLibDirs(PackageInfo package, string rootDir)
        {
            List<string> result = new List<string>();

            string packageDir = rootDir ?? Path.Combine(NuGetCache, package.Name);

            string requiredVersion;

            if (!string.IsNullOrEmpty(package.Version))
                requiredVersion = Path.Combine(packageDir, Path.GetFileName(package.Name) + "." + package.Version);
            else
                requiredVersion = Directory.GetDirectories(packageDir)
                                           .Where(x => IsPackageDir(x, package.Name))
                                           .OrderByDescending(x => x)
                                           .FirstOrDefault();

            string lib = Path.Combine(requiredVersion, "lib");

            if (!Directory.Exists(lib))
                return result.ToArray();

            string compatibleVersion = null;

            if (Directory.GetFiles(lib, "*.dll").Any())
                result.Add(lib);

            if (package.PreferredRuntime.HasText())
                return Directory.GetDirectories(lib, package.PreferredRuntime);

            var libVersions = Directory.GetDirectories(lib, "net*");

            if (libVersions.Length != 0)
            {
                Func<string, string, bool> compatibleWith = (x, y) =>
                {
                    return x.StartsWith(y, StringComparison.OrdinalIgnoreCase) || x.IndexOf(y, StringComparison.OrdinalIgnoreCase) != -1;
                };

                if (Runtime.IsNet45Plus())
                    compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "net45"));

                if (compatibleVersion == null && Runtime.IsNet40Plus())
                    compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "net40"));

                if (compatibleVersion == null && Runtime.IsNet20Plus())
                {
                    compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "net35"));

                    if (compatibleVersion == null)
                        compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "net30"));

                    if (compatibleVersion == null)
                        compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "net20"));
                }

                if (compatibleVersion == null)
                    compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "netstandard"));

                if (compatibleVersion == null)
                {
                    // It's the last chance to find the compatible version. Basically pick any...
                    compatibleVersion = libVersions.OrderBy(x =>
                                                            {
                                                                int ver = 0;
                                                                int.TryParse(Regex.Match(x, @"\d+").Value, out ver);
                                                                return ver;
                                                            }).First();
                    result.Add(compatibleVersion);
                }

                if (compatibleVersion != null)
                    result.Add(compatibleVersion);
            }

            return result.ToArray();
        }

        static Thread StartMonitor(StreamReader stream)
        {
            var retval = new Thread(x =>
            {
                try
                {
                    string line = null;

                    while (null != (line = stream.ReadLine()))
                    {
                        Console.WriteLine(line);
                    }
                }
                catch { }
            });
            retval.Start();
            return retval;
        }

        static void Run(string exe, string args)
        {
            //http://stackoverflow.com/questions/38118548/how-to-install-nuget-from-command-line-on-linux
            //on Linux native "nuget" app doesn't play nice with std.out redirected

            Console.WriteLine("NuGet shell command: ");
            Console.WriteLine("{0} {1}", exe, args);
            Console.WriteLine();

            if (Runtime.IsLinux)
            {
                Process.Start(exe, args).WaitForExit();
            }
            else
                using (var p = new Process())
                {
                    p.StartInfo.FileName = exe;
                    p.StartInfo.Arguments = args;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();

                    var error = StartMonitor(p.StandardError);
                    var output = StartMonitor(p.StandardOutput);

                    p.WaitForExit();

                    error.Abort();
                    output.Abort();
                }
        }
    }
}