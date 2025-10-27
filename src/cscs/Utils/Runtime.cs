using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using CSScripting;

#if class_lib

namespace CSScriptLib
#else

namespace csscript
#endif
{
    /// <summary>
    /// A class that hosts the most common properties of the runtime environment.
    /// </summary>
    public static class Runtime
    {
        /// <summary>
        /// Occurs when an exception is not caught.
        /// </summary>
        static public event UnhandledExceptionEventHandler UnhandledException;

        internal static string CacheDir = GetScriptTempDir().PathJoin("cache");

        // CSScriptLib does not support the cache directory for class library projects.
#if !class_lib

        // summary>
        /// Gets the cache directory path for a given script file.
        /// <para>
        /// Note that the cache directory is not the same as the script directory. The cache
        /// directory is used to store compiled assemblies and other artifacts related to script
        /// execution.
        /// </para>
        /// </summary>
        /// <param name="scriptFileName">Name of the script file.</param>
        /// <returns>The cache directory path.</returns>
        public static string GetCacheDir(string scriptFileName) => csscript.CSExecutor.GetCacheDirectory(scriptFileName);

#endif

        internal static bool RaiseUnhandledExceptionIfSubscribed(object sender, UnhandledExceptionEventArgs e)
        {
            var handlers = UnhandledException?.GetInvocationList();

            if (handlers?.Any() == true)
            {
                handlers.ForEach(x => x.DynamicInvoke(sender, e));
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Returns the name of the temporary folder in the CSSCRIPT subfolder of Path.GetTempPath().
        /// <para>
        /// Under certain circumstances it may be desirable to the use the alternative location for
        /// the CS-Script temporary files. In such cases use SetScriptTempDir() to set the
        /// alternative location.
        /// </para>
        /// </summary>
        /// <returns>Temporary directory name.</returns>
        static public string GetScriptTempDir()
        {
            if (tempDir == null)
            {
                tempDir = Environment.GetEnvironmentVariable("CSS_CUSTOM_TEMPDIR") ??
                          Path.GetTempPath().PathJoin("csscript.core");

                tempDir.EnsureDir();
            }
            return tempDir;
        }

        static string tempDir = null;

        /// <summary>
        /// Cleans the abandoned script execution cache.
        /// </summary>
        public static void CleanAbandonedCache()
        {
            var rootDir = Runtime.CacheDir;

            if (Directory.Exists(rootDir))
            {
                foreach (var cacheDir in Directory.GetDirectories(rootDir))
                    try
                    {
                        // line 0: <clr version> line 1: <dir>
                        var infoFile = cacheDir.PathJoin("css_info.txt");
                        var sourceDir = File.Exists(infoFile) ? File.ReadAllLines(infoFile)[1] : null;

                        if (sourceDir.IsEmpty() || !Directory.Exists(sourceDir))
                        {
                            cacheDir.DeleteDir();
                        }
                        else
                        {
                            string sourceName(string path) =>
                                path.GetFileNameWithoutExtension(); // remove `.dll` in `script.cs.dll`

                            var sorceFiles = Directory.GetFiles(cacheDir, "*.dll")
                                                      .Select(x => new
                                                      {
                                                          Source = sourceDir.PathJoin(sourceName(x)),
                                                          PureName = x.GetFileName().Split('.').First(),
                                                      })
                                                      .ToArray();

                            sorceFiles.Where(x => !File.Exists(x.Source))
                                      .ForEach(file => Directory.GetFiles(cacheDir, $"{file.PureName}.*")
                                                                .ForEach(x =>
                                                                {
                                                                    x.FileDelete(rethrow: false);
                                                                }));
                        }
                    }
                    catch { }
            }
        }

#if !class_lib

        internal static ((string index, int pid)[] scripts, string view) GetScriptProcessLog()
        {
            var result = new List<(string index, int pid)>();

            var currentProcId = Process.GetCurrentProcess().Id;
            var i = 0;

            var builder = new StringBuilder();
            builder.AppendLine($"#  | PID        | Arguments");
            builder.AppendLine($"----------------------------");
            // foreach ((int pid, string args) in Runtime.GetScriptProcesses().OrderByDescending(x => x.pid == currentProcId))
            foreach ((int pid, string args) in Runtime.GetScriptProcesses().Where(x => x.pid != currentProcId).OrderDescending())
            {
                builder.AppendLine($"{++i:D2} | {pid:D10} | {args}");
                result.Add((i.ToString(), pid));
            }

            return (result.ToArray(), builder.ToString());
        }

        static (int pid, string args)[] GetScriptProcesses()
        {
            (int pid, string args)[] result = null;

            var filePath = Environment.SpecialFolder.LocalApplicationData.GetPath().PathJoin("cs-script", "p-list");

            if (DisableCSScriptProcessTrackingEnvar.GetEnvar().IsNotEmpty())
            {
                filePath.DeleteIfExists();
                result = [(0, "<script tracking is disabled>")];
            }
            else
            {
                string mutexName = "Global\\CSScriptProcessListMutex"; // Global name for cross-process synchronization

                List<string> lines = null;
                using (var mutex = new Mutex(false, mutexName))
                {
                    try
                    {
                        mutex.WaitOne();

                        try
                        {
                            if (filePath.FileExists())
                                lines = File.ReadAllLines(filePath).ToList();
                        }
                        catch { }
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }

                if (lines == null)
                    result = [(0, "<script tracking information is not available>")];
                else
                    result = lines.Select(x =>
                                          {
                                              var parts = x.Split(':', 2);
                                              try
                                              {
                                                  if (int.TryParse(parts.First(), out int pid))
                                                      return (pid, parts.Last()?.Trim());
                                              }
                                              catch
                                              {
                                              }
                                              return (0, "<invalid script tracking information>");
                                          })
                                  .Where(x =>
                                         {
                                             try
                                             {
                                                 return Process.GetProcessById(x.Item1) != null;
                                             }
                                             catch { return false; }
                                         })
                                  .ToArray();
            }

            return result;
        }

        internal static void ClearScriptProcessLog()
        {
            var filePath = Environment.SpecialFolder.LocalApplicationData.GetPath().PathJoin("cs-script", "p-list");

            if (DisableCSScriptProcessTrackingEnvar.GetEnvar().IsNotEmpty())
            {
                filePath.DeleteIfExists();
            }
            else
            {
                string mutexName = "Global\\CSScriptProcessListMutex"; // Global name for cross-process synchronization

                using (var mutex = new Mutex(false, mutexName))
                {
                    try
                    {
                        mutex.WaitOne();

                        List<string> lines = null;

                        try
                        {
                            if (filePath.FileExists())
                                lines = File.ReadAllLines(filePath)
                                            .Where(x =>
                                            {
                                                try
                                                {
                                                    return Process.GetProcessById(int.Parse(x.Split(':', 2)[0])) != null;
                                                }
                                                catch { return false; }
                                            })
                                            .ToList();
                        }
                        catch { }

                        if (lines != null)
                            File.WriteAllLines(filePath, lines);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
        }

        internal static string DisableCSScriptProcessTrackingEnvar = "DisableCSScriptProcessTracking";

        internal static void LogScriptProcess()
        {
            var filePath = Environment.SpecialFolder.LocalApplicationData.GetPath().PathJoin("cs-script", "p-list");

            if (DisableCSScriptProcessTrackingEnvar.GetEnvar().IsNotEmpty())
            {
                filePath.DeleteIfExists();
            }
            else
            {
                string mutexName = "Global\\CSScriptProcessListMutex"; // Global name for cross-process synchronization

                using (var mutex = new Mutex(false, mutexName))
                {
                    try
                    {
                        mutex.WaitOne();
                        filePath.EnsureFileDir();
                        var currentScript = $"{Process.GetCurrentProcess().Id}: {Environment.CommandLine}";

                        List<string> lines = null;

                        try
                        {
                            if (filePath.FileExists())
                                lines = File.ReadAllLines(filePath)
                                            .Where(x =>
                                            {
                                                try
                                                {
                                                    return Process.GetProcessById(int.Parse(x.Split(':', 2)[0])) != null;
                                                }
                                                catch { return false; }
                                            })
                                            .ToList();
                        }
                        catch { }

                        lines ??= [];

                        lines.Add(currentScript);

                        File.WriteAllLines(filePath, lines.ToArray());
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
        }

#endif

        /// <summary>
        /// Cleans the exited scripts.
        /// </summary>
        public static void CleanExitedScripts()
        {
            var rootDir = Runtime.CacheDir.GetDirName();

            if (Directory.Exists(rootDir))
            {
                var activeProcesses = Process.GetProcesses()
                    // .Where(x => x.Id != Process.GetCurrentProcess().Id) // we exiting so clean the scripts for the current process too
                    .Select(x => x.Id.ToString());

                foreach (var file in Directory.GetFiles(rootDir))
                    try
                    {
                        // <pid>.<script_id>.*
                        var pid = file.GetFileName().Split('.').FirstOrDefault();

                        if (pid != null && !activeProcesses.Contains(pid))
                            file.DeleteIfExists();

                        var containerDir = file + ".container";
                        containerDir.DeleteIfExists();
                    }
                    catch { }
            }
        }

        internal static bool IsConsole
        {
            get
            {
                try
                {
                    var test = Console.WindowWidth.ToString();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// The delegate for creating unloadable <see cref="AssemblyLoadContext"/> assembly load
        /// context. The delegate is required to be set from the host process at runtime. It is
        /// because it is not available at compile time since CSScriptLib assembly is compiled as
        /// '.NET Standard 2.0' which does not implement <see cref="AssemblyLoadContext"/> but its
        /// abstract type only.
        /// <para>
        /// CS-Script uses intensive reflection technique to retrieve the host environment <see
        /// cref="AssemblyLoadContext"/> implementation. So you no not need to set it.
        /// </para>
        /// </summary>
        internal static Func<AssemblyLoadContext> CreateUnloadableAssemblyLoadContext;

        /// <summary>
        /// Cleans the snippets.
        /// </summary>
        public static void CleanSnippets()
        {
            // CSScript.GetScriptTempDir()
            var dir = Runtime.GetScriptTempDir().PathJoin("snippets");

            if (!Directory.Exists(dir))
                return;

            var runningProcesses = Process.GetProcesses().Select(x => x.Id);

            foreach (var script in Directory.GetFiles(dir, "*.*.cs"))
            {
                int hostProcessId = int.Parse(script.GetFileName().Split('.')[1]);
                if (Process.GetCurrentProcess().Id == hostProcessId || !runningProcesses.Contains(hostProcessId))
                    script.FileDelete(rethrow: false);
            }
        }

        /// <summary>
        /// Cleans the unused temporary files.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="verifyPid">if set to <c>true</c> [verify pid].</param>
        public static void CleanUnusedTmpFiles(string dir, string pattern, bool verifyPid)
        {
            if (!Directory.Exists(dir))
                return;

            string[] oldTempFiles = Directory.GetFiles(dir, pattern);

            foreach (string file in oldTempFiles)
            {
                if (verifyPid)
                {
                    if (!file.IsParentProcessRunning())
                        file.FileDelete(false);
                }
                else
                    file.FileDelete(false);
            }
        }

        /// <summary>
        /// Gets the nuget cache path in the form displayable in Console.
        /// </summary>
        /// <value>The nu get cache view.</value>
        static public string NuGetCacheView => "<not defined>";

        /// <summary>
        /// Gets a value indicating whether the host OS Windows.
        /// </summary>
        /// <value><c>true</c> if the host OS is Windows; otherwise, <c>false</c>.</value>
        public static bool IsWin => !IsLinux;

#if !class_lib
        static string rid;

        public static string TFM => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Replace(" ", "").ToLower().Substring(1).Split('.').Take(2).JoinBy(".");
        public static string TFM_Any => "any";

        public static string RID
        {
            get
            {
                if (rid.HasText())
                    return rid;

                // Determine OS Platform
                string os;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    os = "win";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    os = "linux";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    os = "osx";
                else
                    os = "unknown";

                // Determine Architecture
                string architecture = RuntimeInformation.OSArchitecture switch
                {
                    Architecture.X86 => "x86",
                    Architecture.X64 => "x64",
                    Architecture.Arm => "arm",
                    Architecture.Arm64 => "arm64",
                    _ => "unknown"
                };

                // Compose RID
                rid = $"{os}-{architecture}";
                return rid;
            }
        }

        public static string RID_cpu_nutral
        {
            get
            {
                if (rid.HasText())
                    return rid;

                // Determine OS Platform
                string os;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    os = "win";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    os = "linux";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    os = "osx";
                else
                    os = "unknown";

                // Compose RID
                rid = $"{os}";
                return rid;
            }
        }

#endif

        /// <summary>
        /// Note it is not about OS being exactly Linux but rather about OS having Linux type of
        /// file system. For example path being case sensitive
        /// </summary>
        public static bool IsLinux { get; } = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX);

        /// <summary>
        /// Gets a value indicating whether this process is an application compiled as a single file (published with PublishSingleFile option).
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is single file application; otherwise, <c>false</c>.
        /// </value>
        public static bool IsSingleFileApplication { get; } = "".GetType().Assembly.Location.IsEmpty();

        /// <summary>
        /// Gets a value indicating whether the runtime is core.
        /// </summary>
        /// <value><c>true</c> if the runtime is core; otherwise, <c>false</c>.</value>
        public static bool IsCore { get; } = "".GetType().Assembly.Location.Split(Path.DirectorySeparatorChar).Contains("Microsoft.NETCore.App");

        /// <summary>
        /// Gets a value indicating whether CS-Script is installed globally (system-wide) rather than locally.
        /// <para>
        /// This property checks if the current assembly is located in system-wide installation directories
        /// such as those used by package managers like Chocolatey, dotnet tool, Scoop, WinGet, or Linux package managers.
        /// </para>
        /// <para>
        /// The following installation paths are considered global:
        /// <list type="bullet">
        /// <item><description>Common Application Data directory (Chocolatey, dotnet tool)</description></item>
        /// <item><description>.dotnet tools store directory (dotnet tool global installs)</description></item>
        /// <item><description>User Profile directory (Scoop, WinGet)</description></item>
        /// <item><description>/usr/local/bin/cs-script (Linux Debian package)</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <value>
        ///   <c>true</c> if CS-Script is installed globally; otherwise, <c>false</c> for local installations.
        /// </value>
        public static bool IsGloballyInstalled
        {
            get
            {
                var thisAsm = "CSS_ENTRY_ASM".GetEnvar();
                if (thisAsm.IsEmpty())
                    thisAsm = Assembly.GetExecutingAssembly().Location();

                // check if deployed in system wide location
                var systemWideInstall =
                    thisAsm.StartsWith(Environment.SpecialFolder.CommonApplicationData.GetPath()) // will cover chocolatey and dotnet-tool
                    || thisAsm.Contains(".dotnet".PathJoin("tools", ".store", "cs-script.cli"))
                    || thisAsm.StartsWith(Environment.SpecialFolder.UserProfile.GetPath())       // will cover Scoop and WinGet
                    || thisAsm.Contains("/usr/local/bin/cs-script"); // Linux global install (Debian package)

                return systemWideInstall;
            }
        }

        internal static bool IsScoopInstalled = Assembly.GetExecutingAssembly().Location().StartsWith(Environment.SpecialFolder.UserProfile.GetPath().PathJoin("scoop", "apps", "cs-script"));

        static internal string CustomCommandsDir
            => "CSSCRIPT_COMMANDS".GetEnvar() ??
                Environment.SpecialFolder.CommonApplicationData.GetPath()
                                         .PathJoin("cs-script", "commands")
                                         .EnsureDir(false);

        static internal string DefaultCommandsDir
          => "CSSCRIPT_ROOT".GetEnvar() ??
             "ENTRY_ASM".GetEnvar().GetDirName() ??
             Assembly.GetExecutingAssembly().Location()?.GetDirName();

        static internal string GlobalIncludsDir
        {
            get
            {
                try
                {
                    var globalIncluds = Environment.GetEnvironmentVariable("CSSCRIPT_INC");
                    if (globalIncluds.IsNotEmpty())
                        return globalIncluds.EnsureDir();

                    return Environment.SpecialFolder.CommonApplicationData.GetPath()
                                                    .PathJoin("cs-script", "inc")
                                                    .With(x =>
                                                    {
                                                        Environment.SetEnvironmentVariable("CSSCRIPT_INC", x, EnvironmentVariableTarget.User);
                                                    })
                                                    .EnsureDir(rethrow: false);
                }
                catch { return null; }
            }
        }

        /// <summary>
        /// Returns path to the `Microsoft.WindowsDesktop.App` shared assemblies of the compatible
        /// runtime version.
        /// <para>
        /// Note, there is no warranty that the dotnet dedktop assemblies belong to the same distro
        /// version as dotnet Core:
        /// <para>- C:\Program Files\dotnet\shared\Microsoft.NETCore.App\5.0.0-rc.1.20451.14</para>
        /// <para>- C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\5.0.0-rc.1.20452.2</para>
        /// </para>
        /// </summary>
        public static string DesktopAssembliesDir
        {
            get => FindLatestSharedDir("Microsoft.WindowsDesktop.App");
        }

        /// <summary>
        /// Returns path to the `Microsoft.AspNetCore.App` shared assemblies of the compatible
        /// runtime version.
        /// </summary>
        public static string WebAssembliesDir
        {
            get => FindLatestSharedDir("Microsoft.AspNetCore.App");
        }

        static string FindLatestSharedDir(string appType)
        {
            try
            {
                // There is no warranty that the dotnet dedktop assemblies belong to the same distro
                // version as dotnet Core: C:\Program
                // Files\dotnet\shared\Microsoft.NETCore.App\5.0.0-rc.1.20451.14 C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\5.0.0-rc.1.20452.2
                var netCoreDir = typeof(string).Assembly.Location.GetDirName();
                var dir = netCoreDir.Replace("Microsoft.NETCore.App", appType);

                if (dir.DirExists())
                    return dir; // Microsoft.WindowsDesktop.App and Microsoft.NETCore.App are of teh same version

                var desiredVersion = netCoreDir.GetFileName();

                int howSimilar(string stringA, string stringB)
                {
                    var maxSimilariry = Math.Min(stringA.Length, stringB.Length);

                    for (int i = 0; i < maxSimilariry; i++)
                        if (stringA[i] != stringB[i])
                            return i;

                    return maxSimilariry;
                }

                var allDesktopVersionsRootDir = dir.GetDirName();

                var allInstalledVersions = Directory.GetDirectories(allDesktopVersionsRootDir)
                                                    .Select(d => new
                                                    {
                                                        Path = d,
                                                        Version = d.GetFileName(),
                                                        SimialrityIndex = howSimilar(d.GetFileName(), desiredVersion)
                                                    })
                                                    .OrderByDescending(x => x.SimialrityIndex);

                return allInstalledVersions.FirstOrDefault()?.Path;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Determines whether .NET SDK is installed.
        /// </summary>
        /// <returns><c>true</c> if [is SDK installed]; otherwise, <c>false</c>.</returns>
        public static bool IsSdkInstalled()
        {
            return Globals.csc.FileExists();
            // "dotnet --list-sdks" is more accurate but it is fragile on WLS2
            //var output = "";
            //"dotnet".Run("--list-sdks", null, onOutput: x => output += x);
            //return output.IsNotEmpty();
        }
    }
}