////css_dbg /t:winexe, /args:/prj "e:\cs-script\Dev\Dictionary\Dictionary\dictionary.cs";
////css_args /dbg;
//css_ref System.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using CSScriptLibrary;
using Microsoft.Win32;
using csscript;

namespace VS150 //Visual Studio 2017
{
    class Script
    {
        static string usage = "Usage: cscscript debugVS15.0 [/e] [[/prj] scriptFile]|[[/r] projectFile] ...\nLoads C# script file into temporary VS 10.0 C# project and opens it.\n\n" +
                                "</e> - Express edition of Visual Studio 2017\n" +
                                "</prj> - command switch to create project without opening it\n" +
                                "</print> - prints into STDOUT all files (dependant scripts and assemblies).\n           Not to be used with any other argument.\n" +
                                "</noide> - prepares the project file and exits (used only from VSX).\n           Not to be used with any other argument.\n" +
                                "</r> - command switch to refresh project content of a .csproj file.\n\n" +
                                "use //css_dbg directive in code to set on-fly project settings.\n" +
                                "Example: //css_dbg /t:winexe, /args:\"Test argument\", /platform:x86;\n";
        public enum IDEEditions
        {
            normal,
            express,
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        static string GetTempCSSDir()
        {
            return Path.Combine(Path.GetTempPath(), "CSSCRIPT");
        }

        static public void Main(string[] args)
        {
            ClearOldSolutions();
            //Debug.Assert(false);
            SetEnvironmentVariable("CSScriptDebugging", "VS15.0");
            if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
            {
                Console.WriteLine(usage);
            }
            else if (args[0].Trim().ToLower() == "/prj")
            {
                scriptFile = ResolveScriptFile(args[1]);
                try
                {
                    VS150IDE.IsolateProject(scriptFile, Path.Combine(Path.GetDirectoryName(scriptFile), Path.GetFileNameWithoutExtension(scriptFile)));
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified file could not be linked to the temp project:\n" + e.Message);
                }
            }
            else if (args[0].Trim().ToLower() == "/r")
            {
                string projFile = args[1];

                try
                {
                    VS150IDE.RefreshProject(projFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified file could not be linked to the temp project:\n" + e.Message);
                }
            }
            else if (args[0].Trim().ToLower() == "/print")
            {
                try
                {
                    // Note "/print" is to be only invoked from VSX, which  can only be hosted by full VS. Thus "/print" and "/e" cannot come together.
                    scriptFile = args[1];

                    ScriptParser parser = new ScriptParser(scriptFile, Script.SearchDirs, false);

                    foreach (string dir in parser.SearchDirs)
                        AddSearchDir(dir);

                    Console.WriteLine("Src:{0}", scriptFile);

                    foreach (string file in parser.SaveImportedScripts())
                        Console.WriteLine("Src:{0}", file);

                    string[] defaultAsms = (CSScript.GlobalSettings.DefaultRefAssemblies ?? "")
                                            .Replace(" ", "")
                                            .Split(";,".ToCharArray());

                    List<string> referencedAssemblies = new List<string>();

                    List<string> referencedNamespaces = new List<string>();
                    referencedNamespaces.AddRange(parser.ReferencedNamespaces);
                    referencedNamespaces.AddRange(defaultAsms);

                    foreach (string name in referencedNamespaces)
                        if (!parser.IgnoreNamespaces.Contains(name))
                            referencedAssemblies.AddRange(AssemblyResolver.FindAssembly(name, SearchDirs));

                    foreach (string asm in parser.ReferencedAssemblies) //some assemblies were referenced from code
                        referencedAssemblies.AddRange(AssemblyResolver.FindAssembly(asm, SearchDirs));

                    foreach (string asm in parser.ResolvePackages())
                        referencedAssemblies.Add(asm);

                    foreach (string file in referencedAssemblies.Distinct())
                        Console.WriteLine("Asm:{0}", file);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified file could not be linked to the temp project:\n" + e.Message);
                }
            }
            else
            {
                try
                {
                    bool doNotOpenIDE = false;

                    IDEEditions edition = IDEEditions.normal;
                    scriptFile = args[0];

                    if (args[0].Trim().ToLower() == "/e")
                    {
                        edition = IDEEditions.express;
                        scriptFile = args[1];
                    }

                    // Note "/noide" is to be only invoked from VSX, which  can only be hosted by full VS. Thus "/noide" and "/e" cannot come together.
                    if (args[0].Trim().ToLower() == "/noide")
                    {
                        doNotOpenIDE = true;
                        scriptFile = args[1];
                    }

                    scriptFile = ResolveScriptFile(scriptFile);
                    RunPreScripts(scriptFile);

                    string tempDir = Path.Combine(GetTempCSSDir(), Environment.TickCount.ToString() + ".shell");
                    string solutionFile = VS150IDE.CreateProject(scriptFile, tempDir);
                    string projFile = Path.ChangeExtension(solutionFile, ".csproj");

                    //"lock" the directory to indicate that it is in use
                    File.WriteAllText(Path.Combine(tempDir, "host.pid"), Process.GetCurrentProcess().Id.ToString());

                    //open project
                    Environment.CurrentDirectory = Path.GetDirectoryName(scriptFile);

                    Process myProcess = new Process();
                    myProcess.StartInfo.FileName = VS150IDE.GetIDEFile(edition);

                    if (myProcess.StartInfo.FileName == "<not defined>")
                    {
                        if (edition == IDEEditions.express)
                            myProcess.StartInfo.FileName = VS150IDE.GetIDEFile(IDEEditions.normal);
                        else
                            myProcess.StartInfo.FileName = VS150IDE.GetIDEFile(IDEEditions.express);
                    }

                    AddToRecentScripts(scriptFile);

                    if (!doNotOpenIDE)
                    {
                        myProcess.StartInfo.Arguments = "\"" + solutionFile + "\" " + " /command Edit.OpenFile " + "\"" + scriptFile + "\"";
                        myProcess.Start();
                        myProcess.WaitForExit();
                    }
                    else
                    {
                        Console.WriteLine("Solution File: " + solutionFile);
                    }

                    if (doNotOpenIDE)
                    {
                        //calling party is responsible for cleanup
                    }
                    else
                    {
                        //do not cleanup as VS2017 may relaunch the process so we cannot rely on myProcess.WaitForExit();
                        ClearOldSolutions();
                        return;

                        //do clean up
                        foreach (string file in VS150IDE.GetImportedScripts(projFile))
                        {
                            DeleteSatelliteFiles(file);
                            if (Path.GetFileName(file).StartsWith("i_")) //imported modified files have name "i_file_XXXXXX.cs>"
                            {
                                DeleteSatelliteFiles(file);
                                File.SetAttributes(file, FileAttributes.Normal);
                                File.Delete(file);
                            }
                        }

                        try
                        {
                            Directory.Delete(tempDir, true);
                        }
                        catch { }

                        RunPostScripts(scriptFile);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified file could not be linked to the temp project:\n" + e);
                }
            }
        }

        static void ClearOldSolutions()
        {
            foreach (var dir in Directory.GetDirectories(GetTempCSSDir(), "*.shell", SearchOption.TopDirectoryOnly))
            {
                TimeSpan age = DateTime.Now - Directory.GetCreationTime(dir);

                if (age > TimeSpan.FromHours(6))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch { }
                }
            }
        }

        static void AddToRecentScripts(string file)
        {
            try
            {
                //Debug.Assert(false);

                string historyFile = Path.Combine(GetTempCSSDir(), "VS2017_recent.txt");
                int maxHistoryCount = 5;

                List<string> lines = new List<string>();

                try
                {
                    lines.AddRange(File.ReadAllLines(historyFile));
                }
                catch { }

                using (var writer = new StreamWriter(historyFile + ".pending"))
                {
                    List<string> matchingEntries = new List<string>();

                    foreach (string line in lines)
                        if (line.EndsWith(file))
                            matchingEntries.Add(line);

                    /*
                    var matchingEntries1 = (from line in lines
                                           where line.EndsWith(file)
                                           select line)
                                          .ToArray(); //important to call ToArray() in order to create a disconnected copy of the "lines" subset
                    */
                    foreach (string entry in matchingEntries)
                        lines.Remove(entry);

                    int countEccess = lines.Count - maxHistoryCount - 1;

                    if (countEccess > 0)
                        foreach (string entry in lines.ToArray().Reverse())
                        {
                            if (!entry.StartsWith("<pinned>"))
                            {
                                countEccess--;
                                lines.Remove(entry);
                            }

                            if (countEccess == 0)
                                break;
                        }

                    if (matchingEntries.Count() != 0)
                        lines.Insert(0, matchingEntries.First());
                    else
                        lines.Insert(0, file);

                    foreach (var line in lines)
                        writer.WriteLine(line);
                }

                File.Copy(historyFile + ".pending", historyFile, true);
                File.Delete(historyFile + ".pending");
            }
            catch { }
        }

        static void DeleteSatelliteFiles(string file)
        {
            try
            {
                if (File.Exists(Path.ChangeExtension(file, ".resx")))
                    File.Delete(Path.ChangeExtension(file, ".resx"));
                else if (File.Exists(Path.ChangeExtension(file, ".layout")))
                    File.Delete(Path.ChangeExtension(file, ".layout"));
            }
            catch { }
        }

        static string ResolveScriptFile(string file)
        {
            if (Path.GetExtension(file) == "")
                return Path.GetFullPath(file + ".cs");
            else
                return Path.GetFullPath(file);
        }

        static Settings GetSystemWideSettings()
        {
            string defaultConfig = Path.GetFullPath(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\css_config.xml"));

            if (File.Exists(defaultConfig))
                return Settings.Load(defaultConfig);
            else
                return null;
        }

        static string scriptFile;
        static List<string> searchDirsList = CreateSearchDirs();

        static void AddSearchDir(string newDir)
        {
            foreach (string dir in searchDirsList)
            {
                if (string.Compare(newDir, dir, true) == 0)
                    return;
            }

            searchDirsList.Add(newDir);
        }

        static List<string> CreateSearchDirs()
        {
            var retval = new List<string>();

            if (scriptFile != null)
                retval.Add(Path.GetDirectoryName(Path.GetFullPath(scriptFile)));

            retval.Add(Path.GetFullPath(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\lib")));

            Settings settings = GetSystemWideSettings();

            if (settings != null)
            {
                foreach (string dirItem in settings.SearchDirs.Split(';'))
                    if (dirItem != "")
                    {
                        string dir = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dirItem));
                        bool alreadyAdded = false;

                        foreach (string item in retval)
                        {
                            if (string.Compare(item, dir, true) == 0)
                            {
                                alreadyAdded = true;
                                break;
                            }
                        }
                        if (!alreadyAdded)
                            retval.Add(dir);
                    }
            }

            if (CSScript.GlobalSettings != null && CSScript.GlobalSettings.HideAutoGeneratedFiles == Settings.HideOptions.HideAll)
                if (scriptFile != null)
                    retval.Add(Path.GetFullPath(CSSEnvironment.GetCacheDirectory(Path.GetFullPath(scriptFile))));

            return retval;
        }

        static string[] SearchDirs
        {
            get
            {
                return searchDirsList.ToArray();
            }
        }

        public class VS150IDE
        {
            static public bool isolating = false;

            public delegate string ProcessSourceFile(string srcFile, string projDir);

            static public void IsolateProject(string scriptFile, string tempDir)
            {
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Cannot clean destination folder " + tempDir + "\n" + e.Message);
                    }
                }

                RunPreScripts(scriptFile);
                VS150IDE.isolating = true;

                string solutionFile = CreateProject(scriptFile, tempDir, new ProcessSourceFile(IsolateSourceFile), true);

                //rename project files
                string newSolutionFile = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(scriptFile) + ".sln");
                string newProjectFile = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(scriptFile) + ".csproj");

