using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;

namespace wdbg.cs_script
{
    static class Tools
    {
        public static string cscs_dll;
        public static string syntaxer_dll;

        public static async Task<string> Locate() => await Task.Run(() =>
            {
                try
                {
                    // css: <path>
                    // syntaxer: <path>
                    var output = "syntaxer".Run("-detect")
                            .GetLines()
                            .Where(x => x.HasText())
                            .Select(x => x.Split([':'], 2))
                            .ToDictionary(x => x[0], y => y[1]);

                    cscs_dll = output["css"];
                    syntaxer_dll = output["syntaxer"];
                }
                catch
                {
                    cscs_dll = null;
                    syntaxer_dll = null;
                }

                if (cscs_dll == null)
                    try
                    {
                        var path = "css".Run("-self").Trim();
                        if (File.Exists(path))
                            cscs_dll = path;
                        else
                            cscs_dll = null;
                    }
                    catch
                    {
                        cscs_dll = null;
                    }

                if (syntaxer_dll == null)
                    return "Cannot find external tool Syntaxer. " +
                        "You can install it from the Misc tab of the left-side panel.\n" +
                        "If you installed it recently, refresh the page so the application can detect it";
                else if (cscs_dll == null)
                    return "Cannot find external CS-Script installation. " +
                        "You can install it from the Misc tab of the left-side panel.\n" +
                        "If you installed it recently, refresh the page so the application can detect it";
                else
                    return "";
            });

        public static void InstallSyntaxer(bool update = false)
        {
            Shell.StartProcessInTerminal("dotnet", $"tool {(update ? "update" : "install")} --global cs-syntaxer", null);
        }

        public static void InstallCSScript(bool update = false)
        {
            Shell.StartProcessInTerminal("dotnet", $"tool {(update ? "update" : "install")} --global cs-script.cli", null);
        }

        public static void OpenInVs(string script)
        {
            // it's OK to start `css` instead of `dotnet cscsc.dll` since the terminal will show all possible integration errors
            Shell.StartProcessInTerminal("css", $"-vs {script}", null);
        }
    }

    public static class CSScriptHost
    {
        public static string LocateLoadedScriptDebuggInfo(this string script)
        {
            var scriptName = script.GetFileName();

            var output = CssRun("-cache", script).Trim();
            if (output.HasText() && Directory.Exists(output))
            {
                return output.PathJoin(".wdbg", scriptName, scriptName); ;
            }
            else
            {
                // old version of cscs.dll that does not support -cache <file> option

                var cacheRoot = Environment.GetEnvironmentVariable("CSS_CUSTOM_TEMPDIR") ??
                                Path.GetTempPath().PathJoin("csscript.core", "cache");

                cacheRoot.EnsureDir();
                foreach (var info in Directory.GetFiles(cacheRoot, "css_info.txt", SearchOption.AllDirectories))
                {
                    var linkedDir = File.ReadAllLines(info).Skip(1).FirstOrDefault();

                    if (linkedDir == script.GetDirName())
                    {
                        var decoratedScript = info.GetDirName().PathJoin(".wdbg", scriptName, scriptName);
                        if (File.Exists(decoratedScript))
                        {
                            return decoratedScript;
                        }
                    }
                }
            }
            return null;
        }

        public static string Run(this string exe, params string[] args)
        {
            using var process = new Process();
            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = string.Join(" ", args);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }

        public static string[] GetAllScriptFiles(string script)
        {
            var prefix = "file:";
            var output = CssRun("-proj", script);
            var files = output
                .GetLines()
                .Where(x => x.StartsWith(prefix))
                .Select(x => x.Substring(prefix.Length))
                .ToArray();

            return files;
        }

        public static async Task Start(string script, string[] args = null, string[] ngArgs = null, Action<Process> onStart = null, Action onExit = null,
            Action<Process, string> onOutput = null, Action<string> onError = null, string sessionId = "", bool singleCharOputput = false, (string, string)[] envars = null)
            => _ = Task.Run(() =>
            {
                try
                {
                    // Start the script using the CSScript engine
                    var allArgs = new[] { "-dbg", script.qt() };
                    if (args != null)
                        allArgs = allArgs.Concat(args).ToArray();

                    if (ngArgs != null)
                        allArgs = ngArgs.Concat(allArgs).ToArray(); // goes before the script name

                    allArgs = [Tools.cscs_dll, .. allArgs];
                    var proc = singleCharOputput ?
                        Start("dotnet", allArgs, onOutput, sessionId, envars, onError) :
                        StartAndCatchLines("dotnet", allArgs, onOutput, sessionId, envars);

                    onStart?.Invoke(proc);
                    proc.WaitForExit();
                    onExit?.Invoke();
                }
                catch (Exception e)
                {
                    onError?.Invoke(e.Message);
                }
            });

        public static string CssRun(params string[] args) => "dotnet".Run([Tools.cscs_dll, .. args]);

        public static Process Start(string exe, string[] args, Action<Process, string> onOutput, string sessionId, (string, string)[] envars = null, Action<string> onError = null)
        {
            // Create a new process to run the CSScript engine
            var processStartInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            };

            processStartInfo.EnvironmentVariables["CSS_DBG_SESSION"] = sessionId;
            if (envars != null)
                foreach (var (key, value) in envars)
                    processStartInfo.EnvironmentVariables[key] = value;

            var process = new Process { StartInfo = processStartInfo };
            process.Start();

            // Read StandardOutput char by char
            Task.Run(() =>
            {
                try
                {
                    var buf = new StringBuilder();
                    int ch;
                    while ((ch = process.StandardOutput.Read()) != -1)
                    {
                        char c = (char)ch;
                        onOutput?.Invoke(process, c.ToString());
                        continue;

                        if (c == '\n')
                        {
                            buf.Append(c);
                            var line = buf.ToString();
                            onOutput?.Invoke(process, buf.ToString());
                            buf.Clear();
                        }
                        else
                            buf.Append(c);
                    }
                }
                catch (ObjectDisposedException e)
                {
                    // just ignore, the process has exited
                }
                catch (Exception e)
                {
                    onError?.Invoke(e.Message);
                }
            });

            // Read StandardError char by char
            Task.Run(() =>
            {
                try
                {
                    int ch;
                    while ((ch = process.StandardError.Read()) != -1)
                    {
                        onOutput?.Invoke(process, ((char)ch).ToString());
                    }
                }
                catch { } // doesn't matter why; it's expected behavior if the process exits before we finish reading
            });

            // Wait for the process to exit
            return process;
        }

        public static Process StartAndCatchLines(string exe, string[] args, Action<Process, string> onOutput, string sessionId, (string, string)[] envars = null)
        {
            // Create a new process to run the CSScript engine
            var processStartInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            processStartInfo.EnvironmentVariables["CSS_DBG_SESSION"] = sessionId;
            if (envars != null)
                foreach (var (key, value) in envars)
                    processStartInfo.EnvironmentVariables[key] = value;

            var process = new Process { StartInfo = processStartInfo };
            // Attach event handlers to capture output and error streams
            process.OutputDataReceived += (sender, e) => onOutput?.Invoke(process, e.Data);
            process.ErrorDataReceived += (sender, e) => onOutput?.Invoke(process, e.Data);
            // Start the process and begin reading output asynchronously
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            // Wait for the process to exit
            return process;
        }
    }
}