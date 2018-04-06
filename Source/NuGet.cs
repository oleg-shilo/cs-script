using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

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
                    if (Utils.IsLinux())
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
                                                .Split(Utils.IsLinux() ? ':' : ';')
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

        static public string[] Resolve(string[] packages, bool suppressDownloading, string script)
        {
            List<string> assemblies = new List<string>();

            bool promptPrinted = false;
            foreach (string item in packages)
            {
                // //css_nuget -noref -ng:"-IncludePrerelease –version 1.0beta" cs-script
                // //css_nuget -noref -ver:"4.1.0-alpha1" -ng:"-Pre" NLog

                string package = item;
                string nugetArgs = "";
                string packageVersion = "";

                bool suppressReferencing = item.StartsWith("-noref");
                if (suppressReferencing)
                    package = item.Replace("-noref", "").Trim();

                bool forceDownloading = item.StartsWith("-force");
                uint forceTimeout = 0;

                if (forceDownloading)
                {
                    if (item.StartsWith("-force:")) //'-force:<seconds>'
                    {
                        var pos = item.IndexOf(" ");
                        if (pos != -1)
                        {
                            var forceStatement = item.Substring(0, pos);
                            package = item.Replace(forceStatement, "").Trim();

                            string timeout = forceStatement.Replace("-force:", "");
                            forceTimeout = 0;
                            uint.TryParse(timeout, out forceTimeout);
                        }
                        else
                        {
                            //the syntax is wrong; let it fail
                        }
                    }
                    else
                    {
                        package = item.Replace("-force", "").Trim();
                    }
                }

                if (package.StartsWith("-ver:"))
                {
                    package = package.Replace("-ver:", "");
                    int argEnd = package.IndexOf(" ");

                    if (package.StartsWith("\""))
                    {
                        package = package.Substring(1, package.Length - 1);
                        argEnd = package.IndexOf("\"");
                    }
                    if (argEnd != -1)
                    {
                        packageVersion = package.Substring(0, argEnd).Trim();

                        if (package[argEnd] == '"')
                            package = package.Substring(argEnd + 1).Trim();
                        else
                            package = package.Substring(argEnd).Trim();
                    }
                }

                int nameStart = package.LastIndexOf(" ");
                if (nameStart != -1)
                {
                    if (package.StartsWith("-ng:"))
                    {
                        nugetArgs = package.Substring(0, nameStart).Replace("-ng:", "").Trim();
                        if (nugetArgs.StartsWith("\"") && nugetArgs.EndsWith("\""))
                            nugetArgs = nugetArgs.Substring(1, nugetArgs.Length - 2);
                    }
                    package = package.Substring(nameStart).Trim();
                }

                string packageDir = Path.Combine(NuGetCache, package);

                if (Directory.Exists(packageDir) && forceDownloading)
                {
                    var age = DateTime.Now.ToUniversalTime() - Directory.GetLastWriteTimeUtc(packageDir);
                    if (age.TotalSeconds < forceTimeout)
                        forceDownloading = false;
                }

                if (suppressDownloading)
                {
                    //it is OK if the package is not downloaded (e.g. N++ intellisense)
                    if (!suppressReferencing && IsPackageDownloaded(packageDir, packageVersion))
                        assemblies.AddRange(GetPackageLibDlls(package, packageVersion));
                }
                else
                {
                    if (forceDownloading || !IsPackageDownloaded(packageDir, packageVersion))
                    {
                        bool abort_downloading = Environment.GetEnvironmentVariable("NUGET_INCOMPATIBLE_HOST") != null;

                        if (abort_downloading)
                        {
                            Console.WriteLine("Warning: Resolving (installing) NuGet package has been aborted due to the incompatibility of the CS-Script host with the nuget stdout redirection.\n" +
                                              "Run the script from the terminal (e.g. Ctrl+F5 in ST3) at least once to resolve all missing NuGet packages.");
                            Console.WriteLine();
                        }
                        else
                        {
                            if (!promptPrinted)
                                Console.WriteLine("NuGet> Processing NuGet packages...");

                            promptPrinted = true;

                            try
                            {
                                if (packageVersion != "")
                                    nugetArgs = "-version \"" + packageVersion + "\" " + nugetArgs;
                                var sw = new Stopwatch();
                                sw.Start();

                                Run(NuGetExe, "install " + package + " " + nugetArgs + " -OutputDirectory " + packageDir);
                                newPackageWasInstalled = true;
                                sw.Stop();
                            }
                            catch { }

                            try
                            {
                                Directory.SetLastWriteTimeUtc(packageDir, DateTime.Now.ToUniversalTime());
                            }
                            catch { }
                        }
                    }

                    if (!IsPackageDownloaded(packageDir, packageVersion))
                        throw new ApplicationException("Cannot process NuGet package '" + package + "'");

                    if (!suppressReferencing)
                        assemblies.AddRange(GetPackageLibDlls(package, packageVersion));
                }
            }

            return Utils.RemovePathDuplicates(assemblies.ToArray());
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

        static public string[] GetPackageLibDirs(string package, string version)
        {
            List<string> result = new List<string>();

            //cs-script will always store dependency packages in the package root directory:
            //
            //C:\ProgramData\CS-Script\nuget\WixSharp\WixSharp.1.0.30.4
            //C:\ProgramData\CS-Script\nuget\WixSharp\WixSharp.bin.1.0.30.4

            string packageDir = Path.Combine(NuGetCache, package);

            result.AddRange(GetSinglePackageLibDirs(package, version));

            foreach (string dependency in GetPackageDependencies(packageDir, package))
                result.AddRange(GetSinglePackageLibDirs(dependency, "", packageDir)); //do not assume the dependency has the same version as the major package; Get the latest instead

            return result.ToArray();
        }

        static public string[] GetSinglePackageLibDirs(string package, string version)
        {
            return GetSinglePackageLibDirs(package, version, null);
        }

        /// <summary>
        /// Gets the single package library dirs.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="version">The version.</param>
        /// <param name="rootDir">The root dir.</param>
        /// <returns></returns>
        static public string[] GetSinglePackageLibDirs(string package, string version, string rootDir)
        {
            List<string> result = new List<string>();

            string packageDir = rootDir ?? Path.Combine(NuGetCache, package);

            string requiredVersion;

            if (!string.IsNullOrEmpty(version))
                requiredVersion = Path.Combine(packageDir, Path.GetFileName(package) + "." + version);
            else
                requiredVersion = Directory.GetDirectories(packageDir)
                                           .Where(x => IsPackageDir(x, package))
                                           .OrderByDescending(x => x)
                                           .FirstOrDefault();

            string lib = Path.Combine(requiredVersion, "lib");

            if (!Directory.Exists(lib))
                return result.ToArray();

            string compatibleVersion = null;
            if (Directory.GetFiles(lib, "*.dll").Any())
                result.Add(lib);

            var libVersions = Directory.GetDirectories(lib, "net*");

            if (libVersions.Length != 0)
            {
                Func<string, string, bool> compatibleWith = (x, y) =>
                {
                    return x.StartsWith(y, StringComparison.OrdinalIgnoreCase) || x.IndexOf(y, StringComparison.OrdinalIgnoreCase) != -1;
                };

                if (Utils.IsNet45Plus())
                    compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "net45"));

                if (compatibleVersion == null && Utils.IsNet40Plus())
                    compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "net40"));

                if (compatibleVersion == null && Utils.IsNet20Plus())
                {
                    compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "net35"));

                    if (compatibleVersion == null)
                        compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "net30"));

                    if (compatibleVersion == null)
                        compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "net20"));
                }

                if (compatibleVersion == null)
                    compatibleVersion = libVersions.FirstOrDefault(x => compatibleWith(Path.GetFileName(x), "netstandard"));

                if (compatibleVersion != null)
                    result.Add(compatibleVersion);
            }

            return result.ToArray();
        }

        static string[] GetPackageLibDlls(string package, string version)
        {
            List<string> dlls = new List<string>();
            foreach (string dir in GetPackageLibDirs(package, version))
                dlls.AddRange(Directory.GetFiles(dir, "*.dll"));

            List<string> assemblies = new List<string>();

            foreach (var item in dlls)
            {
                //official NuGet documentation states that .resources.dll is not references so we do the same
                if (!item.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                {
                    if (Utils.IsRuntimeCompatibleAsm(item))
                        assemblies.Add(item);
                }
            }

            return assemblies.ToArray();
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
            //on linux native "nuget" app doesn't play nice with std.out redirected

            if (Utils.IsLinux())
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