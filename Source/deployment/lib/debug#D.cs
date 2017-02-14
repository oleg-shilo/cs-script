using System;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using System.Xml;
using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using CSScriptLibrary;
using csscript;

namespace SD
{
    class Script
    {
        static string usage = "Usage: cscscript debug#D [file]|[/i|/u] ...\nLoads C# script file into temporary SharpDevelop project and opens it.\n</i> / </u> - command switch to install/uninstall shell extension\n";

        static public void Main(string[] args)
        {
            if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
            {
                Console.WriteLine(usage);
            }
            else if (args[0].Trim().ToLower() == "/i")
            {
                SharpDevelopIDE.InstallShellExtension();
            }
            else if (args[0].Trim().ToLower() == "/u")
            {
                SharpDevelopIDE.UninstallShellExtension();
            }
            else
            {
                try
                {
                    FileInfo info = new FileInfo(args[0]);
                    scriptFile = info.FullName;
                    string srcProjDir = @"Lib\Debug\#D"; //relative to CSSCRIPT_DIR
                    string scHomeDir = SharpDevelopIDE.GetEnvironmentVariable("CSSCRIPT_DIR");
                    string tempDir = Path.Combine(Path.Combine(Path.GetTempPath(), "CSSCRIPT"), Environment.TickCount.ToString());

                    string projFile = Path.Combine(tempDir, "DebugScript.prjx");
                    string solutionFile = Path.Combine(tempDir, "DebugScript.cmbx");


                    //copy project template
                    Directory.CreateDirectory(tempDir);

                    foreach (string file in Directory.GetFiles(Path.Combine(scHomeDir, srcProjDir)))
                        File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)), true);

                    //update project template with script specific data
                    SharpDevelopIDE ide = new SharpDevelopIDE();
                    ScriptParser parser = new ScriptParser(scriptFile);
                    AssemblyResolver asmResolver = new AssemblyResolver();

                    ide.InsertFile(scriptFile, projFile);

                    string[] importerdScripts = parser.SaveImportedScripts();
                    foreach (string file in importerdScripts)
                        ide.InsertFile(file, projFile);

                    foreach (string name in parser.ReferencedNamespaces)
                    {
                        bool ignore = false;
                        foreach (string ignoreName in parser.IgnoreNamespaces)
                            if (ignore = (name == ignoreName))
                                break;

                        if (ignore)
                            continue;

                        string[] asmFiles = AssemblyResolver.FindAssembly(name, SearchDirs);
                        foreach (string file in asmFiles)
                            ide.InsertReference(file, projFile);
                    }

                    foreach (string asmName in parser.ReferencedAssemblies) //some assemblies were referenced from code
                    {
                        string[] asmFiles = AssemblyResolver.FindAssembly(asmName, SearchDirs);
                        foreach (string file in asmFiles)
                            ide.InsertReference(file, projFile);
                    }

                    //open project
                    Environment.CurrentDirectory = Path.GetDirectoryName(scriptFile);

                    Process myProcess = new Process();
                    myProcess.StartInfo.FileName = SharpDevelopIDE.GetIDEFile();
                    myProcess.StartInfo.Arguments = "\"" + solutionFile + "\" ";
                    myProcess.Start();
                    myProcess.WaitForExit();

