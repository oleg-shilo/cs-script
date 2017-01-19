using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace csscript
{
    internal class AppArgs
    {
        public const string help = "help";
        public const string question = "?";
        public const string cmd = "cmd";
        public const string syntax = "syntax";
        public const string s = "s";
        public const string nl = "nl";
        public const string verbose = "verbose";
        public const string ver = "ver";
        public const string v = "v";
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
        public const string noconfig = "noconfig";
        public const string commands = "commands";
        public const string cd = "cd";
        public const string provider = "provider";
        public const string pc = "pc";
        public const string precompiler = "precompiler";
        public const string cache = "cache";
        public const string proj = "proj";
        public static string nathash = "nathash"; //instead of const make it static so this hidden option is not picked by autodocumentor
        static public string syntaxHelp = "";

        static public Dictionary<string, ArgInfo> switch1Help = new Dictionary<string, ArgInfo>();
        static public Dictionary<string, ArgInfo> switch2Help = new Dictionary<string, ArgInfo>();
        static public Dictionary<string, ArgInfo> miscHelp = new Dictionary<string, ArgInfo>();

        internal class ArgInfo
        {
            string argSpec;
            string description;
            string doc = "";
            public ArgInfo(string argSpec, string description, string doc)
            {
                this.argSpec = argSpec;
                this.description = description;
                this.doc = doc;
            }
            public ArgInfo(string argSpec, string description)
            {
                this.argSpec = argSpec;
                this.description = description;
            }
            public string ArgSpec { get { return argSpec; } }
            public string Description { get { return description; } }
            public string FullDoc
            {
                get
                {
                    string offset = "    ";
                    string result = argSpec + "\n" +
                           offset + description;
                    if (doc != "")
                        result += "\n" + offset + doc.Replace("\n", "\n" + offset);

                    return result;
                }
            }
        }

        static AppArgs()
        {
            //http://www.csscript.net/help/Online/index.html
            switch1Help[help] =
            switch1Help[question] = new ArgInfo("-help|-? [command]",
                                                    "Displays either generic or command specific help info.");
            switch1Help[e] = new ArgInfo("-e",
                                                   "Compiles script into console application executable.");
            switch1Help[ew] = new ArgInfo("-ew",
                                                   "Compiles script into Windows application executable.");
            switch1Help[c] = new ArgInfo("-c[:<0|1>]",
                                                   "Uses compiled file (cache file .compiled) if found (to improve performance).",
                                                   "   -c:1|-c  enable caching\n" +
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
                                                   " ls    - lists all cache items.\n" +
                                                   " trim  - removes all abandoned cache items.\n" +
                                                   " clear - removes all cache items.");
            switch1Help[co] = new ArgInfo("-co:<options>",
                                                   "Passes compiler options directly to the language compiler.",
                                                   "(e.g.  -co:/d:TRACE pass /d:TRACE option to C# compiler\n" +
                                                   "or  -co:/platform:x86 to produce Win32 executable)");
            switch1Help[s] = new ArgInfo("-s",
                                                   "Prints content of sample script file",
                                                   "(e.g. " + AppInfo.appName + " /s > sample.cs).");
            switch1Help[wait] = new ArgInfo("-wait[:prompt]",
                                                   "Waits for user input after the execution before exiting.",
                                                   "If specified the execution will proceed with exit only after any STD input is received.\n" +
                                                   "Applicable for console mode only.\n" +
                                                   "prompt - if none specified 'Press any key to continue...' will be used\n");

            switch1Help[ac] =
            switch1Help[autoclass] = new ArgInfo("-ac|-autoclass",
                                                   "Automatically generates 'static entry point' class if the script doesn't define any.",
                                                   "\n" +
                                                   "    using System;\n" +
                                                   "                 \n" +
                                                   "    void Main()\n" +
                                                   "    {\n" +
                                                   "        Console.WriteLine(\"Hello World!\";\n" +
                                                   "    }\n" +
                                                   "\n" +
                                                   "Using an alternative 'instance entry point' is even more convenient (and reliable).\n" +
                                                   "The acceptable 'instance entry point' signatures are:\n" +
                                                   "\n" +
                                                   "  void main()\n" +
                                                   "  void main(string[] args)\n" +
                                                   "  int main()\n" +
                                                   "  int main(string[] args)\n" +
                                                   "\n" +
                                                   "Note, having any active code above entry point is acceptable though it complicates \n" +
                                                   "the troubleshooting if such a code contains errors.\n" +
                                                   "(see http://www.csscript.net/help/AutoClass.html)");
            switch2Help[nl] = new ArgInfo("-nl",
                                                   "No logo mode: No banner will be shown/printed at execution time.",
                                                   "Applicable for console mode only.");
            switch2Help[d] =
            switch2Help[dbg] = new ArgInfo("-dbg|-d",
                                                   "Forces compiler to include debug information.");
            switch2Help[l] = new ArgInfo("-l",
                                                   "'local' (makes the script directory a 'current directory').");
            switch2Help[ver] =
            switch2Help[v] = new ArgInfo("-v",
                                                   "Prints CS-Script version information.");
            switch2Help[inmem] = new ArgInfo("-inmem[:<0|1>]",
                                                   "Loads compiled script in memory before execution.",
                                                   "This mode allows preventing locking the compiled script file. \n" +
                                                   "Can be beneficial for fine concurrency control as it allows changing \n" +
                                                   "and executing the scripts that are already loaded (being executed). This mode is incompatible \n" +
                                                   "with the scripting scenarios that require scriptassembly to be file based (e.g. advanced Reflection).\n" +
                                                   "    -inmem:1   enable caching (which might be disabled globally;\n" +
                                                   "    -inmem:0   disable caching (which might be enabled globally;");
            switch2Help[verbose] = new ArgInfo("-verbose",
                                                   "Prints runtime information during the script execution.",
                                                   "(applicable for console clients only)");
            switch2Help[noconfig] = new ArgInfo("-noconfig[:<file>]",
                                                   "Do not use default CS-Script config file or use alternative one.",
                                                   "Value \"out\" of the <file> is reserved for creating the config file (css_config.xml) " +
                                                   "with the default settings in the current directory.\n" +
                                                   "Value \"print\" of the <file> is reserved for printing the default config file content.\n" +
                                                   "(e.g. " + AppInfo.appName + " -noconfig sample.cs\n" +
                                                   AppInfo.appName + " -noconfig:print > css_VB.xml\n" +
                                                   AppInfo.appName + " -noconfig:c:\\cs-script\\css_VB.xml sample.vb)");
            switch2Help[@out] = new ArgInfo("-out[:<file>]",
                                                   "Forces the script to be compiled into a specific location.",
                                                   "Used only for very fine hosting tuning.\n" +
                                                   "(e.g. " + AppInfo.appName + " -out:%temp%\\%pid%\\sample.dll sample.cs");
            switch2Help[sconfig] = new ArgInfo("-sconfig[:file]",
                                                   "Uses script config file or custom config file as a .NET app.config.",
                                                   "This option might be useful for running scripts, which usually cannot be executed without configuration \n" +
                                                   "file (e.g. WCF, Remoting).\n\n" +
                                                   "(e.g. if -sconfig is used the expected config file name is <script_name>.cs.config or <script_name>.exe.config\n" +
                                                   "if -sconfig:myApp.config is used the expected config file name is myApp.config)");
            switch2Help[r] = new ArgInfo("-r:<assembly 1>:<assembly N>",
                                                   "Uses explicitly referenced assembly.", "It is required only for " +
                                                   "rare cases when namespace cannot be resolved into assembly.\n" +
                                                   "(e.g. " + AppInfo.appName + " /r:myLib.dll myScript.cs).");
            switch2Help[dir] = new ArgInfo("-dir:<directory 1>,<directory N>",
                                                   "Adds path(s) to the assembly probing directory list.",
                                                   "You can use a reserved word 'show' as a directory name to print the configured probing directories.\n" +
                                                   "(e.g. " + AppInfo.appName + " -dir:C:\\MyLibraries myScript.cs\n" +
                                                   " " + AppInfo.appName + " -dir:-show).");
            switch2Help[pc] =
            switch2Help[precompiler] = new ArgInfo("-precompiler[:<file 1>,<file N>]",
                                                   "Specifies custom precompiler. This can be either script or assembly file.",
                                                   "Alias - pc[:<file 1>,<file N>]\n" +
                                                   "If no file(s) specified prints the code template for the custom precompiler. The spacial value 'print' has \n" +
                                                   "the same effect (e.g. " + AppInfo.appName + " -pc:print).\n" +
                                                   "There is a special reserved word '" + CSSUtils.noDefaultPrecompilerSwitch + "' to be used as a file name.\n" +
                                                   "It instructs script engine to prevent loading any built-in precompilers \n" +
                                                   "like the one for removing shebang before the execution.\n" +
                                                   "(see http://www.csscript.net/help/precompilers.html)");
            switch2Help[provider] = new ArgInfo("-provider:<file>",
                                                   "Location of alternative code provider assembly.",
                                                   "If set it forces script engine to use an alternative code compiler.\n" +
                                                   "(see http://www.csscript.net/help/non_cs_compilers.html)");
            switch2Help[syntax] = new ArgInfo("-syntax",
                                                  "Prints documentation for CS-Script specific C# syntax.");
            switch2Help[commands] =
            switch2Help[cmd] = new ArgInfo("-commands|-cmd",
                                                  "Prints list of supporeted commands (arguments).");

            miscHelp["file"] = new ArgInfo("file",
                                                   "Specifies name of a script file to be run.");
            miscHelp["params"] = new ArgInfo("params",
                                                   "Specifies optional parameters for a script file to be run.");
            miscHelp["//x"] = new ArgInfo("//x",
                                                   "Launch debugger just before starting the script.");


            #region SyntaxHelp

            syntaxHelp = "**************************************\n" +
                         "Script specific syntax\n" +
                         "**************************************\n" +
                         "\n" +
                         "Engine directives:\n" +
                         "------------------------------------\n" +
                         "//css_include <file>;\n" +
                         "\n" +
                         "Alias - //css_inc\n" +
                         "file - name of a script file to be included at compile-time.\n" +
                         "\n" +
                         "This directive is used to include one script into another one.It is a logical equivalent of '#include' in C++.\n" +
                         "This directive is a simplified version of //css_import.\n" +
                         "If a relative file path is specified with single-dot preficx it will be automatically converted onto the absolute path \n" +
                         "with respect to the location of the file containing the directive being resolved.\n" +
                         "Note if you use wildcard in the imported script name (e.g. *_build.cs) the directive will only import from the first\n" +
                         "probing directory where the matching file(s) is found. Be careful with the wide wildcard as '*.cs' as they may lead to \n" +
                         "unpredictable behavior. For example they may match everything from the very first probing directory, which is typically a current \n" +
                         "directory. Using more specific wildcards is arguably more practical (e.g. 'utils/*.cs', '*Helper.cs', './*.cs')\n" +
                         "------------------------------------\n" +
                         "//css_import <file>[, preserve_main][, rename_namespace(<oldName>, <newName>)];\n" +
                         "\n" +
                         "Alias - //css_imp\n" +
                         "There are also another two aliases //css_include and //css_inc. They are equivalents of //css_import <file>, preserve_main\n" +
                         "If $this (or $this.name) is specified as part of <file> it will be replaced at execution time with the main script full name (or file name only).\n" +
                         "\n" +
                         "file            - name of a script file to be imported at compile-time.\n" +
                         "<preserve_main> - do not rename 'static Main'\n" +
                         "oldName         - name of a namespace to be renamed during importing\n" +
                         "newName         - new name of a namespace to be renamed during importing\n" +
                         "\n" +
                         "This directive is used to inject one script into another at compile time. Thus code from one script can be exercised in another one.\n" +
                         "'Rename' clause can appear in the directive multiple times.\n" +
                         "------------------------------------\n" +
                         "//css_include <file>;\n" +
                         "\n" +
                         "Alias - //css_inc\n" +
                         "This directive is a full but more convenient equivalent of //css_import <file>, preserve_main;\n" +
                         "------------------------------------\n" +
                         "\n" +
                         "//css_nuget [-noref] [-force[:delay]] [-ver:<version>] [-ng:<nuget arguments>] package0[,package1]..[,packageN];\n" +
                         "\n" +
                         "Downloads/Installs the NuGet package. It also automatically references the downloaded package assemblies.\n" +
                         "Note:\n" +
                         "  The directive switches need to be in the order as above.\n" +
                         "  By default the package is not downloaded again if it was already downloaded.\n" +
                         "  If no version is specified then the highest downloaded version (if any) will be used.\n" +
                         "  Referencing the downloaded packages can only handle simple dependency scenarios when all downloaded assemblies are to be referenced.\n" +
                         "  You should use '-noref' switch and reference assemblies manually for all other cases. For example multiple assemblies with the same file name that \n" +
                         "  target different CLRs (e.g. v3.5 vs v4.0) in the same package.\n" +
                         "Switches:\n" +
                         " -noref - switch for individual packages if automatic referencing isn't desired. You can use 'css_nuget' environment variable for\n" +
                         "          further referencing package content (e.g. //css_dir %css_nuget%\\WixSharp\\**)\n" +
                         " -force[:delay] - switch to force individual packages downloading even when they were already downloaded.\n" +
                         "                  You can optionally specify delay for the next forced downloading by number of seconds since last download.\n" +
                         "                  '-force:3600' will delay it for one hour. This option is useful for preventing frequent download interruptions\n" +
                         "                  during active script development.\n" +
                         " -ver: - switch to download/reference a specific package version.\n" +
                         " -ng: - switch to pass NuGet arguments for every individual package.\n" +
                         "Example: //css_nuget cs-script;\n" +
                         "         //css_nuget -ver:4.1.2 NLog\n" +
                         "         //css_nuget -ver:\"4.1.1-rc1\" -ng:\"-Pre -NoCache\" NLog\n" +
                         "This directive will install CS-Script NuGet package.\n" +
                         "(see http://www.csscript.net/help/script_nugets.html)\n" +
            "------------------------------------\n" +
                         "//css_args arg0[,arg1]..[,argN];\n" +
                         "\n" +
                         "Embedded script arguments. The both script and engine arguments are allowed except \"/noconfig\" engine command switch.\n" +
                         " Example: //css_args -dbg, -inmem;\n This directive will always force script engine to execute the script in debug mode.\n" +
                         "------------------------------------\n" +
                         "//css_reference <file>;\n" +
                         "\n" +
                         "Alias - //css_ref\n" +
                         "file - name of the assembly file to be loaded at run-time.\n" +
                         "\n" +
                         "This directive is used to reference assemblies required at run time.\n" +
                         "The assembly must be in GAC, the same folder with the script file or in the 'Script Library' folders (see 'CS-Script settings').\n" +
                         "------------------------------------\n" +
                         "//css_precompiler <file 1>,<file 2>;\n" +
                         "\n" +
                         "Alias - //css_pc\n" +
                         "file - name of the script or assembly file implementing precompiler.\n" +
                         "\n" +
                         "This directive is used to specify the CS-Script precompilers to be loaded and exercised against script at run time just \n" +
                         "before compiling it. Precompilers are typically used to alter the script coder before the execution. Thus CS-Script uses \n" +
                         "built-in precompiler to decorate classless scripts executed with -autoclass switch.\n" +
                         "(see http://www.csscript.net/help/precompilers.html\n" +
                         "------------------------------------\n" +
                         "//css_searchdir <directory>;\n" +
                         "\n" +
                         "Alias - //css_dir\n" +
                         "directory - name of the directory to be used for script and assembly probing at run-time.\n" +
                         "\n" +
                         "This directive is used to extend set of search directories (script and assembly probing).\n" +
#if !net1
                         "The directory name can be a wildcard based expression.In such a case all directories matching the pattern will be this \n" +
                         "case all directories will be probed.\n" +
                         "The special case when the path ends with '**' is reserved to indicate 'sub directories' case. Examples:\n" +
                         "    //css_dir packages\\ServiceStack*.1.0.21\\lib\\net40\n" +
                         "    //css_dir packages\\**\n" +
#endif
                         "------------------------------------\n" +
                         "//css_resource <file>[, <out_file>];\n" +
                         "\n" +
                         "Alias - //css_res\n" +
                         "file     - name of the compiled resource file (.resources) to be used with the script. Alternatively it can be \n" +
                         "           the name of the XML resource file (.resx) that will be compiled on-fly.\n" +
                         "out_file - optional name of the compiled resource file (.resources) to be generated form the .resx input.\n" +
                         "           If not supplied then the compiled file will have the same name as the input file but the file extension '.resx' \n" +
                         "           changed to '.resources'.\n" +
                         "\n" +
                         "This directive is used to reference resource file for script.\n" +
                         " Example: //css_res Scripting.Form1.resources;\n" +
                         "          //css_res Resources1.resx;\n" +
                         "          //css_res Form1.resx, Scripting.Form1.resources;\n" +
                         "------------------------------------\n" +
                         "//css_co <options>;\n" +
                         "\n" +
                         "options - options string.\n" +
                         "\n" +
                         "This directive is used to pass compiler options string directly to the language specific CLR compiler.\n" +
                         " Example: //css_co /d:TRACE pass /d:TRACE option to C# compiler\n" +
                         "          //css_co /platform:x86 to produce Win32 executable\n\n" +
                         "------------------------------------\n" +
                         "//css_ignore_namespace <namespace>;\n" +
                         "\n" +
                         "Alias - //css_ignore_ns\n" +
                         "namespace - name of the namespace. Use '*' to completely disable namespace resolution\n" +
                         "\n" +
                         "This directive is used to prevent CS-Script from resolving the referenced namespace into assembly.\n" +
                         "------------------------------------\n" +
                         "//css_prescript file([arg0][,arg1]..[,argN])[ignore];\n" +
                         "//css_postscript file([arg0][,arg1]..[,argN])[ignore];\n" +
                         "\n" +
                         "Aliases - //css_pre and //css_post\n" +
                         "file    - script file (extension is optional)\n" +
                         "arg0..N - script string arguments\n" +
                         "ignore  - continue execution of the main script in case of error\n" +
                         "\n" +
                         "These directives are used to execute secondary pre- and post-execution scripts.\n" +
                         "If $this (or $this.name) is specified as arg0..N it will be replaced at execution time with the main script full name (or file name only).\n" +
                         "You may find that in many cases precompilers (//css_pc and -pc) are a more powerful and flexible alternative to the pre-execution script.\n" +
                         "------------------------------------\n" +
                         "{$css_host}" +
                         "Note the script engine always sets the following environment variables:\n" +
                         " 'pid' - host processId (e.g. Environment.GetEnvironmentVariable(\"pid\")\n" +
                         " 'CSScriptRuntime' - script engine version\n" +
                         " 'CSScriptRuntimeLocation' - script engine location\n" +
                         " 'EntryScript' - location of the entry script\n" +
                         " 'EntryScriptAssembly' - location of the compiled script assembly\n" +
                         " 'location:<assm_hash>' - location of the compiled script assembly.\n" +
                         "                          This variable is particularly useful as it allows finding the compiled assembly file from the inside of the script code.\n" +
                         "                          Even when the script loaded in-memory (InMemoryAssembly setting) but not from the original file.\n" +
                         "                          (e.g. var location = Environment.GetEnvironmentVariable(\"location:\" + Assembly.GetExecutingAssembly().GetHashCode());\n" +
                         "\n" +
                         "The following is the optional set of environment variables that the script engine uses to improve the user experience:\n" +
                         " 'CSS_NUGET' - location of the NuGet packages scripts can load/reference\n" +
                         " 'CSSCRIPT_DIR' - script engine location. Used by the engine to locate dependencies (e.g. resgen.exe). Typically this variable is during the CS-Script installation.\n" +
                         " 'CSSCRIPT_CONSOLE_ENCODING_OVERWRITE' - script engine output encoding if the one from the css_confix.xml needs to be overwritten.\n" +
                         " 'CSSCRIPT_INC' - a system wide include directory for the all frequently used user scripts.\n" +
                         "------------------------------------\n" +
                         "During the script execution CS-Script always injects a little object inspector class 'dbg'.\n" +
                         "This class contains static printing methods that mimic Python's 'print()'. It is particularly useful for object inspection in the absence of a proper debugger.\n" +
                         "Examples:\n" +
                         "    dbg.print(\"Now:\", DateTime.Now)        - prints concatenated objects.\n" +
                         "    dbg.print(DateTime.Now)                - prints object and values of its properties.\n" +
                         "    dbg.printf(\"Now: {0}\", DateTime.Now)   - formats and prints object and values of its fields and properties.\n" +
                         "------------------------------------\n" +
                         "\n" +
                         "Any directive has to be written as a single line in order to have no impact on compiling by CLI compliant compiler.\n" +
                         "It also must be placed before any namespace or class declaration.\n" +
                         "\n" +
                         "------------------------------------\n" +
                         "Example:\n" +
                         "\n" +
                         " //css_include web_api_host.cs;\n" +
                         " //css_reference media_server.dll;\n" +
                         " //css_nuget Newtonsoft.Json;\n" +
                         " \n" +
                         " using System;\n" +
                         " using static dbg;\n" +
                         " \n" +
                         " class MediaServer\n" +
                         " {\n" +
                         "     static void Main(string[] args)\n" +
                         "     {\n" +
                         "         print(args);\n" +
                         " \n" +
                         "         WebApi.SimpleHost(args)\n" +
                         "               .StartAsConosle(\"http://localhost:8080\");\n" +
                         "   }\n" +
                         " }\n" +
                         "\n" +
                         //"------\n" +
                         "Or shorter form:\n" +
                         "\n" +
                         " //css_args -ac\n" +
                         " //css_inc web_api_host.cs\n" +
                         " //css_ref media_server.dll\n" +
                         " //css_nuget Newtonsoft.Json\n" +
                         " \n" +
                         " using System;\n" +
                         " \n" +
                         " void main(string[] args)\n" +
                         " {\n" +
                         "     print(args);\n" +
                         " \n" +
                         "     WebApi.SimpleHost(args)\n" +
                         "           .StartAsConosle(\"http://localhost:8080\");\n" +
                         " }\n" +
                         " \n" +
                         "------------------------------------\n" +
                         " Project Website: https://github.com/oleg-shilo/cs-script\n" +
                         "\n";

            if (!Utils.IsLinux())
                syntaxHelp = syntaxHelp.Replace("{$css_host}",
                                                "//css_host [-version:<CLR_Version>] [-platform:<CPU>]\n" +
                                                "\n" +
                                                "CLR_Version - version of CLR the script should be execute on (e.g. //css_host /version:v3.5)\n" +
                                                "CPU - indicates which platforms the script should be run on: x86, Itanium, x64, or anycpu.\n" +
                                                "Sample: //css_host /version:v2.0 /platform:x86;" +
                                                "\nNote this directive only supported on Windows due to the fact that on Linux the x86/x64 hosting implemented via runtime launcher 'mono'." +
                                                "\n" +
                                                "These directive is used to execute script from a surrogate host process. The script engine application (cscs.exe or csws.exe) launches the script\n" +
                                                "execution as a separate process of the specified CLR version and CPU architecture.\n" +
                                                "------------------------------------\n");
            else
                syntaxHelp = syntaxHelp.Replace("{$css_host}", "");

            #endregion
        }
    }

    internal class HelpProvider
    {
        public static string ShowHelp(string helpType, params object[] context)
        {
            switch (helpType)
            {
                case AppArgs.dir:
                    {
                        ExecuteOptions options = (ExecuteOptions) context[0];
                        Settings settings = CSExecutor.LoadSettings(options);

                        StringBuilder builder = new StringBuilder();
                        builder.Append(string.Format("{0}\n", Environment.CurrentDirectory));

                        foreach (string dir in Environment.ExpandEnvironmentVariables(settings.SearchDirs).Split(",;".ToCharArray()))
                            if (dir.Trim() != "")
                                builder.Append(string.Format("{0}\n", dir));

                        builder.Append(string.Format("{0}\n", typeof(HelpProvider).Assembly.GetAssemblyDirectoryName()));
                        return builder.ToString();
                    }
                case AppArgs.syntax: return AppArgs.syntaxHelp;
                case AppArgs.cmd:
                case AppArgs.commands:
                    {
                        Dictionary<string, string> map = new Dictionary<string, string>();
                        int longestArg = 0;
                        foreach (FieldInfo info in typeof(AppArgs).GetFields())
                        {
                            if (info.IsPublic && info.IsLiteral && info.IsStatic && info.FieldType == typeof(string))
                            {
                                string arg = (string) info.GetValue(null);
                                string description = "";

                                if (AppArgs.switch1Help.ContainsKey(arg))
                                    description = AppArgs.switch1Help[arg].Description;
                                if (AppArgs.switch2Help.ContainsKey(arg))
                                    description = AppArgs.switch2Help[arg].Description;

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
                if (AppArgs.switch1Help.ContainsKey(arg))
                    return AppArgs.switch1Help[arg].FullDoc;
                else if (AppArgs.switch2Help.ContainsKey(arg))
                    return AppArgs.switch2Help[arg].FullDoc;
                else
                    return "Invalid 'cmd' argument. Use '" + AppInfo.appName + " -cmd' for the list of valid commands.\n" + AppArgs.switch1Help[AppArgs.help].FullDoc;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(AppInfo.appLogo);
            builder.Append("\nUsage: " + AppInfo.appName + " <switch 1> <switch 2> <file> [params] [//x]\n");
            builder.Append("\n");
            builder.Append("<switch 1>\n\n");

            //cannot use Linq as it can be incompatible target
            List<string> printed = new List<string>();

            foreach (AppArgs.ArgInfo info in AppArgs.switch1Help.Values)
            {
                if (printed.Contains(info.ArgSpec))
                    continue;
                builder.Append(info.FullDoc + "\n\n");
                printed.Add(info.ArgSpec);
            }
            builder.Append("---------\n");
            builder.Append("<switch 2>\n\n");
            foreach (AppArgs.ArgInfo info in AppArgs.switch2Help.Values)
            {
                if (printed.Contains(info.ArgSpec))
                    continue;
                builder.Append(info.FullDoc + "\n\n");
                printed.Add(info.ArgSpec);
            }
            builder.Append("---------\n");
            foreach (AppArgs.ArgInfo info in AppArgs.miscHelp.Values)
            {
                if (printed.Contains(info.ArgSpec))
                    continue;
                builder.Append(info.FullDoc + "\n\n");
                printed.Add(info.ArgSpec);
            }
            builder.Append("\n");
            builder.Append("\n");
            builder.Append(AppArgs.syntaxHelp);

            return builder.ToString();
        }

        public static string BuildSampleCode()
        {
            StringBuilder builder = new StringBuilder();
            if (Utils.IsLinux())
            {
                builder.Append("#!<cscs.exe path> " + CSSUtils.Args.DefaultPrefix + "nl " + Environment.NewLine);
                builder.Append("//css_ref System.Windows.Forms;" + Environment.NewLine);
            }

            builder.Append("using System;" + Environment.NewLine);
            builder.Append("using System.Windows.Forms;" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("class Script" + Environment.NewLine);
            builder.Append("{" + Environment.NewLine);
            if (!Utils.IsLinux())
                builder.Append("    [STAThread]" + Environment.NewLine);
            builder.AppendLine("    static public void Main(string[] args)" + Environment.NewLine);
            builder.AppendLine("    {");
            builder.AppendLine("        for (int i = 0; i < args.Length; i++)");
            builder.AppendLine("            Console.WriteLine(args[i]);");
            builder.AppendLine("");
            builder.AppendLine("        MessageBox.Show(\"Just a test!\");");
            builder.AppendLine("");
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
            builder.Append("        //scriptCode = \"code after precompilation\";" + Environment.NewLine);
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

            string dotNetVer = null;

            if (!Utils.IsLinux())
                dotNetVer = GetDotNetVersion.Get45PlusFromRegistry();


            builder.Append(AppInfo.appLogo.TrimEnd() + " www.csscript.net\n");
            builder.Append("\n");
            builder.Append("   CLR:            " + Environment.Version + (dotNetVer != null ? " (.NET Framework v" + dotNetVer + ")" : "") + "\n");
            builder.Append("   System:         " + Environment.OSVersion + "\n");
#if net4
            builder.Append("   Architecture:   " + (Environment.Is64BitProcess ? "x64" : "x86") + "\n");
#endif
            builder.Append("   Home dir:       " + (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") ?? "<not integrated>") + "\n");
            return builder.ToString();
        }

        public class GetDotNetVersion
        {
            public static string Get45PlusFromRegistry()
            {
#if net4
                var subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
                {
                    if (ndpKey != null && ndpKey.GetValue("Release") != null)
                        return CheckFor45PlusVersion((int) ndpKey.GetValue("Release"));
                    else
                        return null;
                }
#else
                    return null;
#endif
            }

            // Checking the version using >= will enable forward compatibility.
            static string CheckFor45PlusVersion(int releaseKey)
            {
                if (releaseKey >= 394802)
                    return "4.6.2 or later";
                if (releaseKey >= 394254)
                    return "4.6.1";
                if (releaseKey >= 393295)
                    return "4.6";
                if ((releaseKey >= 379893))
                    return "4.5.2";
                if ((releaseKey >= 378675))
                    return "4.5.1";
                if ((releaseKey >= 378389))
                    return "4.5";
                // This code should never execute. A non-null release key should mean
                // that 4.5 or later is installed.
                return "No 4.5 or later version detected";
            }
        }
    }

}