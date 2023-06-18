using CSScripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using System.Text;
using static System.Environment;
using static System.Net.Mime.MediaTypeNames;

namespace csscript
{
    internal static class AppArgs
    {
        public static string nl = "nl";
        public static string nathash = "nathash";       // instead of const make it static so this hidden option is not picked by auto-documenter

        public const string help = "help";
        public const string help2 = "-help";
        public const string help3 = "--help";
        public const string question = "?";
        public const string question2 = "-?";
        public const string ver = "ver";
        public const string wpf = "wpf";
        public const string cmd = "cmd";
        public const string syntax = "syntax";
        public const string server = "server";
        public const string install = "install";
        public const string uninstall = "uninstall";
        public const string commands = "commands";
        public const string config = "config";
        public const string s = "s";
        public const string sample = "sample";
        public const string @new = "new";
        public const string verbose = "verbose";
        public const string verbose2 = "verbose2";
        public const string profile = "profile";
        public const string v = "v";
        public const string version = "version";
        public const string version2 = "-version";
        public const string c = "c";
        public const string cd = "cd";
        public const string engine = "engine";
        public const string ng = "ng";
        public const string co = "co";
        public const string check = "check";
        public const string r = "r";
        public const string e = "e";
        public const string ew = "ew";
        public const string dir = "dir";
        public const string @out = "out";
        public const string ca = "ca";
        public const string dbg = "dbg";
        public const string d = "d";
        public const string l = "l";

        // public const string inmem = "inmem"; // may need to resurrect if users do miss it :)
        public const string ac = "ac";

        public const string wait = "wait";
        public const string autoclass = "autoclass";
        public const string sconfig = "sconfig";
        public const string code = "code";
        public const string speed = "speed";
        public const string stop = "stop";
        public const string tc = "tc";
        public const string pvdr = "pvdr";
        public const string nuget = "nuget";
        public const string provider = "provider";
        public const string pc = "pc";
        public const string precompiler = "precompiler";
        public const string cache = "cache";
        public const string dbgprint = "dbgprint";
        public const string vs = "vs";
        public const string vs_init = "vs:init";
        public const string vscode = "vscode";
        public const string proj = "proj";
        public const string publish = "publish";

        internal const string proj_dbg = "proj:dbg";    // for internal use only
        internal const string proj_csproj = "proj:csproj";    // for internal use only
        static public string SyntaxHelp { get { return syntaxHelp.ToConsoleLines(0); } }
        static string syntaxHelp = "";

        static public Dictionary<string, ArgInfo> switch1Help = new Dictionary<string, ArgInfo>();
        static public Dictionary<string, ArgInfo> switch2Help = new Dictionary<string, ArgInfo>();
        static public Dictionary<string, ArgInfo> miscHelp = new Dictionary<string, ArgInfo>();

        static public bool IsHelpRequest(this string arg)
            => arg.IsOneOf(AppArgs.help, AppArgs.question, AppArgs.help2, AppArgs.help3, AppArgs.question2);

        static public bool Supports(string arg)
        {
            var rawArg = arg;
            var normalizedArg = arg;

            if (arg.StartsWith("-"))
                normalizedArg = arg.Substring(1);

            return AppArgs.switch1Help.ContainsKey(rawArg) ||
                   AppArgs.switch2Help.ContainsKey(rawArg) ||
                   AppArgs.switch1Help.ContainsKey(normalizedArg) ||
                   AppArgs.switch2Help.ContainsKey(normalizedArg);
        }

        static public string LookupSwitchHelp(string arg)
        {
            var rawArg = arg;
            var normalizedArg = arg;

            if (arg.StartsWith("-"))
                normalizedArg = arg.Substring(1);

            return AppArgs.switch1Help.ContainsKey(rawArg) ? AppArgs.switch1Help[rawArg].GetFullDoc() :
                   AppArgs.switch2Help.ContainsKey(rawArg) ? AppArgs.switch2Help[rawArg].GetFullDoc() :
                   AppArgs.switch1Help.ContainsKey(normalizedArg) ? AppArgs.switch1Help[normalizedArg].GetFullDoc() :
                   AppArgs.switch2Help.ContainsKey(normalizedArg) ? AppArgs.switch2Help[normalizedArg].GetFullDoc() :
                   null;
        }

        internal class ArgInfo
        {
            string argSpec;
            string description;
            string doc = "";

            public ArgInfo(string argSpec, string description, params string[] docLines)
            {
                this.argSpec = argSpec;
                this.description = description;
                this.doc = string.Join(Environment.NewLine, docLines.SelectMany(x => x.SplitSubParagraphs()).ToArray());
            }

            public ArgInfo(string argSpec, string description)
            {
                this.argSpec = argSpec;
                this.description = description;
            }

            public string ArgSpec { get { return argSpec; } }
            public string Description { get { return description; } }

            public string GetFullDoc(bool markdown = false)
            {
                var actualIndent = markdown ? 0 : indent;

                var buf = new StringBuilder();

                var prefix = markdown ? "### `" : "";
                var suffix = markdown ? "`" : "";

                buf.AppendLine($"{prefix}{argSpec}{suffix}")
                   .Append(' '.Repeat(actualIndent))
                   .Append(markdown ? description : description.Replace("\r\n```", ""));

                if (doc != "")
                    buf.AppendLine().Append(doc.ToConsoleLines(actualIndent, markdown ? int.MaxValue : null));

                if (markdown)
                    return buf.ToString().TrimEnd();
                else
                    return buf.ToString().Replace("```", "").TrimEnd();
            }

            static int indent = 4;
        }

        static string fromLines(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines.SelectMany(x => x.SplitSubParagraphs()).ToArray());
        }

        static string indent(int indent, string text)
        {
            var result = text.ToConsoleLines(indent);
            return text.ToConsoleLines(indent);
        }

        static string indent2(int indent, string text)
        {
            var result = text.ToConsoleLines(indent);
            return text.ToConsoleLines(indent);
        }

        internal const string section_sep = "------------------------------------"; // section separator
        internal const string alias_prefix = "Alias - ";

        const string help_url = "https://www.cs-script.net/cs-script/help-legacy";

