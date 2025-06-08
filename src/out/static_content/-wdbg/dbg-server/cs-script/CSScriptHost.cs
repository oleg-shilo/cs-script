using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;

namespace wdbg.cs_script
{
    public class CSScriptHost
    {
        static CSScriptHost()
        {
            // Initialize the CSScript engine here if needed
            // For example: CSScript.Evaluator.Initialize();
        }

        public static async Task Start(string script, string[] args = null, string[] ngArgs = null, Action<Process> onStart = null, Action onExit = null,
            Action<Process, string> onOutput = null, Action<string> onError = null, string sessionId = "", bool singleCharOputput = false, (string, string)[] envars = null)
            => Task.Run(() =>
            {
                try
                {
                    // Run the script using the CSScript engine
                    var allArgs = new[] { "-dbg", script.qt() };
                    if (args != null)
                        allArgs = allArgs.Concat(args).ToArray();

                    if (ngArgs != null)
                        allArgs = ngArgs.Concat(allArgs).ToArray(); // goes before the script name

                    var proc = singleCharOputput ?
                        Run("css", allArgs, onOutput, sessionId, envars, onError) :
                        RunCatchLines("css", allArgs, onOutput, sessionId, envars);

                    onStart?.Invoke(proc);
                    proc.WaitForExit();
                    onExit?.Invoke();
                }
                catch (Exception e)
                {
                    onError?.Invoke(e.Message);
                }
            });

        //run process with cs-script engine as external process and intercept every line of output
        public static Process CssRun(string[] args)
        {
            // Create a new process to run the CSScript engine
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "css",
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            var process = new Process { StartInfo = processStartInfo };
            process.Start();
            return process;
        }

        public static Process Run(string exe, string[] args, Action<Process, string> onOutput, string sessionId, (string, string)[] envars = null, Action<string> onError = null)
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
                catch (Exception e)
                {
                    onError?.Invoke(e.Message);
                }
            });

            // Read StandardError char by char
            Task.Run(() =>
            {
                int ch;
                while ((ch = process.StandardError.Read()) != -1)
                {
                    onOutput?.Invoke(process, ((char)ch).ToString());
                }
            });

            // Wait for the process to exit
            return process;
        }

        public static Process RunCatchLines(string exe, string[] args, Action<Process, string> onOutput, string sessionId, (string, string)[] envars = null)
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