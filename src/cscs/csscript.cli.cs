using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CSScripting;
using static CSScripting.CSSUtils;

namespace csscript
{
    partial class CSExecutor
    {
        public void ShowEngines()
        {
            print($"dotnet - {Globals.dotnet}");
            print($"csc    - {Globals.csc}");
            print($"roslyn - {Globals.roslyn}");
            print($"---");
            print($".NET SDK - {(Runtime.IsSdkInstalled() ? "installed" : "not unstalled")}");
        }

        public void ProcessConfigCommand(string command)
        {
            //-config                  - lists/print current settings value
            //-config:raw              - print current config file content
            //-config:ls               - lists/print current settings value (same as simple -config)
            //-config:create           - create config file with default settings
            //-config:default          - print default settings
            //-config:get:name         - print current config file value
            //-config:set:name=value   - set current config file value
            try
            {
                if (command == "create")
                {
                    CreateDefaultConfigFile();
                }
                else if (command == "default")
                {
                    print(new Settings().ToStringRaw());
                }
                else if (command == "ls" || command == null)
                {
                    print(Settings.Load(false).ToString());
                }
                else if (command == "raw" || command == "xml")
                {
                    var currentConfig = Settings.Load(false) ?? new Settings();
                    print(currentConfig.ToStringRaw());
                }
                else if (command.StartsWith("get:") || command.StartsWith(":"))
                {
                    string name = command.Split(':', 2).Last();
                    var currentConfig = Settings.Load(false) ?? new Settings();
                    var value = currentConfig.Get(ref name);
                    print(name + ": " + value);
                }
                else if (command.StartsWith("set:"))
                {
                    // set:DefaultArguments=-ac
                    string name, value;

                    string[] tokens = command.Substring(4).Split(new char[] { '=', ':' }, 2);
                    if (tokens.Length != 2)
                        throw new CLIException("Invalid set config property expression. Must be in name 'set:<name>=<value>' format.");

                    name = tokens[0];
                    value = tokens[1].Trim().Trim('"');

                    var currentConfig = Settings.Load(true) ?? new Settings();
                    currentConfig.Set(name, value);
                    currentConfig.Save();

                    var new_value = currentConfig.Get(ref name);
                    print("set: " + name + ": " + new_value);
                }
                else
                {
                    var props = typeof(Settings).GetProperties()
                        .Select(x => new { Name = x.Name, Descr = x.GetCustomAttribute<DescriptionAttribute>()?.Description })
                        .Where(x => x.Descr.HasText());

                    if (!command.IsOneOf("", "*"))
                        props = props.Where(x => x.Name.SameAs(command.Replace("_", ""), ignoreCase: true));

                    props.ForEach(item =>
                                  {
                                      print($"`{item.Name}`");
                                      print(item.Descr);
                                      print("");
                                  });
                }
            }
            catch (Exception e)
            {
                throw new CLIException(e.Message); //only a message, stack info for CLI is too verbose
            }
            throw new CLIExitRequest();
        }

        /// <summary>
        /// Show CS-Script version information.
        /// </summary>
        public void ShowVersion(string arg = null, string context = null)
        {
            var message = HelpProvider.BuildVersionInfo(arg);
            if (context.HasText())
                File.WriteAllText(context, message);
            else
                print(message);
        }

        public void ShowProjectFor(string arg)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Show sample C# script file.
        /// </summary>
        public void Sample(string appType, string outFile)
        {
            if (appType == null && outFile == null)
            {
                print?.Invoke(HelpProvider.BuildSampleHelp());
            }
            else
            {
                var context = outFile;
                if (appType == "cmd")
                    context = outFile ?? "-new_command";

                var samples = HelpProvider.BuildSampleCode(appType, context);
                foreach (var sample in samples)
                {
                    if (outFile.IsNotEmpty())
                    {
                        if (appType == "cmd" && !outFile.StartsWith("-"))
                            outFile = "-" + outFile;

                        if (appType == "cmd" && outFile.GetDirName().IsEmpty())
                        {
                            var prefix = new string(outFile.TakeWhile(x => x == '-').ToArray());
                            var subDirs = prefix + outFile.TrimStart('-').Split('-').JoinBy(Path.DirectorySeparatorChar + "-");// Select(x => "-" + x).ToArray();

                            outFile = Runtime.CustomCommandsDir.PathJoin(subDirs).PathJoin("-run.cs");
                            outFile.EnsureFileDir();
                        }

                        string file;

                        if (sample == samples.First())
                        {
                            if (outFile.GetExtension() == "")
                                file = outFile + sample.FileExtension;
                            else
                                file = outFile;
                        }
                        else
                            file = outFile.ChangeExtension(sample.FileExtension); // IE script.xaml

                        file = file.GetFullPath().EnsureFileDir();

                        print?.Invoke($"Created: {Path.GetRelativePath(Environment.CurrentDirectory, file)}");
                        File.WriteAllText(file, sample.Code);

                        if (appType == "cmd")
                        {
                            var fileDir = file.GetDirName();
                            if (Directory.GetFiles(fileDir, "*.version").IsEmpty())
                                File.WriteAllText(Path.Combine(fileDir, "1.0.0.version"), "");
                        }
                    }
                    else
                    {
                        print?.Invoke($"{NewLine}script{sample.FileExtension}:{NewLine}----------");
                        print?.Invoke(sample.Code);
                    }
                }
            }
        }