        static AppArgs()
        {
            //http://www.csscript.net/help/Online/index.html
            switch1Help[help2] =
            switch1Help[help] =
            switch1Help[question] = new ArgInfo("--help|-help|-? [<command>|<scope[:md]>|<-out:<file>>]",
                                                "Displays either generic or command specific help info.",
                                                "```",
                                                "   <command> - one of the supported CLI commands",
                                                "   <scope>  ",
                                                "         `cli`    - print documentation for all CLI commands",
                                                "         `syntax` - print the complete documentation for scripting syntax",
                                                "         `md`     - print the documentation in GitHub markdown format",
                                                "         (e.g. `-help cli:md`)",
                                                "   <file>    - output file for the help content",
                                                "```",
                                                "Reversed order of parameters for the command specific help is also acceptable. " +
                                                "The all following argument combinations print the same help topic for 'cache' command:",
                                                "```",
                                                "   -help cache",
                                                "   -? cache",
                                                "   -cache help",
                                                "   -cache ?",
                                                "```")
            {
            };
            switch1Help[e] = new ArgInfo("-e",
                                         "Compiles script into console application executable.",
                                         "Note this switch will force the use of `dotnet` compiler as `csc` is not capable of building " +
                                         "executable assemblies. Thus `//css_engine` and `-engine` options will always be ignored.");
            switch1Help[ew] = new ArgInfo("-ew",
                                          "Compiles script into Windows application executable (applicable only on Windows).");
            switch1Help[c] = new ArgInfo("-c[:<0|1>]",
                                         "Executes compiled script cache (e.g. <cache dir>/script.cs.dll) if found.",
                                         "This command improves performance by avoiding compiling the script if it was not changed since last execution.",
                                         "```",
                                         "   -c:1|-c  - enable caching",
                                         "   -c:0     - disable caching (which might be enabled globally)",
                                         "```");
            switch1Help[ca] = new ArgInfo("-ca",
                                          "Compiles script file into cache file (e.g. <cache dir>/script.cs.dll).");
            switch1Help[cd] = new ArgInfo("-cd",
                                          "Compiles script file into assembly (.dll) in the script folder without execution.");
            switch1Help[check] = new ArgInfo("-check",
                                             "Checks script for errors without execution.");
            switch1Help[proj] = new ArgInfo("-proj",
                                            "Shows script 'project info' - script and all its dependencies.");
            switch1Help[vs_init] =
            switch1Help[vs] = new ArgInfo("-vs <script>|-vs:init [index_of_detected_VS_executable]",
                                          "Generates .NET project file and opens it in Visual Studio.",
                                              "The path to the Visual Studio executable (devenv.exe) needs to be defined in the " +
                                              "environment variable `CSSCRIPT_VSEXE`.",
                                              "You can let CS-Script to detect installed Visual Studio executable and interactively select " +
                                              "the detected executable for integrating it with CS-Script by using `-vs:init` option.",
                                              "Alternatively, you can even ask to integrate the first detected executable with: ",
                                              "      " + AppInfo.appName + " -vs:init 0");
            switch1Help[vscode] = new ArgInfo("-vscode",
                                          "Generates .NET project file and opens it in Visual Studio Code.",
                                              "The path to the Visual Studio Code executable (code.exe) needs to be defined in the " +
                                              "environment variable `CSSCRIPT_VSCODEEXE`.");

            switch1Help[cache] = new ArgInfo("-cache[:<ls|trim|clear>]",
                                             "Performs script cache operations.",
                                             "```",
                                             " ls    - lists all cache items.",
                                             " trim  - removes all abandoned cache items.",
                                             " clear - removes all cache items.",
                                             "```");
            switch1Help[co] = new ArgInfo("-co:<options>",
                                          "Passes compiler options directly to the language compiler.",
                                              "(e.g.  `-co:/d:TRACE` pass `/d:TRACE` option to C# compiler",
                                              " or    `-co:/platform:x86` to produce Win32 executable)");
            switch1Help[engine] =
            switch1Help[ng] = new ArgInfo("-ng|-engine:<csc|dotnet|roslyn>]",
                                          "Forces compilation to be done by one of the supported .NET engines.",
                                          "  ",
                                          "`dotnet` - ${<==}dotnet.exe compiler; this is the most versatile compilation engine though " +
                                          "it does have a startup overhead when running the script for the first time. It requires .NET SDK to be installed " +
                                          "on the target system.",
                                          "  ",
                                          "`csc`   - ${<==}csc.exe compiler; the fastest compiler available. It is not suitable " +
                                          "for WPF scripts as csc.exe cannot compile XAML.",
                                          "          ${<==}The compilation is performed in the separate child process build.exe which is somewhat " +
                                          "equivalent to VBCSCompiler.exe (build server) from .NET toolset. It requires .NET SDK to be installed " +
                                          "on the target system.",
                                          "          ${<==}CS-Script communicates with build.exe build server via socket (default port 17001). You can " +
                                          "control port value via the environment variable 'CSS_BUILDSERVER_CSC_PORT'",
                                          "          ${<==}Value `csc-inproc` will suppress spinning off an build server process and .NET csc.exe will be " +
                                          "called directly instead. This option convenient when socket communication is undesirable for whatever reason. " +
                                          "Though in this case all the performance benefits of `-ng:csc` will be lost and then you are better off using " +
                                          "`-ng:dotnet` instead.",
                                          "  ",
                                          "`roslyn` - ${<==}Microsoft.CodeAnalysis.CSharp.Scripting.dll compiler; this is the most portable compilation " +
                                          "engine. It does not require .NET SDK being installed. Though it does have limitations (see documentation).",
                                          "           ${<==}The compilation is performed in the separate child process " + AppInfo.appName + " (another " +
                                          "instance of script engine) which is somewhat equivalent of VBCSCompiler.exe (build server) from .NET toolset.",
                                          "           ${<==}CS-Script communicates with " + AppInfo.appName + " build server via socket (default port 17002). " +
                                          "You can control port value " +
                                          "via the environment variable 'CSS_BUILDSERVER_ROSLYN_PORT'",
                                          "           ${<==}Value `roslyn-inproc` will suppress spinning off an external process and Roslyn compiler will be " +
                                          "hosted in the original process of script engine instead. This option is convenient when socket communication is " +
                                          "undesirable for whatever reason. Though in this case performance will be effected on the first run of the script.",
                                          "  ",
                                          "```",
                                          "(e.g. " + AppInfo.appName + " -engine:dotnet sample.cs",
                                          "      " + AppInfo.appName + " -ng:csc sample.cs)",
                                          "      " + AppInfo.appName + " -ng:roslyn-inproc sample.cs)",
                                          "      " + AppInfo.appName + " -ng:roslyn sample.cs)",
                                          "```");
            switch1Help[sample] =
            switch1Help[s] = new ArgInfo("-s|-sample[:<C# version>]",
                                         " -s:7    - prints C# 7+ sample. Otherwise, it prints the default canonical 'Hello World' sample.",
                                         "(e.g. " + AppInfo.appName + " -s:7 > sample.cs).");

            switch1Help[@new] = new ArgInfo("-new[:<type>] [<script name>]",
                                            "Creates a new script.",
                                            HelpProvider.BuildSampleHelp());

            switch1Help[code] = new ArgInfo("-code[:show] <script code>",
                                            "Executes script code directly without using a script file.",
                                                "Sample:",
                                                    " ",
                                                    "```",
                                                    AppInfo.appName + " -code \"Console.WriteLine(Environment.UserDomainName);#n" +
                                                    "Console.WriteLine(#''%USERNAME%#'');\"",
                                                    AppInfo.appName + " -code \"using System.Linq;#nSystem.Diagnostics.Process.GetProcessesByName(''notepad'').ToList().ForEach(x => x.Kill());\"",
                                                    AppInfo.appName + " -code \"SetEnvironmentVariable(`ntp`,`notepad.exe`, EnvironmentVariableTarget.Machine)\"",
                                                    "```",
                                                    " ",
                                                    "The -code argument must be the last argument in the command. The only argument that is allowed " +
                                                    "after the `<script code>` is `//x`",
                                                    " ",
                                                    "Escaping special characters sometimes can be problematic as many shells have their own techniques " +
                                                    "(e.g. PowerShell collapses two single quote characters) that may conflict with CS-Script escaping approach." +
                                                    "This is the reason why CS-Script offers multiple escape techniques.",
                                                    "It can be beneficial during the troubleshooting to use `-code:show` command that outputs the received " +
                                                    "CLI arguments and the interpreted C# code without the execution.",
                                                    " ",
                                                    "Since command-line interface does not allow some special characters they need to be escaped.",
                                                    "",
                                                    "```",
                                                    "Escaped         Interpreted character",
                                                    "-------------------------------------",
                                                    "#n        ->    <\\n>",
                                                    "#r        ->    <\\r>",
                                                    "#''       ->    \"   ",
                                                    "''        ->    \"   ",
                                                    "#``       ->    \"   ",
                                                    "`n        ->    <\\n>",
                                                    "`r        ->    <\\r>",
                                                    "``        ->    \"   ",
                                                    "```"
                                           );

            switch1Help[wait] = new ArgInfo("-wait[:prompt]",
                                            "Waits for user input after the execution before exiting.",
                                                "If specified the execution will proceed with exit only after any std input is received.",
                                                    "Applicable for console mode only.",
                                                    "prompt - if none specified 'Press any key to continue...' will be used");
            switch1Help[ac] =
            switch1Help[autoclass] = new ArgInfo("-ac|-autoclass[:<0|1|2|out>]",
                                                 "Legacy command: executes scripts without class definition. Use top-level statements (C# 9) scripts instead.",
                                                     "```",
                                                     " -ac     - enables auto-class decoration (which might be disabled globally).",
                                                     " -ac:0   - disables auto-class decoration (which might be enabled globally).",
                                                     " -ac:1   - same as '-ac'",
                                                     " -ac:2   - same as '-ac:1' and '-ac'",
                                                     " -ac:out - ",
                                                     "${<=-11}prints auto-class decoration for a given script file. The argument must be followed by the path to script file.",
                                                     "```",
                                                     " ",
                                                     "Automatically generates 'static entry point' class if the script doesn't define any.",
                                                     " ",
                                                     "```",
                                                     "    using System;",
                                                     " ",
                                                     "    void Main()",
                                                     "    {",
                                                     "        Console.WriteLine(\"Hello World!\";",
                                                     "    }",
                                                     " ",
                                                     "```",
                                                     "Using an alternative 'instance entry point' is even more convenient (and reliable).",
                                                     "The acceptable 'instance entry point' signatures are:",
                                                     " ",
                                                     "    void main()",
                                                     "    void main(string[] args)",
                                                     "    int main()",
                                                     "    int main(string[] args)",
                                                     " ",
                                                     "Note, having any active code above entry point is acceptable though it complicates the troubleshooting if such a code contains errors. " +
                                                     "(see https://github.com/oleg-shilo/cs-script/wiki/CLI---User-Guide#command-auto-class)",
                                                     " ",
                                                     "By default CS-Script decorates the script by adding a class declaration statement to the " +
                                                     "start of the script routine and a class closing bracket to the end. This may have an unintended " +
                                                     "effect as any class declared in the script becomes a 'nested class'. While it is acceptable " +
                                                     "for practically all use-cases it may be undesired for just a few scenarios. For example, any " +
                                                     "class containing method extensions must be a top-level static class, which conflicts with the " +
                                                     "auto-class decoration algorithm.",
                                                     " ",
                                                     "The solution to this problem is to allow some user code to be protected from being included in " +
                                                     "the decorated code.",
                                                     "Users can achieve this by placing '//css_ac_end' statement into the code. Any user code below this " +
                                                     "statement will be excluded from the decoration and stay unchanged.");
            // switch2Help[nl] = new ArgInfo("-nl", "No logo mode: No banner will be shown/printed
            // at execution time.", "Applicable for console mode only.");
            switch2Help[d] =
            switch2Help[dbg] = new ArgInfo("-dbg|-d",
                                           "Forces compiler to include debug information.");
            switch2Help[l] = new ArgInfo("-l[:<0|1>]",
                                         "'local' (makes the script directory a 'current directory'). '1' is a default value.");
            switch2Help[version2] =
            switch2Help[version] =
            switch2Help[ver] =
            switch2Help[v] = new ArgInfo("-v|-ver|--version [output file]",
                                         "Prints CS-Script version information.");

            // may need to resurrect if users do miss it :) switch2Help[inmem] = new
            // ArgInfo("-inmem[:<0|1>]", "Loads compiled script in memory before execution.", "This
            // mode allows preventing locking the compiled script file. " + "Can be beneficial for
            // fine concurrency control as it allows changing " + "and executing the scripts that
            // are already loaded (being executed). This mode is incompatible " + "with the
            // scripting scenarios that require script assembly to be file based (e.g. advanced
            // Reflection).", " -inmem:1 enable caching (which might be disabled globally);", "
            // -inmem:0 disable caching (which might be enabled globally);");

            switch2Help[dbgprint] = new ArgInfo("-dbgprint[:<0:1>]",
                                                "Controls whether to enable Python-like print methods (e.g. dbg.print(DateTime.Now)).",
                                                    "This setting allows controlling dynamic referencing of script engine assembly containing " +
                                                    "the implementation of Python-like print methods `dbg.print` and derived extension methods object.print() " +
                                                    "and object.dup(). While `dbg.print` is extremely useful it can and lead to some referencing challenges when " +
                                                    "the script being executed is referencing assemblies compiled with `dbg.print` already included. " +
                                                    "The simplest way to solve this problem is to disable the `dbg.cs` inclusion.",
                                                    "```",
                                                    " -dbgprint:1   enable `dbg.cs` inclusion; Same as `-dbgprint`;",
                                                    " -dbgprint:0   disable `dbg.cs` inclusion;",
                                                    "```");
            switch2Help[verbose2] =
            switch2Help[verbose] = new ArgInfo("-verbose",
                                               "Prints runtime information during the script execution.",
                                               "'-verbose2' additionally echoes compiling engine (e.g. csc.dll) input and output.",
                                                   "(applicable for console clients only)");
            switch2Help[profile] = new ArgInfo("-profile",
                                               "Prints script loading performance information during the script execution.");
            switch2Help[speed] = new ArgInfo("-speed",
                                             "Prints script initialization/compilation time information of the .NET compiler. ",
                                             "You can use -ng option () ",
                                             "It is a convenient way of testing performance of the .NET distribution.");
            if (Runtime.IsWin)
            {
                switch2Help[uninstall] =
                switch2Help[install] = new ArgInfo("-install/-uninstall",
                                                   "Sets/unsets CSSCRIPT_ROOT environment variable to the location of the script engine" +
                                                   "being executed.",
                                                   "This environment variable is required for integration of CS-Script with Notepad++," +
                                                   "Sublime Text and some other editors, which require CS-Script installed on the host OS.",
                                                   "This command is only supported on Windows");
            }

            switch2Help[server] = new ArgInfo("-server[:<start|stop|restart|add|remove|ping>]",
                                          "Prints the information about build server.",
                                          "Note, the server starts automatically on the script execution that is configured to use the 'csc' " +
                                          "or 'roslyn' engine.",
                                          "Build server is a background process, which implements hop loading of C# compiler csc.exe. " +
                                          "Somewhat similar to VBCSCompiler.exe.",
                                          "These options are only relevant if the compiler engine is set to 'csc' (see '-engine' command).",
                                          "```",
                                          " -server:start   - ${<==}deploys and starts build server. Useful if you want to start the server " +
                                              "on system startup.",
                                          " -server:stop    - ${<==}stops build server",
                                          " -server:restart - ${<==}restarts build server",
                                          " -server:reset   - ${<==}stops, re-deploys and starts build server",
                                          " -server:add     - ${<==}deploys build server",
                                          " -server:remove  - ${<==}removes build server files. Useful for troubleshooting.",
                                          " -server:ping    - ${<==}Pins running instance (if any) of the build server",
                                          "```",
                                          "",
                                          "The following options are only relevant if the compiler engine is set to 'roslyn' (see '-engine' command).",
                                          "Roslyn based build server variant is much simpler so it only exposes start and stop interface.",
                                          "```",
                                          " -server_r:start - ${<==}deploys and starts Roslyn build server",
                                          " -server_r:stop  - ${<==}stops Roslyn build server",
                                          "```",
                                          "",
                                          "And this is how you can start and stop both Roslyn and csc build servers with a single command:",
                                          "```",
                                          " -servers:start - ${<==}deploys and starts both Roslyn and csc build server",
                                          " -servers:stop  - ${<==}stops both Roslyn and csc build server",
                                          " -kill          - ${<==}a complete equivalent of -servers:stop",
                                          "```"
                                             );

            switch2Help[tc] = new ArgInfo("-tc",
                                          "Trace compiler input produced by CS-Script code provider CSSRoslynProvider.dll.",
                                              "It's useful when troubleshooting custom compilers (e.g. Roslyn on Linux).");

            if (Runtime.IsWin)
                switch2Help[wpf] = new ArgInfo("-wpf[:<enable|disable|1|0>]",
                                               "Enables/disables WPF support on Windows by updating the framework name " +
                                               "in the *.runtimeconfig.json file",
                                               "```",
                                               " -wpf               - ${<==}prints current enabled status",
                                               " -wpf:<enable|1>    - ${<==}enables WPF support",
                                               " -wpf:<disable|0>   - ${<==}disables WPF support",
                                               "```");

            switch2Help[config] = new ArgInfo("-config[:<option>]",
                                              "Performs various CS-Script config operations",
                                              "```",
                                              " -config:none                   - ${<==}ignores config file (uses default settings)",
                                              " -config:create                 - ${<==}creates config file with default settings",
                                              " -config:default                - ${<==}prints default config file",
                                              " -config:<raw|xml>              - ${<==}prints current config file content",
                                              " -config[:ls]                   - ${<==}lists/prints current config values",
                                              " -config:<name> ?               - ${<==}prints help for the configuration value specified by name",
                                              " -config:get:<name>             - ${<==}prints current config value",
                                              " -config::<name>                - ${<==}the same as `-config:get:name`",
                                              " -config:set:<name>=<value>     - ${<==}sets current config value",
                                              " -config:set:<name>=add:<value> - ${<==}updates the current config value content by appending the specified value.",
                                              " -config:set:<name>=del:<value> - ${<==}updates the current config value content by removing all occurrences of the specified value.",
                                              " -config:<file>                 - ${<==}uses custom config file",
                                              "```",
                                              " ",
                                                  "Note: The property name in -config:set and -config:set is case insensitive and can also contain '_' " +
                                                  "as a token separator that is ignored during property lookup.",
                                                  "```",
                                                  "(e.g. " + AppInfo.appName + " -config:none sample.cs",
                                                  "${<=6}" + AppInfo.appName + " -config:default > css_VB.xml",

                                                  // "${<=6}" + AppInfo.appName + " -config:set:" +
                                                  // inmem + "=true", // may need to resurrect if
                                                  // users do miss it :)

                                                  "${<=6}" + AppInfo.appName + " -config:set:DefaultCompilerEngine=dotnet",
                                                  "${<=6}" + AppInfo.appName + " -config:set:DefaultArguments=add:-ac",
                                                  "${<=6}" + AppInfo.appName + " -config:set:default_arguments=del:-ac",
                                                  "${<=6}" + AppInfo.appName + " -config:c:\\cs-script\\css_VB.xml sample.vb)",
                                                  "```");

            switch2Help[@out] = new ArgInfo("-out[:<file>]",
                                            "Forces the script to be compiled into a specific location.",
                                                "Used only for very fine hosting tuning.",
                                                    "(e.g. " + AppInfo.appName + " -out:%temp%\\%pid%\\sample.dll sample.cs");
            // .NET core does not support custom app.config switch2Help[sconfig] = new
            // ArgInfo("-sconfig[:<file>|none]", "Uses custom config file as a .NET app.config.",
            // "This option might be useful for running scripts, which usually cannot be executed
            // without custom configuration file (e.g. WCF, Remoting).", "By default CS-Script
            // expects script config file name to be <script_name>.cs.config or
            // <script_name>.exe.config. " + "However if <file> value is specified the it is used as
            // a config file. ", "(e.g. if -sconfig:myApp.config is used the expected config file
            // name is myApp.config)");

            switch2Help[r] = new ArgInfo("-r:<assembly 1>,<assembly N>",
                                         "Uses explicitly referenced assembly.", "It is required only for " +
                                             "rare cases when namespace cannot be resolved into assembly.",
                                                 "(e.g. `" + AppInfo.appName + " /r:myLib.dll myScript.cs`).");

            switch2Help[dir] = new ArgInfo("-dir:<directory 1>,<directory N>",
                                           "Adds path(s) to the assembly probing directory list.",
                                               "You can use the reserved word 'show' as a directory name to print the configured probing directories.",
                                                   "(e.g. `" + AppInfo.appName + " -dir:C:\\MyLibraries myScript.cs; " + AppInfo.appName + " -dir:show`).");
            switch2Help[pc] =
            switch2Help[precompiler] = new ArgInfo("-precompiler[:<file 1>,<file N>]",
                                                   "Specifies custom precompiler. This can be either script or assembly file.",
                                                   alias_prefix + "pc[:<file 1>,<file N>]",
                                                   "If no file(s) specified prints the code template for the custom precompiler. The special value 'print' has " +
                                                   "the same effect (e.g. " + AppInfo.appName + " -pc:print).",
                                                   "There is a special reserved word '" + CSSUtils.noDefaultPrecompilerSwitch + "' to be used as a file name. " +
                                                   "It instructs script engine to prevent loading any built-in precompilers " +
                                                   "like the one for removing shebang before the execution.",
                                                   $"(see {help_url}/precompilers.html)");
            switch2Help[pvdr] =
            switch2Help[provider] = new ArgInfo("-pvdr|-provider:<file>",
                                                "Location of the alternative/custom code provider assembly.",
                                                    alias_prefix + "pvdr:<file>",
                                                    "If set it forces script engine to use an alternative code compiler.",
                                                    " ",
                                                    "C#7 support is implemented via Roslyn based provider: '-pvdr:CSSRoslynProvider.dll'." +
                                                    "If the switch is not specified CSSRoslynProvider.dll file will be use as a code provider " +
                                                    "if it is found in the same folder where the script engine is. Automatic CSSRoslynProvider.dll " +
                                                    "loading can be disabled with a special 'none' argument: -pvdr:none.",
                                                    $"(see {help_url}/help/non_cs_compilers.html)");
            switch2Help[nuget] = new ArgInfo("-nuget[:<package|restore>]",
                                             "Note: A new NuGet support available from version 4.7.0 will obsolete some of the options of this command",
                                             "      New NuGet support can be enabled by setting `LegacyNugetSupport` option to false with `css -config:set:LegacyNugetSupport=false`)",
                                             "      Read more: https://github.com/oleg-shilo/cs-script/wiki/NuGet-Support",
                                             " ",
                                             "Imports new or updates existing NuGet package.",
                                                 "This command allows light management of the NuGet packages in the CS-Script local package repository (%PROGRAMDATA%\\CS-Script\\nuget).",
                                                     "The tasks are limited to installing, updating and listing the local packages.",
                                                     " ",
                                                     "```",
                                                     " -nuget           - ${<==}prints the list of all root packages in the repository",
                                                     "                    ${<==}(Not available with new NuGet support)",
                                                     " -nuget:<package> - ${<==}downloads and installs the latest version of the package(s). ",
                                                     "                    ${<==}Wild cards can be used to update multiple packages. For example '-nuget:ServiceStack*' will update all " +
                                                     "already installed ServiceStack packages.",
                                                     "                    ${<==}You can also use the index of the package instead of its full name.",
                                                     "                    ${<==}(Not available with new NuGet support)",
                                                     "```",
                                                     " ",
                                                     "Installing packages this way is an alternative to having '//css_nuget -force ...' directive in the script code as it may be " +
                                                     "more convenient for the user to update packages manually instead of having them updated on every script execution/recompilation.",
                                                     "```",
                                                     " -nuget:restore - ${<==}downloads and installs all packages specified in the script without executing the script. " +
                                                     "```",
                                                     "Available only with new NuGet support."
                                            );
            switch2Help[syntax] = new ArgInfo("-syntax",
                                              "Prints documentation for CS-Script specific C# syntax.");
            switch2Help[commands] =
            switch2Help[cmd] = new ArgInfo("-commands|-cmd",
                                           "Prints list of supported commands (arguments).");
            miscHelp["file"] = new ArgInfo("file",
                                           "Specifies name of a script file to be run.");
            miscHelp["params"] = new ArgInfo("params",
                                             "Specifies optional parameters for a script file to be run.");
            miscHelp["//x"] = new ArgInfo("//x",
                                          "Launch debugger just before starting the script.");

            #region SyntaxHelp

            syntaxHelp = fromLines(
                         "**************************************",
                         "Script specific syntax",
                         "**************************************",
                         " ",
                         "Engine directives:",
                         "{$directives}",
                         section_sep, //------------------------------------
                         "Engine directives can be controlled (enabled/disabled) with compiler conditional symbols " +
                         "and environment variables via the inline `#if` syntax:",
                         "  //css_include #if DEBUG debug_utils.cs",
                         "  //css_dir #if (DEBUG) .\\bin\\Debug",
                         "  //css_reference #if PRODUCTION_PC d:\\temp\\build\\certificates.dll",
                         section_sep, //------------------------------------
                         "The script engine also always defines special compiler conditional symbol `CS_SCRIPT`:",
                         "  #if CS_SCRIPT",
                         "       Console.WriteLine(\"Running as a script...\");",
                         "  #endif",
                         "The script engine also defines another conditional symbol `NETCORE` to allow user" +
                         "to distinguish between executions under .NET (full) and .NET Core",
                         section_sep, //------------------------------------
                         "//css_include <file>;",
                         " ",
                         alias_prefix + "//css_inc",
                         "file - name of a script file to be included at compile-time.",
                         " ",
                         "This directive is used to import one script into another one. It is a logical equivalent of '#include' in C++. " +
                         "This directive is a full but more convenient equivalent of //css_import <file>, preserve_main;",
                         " ",
                         "If a relative file path is specified with a single-dot prefix it will be automatically converted into the absolute path " +
                         "with respect to the location of the script file containing the `//css_include` directive. " +
                         "Otherwise it will be resolved with respect to the process current directory.",
                         " ",
                         "If for whatever reason it is preferred to always resolve path expression with respect to the parent script location " +
                         "you can configure the script engine to do it with the following command:",
                         " ",
                         "   cscs -config:set:ResolveRelativeFromParentScriptLocation = true",
                         " ",
                         "Note if you use wildcard in the imported script name (e.g. *_build.cs) the directive will only import from the first " +
                         "probing directory where the matching file(s) is found. Be careful with the wide wildcard as '*.cs' as they may lead to " +
                         "unpredictable behavior. For example they may match everything from the very first probing directory, which is typically a current " +
                         "directory. Using more specific wildcards is arguably more practical (e.g. 'utils/*.cs', '*Helper.cs', './*.cs')",
                         section_sep, //------------------------------------
                         "//css_import <file>[, preserve_main][, rename_namespace(<oldName>, <newName>)];",
                         " ",
                         alias_prefix + "//css_imp",
                         "There are also another two aliases: //css_include and //css_inc. They are equivalents of `//css_import <file>, preserve_main`.",
                         "This makes //css_import a more advanced version of //css_include. So you can use it to solve some more unusual runtime " +
                         "scenarios. It is recommended that you use //css_include if you can. And //css_import if you have to. The //css_include " +
                         "directive is simpler and requires no processing of the file being imported.",
                         "This section only describes behavior specific for //css_import. Thus for the generic behavior of the directive go to the //css_include help.",
                         "If $this (or $this.name) is specified as part of <file> it will be replaced at execution time with the main script full name (or file name only).",
                         " ",
                         "file            - ${<==}name of a script file to be imported at compile-time.",
                         "<preserve_main> - ${<==}do not rename 'static Main'. ",
                         "                  ${<==}.NET allows only one entry point 'static Main' method per application.Thus it is a problem if the primary and " +
                         "the imported scripts both contain 'static Main'.To avoid this the script engine searches the imported script for 'static Main' method " +
                         "and renames it in 'i_Main' and then uses a temporary copy of the processed imported script during the execution. If you need to use the " +
                         "imported script as is, then you should use 'preserve_main' argument with the '//css_import' directive.",
                         "oldName         - ${<==}name of a namespace to be renamed during importing",
                         "newName         - ${<==}new name of a namespace to be renamed during importing",
                         " ",
                         "This directive is used to import one script into another at compile time. Thus code from one script can be exercised in another one." +
                         "the `rename_namespace` clause can appear in the directive multiple times.",
                         section_sep, //------------------------------------
                         " ",
                         "//css_nuget [-noref] [-force[:delay]] [-ver:<version>] [-rt:<runtime>] [-ng:<nuget arguments>] package0[,package1]..[,packageN];",
                         " ",
                         "Downloads/Installs the NuGet package. It also automatically references the downloaded package assemblies.",
                         "Note: The directive switches need to be in the order as above.",
                         "      A new NuGet support available from version 4.7.0 will obsolete some of the options of this directive",
                         "      New NuGet support can be enabled by setting `LegacyNugetSupport` option to false with `css -config:set:LegacyNugetSupport=false`)",
                         "      Read more: https://github.com/oleg-shilo/cs-script/wiki/NuGet-Support",
                         " ",
                         "By default the package is not downloaded again if it was already downloaded.",
                         "If no version is specified then the highest downloaded version (if any) will be used.",
                         "Referencing the downloaded packages can only handle simple dependency scenarios when all downloaded assemblies are to be referenced.",
                         "You should use '-noref' switch and reference assemblies manually for all other cases. For example multiple assemblies with the same file name that " +
                         "target different CLRs (e.g. v3.5 vs v4.0) in the same package.",
                         "Switches:",
                         " -noref         - ${<==}switch for individual packages if automatic referencing isn't desired.",
                         "                  ${<==}You can use 'css_nuget' environment variable for further referencing package content (e.g. //css_dir %css_nuget%\\WixSharp\\**)",
                         "                  ${<==}(Not available with new NuGet support)",
                         " -force[:delay] - ${<==}switch to force individual packages downloading even when they were already downloaded.",
                         "                  ${<==}You can optionally specify delay for the next forced downloading by number of seconds since last download.",
                         "                  ${<==}'-force:3600' will delay it for one hour. This option is useful for preventing frequent download interruptions during active script development.",
                         "                  ${<==}(Not available with new NuGet support)",
                         " -ver:<version> - ${<==}switch to download/reference a specific package version.",
                         " -rt:<runtime>  - ${<==}switch to use specific runtime binaries (e.g. '-rt:netstandard1.3').",
                         "                  ${<==}(Not available with new NuGet support)",
                         " -ng:<args>     - ${<==}switch to pass `nuget.exe`/`dotnet restore` arguments for every individual package.",
                         "                  ${<==}(`-restore:` as an alias of this switch)",
                         " ",
                         "Example: //css_nuget cs-script;",
                         "         //css_nuget -restore:\"-v minimal\" -ver:4.1.2 NLog",
                         "         //css_nuget -ver:4.1.2 -restore:\"-f --no-cache\" NLog",
                         "         //css_nuget -ver:\"4.1.1-rc1\" -rt:netstandard2.0 -ng:\"-f --no-cache\" NLog",
                         " ",
                         "This directive will install CS-Script NuGet package.",
                         "(see http://www.csscript.net/help/script_nugets.html)",
                         section_sep,
                         "//css_args arg0[,arg1]..[,argN];",
                         " ",
                         "Embedded script arguments. The both script and engine arguments are allowed except \"/noconfig\" engine command switch.",
                         "Example: //css_args -dbg, -inmem;",
                         "This directive will always force script engine to execute the script in debug mode.",
                         "Note: the arguments must be coma separated.",
                         section_sep, //------------------------------------
                         "//css_reference <file>;",
                         " ",
                         alias_prefix + "//css_ref",
                         "file - name of the assembly file to be loaded at run-time.",
                         "",
                         "This directive is used to reference assemblies required at run time.",
                         "The assembly must be in GAC, the same folder with the script file or in the 'Script Library' folders (see 'CS-Script settings').",
                         " ",
                         "Note if you use wildcard in the referenced assembly name (e.g. socket.*.dll) the directive will only reference from the first " +
                         "probing directory where the matching file(s) is found. Be careful with the wide wildcard as '*.dll' as they may lead to " +
                         "unpredictable behavior. For example they may match everything from the very first probing directory, which is typically a current " +
                         "directory. Using more specific wildcards is arguably more practical (e.g. 'utils/*.dll', '*Helper.dll', './*.dll')",
                         " ",
                         section_sep, //------------------------------------
                         "//css_precompiler <file 1>,<file 2>;",
                         " ",
                         alias_prefix + "//css_pc",
                         "file - name of the script or assembly file implementing precompiler.",
                         " ",
                         "This directive is used to specify the CS-Script precompilers to be loaded and exercised against script at run time just " +
                         "before compiling it. Precompilers are typically used to alter the script coder before the execution. Thus CS-Script uses " +
                         "built-in precompiler to decorate classless scripts executed with -autoclass switch.",
                         "(see http://www.csscript.net/help/precompilers.html",
                         section_sep, //------------------------------------
                         "//css_searchdir <directory>;",
                         " ",
                         alias_prefix + "//css_dir",
                         "directory - name of the directory to be used for script and assembly probing at run-time.",
                         " ",
                         "This directive is used to extend set of search directories (script and assembly probing).",
                         "The directory name can be a wildcard based expression.In such a case all directories matching the pattern will be this " +
                         "case all directories will be probed.",
                         "The special case when the path ends with '**' is reserved to indicate 'sub directories' case. Examples:",
                         "${<=4}//css_dir packages\\ServiceStack*.1.0.21\\lib\\net40",
                         "${<=4}//css_dir packages\\**",
                         section_sep, //------------------------------------
                         "//css_winapp",
                         " ",
                         alias_prefix + "//css_winapp",
                         "Adds search directories required for running WinForm and WPF scripts.",
                         "Note: you need to use csws.exe engine to run WPF scripts.",
                         "Alternatively you can set environment variable 'CSS_WINAPP' to non empty value and css.exe shim will redirect the " +
                         "execution to the csws.exe executable.",
                         section_sep, //------------------------------------
                         "//css_webapp",
                         " ",
                         alias_prefix + "//css_webapp",
                         "Indicates that the script app needs to be compiled against Microsoft.AspNetCore.App framework.",
                         "A typical example is a WebAPI script application.",
                         section_sep, //------------------------------------
                         "//css_autoclass [style]",
                         " ",
                         alias_prefix + "//css_ac",
                         "OBSOLETE, use top-class native C# 9 feature instead",
                         "Automatically generates 'static entry point' class if the script doesn't define any.",
                         " ",
                         "    //css_ac",
                         "    using System;",
                         " ",
                         "    void Main()",
                         "    {",
                         "        Console.WriteLine(\"Hello World!\");",
                         "    }",
                         " ",
                         "Using an alternative 'instance entry point' is even more convenient (and reliable).",
                         "The acceptable 'instance entry point' signatures are:",
                         " ",
                         "    void main()",
                         "    void main(string[] args)",
                         "    int main()",
                         "    int main(string[] args)",
                         " ",
                         "The convention for the classless (auto-class) code structure is as follows:",
                         " - set of 'using' statements" +
                         " - classless 'main' " +
                         " - user code " +
                         " - optional //css_ac_end directive" +
                         " - optional user code that is not a subject of auto-class decoration" +
                         "(see https://github.com/oleg-shilo/cs-script/wiki/CLI---User-Guide#command-auto-class)",
                         " ",
                         "A special case of auto-class use case is a free style C# code that has no entry point 'main' at all:",
                         " ",
                         "    //css_autoclass freestyle",
                         "    using System;",
                         " ",
                         "    Console.WriteLine(Environment.Version);",
                         " ",
                         "Since it's problematic to reliable auto-detect free style auto-classes, they must be defined with the " +
                         "special parameter 'freestyle' after the '//css_ac' directive",
                         " ",
                         "By default CS-Script decorates the script by adding a class declaration statement to the " +
                         "start of the script routine and a class closing bracket to the end. This may have an unintended " +
                         "effect as any class declared in the script becomes a 'nested class'. While it is acceptable " +
                         "for practically all use-cases it may be undesired for just a few scenarios. For example, any " +
                         "class containing method extensions must be a top level static class, what conflicts with the " +
                         "auto-class decoration algorithm.",
                         " ",
                         "An additional '//css_autoclass_end' ('//css_ac_end') directive can be used to solve this problem.",
                         " ",
                         "It's nothing else but a marker indicating the end of the code that needs to be decorated as (wrapped " +
                         "into) an auto-class.",
                         "This directive allows defining top level static classes in the class-less scripts, which is required for " +
                         "implementing extension methods.",
                         " ",
                         " //css_ac",
                         " using System;",
                         " ",
                         " void main()",
                         " {",
                         "     ...",
                         " }",
                         " ",
                         " //css_ac_end",
                         " ",
                         " static class Extensions",
                         " {",
                         "     static public string Convert(this string text)",
                         "     {",
                         "         ...",
                         "     }",
                         " }",
                         section_sep, //------------------------------------
                         "//css_resource <file>[, <out_file>];",
                         " ",
                         alias_prefix + "//css_res",
                         "file     - name of the compiled resource file (.resources) to be used with the script.",
                         "           ${<==}Alternatively it can be the name of the XML resource file (.resx) that will be compiled on-fly.",
                         "out_file - ${<==}Optional name of the compiled resource file (.resources) to be generated form the .resx input." +
                         "If not supplied then the compiled file will have the same name as the input file but the file extension '.resx' " +
                         "changed to '.resources'.",
                         " ",
                         "This directive is used to reference resource file for script.",
                         " Example: //css_res Scripting.Form1.resources;",
                         "          //css_res Resources1.resx;",
                         "          //css_res Form1.resx, Scripting.Form1.resources;",
                         section_sep, //------------------------------------
                         "//css_co <options>;",
                         " ",
                         "options - options string.",
                         " ",
                         "This directive is used to pass compiler options string directly to the language specific CLR compiling engine.",
                         "Note: ",
                         "- the options may not be compatible with the compiling engine of your choice (see //css_engine)." +
                         "Thus `//css_co /define:CS_SCRIPT` will work for `csc` engine but will not for `dotnet` since it does not support /define.",
                         "- character `;` in compiler options interferes with `//css_...` directives so try to avoid it. Thus " +
                         "use `-d:DEBUG -d:NET4` instead of `-d:DEBUG;NET4`",
                         " Example: //css_co /d:TRACE pass /d:TRACE option to C# compiler",
                         "          //css_co /platform:x86 to produce Win32 executable\n",
                         "          //css_co -nullable:enable -warnaserror:nullable to enable nullable reference types.\n",

                         section_sep, //------------------------------------
                         "//css_engine <csc|dotnet|roslyn>;",
                         " ",
                         alias_prefix + "//css_ng",
                         " ",
                         "This directive is used to select compiler services for building a script into an assembly.",
                         "  dotnet - use `dotnet.exe` and on-fly .NET projects.",
                         "           ${<==}This is a default compiler engine that handles well even complicated " +
                         "heterogeneous multi-file scripts like WPF scripts.",
                         "  csc    - use `csc.exe`. ",
                         "           ${<==}This compiler shows much better performance. Though it is not suitable for WPF scripts.",
                         "This feature is conceptually similar to the VBCSCompiler.exe build server, which is not available in in .NET5/.NET-Core. " +
                         "Even though available on .NET-Fx (Roslyn).",
                         "           ${<==}Using this option can in order of magnitude improve compilation speed. However it's not suitable for " +
                         "compiling WPF scripts because csc.exe cannot compile XAML.",
                         "           ${<==}While this feature useful it will be deprecated when .NET5+ starts distributing its own properly" +
                         "working build server VBCSCompiler.exe.",
                         "  roslyn - use `Microsoft.CodeAnalysis.CSharp.Scripting.dll` (Roslyn). ",
                         "           ${<==}This compiler shows good performance and does not require .NET SDK. Though it is not suitable for WPF scripts. " +
                         "See [this wiki](https://github.com/oleg-shilo/cs-script/wiki/Choosing-Compiler-Engine) for details.",
                         " ",
                         " Example: //css_engine csc" + NewLine,
                         section_sep, //------------------------------------
                         "//css_ignore_namespace <namespace>;",
                         " ",
                         alias_prefix + "//css_ignore_ns",
                         "namespace - name of the namespace. Use '*' to completely disable namespace resolution",
                         " ",
                         "This directive is used to prevent CS-Script from resolving the referenced namespace into assembly.",

                         section_sep, //------------------------------------
                         "//css_ac_end",
                         " ",
                         "This directive is only applicable for class-less scripts executed with '-autoclass' CLI argument. " +
                         "It's nothing else but a marker indicating the end of the code that needs to be decorated as (wrapped " +
                         "into) an auto-class.",
                         "This directive allows achieving top level static classes in the class-less scripts, which is required for " +
                         "implementing extension methods.",
                         " ",
                         " //css_args -acutoclass",
                         " using System;",
                         " ",
                         " void main()",
                         " {",
                         "     ...",
                         " }",
                         " ",
                         " //css_ac_end",
                         " ",
                         " static class Extensions",
                         " {",
                         "     static public void Convert(this string text)",
                         "     {",
                         "         ...",
                         "     }",
                         " }",

                         section_sep, //------------------------------------
                         "//css_prescript file([arg0][,arg1]..[,argN])[ignore];",
                         "//css_postscript file([arg0][,arg1]..[,argN])[ignore];",
                         " ",
                         "Aliases - //css_pre and //css_post",
                         "file    - script file (extension is optional)",
                         "arg0..N - script string arguments",
                         "ignore  - ${<==}continue execution of the main script in case of error",
                         " ",
                         "These directives are used to execute secondary pre- and post-execution scripts.",
                         "If $this (or $this.name) is specified as arg0..N it will be replaced at execution time with the main script full name (or file name only).",
                         "You may find that in many cases precompilers (//css_pc and -pc) are a more powerful and flexible alternative to the pre-execution script.",
                         section_sep, //------------------------------------
                         "{$css_host}",
                         " ",
                         "Note the script engine always sets the following environment variables:",
                         " 'pid'                     - ${<==}host processId (e.g. Environment.GetEnvironmentVariable(\"pid\")",
                         " 'CSScriptRuntime'         - ${<==}script engine version",
                         " 'CSScriptRuntimeLocation' - ${<==}script engine location",
                         " 'cscs_exe_dir'            - ${<==}script engine directory",
                         " 'EntryScript'             - ${<==}location of the entry script",
                         " 'EntryScriptAssembly'     - ${<==}location of the compiled script assembly",
                         " 'location:<asm_hash>'    - ${<==}location of the compiled script assembly.",
                         " ",
                         "This variable is particularly useful as it allows finding the compiled assembly file from the inside of the script code. " +
                         "Even when the script loaded in-memory (InMemoryAssembly setting) but not from the original file. " +
                         "(e.g. var location = Environment.GetEnvironmentVariable(\"location:\" + Assembly.GetExecutingAssembly().GetHashCode()); ",
                         " ",
                         "Note that by default setting of 'location:<asm_hash>' is disabled. You can enable it by calling " +
                         " 'CSScript.EnableScriptLocationReflection = true'.",
                         " ",
                         "The following is the optional set of environment variables that the script engine uses to improve the user experience:",
                         " ",
                         " 'CSS_NUGET' ",
                         "${<=6}location of the NuGet packages scripts can load/reference",
                         " ",
                         " 'CSSCRIPT_ROOT'",
                         "${<=6}script engine location. Used by the engine to locate dependencies (e.g. resgen.exe). Typically this variable is during the CS-Script installation.",
                         " ",
                         " 'CSSCRIPT_CONSOLE_ENCODING_OVERWRITE'",
                         "${<=6}script engine output encoding if the one from the css_confix.xml needs to be overwritten.",
                         " ",
                         " 'CSSCRIPT_INC'",
                         "${<=6}a system wide include directory for the all frequently used user scripts.",
                         "$(csscript_roslyn)",
                         " ",
                         " 'CSSCRIPT_CSC_CMD_LOG'",
                         "${<=6}the location of the log file that will be created during the script execution with the 'csc' compiler engine." +
                         "the file will contain the command line that is used to start `csc.exe` to compile the script. This behavior is useful for " +
                         "the advanced debugging scenarios. Of the environment variable is not set or the value is not a valid file path " +
                         "then no log file will be created.",
                         "$(csscript_roslyn)",

                         "---------",
                         "During the script execution CS-Script always injects a little object inspector class 'dbg'. " +
                         "This class contains static printing methods that mimic Python's 'print()'. It is particularly useful for object inspection in the absence of a proper debugger.",
                         " ",
                         "Examples:",
                         "  dbg.print(\"Now:\", DateTime.Now)        - ${<==}prints concatenated objects.",
                         "  dbg.print(DateTime.Now)                - ${<==}prints object and values of its properties.",
                         "  dbg.printf(\"Now: {0}\", DateTime.Now)   - ${<==}formats and prints object and values of its fields and properties.",
                         "---------",
                         " ",
                         "Any directive has to be written as a single line in order to have no impact on compiling by CLI compliant compiler." +
                         "It also must be placed before any namespace or class declaration.",
                         " ",
                         "---------",
                         "Example:",
                         " ",
                         " //css_include web_api_host.cs;",
                         " //css_reference media_server.dll;",
                         " //css_nuget Newtonsoft.Json;",
                         " ",
                         " using System;",
                         " using static dbg;",
                         " ",
                         " class MediaServer",
                         " {",
                         "     static void Main(string[] args)",
                         "     {",
                         "         print(args);",
                         " ",
                         "         WebApi.SimpleHost(args)",
                         "               .StartAsConosle(\"http://localhost:8080\");",
                         "   }",
                         " }",
                         " ",
                         "Or shorter form:",
                         " ",
                         " //css_args -ac",
                         " //css_inc web_api_host.cs",
                         " //css_ref media_server.dll",
                         " //css_nuget Newtonsoft.Json",
                         " ",
                         " using System;",
                         " ",
                         " void main(string[] args)",
                         " {",
                         "     print(args);",
                         " ",
                         "     WebApi.SimpleHost(args)",
                         "           .StartAsConosle(\"http://localhost:8080\");",
                         " }",
                         " ",
                         "---------",
                         " Project Website: https://github.com/oleg-shilo/cs-script",
                         " ");

            if (Runtime.IsWin)
                syntaxHelp = syntaxHelp.Replace("{$css_host}", "")
                                       .Replace("{$css_init}",
                                        fromLines("//css_init CoInitializeSecurity[(<level>, <capabilities>)];",
                                            " ",
                                                "level - dwImpLevel parameter of CoInitializeSecurity function (see MSDN for sdetails)",
                                                "capabilities - dwCapabilities parameter of CoInitializeSecurity function(see MSDN for sdetails) ",
                                                " ",
                                                "This is a directive for special COM client scripting scenario when you may need to call ",
                                                "CoInitializeSecurity. The problem is that this call must be done before any COM-server invoke calls. ",
                                                "Unfortunately when the script is loaded for the execution it is already too late. Thus ",
                                                "CoInitializeSecurity must be invoked from the script engine even befor the script is loaded.",
                                                section_sep))
                                       .Replace("$(csscript_roslyn)", "");
            else
                syntaxHelp = syntaxHelp.Replace("{$css_host}", "")
                                       .Replace("{$css_init}", "")
                                       .Replace("$(csscript_roslyn)", fromLines(
                                           " 'CSSCRIPT_ROSLYN' - a shadow copy of Roslyn compiler files. ",
                                               "It's created during setup in order to avoid locking deployment directories because of the running Roslyn binaries."));

            var directives = syntaxHelp.Split('\n')
                                       .Where(x => x.StartsWith("//css_"))
                                       .Select(x => "- " + x.TrimEnd())
                                       .JoinBy(NewLine);

            syntaxHelp = syntaxHelp.Replace("{$directives}", directives);

            #endregion SyntaxHelp
        }
    }

    internal class HelpProvider
    {
        public static string ShowHelp(string helpType, params object[] context)
        {
            context = context.Where(x => x != null).ToArray();
            switch (helpType)
            {
                case AppArgs.dir:
                    {
                        ExecuteOptions options = (ExecuteOptions)context[0];
                        Settings settings = CSExecutor.LoadSettings(options);

                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine(CurrentDirectory);

                        foreach (string dir in Environment.ExpandEnvironmentVariables(settings.SearchDirs).Split(",;".ToCharArray()))
                            if (dir.Trim() != "")
                                builder.AppendLine(dir);

                        builder.AppendLine(typeof(HelpProvider).Assembly.GetAssemblyDirectoryName());
                        return builder.ToString();
                    }
                case AppArgs.syntax:
                    {
                        if (context.Any())
                        {
                            var directive = context.First().ToString();
                            var alias = AppArgs.alias_prefix + directive;

                            var lines = AppArgs.SyntaxHelp.GetLines();

                            var top_lines = lines.TakeWhile(x => !x.StartsWith(directive) && !x.StartsWith(alias));
                            var bottom_lines = lines.Skip(top_lines.Count())
                                                    .TakeWhile(x => x != AppArgs.section_sep);

                            var help = top_lines.Reverse()
                                                .TakeWhile(x => x != AppArgs.section_sep)
                                                .Reverse()
                                                .Concat(bottom_lines.TakeWhile(x => x != AppArgs.section_sep))
                                                .JoinBy(NewLine);
                            return help;
                        }
                        // else
                        return AppArgs.SyntaxHelp;
                    }
                case AppArgs.cmd:
                case AppArgs.commands:
                    {
                        Dictionary<string, string> map = new Dictionary<string, string>();
                        int longestArg = 0;

                        foreach (FieldInfo info in typeof(AppArgs).GetFields())
                        {
                            if (info.IsPublic && info.IsLiteral && info.IsStatic && info.FieldType == typeof(string))
                            {
                                string arg = (string)info.GetValue(null);
                                string description = "";

                                if (AppArgs.switch1Help.ContainsKey(arg))
                                    description = AppArgs.switch1Help[arg].Description;
                                else if (AppArgs.switch2Help.ContainsKey(arg))
                                    description = AppArgs.switch2Help[arg].Description;
                                else
                                    continue;

                                if (map.ContainsKey(description))
                                {
                                    string capturedArg = map[description];

                                    if (capturedArg.Length > arg.Length)
                                        map[description] = capturedArg + "|" + arg;
                                    else
                                        map[description] = arg + "|" + capturedArg;
                                }
                                else
                                    map[description] = arg;

                                longestArg = Math.Max(map[description].Length, longestArg);
                            }
                        }

                        StringBuilder builder = new StringBuilder();

                        foreach (string key in map.Keys)
                        {
                            string arg = map[key].Trim();
                            arg = string.Format("{0,-" + longestArg + "}", arg);
                            builder.AppendLine($"{arg}   {key}");
                        }
                        return builder.ToString();
                    }

                default:
                    return "<unknown command>";
            }
        }

        public static string BuildCommandInterfaceHelp(string arg)
        {
            // -out:<file>
            // cli:md
            (string scope, string context) = (arg ?? "").Split(':', 2).ToTupleOf2();

            var includeCLI = (scope.IsEmpty() || scope == "cli" || scope == "-out");
            var includeSyntax = (scope.IsEmpty() || scope == "syntax" || scope == "-out");

            bool mdFormat = (context == "md" || GetEnvironmentVariable("css_help_md").HasText());

            if (arg != null && !scope.IsOneOf("cli", "syntax", "-out"))
            {
                return AppArgs.LookupSwitchHelp(arg) ??
                       "Invalid 'cmd' argument. Use '" + AppInfo.appName + " -cmd' for the list of valid commands." + Environment.NewLine + AppArgs.switch1Help[AppArgs.help].GetFullDoc();
            }

            var builder = new StringBuilder();
            var alreadyPrinted = new List<string>(); // args can be effectively duplicated as the same arg may have multiple aliases (e.g. '-ac|-autoclass')

            if (includeCLI)
            {
                var page_intro =
                    "Like with many other tools CS-Script provides an intensive command line interface that can be used from shell/terminal " +
                    "(e.g. Bash, PowerShell, command-prompt). This interface is particularly useful for environments like Linux, " +
                    "where working from terminal is a predominant development approach." + NewLine + NewLine +
                    "_This document is auto-generated with the command `css -help cli:md`._" + NewLine + NewLine +

                    "## CLI Commands" + NewLine;

                var usage = "Usage: " + AppInfo.appName + " <switch 1> <switch 2> <file> [params] [//x]";

                if (mdFormat)
                {
                    builder
                        .AppendLine(page_intro)
                        .AppendLine("```")
                        .AppendLine(usage)
                        .AppendLine("```")
                        .AppendLine()
                        .AppendLine("---")
                        .AppendLine("### <switch 1>")
                        .AppendLine();
                }
                else
                {
                    builder
                        .AppendLine(AppInfo.appLogo)
                        .AppendLine()
                        .AppendLine("<switch 1>")
                        .AppendLine();
                }

                foreach (AppArgs.ArgInfo info in AppArgs.switch1Help.Values)
                {
                    if (alreadyPrinted.Contains(info.ArgSpec))
                        continue;

                    builder.AppendLine(info.GetFullDoc(mdFormat));
                    builder.AppendLine("");
                    if (mdFormat) builder.AppendLine("---").AppendLine();
                    alreadyPrinted.Add(info.ArgSpec);
                }

                if (!mdFormat)
                    builder.AppendLine("---------");

                builder.AppendLine($"{(mdFormat ? "### " : "")}<switch 2>");
                builder.AppendLine("");
                foreach (AppArgs.ArgInfo info in AppArgs.switch2Help.Values)
                {
                    if (alreadyPrinted.Contains(info.ArgSpec))
                        continue;
                    builder.AppendLine(info.GetFullDoc(mdFormat));
                    builder.AppendLine("");
                    alreadyPrinted.Add(info.ArgSpec);
                }
            }

            if (includeSyntax)
            {
                builder.AppendLine("---------");
                foreach (AppArgs.ArgInfo info in AppArgs.miscHelp.Values)
                {
                    if (alreadyPrinted.Contains(info.ArgSpec))
                        continue;
                    builder.AppendLine(info.GetFullDoc(mdFormat));
                    builder.AppendLine("");
                    alreadyPrinted.Add(info.ArgSpec);
                }
                builder.AppendLine("");
                builder.AppendLine("");
                builder.Append(AppArgs.SyntaxHelp);
            }

            if (mdFormat)
                return builder.ToString().GetLines().Select(x => x.TrimEnd()).JoinBy(NewLine);
            else
                return builder.ToString().Replace("```", "");
        }

        internal class SampleInfo
        {
            public SampleInfo(string code, string fileExtension)
            {
                Code = code;
                FileExtension = fileExtension;
            }

            public string Code;
            public string FileExtension;
        }

        static Dictionary<string, Func<string, SampleInfo[]>> sampleBuilders = new Dictionary<string, Func<string, SampleInfo[]>>
        {
            { "", DefaultSample},
            { "console", CSharp_console_Sample},
            { "console-vb", DefaultVbSample},
            { "vb", DefaultVbSample},
            { "toplevel-x", CSharp_toplevel_extended_Sample},
            { "toplevel", CSharp_toplevel_Sample},
            { "top", CSharp_toplevel_Sample},
            { "freestyle", CSharp_freestyle_Sample},
            { "auto", CSharp_auto_Sample},
            { "winform", CSharp_winforms_Sample},
            { "cmd", CSharp_command_Sample},
            { "winform-vb", DefaultVbDesktopSample},
            { "webapi", context => CSharp_webipi_Sample(context, addOpenApi:false) },
            { "webapi-min", CSharp_webipi_min},
            // { "webapi-openapi", context => CSharp_webipi_Sample(context, addOpenApi:true) },
            { "wpf", CSharp_wpf_Sample },
            { "wpf-cm", CSharp_wpf_ss_Sample },
        };

        static string emptyLine = NewLine + " ";

        public static string BuildSampleHelp() =>
$@"Usage: -new[:<type>] [<otput file>]
      type - script template based on available types.
      output - location to place the generated script file(s).
```
Type           Template
---------------------------------------------------
console         Console script application (Default)
console-vb      Console VB script application
winform         Windows Forms (WinForms) script application
winform-vb      Windows Forms (WinForms) VB script application
wpf             WPF script application
wpf-cm          Caliburn.Micro based WPF script application
toplevel|top    Top-level class script application with no entry point
toplevel-x      Top-level class script application with no entry point; an advanced CS-Script integration samples.

{emptyLine}
Legacy templates:
auto            Auto-class (classless) script application; use 'toplevel' instead
freestyle       Freestyle (no entry point) script application; use 'toplevel' instead
{emptyLine}
Examples:
    cscs -new script
    cscs -new:toplevel script.cs
    cscs -new:console console.cs
    cscs -new:winform myapp.cs
    cscs -new:wpf hello
```".NormalizeNewLines();

        internal static SampleInfo[] BuildSampleCode(string appType, string context)
        {
            appType = appType ?? "";

            if (sampleBuilders.ContainsKey(appType))
                return sampleBuilders[appType](context);
            else
                throw new Exception($"Specified unknown script type '{appType}'");
        }

        static SampleInfo[] CSharp_command_Sample(string context)
        {
            var cs =
@$"using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using static dbg;
using static System.Console;
using static System.Environment;

var help =
@""CS-Script custom command for...
  css {context} [args]"";

if (""?,-?,-help,--help"".Split(',').Contains(args.FirstOrDefault()))
{{
    WriteLine(help);
    return;
}}

WriteLine($""Executing {context} for: [{{string.Join(args, "","")}}]"");
";
            return new[] { new SampleInfo(cs.NormalizeNewLines(), ".cs") };
        }

        static SampleInfo[] CSharp_webipi_Sample(string context, bool addOpenApi)
        {
            // using roslyn engine seems also possible but it will require manually referencing all asp.core assemblies
            var cs =
@"//css_webapp
//css_ng csc
$extrapackages$//css_nuget Swashbuckle.AspNetCore
//css_inc global-usings
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Console.WriteLine(""URL sample: http://localhost:5000/test\n"");

var app = builder.Build();

app.UseSwagger()
   .UseSwaggerUI()
   .UseHttpsRedirection()
   .UseStaticFiles();

app.MapGet(""/test"",
           (HttpRequest request) => new { Name = ""Test Response"", Time = Environment.TickCount })$extracode$;

app.Run();
";
            if (addOpenApi)
            {
                cs = cs.Replace("$extracode$", $"{Environment.NewLine}   .WithOpenApi()")
                       .Replace("$extrapackages$", $"//css_nuget Microsoft.AspNetCore.OpenApi -ver:7.0.0 -rt:net7.0{Environment.NewLine}" +
                                                $"//css_nuget Microsoft.OpenApi{Environment.NewLine}");
            }
            else
            {
                cs = cs.Replace("$extracode$", "")
                       .Replace("$extrapackages$", "");
            }

            return new[] { new SampleInfo(cs.NormalizeNewLines(), ".cs") };
        }

        static SampleInfo[] CSharp_webipi_min(string context)
        {
            // using roslyn engine seems also possible but it will require manually referencing all asp.core assemblies
            var cs =
@"//css_webapp
//css_ng csc; // roslyn engine will require referencing all ASP.NET assemblies and full class
//css_inc global-usings
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

Console.WriteLine(""URL sample: http://localhost:5000/test"");

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet(""/test"",
           (HttpRequest request) => new { Name = ""Test Response"", Time = Environment.TickCount });
app.Run();
";
            return new[] { new SampleInfo(cs.NormalizeNewLines(), ".cs") };
        }

        static SampleInfo[] CSharp_winforms_Sample(string context)
        {
            var cs =
    @"//css_winapp
using System;
using System.Windows.Forms;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.Run(new Form());
    }
}";
            return new[] { new SampleInfo(cs.NormalizeNewLines(), ".cs") };
        }

        static SampleInfo[] CSharp_wpf_ss_Sample(string context)
        {
            var xaml = new StringBuilder()
                    .AppendLine("<Window x:Class=\"MainWindow\"")
                    .AppendLine("    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"")
                    .AppendLine("    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"")
                    .AppendLine("    Width=\"200\" Height=\"150\" WindowStartupLocation=\"CenterScreen\">")
                    .AppendLine("    <StackPanel>")
                    .AppendLine("        <TextBox x:Name=\"Name\" Margin=\"10\" />")
                    .AppendLine("        <Button x:Name=\"SayHello\" Width=\"100\" Height=\"30\" Content=\"Click Me\" />")
                    .AppendLine("    </StackPanel>")
                    .AppendLine("</Window>")
                    .ToString();

            var cs = new StringBuilder()
                .AppendLine("//css_ng dotnet")
                .AppendLine("//css_winapp")
                .AppendLine("//css_nuget -ver:3.2.0 -noref Caliburn.Micro")
                .AppendLine(@"//css_dir %css_nuget%\caliburn.micro\3.2.0\lib\net45")
                .AppendLine(@"//css_dir %css_nuget%\caliburn.micro.core\3.2.0\lib\net45")
                .AppendLine($"//css_inc {Path.GetFileNameWithoutExtension(context)}.xaml")
                .AppendLine("//css_ref PresentationFramework")
                .AppendLine("//css_ref Caliburn.Micro.dll;")
                .AppendLine("//css_ref Caliburn.Micro.Platform.dll")
                .AppendLine("//css_ref Caliburn.Micro.Platform.Core.dll")
                .AppendLine("")
                .AppendLine("using System;")
                .AppendLine("using System.Windows;")
                .AppendLine("using Caliburn.Micro;")
                .AppendLine("")
                .AppendLine("public partial class MainWindow : Window")
                .AppendLine("{")
                .AppendLine("    [STAThread]")
                .AppendLine("    static void Main()")
                .AppendLine("    {")
                .AppendLine("        var view = new MainWindow();")
                .AppendLine("        var model = new MainWindowViewModel();")
                .AppendLine("")
                .AppendLine("        ViewModelBinder.Bind(model, view, null);")
                .AppendLine("")
                .AppendLine("        view.ShowDialog();")
                .AppendLine("    }")
                .AppendLine("")
                .AppendLine("    public MainWindow()")
                .AppendLine("    {")
                .AppendLine("        InitializeComponent();")
                .AppendLine("    }")
                .AppendLine("}")
                .AppendLine("")
                .AppendLine("public class MainWindowViewModel : PropertyChangedBase")
                .AppendLine("{")
                .AppendLine("    string name;")
                .AppendLine("")
                .AppendLine("    public string Name")
                .AppendLine("    {")
                .AppendLine("        get { return name; }")
                .AppendLine("        set")
                .AppendLine("        {")
                .AppendLine("            name = value;")
                .AppendLine("            NotifyOfPropertyChange(() => Name);")
                .AppendLine("            NotifyOfPropertyChange(() => CanSayHello);")
                .AppendLine("        }")
                .AppendLine("    }")
                .AppendLine("")
                .AppendLine("    public bool CanSayHello")
                .AppendLine("    {")
                .AppendLine("        get { return !string.IsNullOrWhiteSpace(Name); }")
                .AppendLine("    }")
                .AppendLine("")
                .AppendLine("    public void SayHello()")
                .AppendLine("    {")
                .AppendLine("        MessageBox.Show($\"Hello {name}!\");")
                .AppendLine("    }")
                .AppendLine("}")
                .ToString();

            return new[]
            {
                new SampleInfo (cs,".cs"),
                new SampleInfo (xaml, ".xaml")
            };
        }

        static SampleInfo[] CSharp_wpf_Sample(string context)
        {
            var xaml = new StringBuilder()
                    .AppendLine("<Window x:Class=\"MainWindow\"")
                    .AppendLine("    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"")
                    .AppendLine("    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"")
                    .AppendLine("    Width=\"400\"")
                    .AppendLine("    Height=\"225\">")
                    .AppendLine("    <Grid>")
                    .AppendLine("        <Button x:Name=\"button\" Width=\"100\" Height=\"30\">Say hello</Button>")
                    .AppendLine("    </Grid>")
                    .AppendLine("</Window>")
                    .ToString();

            var cs = new StringBuilder()
                .AppendLine("//css_ng dotnet")
                .AppendLine("//css_winapp")
                .AppendLine($"//css_inc {Path.GetFileNameWithoutExtension(context)}.xaml")
                .AppendLine("//css_ref PresentationFramework")
                .AppendLine("")
                .AppendLine("using System;")
                .AppendLine("using System.Windows;")
                .AppendLine("")
                .AppendLine("public partial class MainWindow : Window")
                .AppendLine("{")
                .AppendLine("    [STAThread]")
                .AppendLine("    static void Main()")
                .AppendLine("    {")
                .AppendLine("        new MainWindow().ShowDialog();")
                .AppendLine("    }")
                .AppendLine("")
                .AppendLine("    public MainWindow()")
                .AppendLine("    {")
                .AppendLine("        InitializeComponent();")
                .AppendLine("        button.Click += (s, e) => System.Windows.MessageBox.Show(\"Hello World!\");")
                .AppendLine("    }")
                .AppendLine("}")
                .ToString();

            return new[]
            {
                new SampleInfo (cs,".cs"),
                new SampleInfo (xaml, ".xaml")
            };
        }

        static SampleInfo[] CSharp_freestyle_Sample(string context)
        {
            StringBuilder builder = new StringBuilder();

            if (!Runtime.IsWin)
            {
                builder.AppendLine("// #!/usr/local/bin/cscs");
            }

            builder
                .AppendLine("//css_ac freestyle")
                .AppendLine("using System;")
                .AppendLine("using System.IO;")
                .AppendLine("")
                .AppendLine("Directory.GetFiles(@\".\\\").print();")
                .AppendLine("");

            return new[] { new SampleInfo(builder.ToString(), ".cs") };
        }

        static SampleInfo[] CSharp_toplevel_Sample(string context)
        {
            var builder = new StringBuilder();

            if (!Runtime.IsWin)
            {
                builder.AppendLine("// #!/usr/local/bin/cscs");
            }

            if (!isGlobalUsingAvailabe)
                builder.AppendLine("using System;")
                       .AppendLine("using System.IO;")
                       .AppendLine("using System.Diagnostics;")
                       .AppendLine("using static dbg; // for print() extension")
                       .AppendLine("using static System.Environment;")
                       .AppendLine();
            else
                builder.AppendLine("//css_include global-usings")
                       .AppendLine();

            builder.AppendLine("print(\"Hello World!\");")
                   .AppendLine("Console.WriteLine(\"Hello World!\");");

            builder.AppendLine("");

            return new[] { new SampleInfo(builder.ToString(), ".cs") };
        }

        static SampleInfo[] CSharp_toplevel_extended_Sample(string context)
        {
            var builder = new StringBuilder();

            if (!Runtime.IsWin)
            {
                builder.AppendLine("// #!/usr/local/bin/cscs");
            }

            if (!isGlobalUsingAvailabe)
                builder.AppendLine("using System;")
                       .AppendLine("using System.IO;")
                       .AppendLine("using System.Diagnostics;")
                       .AppendLine("using static dbg; // for print() extension")
                       .AppendLine("using static System.Environment;")
                       .AppendLine();
            else
                builder.AppendLine("//css_include global-usings")
                       .AppendLine();

            builder.AppendLine("print(\"Script: \", GetEnvironmentVariable(\"EntryScript\"));")
                   .AppendLine("@\".\\\".list_files(); ")
                   .AppendLine()
                   .AppendLine("static class extensions")
                   .AppendLine("{")
                   .AppendLine("    public static void list_files(this string path)")
                   .AppendLine("        => Directory")
                   .AppendLine("               .GetFiles(path)")
                   .AppendLine("               .print();")
                   .AppendLine("}");

            builder.AppendLine("");

            return new[] { new SampleInfo(builder.ToString(), ".cs") };
        }

        static SampleInfo[] CSharp_auto_Sample(string context)
        {
            var cs = new StringBuilder();

            if (!Runtime.IsWin)
                cs.AppendLine("// #!/usr/local/bin/cscs");

            cs.AppendLine("//css_ac")
              .AppendLine("using System;")
              .AppendLine("using System.IO;")
              .AppendLine("using static dbg; // to use 'print' instead of 'dbg.print'")
              .AppendLine("")
              .AppendLine("void main(string[] args)")
              .AppendLine("{")
              .AppendLine("    (string message, int version) setup_say_hello()")
              .AppendLine("    {")
              .AppendLine("        return (\"Hello from C#\", 9);")
              .AppendLine("    }")
              .AppendLine("")
              .AppendLine("    var info = setup_say_hello();")
              .AppendLine("")
              .AppendLine("    print(info);")
              .AppendLine("}");

            return new[] { new SampleInfo(cs.ToString(), ".cs") };
        }

        static bool isGlobalUsingAvailabe => Globals.LibDir.PathJoin("global-usings.cs").FileExists();

        static SampleInfo[] CSharp10_Sample(string context)
        {
            var builder = new StringBuilder();

            if (!Runtime.IsWin)
            {
                builder.AppendLine("// #!/usr/local/bin/cscs");
            }

            if (!isGlobalUsingAvailabe)
                builder.AppendLine("using System;")
                       .AppendLine("using System.IO;")
                       .AppendLine("using System.Collections.Generic;")
                       .AppendLine("using System.Diagnostics;")
                       .AppendLine("using System.Linq;")
                       .AppendLine("using static dbg; // for print() extension")
                       .AppendLine("using static System.Environment;");
            else
                builder.AppendLine("//css_include global-usings");

            builder
                .AppendLine()
                .AppendLine("\"------------------------------------\".print();")
                .AppendLine("Console.WriteLine($\"Date: {DateTime.Now}\");")
                .AppendLine("(string message, int version) setup_say_hello()")
                .AppendLine("{")
                .AppendLine("    return (\"Hello from C#\", 9);")
                .AppendLine("}")
                .AppendLine("")
                .AppendLine("var info = setup_say_hello();")
                .AppendLine()
                .AppendLine("print(info.message, info.version);")
                .AppendLine()
                .AppendLine("print(Environment.GetEnvironmentVariables()")
                .AppendLine("                 .Cast<object>()")
                .AppendLine("                 .Take(5));")
                .AppendLine("\"------------------------------------\".print();")
                .AppendLine();
            return new[] { new SampleInfo(builder.ToString(), ".cs") };
        }

        static SampleInfo[] CSharp_console_Sample(string context)
        {
            var builder = new StringBuilder();

            if (!Runtime.IsWin)
            {
                builder.AppendLine("// #!/usr/local/bin/cscs");
            }

            if (!isGlobalUsingAvailabe)
                builder.AppendLine("using System;")
                       .AppendLine("using System.IO;")
                       .AppendLine("using System.Collections.Generic;")
                       .AppendLine("using System.Diagnostics;")
                       .AppendLine("using System.Linq;")
                       .AppendLine("using static dbg; // to use 'print' instead of 'dbg.print'");
            else
                builder.AppendLine("//css_include global-usings");

            builder
                .AppendLine("            ")
                .AppendLine("class Script")
                .AppendLine("{")
                .AppendLine("    static public void Main(string[] args)")
                .AppendLine("    {")
                .AppendLine("        (string message, int version) setup_say_hello()")
                .AppendLine("        {")
                .AppendLine("            return (\"Hello from C#\", 9);")
                .AppendLine("        }")
                .AppendLine("")
                .AppendLine("        var info = setup_say_hello();")
                .AppendLine("")
                .AppendLine("        print(info.message, info.version);")
                .AppendLine("")
                .AppendLine("        print(Environment.GetEnvironmentVariables()")
                .AppendLine("                            .Cast<object>()")
                .AppendLine("                            .Take(5));")
                .AppendLine("    }")
                .AppendLine("}");
            return new[] { new SampleInfo(builder.ToString(), ".cs") };
        }

        static SampleInfo[] DefaultSample(string context) => CSharp10_Sample(context);

        static SampleInfo[] DefaultVbSample(string context)
        {
            var code =
        @"' //css_ref System

Imports System

Module Module1
    Sub Main()
        Console.WriteLine(""Hello World! (VB)"")
    End Sub
End Module";

            return new[] { new SampleInfo(code.NormalizeNewLines(), ".vb") };
        }

        static SampleInfo[] DefaultVbDesktopSample(string context)
        {
            var code =
        @"' //css_winapp
' //css_ref System
' //css_ref System.Windows.Forms

Imports System
Imports System.Windows.Forms

Module Module1
    Sub Main()
        MessageBox.Show(""Hello World!(VB)"")
        Console.WriteLine(""Hello World! (VB)"")
    End Sub
End Module";
            return new[] { new SampleInfo(code.NormalizeNewLines(), ".vb") };
        }

        public static string BuildPrecompilerSampleCode()
        {
            return
        @"using System;
using System.Collections;
using System.Collections.Generic;

public class Sample_Precompiler //precompiler class name must end with 'Precompiler'
{
    // possible signatures bool Compile(dynamic context) bool Compile(csscript.PrecompilationContext context)
    public static bool Compile(ref string scriptCode, string scriptFile, bool isPrimaryScript, Hashtable context)
    {
        //The context Hashtable items are:
        //- out context:
        //    NewDependencies
        //    NewSearchDirs
        //    NewReferences
        //    NewIncludes
        //- in context:
        //    SearchDirs
        //    ConsoleEncoding
        //    CompilerOptions
        //if new assemblies are to be referenced add them (see 'Precompilers' in the documentation)
        //var newReferences = (List<string>)context[\""NewReferences\""];
        //newReferences.Add(\""System.Xml.dll\"");

        //if scriptCode needs to be altered assign scriptCode the new value and return true. Otherwise return false

        //scriptCode = \""code after pre-compilation\"";
        //return true;

        return false;
    }
}".NormalizeNewLines();
        }

        // Needed to be a separate property to avoid premature loading of *.CSharp.Scripting.CSharpScript.dll simply because we want to display the engine info
        // Addresses the situation when Roslyn is not a current compiler and cscs is distributed as a single file
        static string Microsoft_CodeAnalysis_CSharp_Scripting_CSharpScript_Assembly_Location => typeof(Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript).Assembly.Location;

        public static string BuildVersionInfo(string arg)
        {
            StringBuilder builder = new StringBuilder();

            string dotNetVer = null;

            if (arg == "--version")
            {
                builder.Append($"{Assembly.GetExecutingAssembly().GetName().Version}");
            }
            else
            {
                builder.AppendLine(AppInfo.appLogo.TrimEnd() + " www.csscript.net (github.com/oleg-shilo/cs-script)")
                       .AppendLine()
                       .AppendLine("   CLR:             " + Environment.Version + (dotNetVer != null ? " (.NET Framework v" + dotNetVer + ")" : ""))
                       .AppendLine("   System:          " + Environment.OSVersion)
                       .AppendLine("   Architecture:    " + (Environment.Is64BitProcess ? "x64" : "x86"));
                if (Runtime.IsWin)
                    builder.AppendLine("   Install dir:     " + (Environment.GetEnvironmentVariable("CSSCRIPT_INSTALLED") ?? "<not integrated>"));

                var asm_path = Assembly.GetExecutingAssembly().Location;
                try
                {
                    builder.AppendLine("   Script engine:   " + Assembly.GetExecutingAssembly().Location);
                }
                catch { }

                // builder.AppendLine("   Config file:     " + (Settings.DefaultConfigFile.FileExists() ? Settings.DefaultConfigFile : "<none>"));
                var compiler = "<default>";

                if (!string.IsNullOrEmpty(asm_path))
                {
                    var settings = Settings.Load(Settings.DefaultConfigFile, false) ?? new Settings();

                    var alt_compiler = settings.ExpandUseAlternativeCompiler();

                    if (!string.IsNullOrEmpty(alt_compiler))
                    {
                        builder.Append("   Provider:          ");
                        builder.AppendLine(alt_compiler);
                        try
                        {
                            var asm = Assembly.LoadFile(CSExecutor.LookupAltCompilerFile(alt_compiler));
                            Type[] types = asm.GetModules()[0].FindTypes(Module.FilterTypeName, "CSSCodeProvider");

                            MethodInfo method = types[0].GetMethod("GetCompilerInfo");

                            if (method != null)
                            {
                                var info = (Dictionary<string, string>)method.Invoke(null, new object[0]);
                                var maxLength = info.Keys.Max(x => x.Length);

                                foreach (var key in info.Keys)
                                    builder.AppendLine("                    " + key + $" - {NewLine}                        " + info[key]);
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        string sdkWarning = Globals.CheckAndGenerateSdkWarning();

                        if (settings.DefaultCompilerEngine == Directives.compiler_csc)
                        {
                            builder.AppendLine($"   Compiler engine: {settings.DefaultCompilerEngine} ({Globals.csc})");
                            builder.AppendLine($"                    -> dotnet ({Globals.dotnet})");
                            if (sdkWarning.HasText())
                                builder.AppendLine($"                    {sdkWarning}");
                        }
                        else if (settings.DefaultCompilerEngine == Directives.compiler_roslyn)
                        {
                            builder.AppendLine($"   Compiler engine: {settings.DefaultCompilerEngine} ({Microsoft_CodeAnalysis_CSharp_Scripting_CSharpScript_Assembly_Location})");
                        }
                        else if (settings.DefaultCompilerEngine == Directives.compiler_dotnet)
                        {
                            builder.AppendLine($"   Compiler engine: {settings.DefaultCompilerEngine} ({Globals.dotnet})");
                            if (sdkWarning.HasText())
                                builder.AppendLine($"                    {sdkWarning}");
                        }
                    }
                }
                else
                    builder.AppendLine(compiler);

                builder.AppendLine("   NuGet manager:   " + NuGet.NuGetExeView)
                       .AppendLine("   NuGet cache:     " + NuGet.NuGetCacheView)
                       .AppendLine("   Custom commands: " + Runtime.CustomCommandsDir)
                       .AppendLine("   Global includes: " + Runtime.GlobalIncludsDir);
            }
            return builder.ToString();
        }
    }
}