                    //do clean up
                    Directory.Delete(tempDir, true);
                    foreach (string file in importerdScripts)
                    {
                        if (Path.GetFileName(file).StartsWith("i_")) //imported modified files have name "i_file_XXXXXX.cs>"
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified file could not be linked to the temp project:\n" + e.Message);
                }
            }
        }
        
        static string scriptFile;
        
        static string[] SearchDirs
        {
            get
            {
                string defaultConfig = Path.GetFullPath(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\css_config.xml"));

                ArrayList retval = new ArrayList();

                if (scriptFile != null)
                    retval.Add(Path.GetDirectoryName(Path.GetFullPath(scriptFile)));

                retval.Add(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\lib"));

                if (File.Exists(defaultConfig))
                {
                    foreach (string dir in Settings.Load(defaultConfig).SearchDirs.Split(';'))
                        retval.Add(dir);
                }

                if (CSScript.GlobalSettings != null && CSScript.GlobalSettings.HideAutoGeneratedFiles == Settings.HideOptions.HideAll)
                    retval.Add(CSSEnvironment.GetCacheDirectory(Path.GetFullPath(scriptFile)));

                return (string[])retval.ToArray(typeof(string));
            }
        }
        
        public class SharpDevelopIDE
        {
            public void InsertFile(string scriptFile, string projFile)
            {
                try
                {
                    //<File name="C:\cs-script\Dev\debug\SharpDevelop\tick.cs" subtype="Code" buildaction="Compile" dependson="" data="" />

                    XmlDocument doc = new XmlDocument();
                    doc.Load(projFile);

                    //Create a new node.
                    XmlElement elem = doc.CreateElement("File");
                    XmlAttribute newAttr;

                    newAttr = doc.CreateAttribute("name");
                    newAttr.Value = scriptFile;
                    elem.Attributes.Append(newAttr);

                    newAttr = doc.CreateAttribute("subtype");
                    newAttr.Value = "Code";
                    elem.Attributes.Append(newAttr);

                    newAttr = doc.CreateAttribute("buildaction");
                    newAttr.Value = "Compile";
                    elem.Attributes.Append(newAttr);

                    newAttr = doc.CreateAttribute("dependson");
                    newAttr.Value = "";
                    elem.Attributes.Append(newAttr);

                    newAttr = doc.CreateAttribute("data");
                    newAttr.Value = "";
                    elem.Attributes.Append(newAttr);

                    newAttr = doc.CreateAttribute("SubType");
                    newAttr.Value = "Form";
                    elem.Attributes.Append(newAttr);

                    XmlNode contentsNode = doc.FirstChild.FirstChild;
                    contentsNode.AppendChild(elem);

                    doc.Save(projFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified file could not be inserted to the temp project:\n" + e.Message);
                }
            }
            public void InsertReference(string refFile, string projFile)
            {
                try
                {
                    //<Reference type="Assembly" refto="C:\cs-script\Dev\debug\SharpDevelop\CSScriptLibrary.dll" localcopy="True" />

                    XmlDocument doc = new XmlDocument();
                    doc.Load(projFile);

                    //Create a new node.
                    XmlElement elem = doc.CreateElement("Reference");
                    XmlAttribute newAttr;

                    newAttr = doc.CreateAttribute("type");
                    newAttr.Value = "Assembly";
                    elem.Attributes.Append(newAttr);

                    newAttr = doc.CreateAttribute("refto");
                    newAttr.Value = refFile;
                    elem.Attributes.Append(newAttr);

                    newAttr = doc.CreateAttribute("localcopy");
                    newAttr.Value = "True";
                    elem.Attributes.Append(newAttr);

                    XmlNode ReferencesNode = doc.FirstChild.ChildNodes[1];
                    ReferencesNode.AppendChild(elem);

                    doc.Save(projFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Specified reference could not be inserted into the temp project:\n" + e.Message);
                }
            }
            static public string GetIDEFile()
            {
                string retval = "<not defined>";
                try
                {
                    RegistryKey IDE = Registry.ClassesRoot.OpenSubKey(@"SD.cmbxfile\shell\open\command");

                    if (IDE != null)
                    {
                        retval = IDE.GetValue("").ToString().TrimStart("\"".ToCharArray()).Split("\"".ToCharArray())[0];
                    }
                }
                catch{}
                return retval;
            }
            static public string[] GetAvailableIDE()
            {
                if (GetIDEFile() != "<not defined>")
                {
                    string scHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");

                    return new string[] { 
                        "Open (#Develop)",
                        "\t- Open with SharpDevelop",
                        "\"" + scHomeDir + "\\csws.exe\" /c \"" + scHomeDir + "\\lib\\Debug#D.cs\" \"%1\""};

                }
                return null;
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

            static public void InstallShellExtension()
            {
                string fileTypeName = null;
                RegistryKey csFile = Registry.ClassesRoot.OpenSubKey(".cs");

                if (csFile != null)
                {
                    fileTypeName = (string)csFile.GetValue("");
                }
                if (fileTypeName != null)
                {
                    //Shell extensions
                    Console.WriteLine("Create 'Open as script (#D)' shell extension...");
                    RegistryKey shell = Registry.ClassesRoot.CreateSubKey(fileTypeName + "\\shell\\Open as script(#D)\\command");
                    string scHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");
                    if (scHomeDir != null)
                    {
                        string regValue = "\"" + Path.Combine(scHomeDir, "csws.exe") + "\"" +
                            " /c " +
                            "\"" + Path.Combine(scHomeDir, "lib\\Debug#D.cs") + "\" " +
                            "\"%1\"";

                        shell.SetValue("", regValue);
                        shell.Close();
                    }
                    else
                        Console.WriteLine("\n" +
                                            "The Environment variable CSSCRIPT_DIR is not set.\n" +
                                            "This can be the result of running the script from the Total Commander or similar utility.\n" +
                                            "Please rerun install.bat from Windows Explorer or from the new instance of the Total Commander.");

                }
            }
            static public void UninstallShellExtension()
            {
                string fileTypeName = null;
                RegistryKey csFile = Registry.ClassesRoot.OpenSubKey(".cs");

                if (csFile != null)
                {
                    fileTypeName = (string)csFile.GetValue("");
                }
                if (fileTypeName != null)
                {
                    try
                    {
                        if (Registry.ClassesRoot.OpenSubKey(fileTypeName + "\\shell\\Open as script(#D)") != null)
                        {
                            Console.WriteLine("Remove 'Open as script (#D)' shell extension...");
                            Registry.ClassesRoot.DeleteSubKeyTree(fileTypeName + "\\shell\\Open as script(#D)");
                        }
                    }
                    catch { }
                }
            }
        }
    }
}