                using (StreamReader sr = new StreamReader(Path.ChangeExtension(solutionFile, ".csproj")))
                using (StreamWriter sw = new StreamWriter(newProjectFile))
                    sw.Write(sr.ReadToEnd().Replace(Path.GetFileNameWithoutExtension(solutionFile), Path.GetFileNameWithoutExtension(newProjectFile)));
                File.Delete(Path.ChangeExtension(solutionFile, ".csproj"));

                using (StreamReader sr = new StreamReader(solutionFile)) //repoint solution to the right project file
                {
                    using (FileStream fs = new FileStream(newSolutionFile, FileMode.CreateNew))
                    {
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            //bw.Write((byte)0xEF); //VS2005 binary header
                            //bw.Write((byte)0xBB);
                            //bw.Write((byte)0xBF);

                            string text = sr.ReadToEnd().Replace(Path.GetFileNameWithoutExtension(solutionFile), Path.GetFileNameWithoutExtension(newProjectFile));
                            char[] buf = text.ToCharArray();
                            bw.Write(buf, 0, buf.Length);
                        }
                    }
                }

                using (StreamReader sr = new StreamReader(newProjectFile))
                    if (sr.ReadToEnd().IndexOf(".layout\"") == -1) //is not WWF StateMachine project
                        foreach (string file in Directory.GetFiles(Path.GetDirectoryName(newProjectFile), "*.layout"))
                            File.Delete(file);

                File.Delete(Path.ChangeExtension(solutionFile, ".csproj.user"));
                File.Delete(Path.Combine(Path.GetDirectoryName(solutionFile), "wwf.layout"));

                File.Delete(solutionFile);
                RunPostScripts(scriptFile);
                Console.WriteLine("Script " + Path.GetFileName(scriptFile) + " is isolated to folder: " + new DirectoryInfo(tempDir).FullName);
            }

            static public string IsolateSourceFile(string srcFile, string projDir)
            {
                string newName = Path.Combine(projDir, Path.GetFileName(srcFile));

                if (Path.GetFileName(newName).StartsWith("i_")) //rename imported files to their original names
                {
                    int end = newName.LastIndexOf("_");

                    if (end != -1)
                    {
                        string newFile = Path.GetFileName(newName.Substring(0, end).Replace("i_", "") + Path.GetExtension(newName));
                        newFile = Path.Combine(projDir, newFile);
                        if (File.Exists(newFile))
                        {
                            newFile = GetCopyName(newFile);
                        }
                        newName = newFile;
                    }
                }

                File.Copy(srcFile, newName);
                File.SetAttributes(newName, FileAttributes.Normal);
                try
                {
                    if (Path.GetFileName(srcFile).StartsWith("i_"))
                        File.Delete(srcFile);
                }
                catch { }
                return newName;
            }

