using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace csscript
{
    internal class AppArgs
    {
        public static string nl = "nl";
        public static string nathash = "nathash";       // instead of const make it static so this hidden option is not picked by auto-documenter

        public const string help = "help";
        public const string help2 = "-help";
        public const string question = "?";
        public const string question2 = "-?";
        public const string cmd = "cmd";
        public const string syntax = "syntax";
        public const string s = "s";
        public const string sample = "sample";
        public const string verbose = "verbose";
        public const string v = "v";
        public const string ver = "ver";
        public const string version = "version";
        public const string version2 = "-version";
        public const string c = "c";
        public const string ca = "ca";
        public const string co = "co";
        public const string check = "check";
        public const string r = "r";
        public const string e = "e";
        public const string ew = "ew";
        public const string dir = "dir";
        public const string @out = "out";
        public const string dbg = "dbg";
        public const string d = "d";
        public const string l = "l";
        public const string inmem = "inmem";
        public const string ac = "ac";
        public const string wait = "wait";
        public const string autoclass = "autoclass";
        public const string sconfig = "sconfig";
        public const string config = "config";
        public const string stop = "stop";
        public const string commands = "commands";
        public const string cd = "cd";
        public const string tc = "tc";
        public const string pvdr = "pvdr";
        public const string nuget = "nuget";
        public const string provider = "provider";
        public const string pc = "pc";
        public const string precompiler = "precompiler";
        public const string cache = "cache";
        public const string dbgprint = "dbgprint";
        public const string proj = "proj";
        internal const string proj_dbg = "proj:dbg";    // for internal use only
        static public string SyntaxHelp { get { return syntaxHelp.ToConsoleLines(0); } }
        static string syntaxHelp = "";

        static public Dictionary<string, ArgInfo> switch1Help = new Dictionary<string, ArgInfo>();
        static public Dictionary<string, ArgInfo> switch2Help = new Dictionary<string, ArgInfo>();
        static public Dictionary<string, ArgInfo> miscHelp = new Dictionary<string, ArgInfo>();

        static public bool IsHelpRequest(string arg)
        {
            return (arg == AppArgs.help || arg == AppArgs.question || arg == AppArgs.help2 || arg == AppArgs.question2);
        }

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

            public string GetFullDoc()
            {
                var buf = new StringBuilder();
                buf.AppendLine(argSpec)
                   .Append(' '.Repeat(indent))
                   .Append(description);

                if (doc != "")
                    buf.AppendLine().Append(doc.ToConsoleLines(indent));

                return buf.ToString().TrimEnd();
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

        // #pragma warning disable S3963 // "static" fields should be initialized inline

        static AppArgs()
        {
            //http://www.csscript.net/help/Online/index.html
            switch1Help[help2] =
            switch1Help[help] =
            switch1Help[question] = new ArgInfo("--help|-help|-? [command]",
                                                "Displays either generic or command specific help info.",
                                                    "Reversed order of parameters for the command specific help is also acceptable. " +
                                                    "The all following argument combinations print the same help topic for 'cache' command:",
                                                    "   -help cache",
                                                    "   -? cache",
                                                    "   -cache help",
                                                    "   -cache ?");
            switch1Help[e] = new ArgInfo("-e",
                                         "Compiles script into console application executable.");
            switch1Help[ew] = new ArgInfo("-ew",
                                          "Compiles script into Windows application executable.");
            switch1Help[c] = new ArgInfo("-c[:<0|1>]",
                                         "Uses compiled file (cache file .compiled) if found (to improve performance).",
                                             "   -c:1|-c  enable caching",
                                                 "   -c:0     disable caching (which might be enabled globally);");
            switch1Help[ca] = new ArgInfo("-ca",
                                          "Compiles script file into assembly (cache file .compiled) without execution.");
            switch1Help[cd] = new ArgInfo("-cd",
                                          "Compiles script file into assembly (.dll) without execution.");
            switch1Help[check] = new ArgInfo("-check",
                                             "Checks script for errors without execution.");
            switch1Help[proj] = new ArgInfo("-proj",
                                            "Shows script 'project info' - script and all its dependencies.");

            switch1Help[cache] = new ArgInfo("-cache[:<ls|trim|clear>]",
                                             "Performs script cache operations.",
                                                 " ls    - lists all cache items.",
                                                     " trim  - removes all abandoned cache items.",
                                                     " clear - removes all cache items.");
            switch1Help[co] = new ArgInfo("-co:<options>",
                                          "Passes compiler options directly to the language compiler.",
                                              "(e.g.  -co:/d:TRACE pass /d:TRACE option to C# compiler",
                                                  "or  -co:/platform:x86 to produce Win32 executable)");
            switch1Help[sample] =
            switch1Help[s] = new ArgInfo("-s|-sample[:<C# version>]",
                                         "Prints content of sample script file.",
                                             " -s:7    - prints C# 7 sample. Otherwise it prints the default canonical 'Hello World' sample.",
                                                 "(e.g. " + AppInfo.appName + " -s:7 > sample.cs).");
            switch1Help[wait] = new ArgInfo("-wait[:prompt]",
                                            "Waits for user input after the execution before exiting.",
                                                "If specified the execution will proceed with exit only after any std input is received.",
                                                    "Applicable for console mode only.",
                                                    "prompt - if none specified 'Press any key to continue...' will be used");
            switch1Help[ac] =
            switch1Help[autoclass] = new ArgInfo("-ac|-autoclass[:<0|1|2|out>]",
                                                 "",
                                                     "This argument has the same effect as the script directive '//css_autoclass' placed in directly in the code.",
                                                     " -ac     - enables auto-class decoration (which might be disabled globally).",
                                                     " -ac:0   - disables auto-class decoration (which might be enabled globally).",
                                                     " -ac:1   - same as '-ac'",
                                                     // " -ac:2   - same as '-ac:1' and '-ac' with the additional berakpoint insertion",
                                                     " -ac:out - ",
                                                     "${<=-11}prints auto-class decoration for a given script file. The argument must be followed by the path to script file.",
                                                     " ",
                                                     "Automatically generates 'static entry point' class if the script doesn't define any.",
                                                     " ",
                                                     "See '//css_autoclass' directive for details. Or find the full documentation on GitHub: " +
                                                     " https://github.com/oleg-shilo/cs-script/wiki/CLI---User-Guide#command-auto-class",
                                                     " ");
            // switch2Help[nl] = new ArgInfo("-nl",
            //                                        "No logo mode: No banner will be shown/printed at execution time.",
            //                                        "Applicable for console mode only.");
            switch2Help[d] =
            switch2Help[dbg] = new ArgInfo("-dbg|-d",
                                       "Forces compiler to include debug information.");
            switch2Help[l] = new ArgInfo("-l[:<0|1>]",
                                         "'local'- makes the script directory a 'current directory' (enabled by default).",
                                             " -l:1   the process current directory is assigned to the script directory (default);",
                                                 " -l:0   the process current directory is not adjusted in any way;");
            switch2Help[version2] =
            switch2Help[version] =
            switch2Help[ver] =
            switch2Help[v] = new ArgInfo("-v|-ver|--version",
                                         "Prints CS-Script version information.");

            switch2Help[inmem] = new ArgInfo("-inmem[:<0|1>]",
                                             "Loads compiled script in memory before execution.",
                                                 "This mode allows preventing locking the compiled script file. " +
                                                     "Can be beneficial for fine concurrency control as it allows changing " +
                                                     "and executing the scripts that are already loaded (being executed). This mode is incompatible " +
                                                     "with the scripting scenarios that require script assembly to be file based (e.g. advanced Reflection).",
                                                     " -inmem:1   enable caching (which might be disabled globally);",
                                                     " -inmem:0   disable caching (which might be enabled globally);");
            switch2Help[dbgprint] = new ArgInfo("-dbgprint[:<0:1>]",
                                                "Controls whether to enable Python-like print methods (e.g. dbg.print(DateTime.Now)).",
                                                    "This setting allows controlling dynamic inclusion of the embedded dbg.cs script containing " +
                                                    "implementation of Python-like print methods `dbg.print` and derived extension methods object.print() " +
                                                    "and object.dup(). While `dbg.print` is extremely useful it can and lead to some referencing challenges when " +
                                                    "the script being executed is referencing assemblies compiled with `dbg.print` already included. " +
                                                    "The simplest way o solve this problem is disable the `dbg.cs` inclusion.",
                                                    " -dbgprint:1   enable `dbg.cs` inclusion; Same as `-dbgprint`;",
                                                    " -dbgprint:0   disable `dbg.cs` inclusion;");
            switch2Help[verbose] = new ArgInfo("-verbose",
                                               "Prints runtime information during the script execution.",
                                                   "(applicable for console clients only)");
            switch2Help[stop] = new ArgInfo("-stop",
                                            "Stops all running instances of Roslyn sever (VBCSCompiler.exe).",
                                                "(applicable for .NET/Windows only)");
            switch2Help[tc] = new ArgInfo("-tc",
                                          "Trace compiler input produced by CS-Script code provider CSSRoslynProvider.dll.",
                                              "It's useful when troubleshooting custom compilers (e.g. Roslyn on Linux).");
            switch2Help[config] = new ArgInfo("-config[:<option>]",
                                              "Performs various CS-Script config operations",
                                                  " -config:none               - ${<==}ignores config file (uses default settings)",
                                                  " -config:create             - ${<==}creates config file with default settings",
                                                  " -config:default            - ${<==}prints default config file",
                                                  " -config:<raw|xml>          - ${<==}prints current config file content",
                                                  " -config[:ls]               - ${<==}lists/prints current config values",
                                                  " -config:get:name           - ${<==}prints current config value",
                                                  " -config:set:name=value     - ${<==}sets current config value",
                                                  " -config:set:name=add:value - ${<==}updates the current config value content by appending the specified value.",
                                                  " -config:set:name=del:value - ${<==}updates the current config value content by removing all occurrences of the specified value.",
                                                  " -config:set:roslyn         - ${<==}enables Roslyn integration via configuration (C#7 support)",
                                                  " -config:<file>             - ${<==}uses custom config file",
                                                  " ",
                                                      "Note: The property name in -config:set and -config:set is case insensitive and can also contain '_' " +
                                                      "as a token separator that is ignored during property lookup.",
                                                      "(e.g. " + AppInfo.appName + " -config:none sample.cs",
                                                      "${<=6}" + AppInfo.appName + " -config:default > css_VB.xml",
                                                      "${<=6}" + AppInfo.appName + " -config:set:" + inmem + "=true",
                                                      "${<=6}" + AppInfo.appName + " -config:set:DefaultArguments=add:-ac",
                                                      "${<=6}" + AppInfo.appName + " -config:set:default_arguments=del:-ac",
                                                      "${<=6}" + AppInfo.appName + " -config:c:\\cs-script\\css_VB.xml sample.vb)");
            switch2Help[@out] = new ArgInfo("-out[:<file>]",
                                            "Forces the script to be compiled into a specific location.",
                                                "Used only for very fine hosting tuning.",
                                                    "(e.g. " + AppInfo.appName + " -out:%temp%\\%pid%\\sample.dll sample.cs");

            switch2Help[sconfig] = new ArgInfo("-sconfig[:<file>|none]",
                                               "Uses custom config file as a .NET app.config.",
                                                   "This option might be useful for running scripts, which usually cannot be executed without custom configuration file (e.g. WCF, Remoting).",
                                                   "By default CS-Script expects script config file name to be <script_name>.cs.config or <script_name>.exe.config. " +
                                                   "However if <file> value is specified the it is used as a config file. ",
                                                   "(e.g. if -sconfig:myApp.config is used the expected config file name is myApp.config)");

            switch2Help[r] = new ArgInfo("-r:<assembly 1>,<assembly N>",
                                         "Uses explicitly referenced assembly.", "It is required only for " +
                                             "rare cases when namespace cannot be resolved into assembly.",
                                                 "(e.g. " + AppInfo.appName + " /r:myLib.dll myScript.cs).");

            switch2Help[dir] = new ArgInfo("-dir:<directory 1>,<directory N>",
                                           "Adds path(s) to the assembly probing directory list.",
                                               "You can use a reserved word 'show' as a directory name to print the configured probing directories.",
                                                   "(e.g. " + AppInfo.appName + " -dir:C:\\MyLibraries myScript.cs; " + AppInfo.appName + " -dir:show).");
            switch2Help[pc] =
            switch2Help[precompiler] = new ArgInfo("-precompiler[:<file 1>,<file N>]",
                                                   "Specifies custom precompiler. This can be either script or assembly file.",
                                                   "Alias - pc[:<file 1>,<file N>]",
                                                   "If no file(s) specified prints the code template for the custom precompiler. The spacial value 'print' has " +
                                                   "the same effect (e.g. " + AppInfo.appName + " -pc:print).",
                                                   "There is a special reserved word '" + CSSUtils.noDefaultPrecompilerSwitch + "' to be used as a file name. " +
                                                   "It instructs script engine to prevent loading any built-in precompilers " +
                                                   "like the one for removing shebang before the execution.",
                                                   "(see http://www.csscript.net/help/precompilers.html)");
            switch2Help[pvdr] =
            switch2Help[provider] = new ArgInfo("-pvdr|-provider:<file>",
                                                "Location of the alternative/custom code provider assembly.",
                                                    "Alias - pvdr:<file>",
                                                    "If set it forces script engine to use an alternative code compiler.",
                                                    " ",
                                                    "C#7 support is implemented via Roslyn based provider: '-pvdr:CSSRoslynProvider.dll'." +
                                                    "If the switch is not specified CSSRoslynProvider.dll file will be use as a code provider " +
                                                    "if it is found in the same folder where the script engine is. Automatic CSSRoslynProvider.dll " +
                                                    "loading can be disabled with special 'none' argument: -pvdr:none.",
                                                    "(see http://www.csscript.net/help/non_cs_compilers.html)");
            switch2Help[nuget] = new ArgInfo("-nuget[:<package|purge>]",
                                             "Installs new or updates existing NuGet package.",
                                                 "This command allows light management of the NuGet packages in the CS-Script local package repository (%PROGRAMDATA%\\CS-Script\\nuget).",
                                                     "The tasks are limited to installing, updating and listing the local packages.",
                                                     " ",
                                                     " -nuget           - ${<==}prints the list of all root packages in the repository",
                                                     // " -nuget:purge     - ${<==}detects multiple versions of the same package and removes all but the latest one. ",
                                                     " -nuget:<package> - ${<==}downloads and installs the latest version of the package(s). ",
                                                     "                    ${<==}Wild cards can be used to update multiple packages. For example '-nuget:ServiceStack*' will update all " +
                                                     "already installed ServiceStack packages.",
                                                     "                    ${<==}You can also use the index of the package instead of its full name.",
                                                     " ",
                                                     "Installing packages this way is an alternative to have '//css_nuget -force ...' directive in the script code as it may be " +
                                                     "more convenient for the user to update packages manually instead of having them updated on every script execution/recompilation.");
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

            const string section_sep = "------------------------------------"; // section_sep separator

            var syntaxDetails = fromLines(
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
                    section_sep, //------------------------------------
                    "Note, All //css_* directives should escape any internal CS-Script delimiters by doubling the delimiter character. " +
                    "For example //css_include for 'script(today).cs' should escape brackets as they are the directive delimiters. Thus " +
                    "the correct syntax would be as follows '//css_include script((today)).cs;'",
                    "The delimiters characters are ';,(){}' (e.g. C:\\Program Files ((x86))\\Common...)",
                    "However you should always check CSharpParser.DirectiveDelimiters for the accurate " +
                    "list of the all delimiters.",
                    section_sep, //------------------------------------
                    "//css_include <file>;",
                    "Alias - //css_inc",
                    "file - name of a script file to be included at compile-time.",
                    " ",
                    "This directive is used to include one script into another one. It is a logical equivalent of '#include' in C++. " +
                    "This directive is a full but more convenient equivalent of //css_import <file>, preserve_main;",
                    " ",
                    "If a relative file path is specified with a single-dot prefix it will be automatically converted into the absolute path " +
                    "with respect to the location of the file containing the directive being resolved. " +
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
                    "//css_reference <file>;",
                    " ",
                    "Alias - //css_ref",
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
                    " ",
                    "//css_nuget [-source] [-noref] [-rt:<runtime>] [-force[:delay]] [-ver:<version>] [-ng:<nuget arguments>] package0[,package1]..[,packageN]|sourceUrl ;",
                    " ",
                    "Downloads/Installs the NuGet package. It also automatically references the downloaded package assemblies.",
                    "Note: The directive switches need to be in the order as above.",
                    " ",
                    "By default the package is not downloaded again if it was already downloaded.",
                    "If no version is specified then the highest downloaded version (if any) will be used.",
                    "Referencing the downloaded packages can only handle simple dependency scenarios when all downloaded assemblies are to be referenced.",
                    "You should use '-noref' switch and reference assemblies manually for all other cases. For example multiple assemblies with the same file name that " +
                    "target different CLRs (e.g. v3.5 vs v4.0) in the same package.",
                    "Switches:",
                    " -noref              - ${<==}switch for individual packages if automatic referencing isn't desired. ",
                    "                       ${<==}You can use 'css_nuget' environment variable for further referencing package content (e.g. //css_dir %css_nuget%\\WixSharp\\**)",
                    " -force[:delay]      - ${<==}switch to force individual packages downloading even when they were already downloaded.",
                    "                       ${<==}You can optionally specify delay for the next forced downloading by number of seconds since last download.",
                    "                       ${<==}'-force:3600' will delay it for one hour. This option is useful for preventing frequent download interruptions during active script development.",
                    " -ver:<version>      - ${<==}switch to download/reference a specific package version.",
                    " -ng:<args>          - ${<==}switch to pass NuGet arguments for every individual package.",
                    " -rt:<runtime>  - ${<==}switch to use specific runtime binaries (e.g. '-rt:netstandard1.3').",
                    " -source <sourceUrl> - ${<==}specifies the package source.",
                    "                       ${<==}If '-source' switch is used then no other switches are allowed in the directive.",
                    "                       ${<==}If '-source' is not specified then the default NuGet sources will be used.",
                    " ",
                    "Example: //css_nuget cs-script;",
                    "         //css_nuget -ver:4.1.2 NLog",
                    "         //css_nuget -ver:\"4.1.1-rc1\" -ng:\"-Pre -NoCache\" NLog",
                    "         //css_nuget -source https://api.mycompany.com/nuget/index.json;",
                    " ",
                    "This directive will install CS-Script NuGet package.",
                    "(see http://www.cs-script.net)",
                    section_sep,
                    "//css_args arg0[ arg1]..[ argN];",
                    " ",
                    "Embedded script arguments. The both script and engine arguments are allowed except \"/noconfig\" engine command switch.",
                    "Example: //css_args -dbg -inmem;",
                    "This directive will always force script engine to execute the script in debug mode.",
                    section_sep, //------------------------------------
                    "//css_searchdir <directory>;",
                    " ",
                    "Alias - //css_dir",
                    "directory - name of the directory to be used for script and assembly probing at run-time.",
                    " ",
                    "This directive is used to extend set of search directories (script and assembly probing).",
                    "The directory name can be a wildcard based expression.In such a case all directories matching the pattern will be this " +
                    "case all directories will be probed.",
                    "The special case when the path ends with '**' is reserved to indicate 'sub directories' case. Examples:",
                    "${<=4}//css_dir packages\\ServiceStack*.1.0.21\\lib\\net40",
                    "${<=4}//css_dir packages\\**",
                    section_sep, //------------------------------------
                    "//css_autoclass [style]",
                    " ",
                    "Alias - //css_ac",
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
                    "//css_precompiler <file 1>,<file 2>;",
                    " ",
                    "Alias - //css_pc",
                    "file - name of the script or assembly file implementing precompiler.",
                    " ",
                    "This directive is used to specify the CS-Script precompilers to be loaded and exercised against script at run time just " +
                    "before compiling it. Precompilers are typically used to alter the script coder before the execution. Thus CS-Script uses " +
                    "built-in precompiler to decorate classless scripts executed with -autoclass switch.",
                    "(see http://www.csscript.net/help/precompilers.html",
                    section_sep, //------------------------------------
                    "//css_resource <file>[, <out_file>];",
                    " ",
                    "Alias - //css_res",
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
                    "//css_import <file>[, preserve_main][, rename_namespace(<oldName>, <newName>)];",
                    " ",
                    "Alias - //css_imp",
                    "There are also another two aliases //css_include and //css_inc. They are equivalents of //css_import <file>, preserve_main",
                    "If $this (or $this.name) is specified as part of <file> it will be replaced at execution time with the main script full name (or file name only).",
                    " ",
                    "file            - ${<==}name of a script file to be imported at compile-time.",
                    "<preserve_main> - ${<==}do not rename 'static Main'",
                    "oldName         - ${<==}name of a namespace to be renamed during importing",
                    "newName         - ${<==}new name of a namespace to be renamed during importing",
                    " ",
                    "This directive is used to inject one script into another at compile time. Thus code from one script can be exercised in another one." +
                    "'Rename' clause can appear in the directive multiple times.",
                    section_sep, //------------------------------------
                    "//css_co <options>;",
                    " ",
                    "options - options string.",
                    " ",
                    "This directive is used to pass compiler options string directly to the language specific CLR compiler.",
                    "Note: `;` in compiler options interferes with `//css_...` directives so try to avoid it. Thus " +
                    "use `-d:DEBUG -d:NET4` instead of `-d:DEBUG;NET4`",
                    " Example: //css_co /d:TRACE pass /d:TRACE option to C# compiler",
                    "          //css_co /platform:x86 to produce Win32 executable\n",
                    section_sep, //------------------------------------
                    "{$css_init}",
                    "//css_ignore_namespace <namespace>;",
                    " ",
                    "Alias - //css_ignore_ns",
                    "namespace - name of the namespace. Use '*' to completely disable namespace resolution",
                    " ",
                    "This directive is used to prevent CS-Script from resolving the referenced namespace into assembly.",

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
                    " 'location:<assm_hash>'    - ${<==}location of the compiled script assembly.",
                    " ",
                    "This variable is particularly useful as it allows finding the compiled assembly file from the inside of the script code. " +
                    "Even when the script loaded in-memory (InMemoryAssembly setting) but not from the original file. " +
                    "(e.g. var location = Environment.GetEnvironmentVariable(\"location:\" + Assembly.GetExecutingAssembly().GetHashCode()); ",
                    " ",
                    "Note that by default setting of 'location:<assm_hash>' is disabled. You can enable it by calling " +
                    " 'CSScript.EnableScriptLocationReflection = true'.",
                    " ",
                    "The following is the optional set of environment variables that the script engine uses to improve the user experience:",
                    " ",
                    " 'CSS_NUGET' ",
                    "${<=6}location of the NuGet packages scripts can load/reference",
                    " ",
                    " 'CSSCRIPT_DIR'",
                    "${<=6}script engine location. Used by the engine to locate dependencies (e.g. resgen.exe). Typically this variable is during the CS-Script installation.",
                    " ",
                    " 'CSSCRIPT_CONSOLE_ENCODING_OVERWRITE'",
                    "${<=6}script engine output encoding if the one from the css_confix.xml needs to be overwritten.",
                    " ",
                    " 'CSSCRIPT_INC'",
                    "${<=6}a system wide include directory for the all frequently used user scripts.",
                    "$(csscript_roslyn)",

                    "---------",
#if net4
                    "During the script execution CS-Script always injects a little object inspector class 'dbg'. " +
                    "This class contains static printing methods that mimic Python's 'print()'. It is particularly useful for object inspection in the absence of a proper debugger.",
                    " ",
                    "Examples:",
                    "  dbg.print(\"Now:\", DateTime.Now)        - ${<==}prints concatenated objects.",
                    "  dbg.print(DateTime.Now)                - ${<==}prints object and values of its properties.",
                    "  dbg.printf(\"Now: {0}\", DateTime.Now)   - ${<==}formats and prints object and values of its fields and properties.",
                    "---------",
#endif
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
                    " //css_autoclass",
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
                syntaxDetails = syntaxDetails.Replace("{$css_host}",
                                              fromLines(
                                                  "//css_host [-version:<CLR_Version>] [-platform:<CPU>]",
                                                  " ",
                                                  "CLR_Version - version of CLR the script should be execute on (e.g. //css_host /version:v3.5)",
                                                  "CPU - indicates which platforms the script should be run on: x86, Itanium, x64, or anycpu.",
                                                  "Sample: //css_host /version:v2.0 /platform:x86;",
                                                  " ",
                                                  "Note this directive only supported on Windows due to the fact that on Linux the x86/x64 hosting implemented via runtime launcher 'mono'.",
                                                  " ",
                                                  "These directive is used to execute script from a surrogate host process. The script engine application (cscs.exe or csws.exe) launches the script",
                                                  "execution as a separate process of the specified CLR version and CPU architecture.",
                                                  section_sep))
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
                syntaxDetails = syntaxDetails.Replace("{$css_host}", "")
                                             .Replace("{$css_init}", "")
                                             .Replace("$(csscript_roslyn)", fromLines(
                                                      " 'CSSCRIPT_ROSLYN' - a shadow copy of Roslyn compiler files. ",
                                                      "It's created during setup in order to avoid locking deployment directories because of the running Roslyn binaries."));

            var directives = syntaxDetails.Split('\n')
                                          .Where(x => x.StartsWith("//css_"))
                                          .Select(x => "- " + x.TrimEnd());

            syntaxHelp = fromLines(
                         "**************************************",
                         "Script specific syntax",
                         "**************************************",
                         "Engine directives:",
                         " ",
                         directives.JoinBy(Environment.NewLine),
                         " ",
                         section_sep);

            syntaxHelp += Environment.NewLine + syntaxDetails;

            #endregion SyntaxHelp
        }

        // #pragma warning restore S3963 // "static" fields should be initialized inline
    }

    internal class HelpProvider
    {
        public static string ShowHelp(string helpType, params object[] context)
        {
            switch (helpType)
            {
                case AppArgs.dir:
                    {
                        ExecuteOptions options = (ExecuteOptions)context[0];
                        Settings settings = CSExecutor.LoadSettings(options);

                        StringBuilder builder = new StringBuilder();
                        builder.Append(string.Format("{0}\n", Environment.CurrentDirectory));

                        foreach (string dir in Environment.ExpandEnvironmentVariables(settings.SearchDirs).Split(",;".ToCharArray()))
                            if (dir.Trim() != "")
                                builder.Append(string.Format("{0}\n", dir));

                        builder.Append(string.Format("{0}\n", typeof(HelpProvider).Assembly.GetAssemblyDirectoryName()));
                        return builder.ToString();
                    }
                case AppArgs.syntax: return AppArgs.SyntaxHelp;
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
                            arg = String.Format("{0,-" + longestArg + "}", arg);
                            builder.Append(string.Format("{0}   {1}\n", arg, key));
                        }
                        return builder.ToString();
                    }

                default:
                    return "<unknown command>";
            }
        }

        public static string BuildCommandInterfaceHelp(string arg)
        {
            if (arg != null)
            {
                return AppArgs.LookupSwitchHelp(arg) ??
                       "Invalid 'cmd' argument. Use '" + AppInfo.appName + " -cmd' for the list of valid commands." + Environment.NewLine + AppArgs.switch1Help[AppArgs.help].GetFullDoc();
            }

            var builder = new StringBuilder();
            builder.AppendLine(AppInfo.appLogo);
            builder.AppendLine("Usage: " + AppInfo.appName + " <switch 1> <switch 2> <file> [params] [//x]");
            builder.AppendLine("");
            builder.AppendLine("<switch 1>");
            builder.AppendLine("");

            //cannot use LINQ as it can be incompatible target
            var printed = new List<string>();

            // string fullDoc = GetFullDoc();
            foreach (AppArgs.ArgInfo info in AppArgs.switch1Help.Values)
            {
                if (printed.Contains(info.ArgSpec))
                    continue;
                builder.AppendLine(info.GetFullDoc());
                builder.AppendLine("");
                printed.Add(info.ArgSpec);
            }

            builder.AppendLine("---------");
            builder.AppendLine("<switch 2>");
            builder.AppendLine("");
            foreach (AppArgs.ArgInfo info in AppArgs.switch2Help.Values)
            {
                if (printed.Contains(info.ArgSpec))
                    continue;
                builder.AppendLine(info.GetFullDoc());
                builder.AppendLine("");
                printed.Add(info.ArgSpec);
            }

            builder.AppendLine("---------");
            foreach (AppArgs.ArgInfo info in AppArgs.miscHelp.Values)
            {
                if (printed.Contains(info.ArgSpec))
                    continue;
                builder.AppendLine(info.GetFullDoc());
                builder.AppendLine("");
                printed.Add(info.ArgSpec);
            }
            builder.AppendLine("");
            builder.AppendLine("");
            builder.Append(AppArgs.SyntaxHelp);

            return builder.ToString();
        }

        public static string BuildSampleCode(string version)
        {
            if (version == "7")
                return CSharp7_Sample();
            else
                return DefaultSample();
        }

        static string CSharp7_Sample()
        {
            StringBuilder builder = new StringBuilder();
            if (Runtime.IsLinux)
            {
                builder.AppendLine("// #!/usr/local/bin/cscs");
                builder.AppendLine("//css_ref System.Windows.Forms;");
            }

            builder.AppendLine("using System;");
            builder.AppendLine("using System.Linq;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using static dbg; // to use 'print' instead of 'dbg.print'");
            builder.AppendLine("            ");
            builder.AppendLine("class Script");
            builder.AppendLine("{");
            builder.AppendLine("    static public void Main(string[] args)");
            builder.AppendLine("    {");
            builder.AppendLine("        (string message, int version) setup_say_hello()");
            builder.AppendLine("        {");
            builder.AppendLine("            return (\"Hello from C#\", 7);");
            builder.AppendLine("        }");
            builder.AppendLine("");
            builder.AppendLine("        var info = setup_say_hello();");
            builder.AppendLine("");
            builder.AppendLine("        print(info.message, info.version);");
            builder.AppendLine("");
            builder.AppendLine("        print(Environment.GetEnvironmentVariables()");
            builder.AppendLine("                            .Cast<object>()");
            builder.AppendLine("                            .Take(5));");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        static string DefaultSample()
        {
            StringBuilder builder = new StringBuilder();
            if (Runtime.IsLinux)
            {
                builder.AppendLine("#!<cscs.exe path> " + CSSUtils.Args.DefaultPrefix + "nl ");
                builder.AppendLine("//css_ref System.Windows.Forms;");
            }

            builder.AppendLine("using System;");
            builder.AppendLine("using System.Windows.Forms;");
            builder.AppendLine();
            builder.AppendLine("class Script");
            builder.AppendLine("{");
            builder.AppendLine("    static void Main(string[] args)");
            builder.AppendLine("    {");
            builder.AppendLine("        for (int i = 0; i < args.Length; i++)");
            builder.AppendLine("            Console.WriteLine(args[i]);");
            builder.AppendLine("");
            builder.AppendLine("        MessageBox.Show(\"Just a test!\");");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        public static string BuildPrecompilerSampleCode()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("using System;" + Environment.NewLine);
            builder.Append("using System.Collections;" + Environment.NewLine);
            builder.Append("using System.Collections.Generic;" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("public class Sample_Precompiler //precompiler class name must end with 'Precompiler'" + Environment.NewLine);
            builder.Append("{" + Environment.NewLine);
            builder.Append("    public static bool Compile(ref string scriptCode, string scriptFile, bool isPrimaryScript, Hashtable context)" + Environment.NewLine);
            builder.Append("    {" + Environment.NewLine);
            builder.Append("        //The context Hashtable items are:" + Environment.NewLine);
            builder.Append("        //- out context:" + Environment.NewLine);
            builder.Append("        //    NewDependencies" + Environment.NewLine);
            builder.Append("        //    NewSearchDirs" + Environment.NewLine);
            builder.Append("        //    NewReferences" + Environment.NewLine);
            builder.Append("        //    NewIncludes" + Environment.NewLine);
            builder.Append("        //- in context:" + Environment.NewLine);
            builder.Append("        //    SearchDirs" + Environment.NewLine);
            builder.Append("        //    ConsoleEncoding" + Environment.NewLine);
            builder.Append("        //    CompilerOptions" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("        //if new assemblies are to be referenced add them (see 'Precompilers' in the documentation)" + Environment.NewLine);
            builder.Append("        //var newReferences = (List<string>)context[\"NewReferences\"];" + Environment.NewLine);
            builder.Append("        //newReferences.Add(\"System.Xml.dll\");" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("        //if scriptCode needs to be altered assign scriptCode the new value and return true. Otherwise return false" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("        //scriptCode = \"code after pre-compilation\";" + Environment.NewLine);
            builder.Append("        //return true;" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("        return false;" + Environment.NewLine);
            builder.Append("    }" + Environment.NewLine);
            builder.Append("}" + Environment.NewLine);

            return builder.ToString();
        }

        public static string BuildVersionInfo()
        {
            StringBuilder builder = new StringBuilder();
            var corlib = "".GetType().Assembly.Location;

            string dotNetVer = null;

            if (Runtime.IsWin && !corlib.Contains("mono"))
                dotNetVer = DotNetVersion.Get45PlusFromRegistry();

            builder.AppendLine(AppInfo.appLogo.TrimEnd() + " www.csscript.net (github.com/oleg-shilo/cs-script)");
            builder.AppendLine("");
            builder.AppendLine("   CLR:             " + Environment.Version + (dotNetVer != null ? " (.NET Framework v" + dotNetVer + ")" : ""));
            builder.AppendLine("   System:          " + Environment.OSVersion);
            builder.AppendLine("   Corlib:          " + "".GetType().Assembly.Location);
#if net4
            builder.AppendLine("   Architecture:    " + (Environment.Is64BitProcess ? "x64" : "x86"));
#endif
            builder.AppendLine("   Install dir:     " + (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") ?? "<not integrated>"));

            var asm_path = Assembly.GetExecutingAssembly().Location;
            builder.AppendLine("   Location:        " + asm_path);
            builder.AppendLine("   Config file:     " + (Settings.DefaultConfigFile ?? "<none>"));
            builder.Append("   Compiler:        ");
            var compiler = "<default>";
            if (!string.IsNullOrEmpty(asm_path))
            {
                //System.Diagnostics.Debug.Assert(false);
                var alt_compiler = (Settings.Load(Settings.DefaultConfigFile, false) ?? new Settings()).ExpandUseAlternativeCompiler();
                if (!string.IsNullOrEmpty(alt_compiler))
                {
                    builder.AppendLine(alt_compiler);
                    try
                    {
                        var asm = Assembly.LoadFrom(CSExecutor.LookupAltCompilerFile(alt_compiler));
                        Type[] types = asm.GetModules()[0].FindTypes(Module.FilterTypeName, "CSSCodeProvider");

                        MethodInfo method = types[0].GetMethod("GetCompilerInfo");
                        if (method != null)
                        {
                            var info = (Dictionary<string, string>)method.Invoke(null, new object[0]);
                            var maxLength = info.Keys.Max(x => x.Length);
                            foreach (var key in info.Keys)
                            {
                                builder.AppendLine("                    " + key + " - ")
                                       .AppendLine("                        " + info[key]);
                            }
                        }
                    }
                    catch { }
                }
                else
                    builder.AppendLine(compiler);
            }
            else
                builder.AppendLine(compiler);

            builder.AppendLine("   NuGet manager:   " + NuGet.NuGetExe);
            builder.AppendLine("   NuGet cache:     " + NuGet.NuGetCacheView);

            return builder.ToString();
        }
    }
}