        /// <summary>
        /// Show sample precompiler C# script file.
        /// </summary>
        public void ShowPrecompilerSample()
        {
            if (print != null)
                print(HelpProvider.BuildPrecompilerSampleCode());
        }

        /// <summary>
        /// Performs the cache operations and shows the operation output.
        /// </summary>
        /// <param name="command">The command.</param>
        public void DoCacheOperations(string command)
        {
            if (print != null)
            {
                if (command == "ls")
                    print(Cache.List());
                else if (command == "trim")
                    print(Cache.Trim());
                else if (command == "clear")
                    print(Cache.Clear());
                else
                    print("Unknown cache command." + Environment.NewLine
                        + "Expected: 'cache:ls', 'cache:trim' or 'cache:clear'" + Environment.NewLine);
            }
        }

        /// <summary>
        /// Creates the default config file in the CurrentDirectory.
        /// </summary>
        public void CreateDefaultConfigFile()
        {
            string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "css_config.xml");
            new Settings().Save(file);
            print("The default config file has been created: " + file);
        }

        /// <summary>
        /// Prints the config file default content.
        /// </summary>
        public void PrintDefaultConfig()
        {
            print(new Settings().ToStringRaw());
        }

        public void PrintDecoratedAutoclass(string script)
        {
            string code = File.ReadAllText(script);

            var decorated = AutoclassPrecompiler.Process(code);

            print(decorated);
        }

        /// <summary>
        /// Prints Help info.
        /// </summary>
        public void ShowHelpFor(string arg)
        {
            if (arg?.StartsWith("-out:") == true)
            {
                var content = HelpProvider.BuildCommandInterfaceHelp("-out");
                File.WriteAllText(arg.Substring(5), content);
            }
            else
                print?.Invoke(HelpProvider.BuildCommandInterfaceHelp(arg));
        }

        /// <summary>
        /// Prints CS-Script specific C# syntax help info.
        /// </summary>
        public void ShowHelp(string helpType, params object[] context)
        {
            var help = HelpProvider.ShowHelp(helpType, context.Concat([print]).Where(x => x != null).ToArray());
            print?.Invoke(help);
        }

        /// <summary>
        /// Prints CS-Script specific C# syntax help info.
        /// </summary>
        public void InteractiveCommand(string cmdType, params object[] context) => Command(cmdType, context);

        public static void Command(string cmdType, params object[] context)
        {
            switch (cmdType)
            {
                case AppArgs.ls:
                case AppArgs.list:
                    {
                        if (!Runtime.IsConsole)
                        {
                            print?.Invoke($"Command '{cmdType}' is only available in the console mode.");
                        }
                        else
                        {
                            var result = Runtime.GetScriptProcessLog();

                            if (result.scripts.IsEmpty())
                            {
                                Console.WriteLine("No running scripts found.");
                            }
                            else
                            {
                                if (context.Contains("kill") || context.Contains("-kill") || context.Contains("k"))
                                {
                                    // not sure ignoring any script process is a good idea but it is better than killing the current process
                                    var thisProcess = -1;
                                    // var thisProcess = Process.GetCurrentProcess().Id;

                                    if (context.Contains("*"))
                                    {
                                        foreach (var pid in result.scripts.Select(x => x.pid).Where(x => x != 0 && x != thisProcess))
                                            try { Process.GetProcessById(pid).Kill(); }
                                            catch { }
                                    }
                                    else
                                    {
                                        if (result.scripts.Any())
                                        {
                                            Console.WriteLine(result.view);
                                            Console.WriteLine("Enter index of the script process you want to terminate or '*' to terminate them all:");
                                            var userInput = Console.ReadLine().ToLower();
                                            if (userInput != "x")
                                            {
                                                if (userInput == "*")
                                                {
                                                    foreach (var pid in result.scripts.Select(x => x.pid).Where(x => x != 0 && x != thisProcess))
                                                        try { Process.GetProcessById(pid).Kill(); }
                                                        catch { }
                                                }
                                                else
                                                {
                                                    var pid = result.scripts.FirstOrDefault(x => x.index == userInput).pid;
                                                    if (pid != 0)
                                                        try
                                                        {
                                                            Process.GetProcessById(pid).Kill();
                                                        }
                                                        catch { }
                                                    else
                                                        Console.WriteLine("Invalid user input.");
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                    Console.WriteLine(result.view);
                            }
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public void EnableWpf(string arg)
        {
            EnableWpf(arg, Assembly.GetExecutingAssembly().Location.ChangeExtension(".runtimeconfig.json"), true);
            EnableWpf(arg, Assembly.GetExecutingAssembly().Location.ChangeFileName("css.runtimeconfig.json"), false);
        }

        void EnableWpf(string arg, string configFile, bool primaryConfig)
        {
            if (configFile.FileExists())
            {
                const string console_type = "\"name\": \"Microsoft.NETCore.App\"";
                const string win_type = "\"name\": \"Microsoft.WindowsDesktop.App\"";

                var content = File.ReadAllText(configFile);

                if (arg == "enable" || arg == "1")
                    content = content.Replace(console_type, win_type);
                else if (arg == "disable" || arg == "0")
                    content = content.Replace(win_type, console_type);

                File.WriteAllText(configFile, content);
                if (primaryConfig)
                    CSExecutor.print($"WPF support is {(content.Contains(win_type) ? "enabled" : "disabled")}");
            }
            else
            {
                if (primaryConfig)
                    CSExecutor.print($"WPF support is not available as the runtime configuration file is missing: {configFile}");
            }
        }
    }
}