            static public string IgnoreSourceFile(string srcFile, string projDir)
            {
                return srcFile;
            }

            public static bool IsResxRequired(string scriptFile)
            {
                if (!File.Exists(scriptFile))
                    return false;
                using (StreamReader sr = new StreamReader(scriptFile))
                {
                    //Form class containing InitializeComponent call require resx dependant designer
                    //SequentialWorkflowActivity  class contains InitializeComponent call but it does not require resx dependant designer
                    string text = sr.ReadToEnd();

                    return text.IndexOf("InitializeComponent();") != -1 &&
                    text.IndexOf("SequentialWorkflowActivity") == -1 &&
                    text.IndexOf("StateMachineWorkflowActivity") == -1;
                }
            }

            static public string CreateProject(string scriptFile, string tempDir)
            {
                return CreateProject(scriptFile, tempDir, new ProcessSourceFile(IgnoreSourceFile), false);
            }

            static string FindAssociatedXml(string srcFile, string[] srcFiles)
            {
                string retval = "";

                if (srcFile.EndsWith(".cs"))
                {
                    if (Path.GetFileNameWithoutExtension(srcFile).ToLower().EndsWith(".xaml")) //Window1.xaml.cs + Window1.xaml
                    {
                        retval = Path.Combine(Path.GetDirectoryName(srcFile), Path.GetFileNameWithoutExtension(srcFile));
                    }
                    else
                    {
                        string expectedXAML = (Path.GetFileNameWithoutExtension(srcFile) + ".xaml").ToLower();

                        foreach (string file in srcFiles)
                            if (Path.GetFileName(file).ToLower() == expectedXAML)
                                retval = file;
                    }
                }
                return retval;
            }

