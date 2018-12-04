using CSScripting.CodeDom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace csscript
{
    class PackageInfo
    {
        public string SpecFile;
        public string Version;
        public string PreferredRuntime;
        public string Name;
    }

    class NuGetCore : INuGet
    {
        //https://docs.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders
        // .NET Mono, .NET Core
        public string NuGetCache => Utils.IsWin ?
                                          Environment.ExpandEnvironmentVariables(@"%userprofile%\.nuget\packages") :
                                          "~/.nuget/packages";

        public string NuGetExe => "dotnet";

        public bool NewPackageWasInstalled { get; set; }

        public void InstallPackage(string packageNameMask, string version = null)
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
                // Regex is too much at this stage
                // string pattern = CSSUtils.ConvertSimpleExpToRegExp();
                // Regex wildcard = new Regex(pattern, RegexOptions.IgnoreCase);

                if (packageNameMask.EndsWith("*"))
                    packages = ListPackages().Where(x => x.StartsWith(packageNameMask.Substring(0, packageNameMask.Length - 1))).ToArray();
                else
                    packages = new[] { packageNameMask };
            }

            // C:\Users\user\AppData\Local\Temp\csscript.core\.nuget\333
            var nuget_dir = CSExecutor.GetScriptTempDir()
                                      .PathJoin(".nuget", Process.GetCurrentProcess().Id)
                                      .EnsureDir();

            try
            {
                var proj_template = nuget_dir.PathJoin("build.csproj");

                if (!File.Exists(proj_template))
                {
                    Utils.Run("dotnet", "new console", nuget_dir);
                    foreach (var name in packages)
                    {
                        var ver = "";
                        if (version != null)
                            ver = "-v " + version;
                        Utils.Run("dotnet", $"add package {name} {ver}", nuget_dir, x => Console.WriteLine(x));
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

        void ClearAnabdonedNugetDirs(string nuget_root)
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

        string[] GetPackageAssemblies(PackageInfo package)
        {
            var frameworks = Directory.GetDirectories(package.SpecFile.GetDirName().PathJoin("lib"))
                                      .OrderByDescending(x => x);
            string lib = null;
            if (package.PreferredRuntime != null)
            {
                lib = frameworks.FirstOrDefault(x => x.EndsWith(package.PreferredRuntime));
            }
            else
            {
                if (CSharpCompiler.DefaultCompilerRuntime == DefaultCompilerRuntime.Standard)
                {
                    lib = frameworks.FirstOrDefault(x => x.GetFileName().StartsWith("netstandard"));
                }
                else // host runtime
                {
                    if (Utils.IsCore)
                        lib = frameworks.FirstOrDefault(x => x.GetFileName().StartsWith("netcore"));
                    else
                        lib = frameworks.FirstOrDefault(x =>
                                                        {
                                                            var runtime = x.GetFileName();
                                                            return runtime.StartsWith("net") && !runtime.StartsWith("netcore") && !runtime.StartsWith("netstandard");
                                                        });
                }
            }

            if (lib == null)
                lib = frameworks.FirstOrDefault(x => x.GetFileName().StartsWith("netstandard"));

            if (lib != null)
            {
                var asms = Directory.GetFiles(lib, "*.dll");
                return asms;
            }
            else
                return new string[0];
        }

        public string[] ListPackages()
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

        PackageInfo FindPackage(string name, string version)
        {
            return Directory.GetDirectories(NuGetCache)
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
                            .FirstOrDefault(x => x.Name == name && (version.IsEmpty() || version == x.Version));
        }

        public string[] Resolve(string[] packages, bool suppressDownloading, string script)
        {
            List<string> assemblies = new List<string>();

            bool promptPrinted = false;
            foreach (string item in packages)
            {
                // //css_nuget -noref -ng:"-IncludePrerelease –version 1.0beta" cs-script
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
                        assemblies.AddRange(GetPackageAssemblies(package_info));
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
                            InstallPackage(package, packageVersion);
                            package_info = FindPackage(package, packageVersion);
                            this.NewPackageWasInstalled = true;
                        }
                        catch { }

                        try
                        {
                            File.SetLastWriteTimeUtc(package_info.SpecFile, DateTime.Now.ToUniversalTime());
                        }
                        catch { }
                    }

                    if (package_info == null)
                        throw new ApplicationException("Cannot process NuGet package '" + package + "'");

                    if (!suppressReferencing)
                    {
                        package_info.PreferredRuntime = packageArgs.ArgValue("-rt");
                        assemblies.AddRange(GetPackageAssemblies(package_info));
                    }
                }
            }

            return Utils.RemovePathDuplicates(assemblies.ToArray());
        }
    }
}