using System;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using CSScriptLibrary;

class Script
{
    static string usage = "Usage: cscscript isolate <fileName> [/vs7|/vs8]...\nIsolates the script and all it's dependencies in the folder with the same name as the script file\n</vs7> - also creates Visual Studio 2003 C# solution\n";

    static public void Main(string[] args)
    {
        if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
        {
            Console.WriteLine(usage);
        }
        else if (args.Length == 1)
        {
            try
            {
                scriptFile = ResolveScriptFile(args[0]);

                string tempDir = Path.Combine(Path.GetDirectoryName(scriptFile), Path.GetFileNameWithoutExtension(scriptFile));
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Cannot clean destination folder "+tempDir+"\n"+e.Message);
                    }
                }

                Directory.CreateDirectory(tempDir);

                parser = new ScriptParser(scriptFile);

                foreach (string file in parser.FilesToCompile) //contains scriptFile + imported
                {
                    IsolateFile(file, tempDir, Path.GetDirectoryName(scriptFile));
                }

                foreach (string name in parser.ReferencedAssemblies)
                    foreach (string file in AssemblyResolver.FindAssembly(name, SearchDirs))
                        if (file.IndexOf("assembly\\GAC") == -1 && file.IndexOf("assembly/GAC") == -1)
                            File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)), true);

                foreach (string name in parser.ReferencedNamespaces)
                    foreach (string file in AssemblyResolver.FindAssembly(name, SearchDirs))
                        if (file.IndexOf("assembly\\GAC") == -1 && file.IndexOf("assembly/GAC") == -1)
                            File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)), true);

                Console.WriteLine("Script "+Path.GetFileName(scriptFile)+" is isolated to folder: "+ new DirectoryInfo(tempDir).FullName);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        else if (args[1].ToLower() == @"/vs7" || args[1].ToLower() == @"/vs8")
        {
            string scriptFile = args[0];
            if (!File.Exists(scriptFile) && !scriptFile.EndsWith(".cs") && File.Exists(scriptFile+".cs"))
                scriptFile = scriptFile+".cs";
            scriptFile = Path.GetFullPath(scriptFile);
            string scHomeDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");
            if (args[1].ToLower() == @"/vs7")
                RunScript("\"" + Path.Combine(scHomeDir, @"lib\DebugVS7.1.cs ") + "\" /prj \""+scriptFile+"\"");
            else
                RunScript("\"" + Path.Combine(scHomeDir, @"lib\DebugVS8.0.cs ") + "\" /prj \"" + scriptFile + "\"");
        }
    }

    static string ResolveScriptFile(string file)
    {
        if (Path.GetExtension(file) == "")
            return Path.GetFullPath(file + ".cs");
        else
            return Path.GetFullPath(file);
    }

    static string scriptFile;
    static ScriptParser parser;
    static string[] SearchDirs
    {
        get
        {
            string defaultConfig = Path.GetFullPath(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\css_config.xml"));

            ArrayList retval = new ArrayList(parser.SearchDirs);

            if (scriptFile != null)
                retval.Add(Path.GetDirectoryName(Path.GetFullPath(scriptFile)));

            retval.Add(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\lib"));

            if (File.Exists(defaultConfig))
                retval.AddRange(csscript.Settings.Load(defaultConfig).SearchDirs.Split(';'));

            return (string[])retval.ToArray(typeof(string));
        }
    }

    static void IsolateFile(string scriptFile, string isolationDir, string mainScriptDir)
    {
        //Isolation scenarios:
        //1. if file is imported (i_file_xxxxx.cs) copy it to the root level with the original name (root\file.cs)
        //2. if file is 'included' from lib or main scriptfolder (file.cs) copy it to the root level (root\file.cs)
        //3. if file is 'included' from 'down' folder (\folder\file.cs) copy it to the appropriate folder (root\folder\file.cs)
        //4. if file is 'included' from 'up' folder (..\..\file.cs) copy it to the appropriate folder (root\up\up\file.cs) and print warning.
        //5. if file is 'included' with absolute path folder (c:\temp\file.cs) copy it to the conflict folder (root\conflicts\file.cs)  and print warning.

		string isolatedFile = Path.GetFileName(scriptFile);
        string libDir = Path.Combine(Environment.GetEnvironmentVariable("CSSCRIPT_DIR"), "Lib");

        if (isolatedFile.StartsWith("i_")) // 1.
        {
            int end = isolatedFile.LastIndexOf("_");
            if (end != -1)
                isolatedFile = Path.GetFileName(isolatedFile.Substring(0, end).Replace("i_", "")+Path.GetExtension(isolatedFile));
        }
        else if (Path.GetDirectoryName(scriptFile) == mainScriptDir || Path.GetDirectoryName(scriptFile) == libDir) // 2.
        {
            //do nothing (isolatedFile is correct)
        }
        else if (Path.GetDirectoryName(scriptFile).StartsWith(mainScriptDir)) // 3.
        {
            isolatedFile = scriptFile.Substring(mainScriptDir.Length+1);

            string scriptIsolationFolder = Path.Combine(isolationDir, isolatedFile);
            scriptIsolationFolder = Path.GetDirectoryName(scriptIsolationFolder);
            if (!Directory.Exists(scriptIsolationFolder))
                Directory.CreateDirectory(scriptIsolationFolder);
        }
        else if (mainScriptDir.StartsWith(Path.GetDirectoryName(scriptFile))) // 4.
        {
            string upDirs = mainScriptDir.Substring(0, Path.GetDirectoryName(scriptFile).Length);
            foreach (string upDir in upDirs.Split(@"\".ToCharArray()))
                isolatedFile = Path.Combine(@"up\", isolatedFile);

            Console.WriteLine("Warning: Imported file '"+isolatedFile.Replace("up\\","..\\")+"' has been placed in 'up' folder(s).\nImport statement in the main script file has to be adjusted.\n");
		}
		else
		{
			Console.WriteLine("Warning: file '"+isolatedFile+"' has been placed in 'conflict' folder because file's absolute path cannot be converted into relative.\nThe main script file has to be adjusted.\n");
			isolatedFile = Path.Combine("Conflicts", isolatedFile);
		}
		string scriptCopy = Path.Combine(isolationDir, isolatedFile);		
		if (File.Exists(scriptCopy))
			Console.WriteLine("Warning: file '"+isolatedFile+"' already exist (it will be overwritten).");
		if (!Directory.Exists(Path.GetDirectoryName(scriptCopy)))
			Directory.CreateDirectory(Path.GetDirectoryName(scriptCopy));
		File.Copy(scriptFile, Path.Combine(isolationDir, isolatedFile), true);
	}
	static void RunScript(string scriptFileCmd)
	{
		Process myProcess = new Process();
		myProcess.StartInfo.FileName = "cscs.exe";
		myProcess.StartInfo.Arguments = "/nl " + scriptFileCmd;
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
}