            static private bool UsesPreprocessor(string file, out string precompiler)
            {
                precompiler = "";

                if (!File.Exists(file))
                    return false;

                if (!Path.GetFileName(file).ToLower().StartsWith("debugvs") &&
                    !Path.GetFileName(file).ToLower().StartsWith("i_debugvs".ToLower())) //do not parse itself
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string code = sr.ReadToEnd();

                        if (code != null)
                        {
                            int start = code.IndexOf("//css_pre precompile");

                            if (start != -1)
                            {
                                start += "//css_pre".Length;

                                int end = code.IndexOf("(", start);

                                precompiler = code.Substring(start, end - start).Trim();

                                precompiler = FileResolver.ResolveFile(precompiler, SearchDirs, ".cs");
                                return true;
                            }
                            else
                                return false;
                        }
                        else
                            return false;
                    }
                }
                return false;
            }

            static private string CreateProject(string scriptFile, string tempDir, ProcessSourceFile fileHandler, bool copyLocalAsm)
            {
                string srcProjDir = @"Lib\Debug\VS15.0"; //relative to CSSCRIPT_DIR
                string scHomeDir = VS150IDE.GetEnvironmentVariable("CSSCRIPT_DIR");
                string scriptShortName = Path.GetFileName(scriptFile);
                string projFile = Path.Combine(tempDir, "DebugScript.csproj");
                string solutionFile = Path.Combine(tempDir, "DebugScript.sln");

                //copy project template
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                foreach (string file in Directory.GetFiles(Path.Combine(scHomeDir, srcProjDir)))
                {
                    if (Path.GetExtension(file) != ".resx")
                    {
                        if (string.Compare(Path.GetFileName(file), "AssemblyInfo.cs", true) == 0)
                        {
                            using (StreamReader sr = new StreamReader(file))
                            using (StreamWriter sw = new StreamWriter(Path.Combine(tempDir, Path.GetFileName(file))))
                            {
                                string code = sr.ReadToEnd().Replace("ScriptDebugger", Path.GetFileNameWithoutExtension(scriptFile))
                                                            .Replace("ScriptFullPath", scriptFile);
                                sw.Write(code);
                            }
                        }
                        else
                        {
                            File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)), true);
                        }
                    }
                }

                //update project template with script specific data
                VS150IDE ide = new VS150IDE();

                ScriptParser parser = new ScriptParser(scriptFile, Script.SearchDirs, VS150IDE.isolating);
                AssemblyResolver asmResolver = new AssemblyResolver();

                foreach (string dir in parser.SearchDirs)
                    AddSearchDir(dir);

                string resxSrcFile = Path.Combine(Path.Combine(scHomeDir, srcProjDir), "Form1.resx");
                bool XAML = false;
                bool WWF = false;

                ArrayList importerdScripts = new ArrayList();
                ArrayList precompilibleScripts = new ArrayList();
                importerdScripts.AddRange(parser.SaveImportedScripts());

                string srcFile = fileHandler(scriptFile, tempDir);
                XAML = srcFile.ToLower().EndsWith(".xaml");

                string associatedXml = FindAssociatedXml(srcFile, (string[])importerdScripts.ToArray(typeof(string)));

                string precompiler = "";

                if (UsesPreprocessor(srcFile, out precompiler))
                    precompilibleScripts.Add(new string[] { srcFile, precompiler });

                if (VS150IDE.IsResxRequired(srcFile) && associatedXml == "")
                    ide.InsertFile(srcFile, projFile, resxSrcFile, "");
                else
                {
                    if (associatedXml != "")
                        ide.InsertXamlCSFile(srcFile, projFile, associatedXml);
                    else
                        ide.InsertFile(srcFile, projFile, "", "");
                }

                foreach (string file in importerdScripts)
                {
                    if (UsesPreprocessor(file, out precompiler))
                        precompilibleScripts.Add(new string[] { file, precompiler });

                    srcFile = fileHandler(file, tempDir);
                    associatedXml = FindAssociatedXml(srcFile, (string[])importerdScripts.ToArray(typeof(string)));
                    XAML = srcFile.ToLower().EndsWith(".xaml");
                    if (!Path.GetFileName(srcFile).StartsWith("i_") && VS150IDE.IsResxRequired(srcFile) && associatedXml == "")
                    {
                        ide.InsertFile(srcFile, projFile, resxSrcFile, "");
                    }
                    else
                    {
                        if (associatedXml != "")
                            ide.InsertXamlCSFile(srcFile, projFile, associatedXml);
                        else
                            ide.InsertFile(srcFile, projFile, "", "");
                    }
                }

                if (XAML)
                    ide.InsertImport(@"$(MSBuildBinPath)\Microsoft.WinFX.targets", projFile);

                ArrayList referencedNamespaces = new ArrayList(parser.ReferencedNamespaces);

                string[] defaultAsms = (CSScript.GlobalSettings.DefaultRefAssemblies ?? "")
                                        .Replace(" ", "")
                                        .Split(";,".ToCharArray());
                referencedNamespaces.AddRange(defaultAsms);

                if (precompilibleScripts.Count > 0)
                {
                    referencedNamespaces.Add("CSScriptLibrary");

                    Hashtable ht = new Hashtable();

                    foreach (string[] info in precompilibleScripts)
                    {
                        if (!ht.ContainsKey(info[1])) //to avoid duplication
                        {
                            ht[info[1]] = true;

                            string t = Path.GetDirectoryName(scriptFile);

                            ide.InsertFile(Path.Combine(Path.GetDirectoryName(scriptFile), info[1]), projFile, "", "");
                        }
                    }

                    string commands = "";

                    foreach (string[] info in precompilibleScripts)
                        commands += "cscs.exe \"" + Path.Combine(Path.GetDirectoryName(scriptFile), info[1]) + "\" \"" + info[0] + "\" \"/primary:" + scriptFile + "\"" + "\r\n";

                    string firstPrecompiler = (precompilibleScripts[0] as string[])[1];
                    ide.InsertFile(Path.Combine(scHomeDir, "Lib\\precompile.part.cs"), projFile, "", firstPrecompiler);
                    ide.InsertPreBuildEvent(commands, projFile);
                    //<PropertyGroup>
                    //<PreBuildEvent>cscs.exe "C:\cs-script\Dev\Macros C#\precompile.cs" "C:\cs-script\Dev\Macros C#\code.cs"</PreBuildEvent>
                    //</PropertyGroup>
                }

                foreach (string name in referencedNamespaces)
                {
                    bool ignore = false;

                    foreach (string ignoreName in parser.IgnoreNamespaces)
                        if (ignore = (name == ignoreName))
                            break;

                    if (ignore)
                        continue;

                    string[] asmFiles = AssemblyResolver.FindAssembly(name, SearchDirs);

                    foreach (string file in asmFiles)
                    {
                        if (!WWF && file.ToLower().IndexOf("system.workflow.runtime") != -1)
                            WWF = true;

                        if (!copyLocalAsm || file.IndexOf("assembly\\GAC") != -1 || file.IndexOf("assembly/GAC") != -1)
                            ide.InsertReference(file, projFile);
                        else
                        {
                            string asmCopy = Path.Combine(tempDir, Path.GetFileName(file));
                            File.Copy(file, asmCopy, true);

                            ide.InsertReference(Path.GetFileName(asmCopy), projFile);
                        }
                    }
                }

                List<string> refAssemblies = new List<string>(parser.ReferencedAssemblies);

                //process referenced assemblies from the compiler options:
                //   /r:LibA=LibA.dll /r:LibB=LibB.dll
                foreach (string optionsDef in parser.CompilerOptions)
                    foreach (string optionArg in ParseArguments(optionsDef))
                    {
                        string[] keyValue = optionArg.Split(new char[] { ':' }, 2);

                        if (keyValue[0] == "/r" || keyValue[0] == "/reference")
                        {
                            refAssemblies.Add(keyValue.Last());
                        }
                    }

                //Note we are suppressing package downloads as it is not the first time we are
                //loading the script so we already tried to download the packages
                foreach (string asm in parser.ResolvePackages(true))
                    refAssemblies.Add(asm);

                foreach (string assemblyDef in refAssemblies) //some assemblies were referenced from code
                {
                    string[] tokens = assemblyDef.Split(new char[] { '=' }, 2);

                    string asm = tokens.Last();
                    string aliase = null;

                    if (tokens.Length > 1)
                        aliase = tokens.First();

                    foreach (string file in AssemblyResolver.FindAssembly(asm, SearchDirs))
                    {
                        if (!WWF && file.ToLower().IndexOf("system.workflow.runtime") != -1)
                            WWF = true;

                        if (!copyLocalAsm || file.IndexOf("assembly\\GAC") != -1 || file.IndexOf("assembly\\GAC") != -1)
                            ide.InsertReference(file, projFile, aliase);
                        else
                        {
                            string asmCopy = Path.Combine(tempDir, Path.GetFileName(file));

                            if (Path.IsPathRooted(file) || File.Exists(file))
                                File.Copy(file, asmCopy, true);
                            ide.InsertReference(Path.GetFileName(asmCopy), projFile, aliase);
                        }
                    }
                }

                //adjust project settings
                if (WWF)
                {
                    ide.InsertProjectTypeGuids("{14822709-B5A1-4724-98CA-57A101D1B079};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", projFile);
                    ide.InsertImport(@"$(MSBuildExtensionsPath)\Microsoft\Windows Workflow Foundation\v3.0\Workflow.Targets", projFile);

                    foreach (string file in importerdScripts)
                        if (file.ToLower().IndexOf("designer.cs") != -1)
                        {
                            string className = Path.GetFileNameWithoutExtension(file.ToLower()).Replace(".designer", "");
                            string template = Path.Combine(tempDir, "wwf.layout");
                            string layoutFile = file.Replace("designer.cs", "layout");

                            if (copyLocalAsm) //isolating
                                layoutFile = Path.Combine(Path.GetDirectoryName(projFile), Path.GetFileName(layoutFile));

                            File.Copy(template, layoutFile, true);
                            ReplaceInFile(layoutFile, "WFInitialState", className + "InitialState");

                            ide.InsertResource(layoutFile, projFile);
                        }
                }

                CSharpParser fileParser = new CSharpParser(scriptFile, true, new string[] { "//css_dbg", "//css_args", "//css_co" });

                //foreach (string statement in fileParser.CustomDirectives["//css_dbg"] as List<string>)//should be  reenabled when CS-Script is compiled for  .NET 2.0
                foreach (string statement in fileParser.CustomDirectives["//css_dbg"] as IEnumerable)
                    foreach (string directive in statement.Split(','))
                    {
                        string d = directive.Trim();

                        if (d.StartsWith("/t:"))
                            ide.SetOutputType(d.Substring("/t:".Length), projFile);
                        else if (d.StartsWith("/platform:"))
                            ide.SetPlatformType(d.Substring("/platform:".Length), projFile, solutionFile);
                        else if (d.Trim().StartsWith("/args:"))
                            ide.SetArguments(d.Substring("/args:".Length), projFile + ".user");
                    }

                //foreach (string statement in fileParser.CustomDirectives["//css_args"] as List<string>) //should be  reenabled when CS-Script is compiled for  .NET 2.0
                foreach (string statement in fileParser.CustomDirectives["//css_args"] as IEnumerable)
                    foreach (string directive in statement.Split(','))
                    {
                        string d = directive.Trim();

                        if (d.StartsWith("/co"))
                        {
                            string[] compilerOptions = d.Substring(4).Split(',');

                            foreach (string option in compilerOptions)
                            {
                                string o = option.Trim();

                                if (o.StartsWith("/unsafe"))
                                    ide.SetAllowUnsafe(projFile);
                                else if (o.StartsWith("/platform:"))
                                    ide.SetPlatformType(o.Substring("/platform:".Length), projFile, solutionFile);
                            }
                        }
                    }

                foreach (string statement in fileParser.CustomDirectives["//css_co"] as IEnumerable)
                    foreach (string directive in statement.Split(','))
                    {
                        string d = directive.Trim();

                        if (d.StartsWith("/unsafe"))
                            ide.SetAllowUnsafe(projFile);
                        else if (d.StartsWith("/platform:"))
                            ide.SetPlatformType(d.Substring("/platform:".Length), projFile, solutionFile);
                    }

                Settings settings = GetSystemWideSettings();

                if (settings != null)
                    ide.SetTargetFramework(settings.TargetFramework, projFile);

                ide.SetWorkingDir(Path.GetDirectoryName(scriptFile), projFile + ".user");

                if (Environment.GetEnvironmentVariable("CSSCRIPT_VS_DROPASMINFO") != null && !VS150IDE.isolating)
                    ide.RemoveFile("AssemblyInfo.cs", projFile);

                string appConfigFile = "";

                if (File.Exists(Path.ChangeExtension(scriptFile, ".cs.config")))
                    appConfigFile = Path.ChangeExtension(scriptFile, ".cs.config");
                else if (File.Exists(Path.ChangeExtension(scriptFile, ".exe.config")))
                    appConfigFile = Path.ChangeExtension(scriptFile, ".exe.config");

                if (appConfigFile != "")
                    ide.InsertAppConfig(appConfigFile, projFile);

                ///////////////////////////////////
                //rename project files
                //'#' is an illegal character for the project/solution file name
                string newSolutionFile = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(scriptFile).Replace("#", "Sharp") + " (script).sln");
                string newProjectFile = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(newSolutionFile).Replace("#", "Sharp") + ".csproj");

                FileMove(solutionFile, newSolutionFile);
                FileMove(projFile, newProjectFile);
                FileMove(projFile + ".user", newProjectFile + ".user");

                ReplaceInFile(newSolutionFile, Path.GetFileNameWithoutExtension(projFile), Path.GetFileNameWithoutExtension(newProjectFile));
                ReplaceInFile(newProjectFile, "DebugScript", Path.GetFileNameWithoutExtension(scriptFile));

                //remove xmlns=""
                VSProjectDoc.FixFile(newProjectFile);
                VSProjectDoc.FixFile(newProjectFile + ".user");

                ///////////////////////
                return newSolutionFile;
            }

            //very primitive command-line parser
            static string[] ParseArguments(string commandLine)
            {
                var parmChars = commandLine.ToCharArray();
                var inSingleQuote = false;
                var inDoubleQuote = false;

                for (var index = 0; index < parmChars.Length; index++)
                {
                    if (parmChars[index] == '"' && !inSingleQuote)
                    {
                        inDoubleQuote = !inDoubleQuote;
                        parmChars[index] = '\n';
                    }
                    if (parmChars[index] == '\'' && !inDoubleQuote)
                    {
                        inSingleQuote = !inSingleQuote;
                        parmChars[index] = '\n';
                    }
                    if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ' ')
                        parmChars[index] = '\n';
                }
                return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }

            static void FileMove(string src, string dest)
            {
                if (File.Exists(dest))
                    File.Delete(dest);
                File.Move(src, dest);
            }

            static void ReplaceInFile(string file, string oldValue, string newValue)
            {
                ReplaceInFile(file, new string[] { oldValue }, new string[] { newValue });
            }

            static void ReplaceInFile(string file, string[] oldValue, string[] newValue)
            {
                string content = "";

                using (StreamReader sr = new StreamReader(file))
                    content = sr.ReadToEnd();

                for (int i = 0; i < oldValue.Length; i++)
                    content = content.Replace(oldValue[i], newValue[i]);

                using (StreamWriter sw = new StreamWriter(file))
                    sw.Write(content);
            }

            public void InsertImport(string importStr, string projFile)
            {
                //<Import Project="$(MSBuildBinPath)\Microsoft.WinFX.targets" />
                VSProjectDoc doc = new VSProjectDoc(projFile);

                XmlElement elem = doc.CreateElement("Import");
                XmlAttribute newAttr;

                newAttr = doc.CreateAttribute("Project");
                newAttr.Value = importStr;
                elem.Attributes.Append(newAttr);

                XmlNode node = doc.SelectFirstNode("//Project");
                node.AppendChild(elem);

                doc.Save(projFile);
            }

            public void InsertResource(string file, string projFile)
            {
                //<EmbeddedResource Include="Workflow1.layout">
                //    <DependentUpon>Workflow1.cs</DependentUpon>
                //</EmbeddedResource>

                VSProjectDoc doc = new VSProjectDoc(projFile);

                XmlElement group = doc.CreateElement("ItemGroup");
                XmlElement res = doc.CreateElement("EmbeddedResource");

                XmlAttribute newAttr;

                newAttr = doc.CreateAttribute("Include");
                newAttr.Value = file;
                res.Attributes.Append(newAttr);

                XmlElement dependency = doc.CreateElement("DependentUpon");
                dependency.InnerText = Path.ChangeExtension(Path.GetFileName(file), ".cs");

                res.AppendChild(dependency);
                group.AppendChild(res);

                doc.SelectFirstNode("//Project").AppendChild(group);

                doc.Save(projFile);
            }

            public void InsertPreBuildEvent(string commands, string projFile)
            {
                //<PropertyGroup>
                //<PreBuildEvent>
                //cscs.exe "C:\cs-script\Dev\Macros C#\precompile.cs" "C:\cs-script\Dev\Macros C#\code.cs"
                //</PreBuildEvent>
                //</PropertyGroup>
                VSProjectDoc doc = new VSProjectDoc(projFile);

                XmlElement elem = doc.CreateElement("PropertyGroup");
                XmlElement elem1 = doc.CreateElement("PreBuildEvent");
                elem1.InnerXml = commands;
                elem.AppendChild(elem1);

                doc.SelectFirstNode("//Project").AppendChild(elem);

                doc.Save(projFile);
            }

            public void InsertProjectTypeGuids(string guids, string projFile)
            {
                //<ProjectTypeGuids>{14822709-B5A1-4724-98CA-57A101D1B079};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
                VSProjectDoc doc = new VSProjectDoc(projFile);

                XmlElement elem = doc.CreateElement("ProjectTypeGuids");
                elem.InnerXml = guids;

                doc.SelectFirstNode("//Project/PropertyGroup/Configuration").ParentNode.AppendChild(elem);
                doc.Save(projFile);
            }

            public void RemoveFile(string file, string projFile)
            {
                try
                {
                    VSProjectDoc doc = new VSProjectDoc(projFile);

                    XmlNode node = doc.SelectFirstNode("//Project/ItemGroup/Compile[@Include='" + file + "']");
                    node.ParentNode.RemoveChild(node);

                    doc.Save(projFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified file could not be inserted to the temp project:\n" + e.Message);
                }
            }

            public void InsertFile(string scriptFile, string projFile, string resxSrcFile, string parentFile)
            {
                string srcFile = scriptFile;

                if (VS150IDE.isolating)
                    srcFile = Path.GetFileName(srcFile); //remove absolute path

                try
                {
                    string parent = "";

                    if (srcFile.ToLower().EndsWith(".designer.cs")) //needs to be dependent on 'base include'
                    {
                        parent = Path.GetFileName(srcFile);
                        parent = parent.Substring(0, parent.Length - ".designer.cs".Length) + ".cs";
                    }
                    if (srcFile.ToLower().EndsWith(".g.cs")) //needs to be dependent on 'base include'
                    {
                        parent = Path.GetFileName(srcFile);
                        parent = parent.Substring(0, parent.Length - ".g.cs".Length) + ".cs";
                    }
                    if (srcFile.ToLower().EndsWith(".part.cs")) //needs to be dependent on 'base include'
                    {
                        parent = Path.GetFileName(srcFile);
                        parent = parent.Substring(0, parent.Length - ".part.cs".Length) + ".cs";
                    }
                    if (parentFile != null && parentFile != "")
                        parent = parentFile;

                    //<Compile Include="C:\cs-script\Samples\tick.cs" />

                    //NOTE: VS7.1 is able to create .resx file for linked (not in the .proj directory) .cs files correctly. However VS9.0 can do this only for
                    //non-linked source files. Yes this is a new VS bug. Thus I have to create .resex in case if the file contains
                    //the class inherited from System.Windows.Form.

                    VSProjectDoc doc = new VSProjectDoc(projFile);

                    //Create a new node.
                    XmlElement elem = doc.CreateElement(srcFile.EndsWith(".xaml") ? "Page" : "Compile");
                    XmlAttribute newAttr;

                    newAttr = doc.CreateAttribute("Include");
                    newAttr.Value = srcFile;
                    elem.Attributes.Append(newAttr);

                    if (parent != "")
                    {
                        //<DependentUpon>Form1.cs</DependentUpon>
                        XmlElement nestedElem = doc.CreateElement("DependentUpon");
                        nestedElem.InnerText = Path.GetFileName(parent);
                        elem.AppendChild(nestedElem);
                    }
                    else if (srcFile.ToLower().EndsWith(".includes.cs"))
                    {
                        XmlElement nestedElem = doc.CreateElement("Link");
                        nestedElem.InnerText = "Includes\\" + Path.GetFileName(srcFile);
                        elem.AppendChild(nestedElem);
                    }

                    XmlNode contentsNode = doc.SelectNodes("//Project/ItemGroup")[1];

                    contentsNode.AppendChild(elem);

                    if (resxSrcFile != "")
                    {
                        //<EmbeddedResource Include="G:\Dev\WindowsApplication1\Form1.resx">
                        //<DependentUpon>Form1.cs</DependentUpon>
                        //</EmbeddedResource>
                        string resxFile = Path.ChangeExtension(srcFile, ".resx");
                        File.Copy(resxSrcFile, Path.Combine(Path.GetDirectoryName(scriptFile), resxFile), true);

                        elem = doc.CreateElement("EmbeddedResource");
                        newAttr = doc.CreateAttribute("Include");
                        newAttr.Value = resxFile;
                        elem.Attributes.Append(newAttr);

                        XmlElement nestedElem = doc.CreateElement("DependentUpon");
                        nestedElem.InnerText = Path.GetFileName(srcFile);
                        elem.AppendChild(nestedElem);

                        contentsNode.AppendChild(elem);
                    }
                    doc.Save(projFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified file could not be inserted to the temp project:\n" + e.Message);
                }
            }

            public void InsertAppConfig(string appConfigFile, string projFile)
            {
                string destFile = Path.Combine(Path.GetDirectoryName(projFile), "app.config");

                if (File.Exists(destFile))
                {
                    File.SetAttributes(destFile, FileAttributes.Normal);
                    File.Delete(destFile);
                }
                using (StreamReader sr = new StreamReader(appConfigFile))
                using (StreamWriter sw = new StreamWriter(destFile))
                {
                    sw.Write(sr.ReadToEnd());
                    sw.WriteLine("<!-- read-only copy of " + appConfigFile + " -->");
                }

                File.SetAttributes(destFile, FileAttributes.ReadOnly);

                try
                {
                    VSProjectDoc doc = new VSProjectDoc(projFile);

                    XmlElement group = doc.CreateElement("ItemGroup");
                    XmlElement none = doc.CreateElement("None");
                    XmlAttribute attr = doc.CreateAttribute("Include");
                    attr.Value = "app.config";

                    group.AppendChild(none);
                    none.Attributes.Append(attr);

                    doc.SelectFirstNode("//Project").AppendChild(group);

                    doc.Save(projFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified file could not be inserted to the temp project:\n" + e.Message);
                }
            }

            public void InsertXamlCSFile(string scriptFile, string projFile, string xamlSrcFile)
            {
                string srcFile = scriptFile;

                if (VS150IDE.isolating)
                    srcFile = Path.GetFileName(srcFile); //remove absolute path

                try
                {
                    VSProjectDoc doc = new VSProjectDoc(projFile);

                    //Create a new node.
                    XmlElement elem = doc.CreateElement(srcFile.EndsWith(".xaml") ? "Page" : "Compile");
                    XmlAttribute newAttr;

                    newAttr = doc.CreateAttribute("Include");
                    newAttr.Value = srcFile;
                    elem.Attributes.Append(newAttr);

                    //<DependentUpon>Window1.xaml</DependentUpon>
                    XmlElement nestedElem = doc.CreateElement("DependentUpon");
                    nestedElem.InnerText = Path.GetFileName(xamlSrcFile);
                    elem.AppendChild(nestedElem);

                    doc.SelectNodes("//Project/ItemGroup")[1].AppendChild(elem);

                    doc.Save(projFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified file could not be inserted to the temp project:\n" + e.Message);
                }
            }

            public void InsertReference(string asmFile, string projFile, string aliase = null)
            {
                string refFile = asmFile;

                if (VS150IDE.isolating || asmFile.IndexOf("assembly\\GAC") != -1 || asmFile.IndexOf("assembly/GAC") != -1)
                    refFile = Path.GetFileName(refFile); //remove absolute path

                try
                {
                    //<Reference Include="CSScriptLibrary">
                    //  <SpecificVersion>False</SpecificVersion>
                    //  <HintPath>..\VS9.0\CSScriptLibrary.dll</HintPath>
                    //  <Aliases>CSScript</Aliases>
                    //</Reference>

                    VSProjectDoc doc = new VSProjectDoc(projFile);

                    //Create a new node.
                    XmlElement elem = doc.CreateElement("Reference");
                    XmlAttribute newAttr;

                    newAttr = doc.CreateAttribute("Include");
                    newAttr.Value = Path.GetFileNameWithoutExtension(refFile);
                    elem.Attributes.Append(newAttr);

                    XmlElement elemSpecVer = doc.CreateElement("SpecificVersion");
                    elemSpecVer.InnerText = "False";
                    elem.AppendChild(elemSpecVer);

                    XmlElement elemPath = doc.CreateElement("HintPath");
                    elemPath.InnerText = refFile;
                    elem.AppendChild(elemPath);

                    if (aliase != null)
                    {
                        XmlElement elemAliase = doc.CreateElement("Aliases");
                        elemAliase.InnerText = aliase;
                        elem.AppendChild(elemAliase);
                    }

                    doc.SelectFirstNode("//Project/ItemGroup").AppendChild(elem);

                    doc.Save(projFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified reference could not be inserted into the temp project:\n" + e.Message);
                }
            }

            public void SetWorkingDir(string dir, string file)
            {
                try
                {
                    //<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
                    //  <StartWorkingDirectory>C:\Program Files\Google\</StartWorkingDirectory>
                    //</PropertyGroup>

                    VSProjectDoc doc = new VSProjectDoc(file);

                    XmlElement elem = doc.CreateElement("StartWorkingDirectory");
                    elem.InnerText = dir;

                    XmlNodeList nodes = doc.SelectNodes("//Project/PropertyGroup");
                    nodes[0].AppendChild(elem);
                    nodes[1].AppendChild(elem.Clone());

                    doc.Save(file);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified 'working directory' could not be set for the temp project:\n" + e.Message);
                }
            }

            public void SetArguments(string args, string file)
            {
                try
                {
                    //<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
                    //  <StartArguments>test</StartArguments>
                    //</PropertyGroup>

                    VSProjectDoc doc = new VSProjectDoc(file);

                    XmlElement elem = doc.CreateElement("StartArguments");
                    elem.InnerText = args;

                    XmlNodeList nodes = doc.SelectNodes("//Project/PropertyGroup");
                    nodes[0].AppendChild(elem);
                    nodes[1].AppendChild(elem.Clone());

                    doc.Save(file);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified 'working directory' could not be set for the temp project:\n" + e.Message);
                }
            }

            public void SetAllowUnsafe(string file)
            {
                try
                {
                    //<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
                    // <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                    //</PropertyGroup>
                    VSProjectDoc doc = new VSProjectDoc(file);

                    //Create a new node.
                    XmlElement elem = doc.CreateElement("AllowUnsafeBlocks");
                    elem.InnerText = "true";

                    XmlNodeList nodes = doc.SelectNodes("//Project/PropertyGroup");
                    nodes[1].AppendChild(elem);
                    nodes[2].AppendChild(elem.Clone());

                    doc.Save(file);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified 'unsafe' could not be set for the temp project:\n" + e.Message);
                }
            }

            public void SetOutputType(string type, string file)
            {
                try
                {
                    //<OutputType>Exe</OutputType>
                    ReplaceInFile(file, "<OutputType>Exe</OutputType>", "<OutputType>" + type + "</OutputType>");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified 'working directory' could not be set for the temp project:\n" + e.Message);
                }
            }

            public void SetTargetFramework(string target, string file)
            {
                try
                {
                    //<TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
                    ReplaceInFile(file, "<TargetFrameworkVersion>v4.0</TargetFrameworkVersion>", "<TargetFrameworkVersion>" + target + "</TargetFrameworkVersion>");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified 'working directory' could not be set for the temp project:\n" + e.Message);
                }
            }

            public void SetPlatformType(string type, string file, string solutionFile)
            {
                try
                {
                    //<PlatformTarget>x86</PlatformTarget>
                    VSProjectDoc doc = new VSProjectDoc(file);

                    //Create a new node.
                    XmlNodeList existingElement = doc.SelectNodes("//Project/PropertyGroup/PlatformTarget");
                    XmlElement elem;

                    if (existingElement.Count > 0)
                        elem = (XmlElement)existingElement[0];
                    else
                        elem = doc.CreateElement("PlatformTarget");

                    elem.InnerText = type;

                    XmlNodeList nodes = doc.SelectNodes("//Project/PropertyGroup");
                    nodes[1].AppendChild(elem);
                    nodes[2].AppendChild(elem.Clone());

                    doc.Save(file);
                    ReplaceInFile(file, "AnyCPU", "x86");
                    ReplaceInFile(solutionFile, "AnyCPU", "x86");
                    ReplaceInFile(solutionFile, "Any CPU", "x86");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified '" + type + "' could not be set for the temp project:\n" + e.Message);
                }
            }

            public void SetProjectTypeGuids(string type, string file)
            {
                try
                {
                    //<ProjectTypeGuids></ProjectTypeGuids>
                    if (type != null && type != "")
                        ReplaceInFile(file, "<ProjectTypeGuids></ProjectTypeGuids>", "");
                    else
                        ReplaceInFile(file, "<ProjectTypeGuids></ProjectTypeGuids>", "<ProjectTypeGuids>" + type + "</ProjectTypeGuids>");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified 'working directory' could not be set for the temp project:\n" + e.Message);
                }
            }

            static public string[][] GetAvailableIDE()
            {
                ArrayList retval = new ArrayList();
                string name = "";
                string hint = "";
                string command = "";

                string scHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");

                if (GetIDEFile(IDEEditions.express) != "<not defined>")
                {
                    name = "Open with VS2017E";
                    hint = "\t- Open with MS Visual Studio 2017 Express";
                    command = "\"" + scHomeDir + "\\csws.exe\" /c \"" + scHomeDir + "\\lib\\DebugVS15.0.cs\" /e \"%1\"";
                    retval.Add(new string[] { name, hint, command });
                }

                if (GetIDEFile(IDEEditions.normal) != "<not defined>")
                {
                    name = "Open with VS2017";
                    hint = "\t- Open with MS Visual Studio 2017";
                    command = "\"" + scHomeDir + "\\csws.exe\" /c \"" + scHomeDir + "\\lib\\DebugVS15.0.cs\" \"%1\"";
                    retval.Add(new string[] { name, hint, command });
                }

                return (string[][])retval.ToArray(typeof(string[]));
            }

            static public string GetIDEFile(IDEEditions edition)
            {
                string retval = "<not defined>";

                try
                {
                    var EnterpriseEdition = @"VisualStudio.csproj.9223cb7f\shell\Open\Command";
                    var professionalEdition = @"VisualStudio.csproj.08b5bc5a\shell\Open\Command";
                    var communityEdition = @"VisualStudio.csproj.1d6c1d8e\shell\Open\Command";
                    var expressEdition = @"WDExpress.cs.15.0\shell\Open\Command";
                    var isExpress = (edition == IDEEditions.express);

                    string keyname = isExpress ? expressEdition : communityEdition;
                    RegistryKey IDE = Registry.ClassesRoot.OpenSubKey(keyname);

                    //Professional
                    if (IDE == null && !isExpress)
                        IDE = Registry.ClassesRoot.OpenSubKey(professionalEdition);
                    //Enterprise
                    if (IDE == null && !isExpress)
                        IDE = Registry.ClassesRoot.OpenSubKey(EnterpriseEdition);

                    if (IDE != null)
                    {
                        if (IDE.GetValue("") != null)
                            retval = IDE.GetValue("").ToString().TrimStart("\"".ToCharArray()).Split("\"".ToCharArray())[0];
                    }
                }
                catch { }
                return retval;
            }

            static public string GetEnvironmentVariable(string name)
            {
                //It is important in the all "installation" scripts to have reliable GetEnvironmentVariable().
                //Under some circumstances freshly set environment variable CSSCRIPT_DIR cannot be obtained with
                //Environment.GetEnvironmentVariable(). For example when running under Total Commander or similar
                //shell utility. Even SendMessageTimeout does not help in all cases. That is why GetEnvironmentVariable
                //is reimplemented here.
                object value = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment").GetValue(name);
                return value == null ? null : value.ToString();
            }

            static private string GetCopyName(string file)
            {
                string retval = file;
                int i = 1;

                while (File.Exists(retval))
                {
                    retval = Path.Combine(Path.GetDirectoryName(file), "Copy" + (i == 1 ? " of " : " (" + i.ToString() + ") ") + Path.GetFileName(file));
                    i++;
                }
                return retval;
            }

            static public string[] GetImportedScripts(string projFile)
            {
                ArrayList retval = new ArrayList();
                XmlDocument doc = new XmlDocument();
                doc.Load(projFile);

                foreach (XmlNode child in doc.GetElementsByTagName("Compile"))
                {
                    foreach (XmlAttribute attribute in child.Attributes)
                    {
                        if (attribute != null && attribute.Name == "Include")
                            retval.Add(attribute.Value.ToString().ToString());
                    }
                }
                return (string[])retval.ToArray(typeof(string));
            }

            internal static void RefreshProject(string projFile)
            {
                string[] content = VS150IDE.GetImportedScripts(projFile);

                foreach (string file in content)    //remove original imported files
                {
                    if (Path.GetFileName(file).StartsWith("i_")) //imported modified files have name "i_file_XXXXXX.cs>"
                    {
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                        }
                        catch { }
                    }
                }
                //regenerate project
                scriptFile = ResolveScriptFile(content[0]);
                RunPreScripts(scriptFile);

                string newSolution = VS150IDE.CreateProject(content[0], Path.GetDirectoryName(projFile));
                string[] newContent = VS150IDE.GetImportedScripts(Path.ChangeExtension(newSolution, ".csproj"));

                //remove not needed .resx files from includes (not imported) files
                for (int i = 1; i < content.Length; i++)
                {
                    string file = content[i];
                    bool used = false;

                    if (!Path.GetFileName(file).StartsWith("i_")) //not imported script file
                    {
                        foreach (string newFile in newContent)
                            if (used = (String.Compare(newFile, file, true) == 0))
                                break;
                        try
                        {
                            if (!used)
                            {
                                if (File.Exists(Path.ChangeExtension(file, ".resx")))
                                    File.Delete(Path.ChangeExtension(file, ".resx"));
                                else if (File.Exists(Path.ChangeExtension(file, ".layout")))
                                    File.Delete(Path.ChangeExtension(file, ".layout"));
                            }
                        }
                        catch { }
                    }
                }
                RunPostScripts(scriptFile);
            }
        }

        static void RunScript(string scriptFileCmd)
        {
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = "cscs.exe";
            myProcess.StartInfo.Arguments = scriptFileCmd;
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.Start();

            string line = null;

            while (null != (line = myProcess.StandardOutput.ReadLine()))
            {
                Console.WriteLine(line);
            }
            myProcess.WaitForExit();
        }

        static string CompileScript(string scriptFile)
        {
            string retval = "";
            StringBuilder sb = new StringBuilder();

            Process myProcess = new Process();
            myProcess.StartInfo.FileName = "cscs.exe";
            myProcess.StartInfo.Arguments = "/nl /ca \"" + scriptFile + "\"";
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.Start();

            string line = null;

            while (null != (line = myProcess.StandardOutput.ReadLine()))
            {
                sb.Append(line);
                sb.Append("\n");
            }
            myProcess.WaitForExit();

            retval = sb.ToString();

            string compiledFile = Path.ChangeExtension(scriptFile, ".csc");

            if (retval == "" && File.Exists(compiledFile))
                File.Delete(compiledFile);

            return retval;
        }

        static void RunPreScripts(string scriptFile)
        {
            RunPrePostScripts(scriptFile, true);
        }

        static void RunPostScripts(string scriptFile)
        {
            RunPrePostScripts(scriptFile, false);
        }

        static void RunPrePostScripts(string scriptFile, bool prescript)
        {
            // Compile the script in order to proper execute all pre- and post-scripts.
            // The RunScript(cmd) approach is attractive but not sutable as some pre-scripts must be run only from the primary script
            // but not as a stand alone scripts. That is why it is disabled for now by "return;"
            if (prescript)
            {
                CompileScript(scriptFile);
            }
            else
            {
                //do nothing
                //yes this is a limitation of the debugVS9.0.cs
            }

            return;

            //run pre- and post-scripts as an external process to ensure using the same css_config.xml file
            string currDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Path.GetDirectoryName(scriptFile);

            foreach (csscript.CSharpParser.CmdScriptInfo cmdScript in new csscript.CSharpParser(scriptFile, true).CmdScripts)
            {
                if (cmdScript.preScript == prescript)
                {
                    string cmd = "";

                    foreach (string arg in cmdScript.args)
                        cmd += "\"" + arg + "\" ";

                    //The problem with RunScript(cmd) is that all pre- and post-script are executed as stand alone but not as a pre-/post-script of the primary script
                    RunScript(cmd);

                    //CSExecutor hard should be used here but it is hard as it is not esposed
                    //originalOptions.ScriptFilePrimary = scriptFile;
                    //CSExecutor exec = new CSExecutor(false, originalOptions);
                    //exec.Execute(cmdScript.args, null, scriptFile);
                }
            }
            Environment.CurrentDirectory = currDir;
        }
    }

    class FileResolver
    {
        static public string ResolveFile(string file, string[] extraDirs, string extension)
        {
            string fileName = file;

            if (Path.GetExtension(fileName) == "")
                fileName += extension;

            //arbitrary directories
            if (extraDirs != null)
            {
                foreach (string extraDir in extraDirs)
                {
                    string dir = extraDir;

                    if (File.Exists(Path.Combine(dir, fileName)))
                    {
                        return Path.GetFullPath(Path.Combine(dir, fileName));
                    }
                }
            }

            //PATH
            string[] pathDirs = Environment.GetEnvironmentVariable("PATH").Replace("\"", "").Split(';');

            foreach (string pathDir in pathDirs)
            {
                string dir = pathDir;

                if (File.Exists(Path.Combine(dir, fileName)))
                {
                    return Path.GetFullPath(Path.Combine(dir, fileName));
                }
            }

            return "";
        }
    }

    class VSProjectDoc : XmlDocument
    {
        public VSProjectDoc(string projFile)
        {
            Load(projFile);
        }

        static public void FixFile(string projFile)
        {
            //remove xmlns=""

            //using (FileStream fs = new FileStream(projFile, FileMode.OpenOrCreate))
            //using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode))
            //    sw.Write(FormatXml(this.InnerXml.Replace("xmlns=\"\"", "")));

            string content = "";

            using (StreamReader sr = new StreamReader(projFile))
                content = sr.ReadToEnd();

            content = content.Replace("xmlns=\"\"", "");

            using (StreamWriter sw = new StreamWriter(projFile))
                sw.Write(content);
        }

        public new XmlNodeList SelectNodes(string path)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(NameTable);
            nsmgr.AddNamespace("ab", "http://schemas.microsoft.com/developer/msbuild/2003");
            if (path.StartsWith("//"))
            {
                path = path.Substring(1);
                path = "/" + path.Replace("/", "/ab:");
            }
            else
            {
                path = path.Replace("/", "/ab:");
            }

            return SelectNodes(path, nsmgr);
        }

        public XmlNode SelectFirstNode(string path)
        {
            XmlNodeList nodes = SelectNodes(path);

            if (nodes != null && nodes.Count != 0)
                return nodes[0];
            else
                return null;
        }

        //static string FormatXml(string xml)
        //{
        //    StringReader sr = new StringReader(xml);
        //    XmlReader reader = XmlReader.Create(sr);
        //    reader.MoveToContent();
        //    StringBuilder sb = new StringBuilder();
        //    XmlWriterSettings settings = new XmlWriterSettings();
        //    settings.Indent = true;
        //    settings.IndentChars = ("\t");
        //    settings.Encoding = System.Text.Encoding.Unicode;
        //    using (XmlWriter writer = XmlWriter.Create(sb, settings))
        //        writer.WriteNode(reader, true);

        //    return sb.ToString();
        //}
    }
}
