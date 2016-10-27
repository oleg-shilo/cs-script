using System;
using System.Text;

namespace csscript
{

    internal class HelpProvider
    {
        public static string BuildCommandInterfaceHelp()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(AppInfo.appLogo);
            builder.Append("\nUsage: " + AppInfo.appName + " <switch 1> <switch 2> <file> [params] [//x]\n");
            builder.Append("\n");
            builder.Append("<switch 1>\n");
            builder.Append(" {0}?     - Display help info.\n");
            builder.Append(" {0}e     - Compile script into console application executable.\n");
            builder.Append(" {0}ew    - Compile script into Windows application executable.\n");
            builder.Append(" {0}c[:<0|1>]\n");
            builder.Append("          - Use compiled file (cache file .compiled) if found (to improve performance).\n");
            builder.Append("         {0}c:1|{0}c - enable caching\n");
            builder.Append("         {0}c:0 - disable caching (which might be enabled globally);\n");
            builder.Append(" {0}ca    - Compile script file into assembly (cache file .compiled) without execution.\n");
            builder.Append(" {0}cd    - Compile script file into assembly (.dll) without execution.\n\n");
            builder.Append(" {0}check - Check script fro errors without execution.\n\n");
            builder.Append(" {0}cache[:<ls|triem|clear>]\n");
            builder.Append("          - Performs script cache operations.\n");
            builder.Append("            ls    - lists all cache items.\n");
            builder.Append("            trim  - removes all abandoned cache items.\n");
            builder.Append("            clear - removes all cache items.\n\n");
            builder.Append(" {0}co:<options>\n");
            builder.Append("       - Pass compiler options directly to the language compiler\n");
            builder.Append("       (e.g.  {0}co:/d:TRACE pass /d:TRACE option to C# compiler\n");
            builder.Append("        or  {0}co:/platform:x86 to produce Win32 executable)\n\n");
            builder.Append(" {0}s     - Print content of sample script file (e.g. " + AppInfo.appName + " /s > sample.cs).\n");
            builder.Append(" {0}ac | {0}autoclass\n");
            builder.Append("       - Automatically generates wrapper class if the script does not define any class of its own:\n");
            builder.Append("\n");
            builder.Append("         using System;\n");
            builder.Append("                      \n");
            builder.Append("         void Main()\n");
            builder.Append("         {\n");
            builder.Append("             Console.WriteLine(\"Hello World!\");\n");
            builder.Append("         }\n");
            builder.Append("\n");
            builder.Append("\n");
            builder.Append("<switch 2>\n");
            if (AppInfo.appParamsHelp != "")
                builder.Append(" {0}" + AppInfo.appParamsHelp);	//application specific usage info
            builder.Append(" {0}dbg|" + CSSUtils.Args.DefaultPrefix + "d\n");
            builder.Append("         - Force compiler to include debug information.\n");
            builder.Append(" {0}l    - 'local'(makes the script directory a 'current directory')\n");
            builder.Append(" {0}v    - Prints CS-Script version information\n");
            builder.Append("         - Use compiled file (cache file .compiled) if found (to improve performance).\n");
            builder.Append(" {0}inmem[:<0|1>]\n");
            builder.Append("         Loads compiled script in memory before execution. This mode allows preventing locking the \n");
            builder.Append("         compiled script file. Can be beneficial for fine concurrency control as it allows changing \n");
            builder.Append("         and executing the scripts that are already loaded (being executed). This mode is incompatible \n");
            builder.Append("         with the scripting scenarios that require scriptassembly to be file based (e.g. advanced Reflection).\n");
            builder.Append("         {0}inmem:1|inmem - disable caching (which might be enabled globally);\n");
            builder.Append("         {0}inmem:0 - disable caching (which might be enabled globally);\n");
            builder.Append(" {0}verbose \n");
            builder.Append("       - prints runtime information during the script execution (applicable for console clients only)\n");
            builder.Append(" {0}noconfig[:<file>]\n       - Do not use default CS-Script config file or use alternative one.\n");
            builder.Append("         Value \"out\" of the <file> is reserved for creating the config file (css_config.xml) with the default settings.\n");
            builder.Append("         (e.g. " + AppInfo.appName + " {0}noconfig sample.cs\n");
            builder.Append("         " + AppInfo.appName + " {0}noconfig:c:\\cs-script\\css_VB.dat sample.vb)\n");
            builder.Append(" {0}out[:<file>]\n       - Forces the script to be compiled into a specific location. Used only for very fine hosting tuning.\n");
            builder.Append("         (e.g. " + AppInfo.appName + " {0}out:%temp%\\%pid%\\sample.dll sample.cs\n");
            builder.Append(" {0}sconfig[:file]\n       - Use script config file or custom config file as a .NET application configuration file.\n");
            builder.Append("  This option might be useful for running scripts, which usually cannot be executed without configuration file (e.g. WCF, Remoting).\n\n");
            builder.Append("          (e.g. if {0}sconfig is used the expected config file name is <script_name>.cs.config or <script_name>.exe.config\n");
            builder.Append("           if {0}sconfig:myApp.config is used the expected config file name is myApp.config)\n");
            builder.Append(" {0}r:<assembly 1>:<assembly N>\n");
            builder.Append("       - Use explicitly referenced assembly. It is required only for\n");
            builder.Append("         rare cases when namespace cannot be resolved into assembly.\n");
            builder.Append("         (e.g. " + AppInfo.appName + " /r:myLib.dll myScript.cs).\n");
            builder.Append(" {0}dir:<directory 1>,<directory N>\n");
            builder.Append("       - Add path(s) to the assembly probing directory list.\n");
            builder.Append("         (e.g. " + AppInfo.appName + " /dir:C:\\MyLibraries myScript.cs).\n");
            builder.Append(" {0}co:<options>\n");
            builder.Append("       -  Passes compiler options directy to the language compiler.\n");
            builder.Append("         (e.g. {0}co:/d:TRACE pass /d:TRACE option to C# compiler).\n");
            builder.Append(" {0}precompiler[:<file 1>,<file N>]\n");
            builder.Append("         Alias - pc[:<file 1>,<file N>]\n");
            builder.Append("       - specifies custom precompiler file(s). This can be either script or assembly file.\n");
            builder.Append("         If no file(s) specified prints the code template for the custom precompiler.\n");
            builder.Append("         There is a special reserved word '" + CSSUtils.noDefaultPrecompilerSwitch + "' to be used as a file name.\n");
            builder.Append("         It instructs script engine to prevent loading any built-in precompilers \n");
            builder.Append("         like the one for removing shebang before the execution.\n");
            builder.Append("         (see Precompilers chapter in the documentation)\n");
            builder.Append(" {0}provider:<file>\n");
            builder.Append("       - Location of alternative code provider assembly. If set it forces script engine to use an alternative code compiler.\n");
            builder.Append("         (see \"Alternative compilers\" chapter in the documentation)\n");
            builder.Append("\n");
            builder.Append("file   - Specifies name of a script file to be run.\n");
            builder.Append("params - Specifies optional parameters for a script file to be run.\n");
            builder.Append(" //x   - Launch debugger just before starting the script.\n");
            builder.Append("\n");
            if (AppInfo.appConsole) // a temporary hack to prevent showing a huge message box when not in console mode
            {
                builder.Append("\n");
                builder.Append("**************************************\n");
                builder.Append("Script specific syntax\n");
                builder.Append("**************************************\n");
                builder.Append("\n");
                builder.Append("Engine directives:\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_include <file>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_inc\n");
                builder.Append("\n");
                builder.Append("file - name of a script file to be included at compile-time.\n");
                builder.Append("\n");
                builder.Append("This directive is used to include one script into another one.It is a logical equivalent of '#include' in C++.\n");
                builder.Append("This directive is a simplified version of //css_import.\n");
                builder.Append("Note if you use wildcard in the imported script name (e.g. *_build.cs) the directive will only import from the first\n");
                builder.Append("probing directory where the match ing file(s) is found.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_import <file>[, preserve_main][, rename_namespace(<oldName>, <newName>)];\n");
                builder.Append("\n");
                builder.Append("Alias - //css_imp\n");
                builder.Append("There are also another two aliases //css_include and //css_inc. They are equivalents of //css_import <file>, preserve_main\n");
                builder.Append("If $this (or $this.name) is specified as part of <file> it will be replaced at execution time with the main script full name (or file name only).\n");
                builder.Append("\n");
                builder.Append("file            - name of a script file to be imported at compile-time.\n");
                builder.Append("<preserve_main> - do not rename 'static Main'\n");
                builder.Append("oldName         - name of a namespace to be renamed during importing\n");
                builder.Append("newName         - new name of a namespace to be renamed during importing\n");
                builder.Append("\n");
                builder.Append("This directive is used to inject one script into another at compile time. Thus code from one script can be exercised in another one.\n");
                builder.Append("'Rename' clause can appear in the directive multiple times.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_nuget [-noref] [-force[:delay]] [-ver:<version>] [-ng:<nuget arguments>] package0[,package1]..[,packageN];\n");
                builder.Append("\n");
                builder.Append("Downloads/Installs the NuGet package. It also automatically references the downloaded package assemblies.\n");
                builder.Append("Note:\n");
                builder.Append("  The directive switches need to be in the order as above.\n");
                builder.Append("  By default the package is not downloaded again if it was already downloaded.\n");
                builder.Append("  If no version is specified then the highest downloaded version (if any) will be used.\n");
                builder.Append("  Referencing the downloaded packages can only handle simple dependency scenarios when all downloaded assemblies are to be referenced.\n");
                builder.Append("  You should use '-noref' switch and reference assemblies manually for all other cases. For example multiple assemblies with the same file name that \n");
                builder.Append("  target different CLRs (e.g. v3.5 vs v4.0) in the same package.\n");
                builder.Append("Switches:\n");
                builder.Append(" -noref - switch for individual packages if automatic referencing isn't desired. You can use 'css_nuget' environment variable for\n");
                builder.Append("          further referencing package content (e.g. //css_dir %css_nuget%\\WixSharp\\**)\n");
                builder.Append(" -force[:delay] - switch to force individual packages downloading even when they were already downloaded.\n");
                builder.Append("                  You can optionally specify delay for the next forced downloading by number of seconds since last download.\n");
                builder.Append("                  '-force:3600' will delay it for one hour. This option is useful for preventing frequent download interruptions\n");
                builder.Append("                  during active script development.\n");
                builder.Append(" -ver: - switch to download/reference a specific package version.\n");
                builder.Append(" -ng: - switch to pass NuGet arguments for every individual package.\n");
                builder.Append("Example: //css_nuget cs-script;\n");
                builder.Append("         //css_nuget -ver:4.1.2 NLog\n");
                builder.Append("         //css_nuget -ver:\"4.1.1-rc1\" -ng:\"-Pre -NoCache\" NLog\n");
                builder.Append("This directive will install CS-Script NuGet package.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_args arg0[,arg1]..[,argN];\n");
                builder.Append("\n");
                builder.Append("Embedded script arguments. The both script and engine arguments are allowed except \"/noconfig\" engine command switch.\n");
                builder.Append(" Example: //css_args {0}dbg, {0}inmem;\n This directive will always force script engine to execute the script in debug mode.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_reference <file>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_ref\n");
                builder.Append("\n");
                builder.Append("file - name of the assembly file to be loaded at run-time.\n");
                builder.Append("\n");
                builder.Append("This directive is used to reference assemblies required at run time.\n");
                builder.Append("The assembly must be in GAC, the same folder with the script file or in the 'Script Library' folders (see 'CS-Script settings').\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_precompiler <file 1>,<file 2>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_pc\n");
                builder.Append("\n");
                builder.Append("file - name of the script or assembly file implementing precompiler.\n");
                builder.Append("\n");
                builder.Append("This directive is used to specify the CS-Script precompilers to be loaded and exercised against script at run time.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_searchdir <directory>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_dir\n");
                builder.Append("\n");
                builder.Append("directory - name of the directory to be used for script and assembly probing at run-time.\n");
                builder.Append("\n");
                builder.Append("This directive is used to extend set of search directories (script and assembly probing).\n");
#if !net1
                builder.Append("The directory name can be a wild card based expression.In such a case all directories matching the pattern will be this \n");
                builder.Append("case all directories will be probed.\n");
                builder.Append("The special case when the path ends with '**' is reserved to indicate 'sub directories' case. Examples:\n");
                builder.Append("    //css_dir packages\\ServiceStack*.1.0.21\\lib\\net40\n");
                builder.Append("    //css_dir packages\\**\n");
#endif
                builder.Append("------------------------------------\n");
                builder.Append("//css_resource <file>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_res\n");
                builder.Append("\n");
                builder.Append("file - name of the resource file (.resources) to be used with the script.\n");
                builder.Append("\n");
                builder.Append("This directive is used to reference resource file for script.\n");
                builder.Append(" Example: //css_res Scripting.Form1.resources;\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_co <options>;\n");
                builder.Append("\n");
                builder.Append("options - options string.\n");
                builder.Append("\n");
                builder.Append("This directive is used to pass compiler options string directly to the language specific CLR compiler.\n");
                builder.Append(" Example: //css_co /d:TRACE pass /d:TRACE option to C# compiler\n");
                builder.Append("          //css_co /platform:x86 to produce Win32 executable\n\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_ignore_namespace <namespace>;\n");
                builder.Append("\n");
                builder.Append("Alias - //css_ignore_ns\n");
                builder.Append("\n");
                builder.Append("namespace - name of the namespace. Use '*' to completely disable namespace resolution\n");
                builder.Append("\n");
                builder.Append("This directive is used to prevent CS-Script from resolving the referenced namespace into assembly.\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_prescript file([arg0][,arg1]..[,argN])[ignore];\n");
                builder.Append("//css_postscript file([arg0][,arg1]..[,argN])[ignore];\n");
                builder.Append("\n");
                builder.Append("Aliases - //css_pre and //css_post\n");
                builder.Append("\n");
                builder.Append("file    - script file (extension is optional)\n");
                builder.Append("arg0..N - script string arguments\n");
                builder.Append("ignore  - continue execution of the main script in case of error\n");
                builder.Append("\n");
                builder.Append("These directives are used to execute secondary pre- and post-action scripts.\n");
                builder.Append("If $this (or $this.name) is specified as arg0..N it will be replaced at execution time with the main script full name (or file name only).\n");
                builder.Append("------------------------------------\n");
                builder.Append("//css_host [/version:<CLR_Version>] [/platform:<CPU>]\n");
                builder.Append("\n");
                builder.Append("CLR_Version - version of CLR the script should be execute on (e.g. //css_host /version:v3.5)\n");
                builder.Append("CPU - indicates which platforms the script should be run on: x86, Itanium, x64, or anycpu.\n");
                builder.Append("Sample: //css_host /version:v2.0 /platform:x86;");
                builder.Append("\n");
                builder.Append("These directive is used to execute script from a surrogate host process. The script engine application (cscs.exe or csws.exe) launches the script\n");
                builder.Append("execution as a separate process of the specified CLR version and CPU architecture.\n");
                builder.Append("------------------------------------\n");
                builder.Append("Note the script engine always sets the following environment variables:\n");
                builder.Append(" 'pid' - host processId (e.g. Environment.GetEnvironmentVariable(\"pid\")\n");
                builder.Append(" 'CSScriptRuntime' - script engine version\n");
                builder.Append(" 'CSScriptRuntimeLocation' - script engine location\n");
                builder.Append(" 'css_nuget' - location of the NuGet packages scripts can load/reference\n");
                builder.Append(" 'EntryScript' - location of the entry script\n");
                builder.Append(" 'EntryScriptAssembly' - location of the compiled script assembly\n");
                builder.Append(" 'location:<assm_hash>' - location of the compiled script assembly.\n");
                builder.Append("                          This variable is particularly useful as it allows finding the compiled assembly file from the inside of the script code.\n");
                builder.Append("                          Even when the script loaded in-memory (InMemoryAssembly setting) but not from the original file.\n");
                builder.Append("                          (e.g. var location = Environment.GetEnvironmentVariable(\"location:\" + Assembly.GetExecutingAssembly().GetHashCode());\n");
                builder.Append("------------------------------------\n");
                builder.Append("\n");
                builder.Append("Any directive has to be written as a single line in order to have no impact on compiling by CLI compliant compiler.\n");
                builder.Append("It also must be placed before any namespace or class declaration.\n");
                builder.Append("\n");
                builder.Append("------------------------------------\n");
                builder.Append("Example:\n");
                builder.Append("\n");
                builder.Append(" using System;\n");
                builder.Append(" //css_prescript com(WScript.Shell, swshell.dll);\n");
                builder.Append(" //css_import tick, rename_namespace(CSScript, TickScript);\n");
                builder.Append(" //css_reference teechart.lite.dll;\n");
                builder.Append(" \n");
                builder.Append(" namespace CSScript\n");
                builder.Append(" {\n");
                builder.Append("   class TickImporter\n");
                builder.Append("   {\n");
                builder.Append("      static public void Main(string[] args)\n");
                builder.Append("      {\n");
                builder.Append("         TickScript.Ticker.i_Main(args);\n");
                builder.Append("      }\n");
                builder.Append("   }\n");
                builder.Append(" }\n");
                builder.Append("\n");
            }

            //return string.Format(builder.ToString(), CSSUtils.Args.DefaultPrefix); //for some reason Format(..) fails
            return builder.ToString().Replace("{0}", "-");
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
            builder.Append("    static public void Main(string[] args)" + Environment.NewLine);
            builder.Append("    {" + Environment.NewLine);
            builder.Append("        for (int i = 0; i < args.Length; i++)" + Environment.NewLine);
            builder.Append("            Console.WriteLine(args[i]);" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("        MessageBox.Show(\"Just a test!\");" + Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append("    }" + Environment.NewLine);
            builder.Append("}" + Environment.NewLine);

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
            builder.Append(AppInfo.appLogo.TrimEnd() + " www.csscript.net\n");
            builder.Append("\n");
            builder.Append("   CLR:            " + Environment.Version + "\n");
            builder.Append("   System:         " + Environment.OSVersion + "\n");
#if net4
            builder.Append("   Architecture:   " + (Environment.Is64BitProcess ? "x64" : "x86") + "\n");
#endif
            builder.Append("   Home dir:       " + (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") ?? "<not integrated>") + "\n");
            return builder.ToString();
        }
    }

}