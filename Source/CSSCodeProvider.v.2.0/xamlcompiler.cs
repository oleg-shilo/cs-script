using System;
//using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.CodeDom.Compiler;
using Microsoft.JScript;
using System.CodeDom;
using Microsoft.CSharp;


namespace CSScriptCompilers //CS-Script
{
    /// <summary>
    /// C#+XAML compiler. 
    /// This class is capable of compiling (with MSBuild) dynamically created VS C# WPF project based on cs file(s) 
    /// </summary>
    public class CSCompiler : ICodeCompiler
    {
        object options;
        public CSCompiler(object options)
        {
            this.options = options;
        }
        #region Dummy interface implementations
        public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit)
        {
            throw new NotImplementedException("CompileAssemblyFromDom is not implemented");
        }
        public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits)
        {
            throw new NotImplementedException("CompileAssemblyFromDomBatch is not implemented");
        }
        public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName)
        {
            throw new NotImplementedException("CompileAssemblyFromFile is not implemented");
        }
        public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
        {
            throw new NotImplementedException("CompileAssemblyFromSource is not implemented");
        }
        public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
        {
            throw new NotImplementedException("CompileAssemblyFromSourceBatch is not implemented");
        }
        #endregion

        const string reference_template =
                                    "<Reference Include=\"{0}\">\n" +
                                    "  <SpecificVersion>False</SpecificVersion>\n" +
                                    "  <HintPath>{1}</HintPath>" +
                                    "</Reference>";
        const string include_template =
                                    "<Compile Include=\"{0}\">\n" +
                                    "  <!-- <DependentUpon>.xaml</DependentUpon> -->\n" +
                                    "  <SubType>Code</SubType>\n" +
                                    "</Compile>";
        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            //System.Diagnostics.Debug.Assert(false);
            foreach (string file in fileNames)
                if (file.ToLower().EndsWith(".xaml"))
                    return CompileAssemblyFromFileBatchImpl(options, fileNames);

            if (options.ToString() == "" || options == null)
                return new CSharpCodeProvider().CreateCompiler().CompileAssemblyFromFileBatch(options, fileNames);
            else
                return new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", options.ToString() } }).CreateCompiler().CompileAssemblyFromFileBatch(options, fileNames);
        }
        static bool appIncluded = false;
        static bool IsAppXAML(string file)
        {

            return false; //do not support app.xamel
            if (!appIncluded)
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    if (sr.ReadToEnd().IndexOf("<Application x:Class") == -1)
                        return false;

                    appIncluded = true;
                    return true;
                }
            }
            else
                return false;
        }
        CompilerResults CompileAssemblyFromFileBatchImpl(CompilerParameters options, string[] fileNames)
        {
            //System.Diagnostics.Debug.Assert(false);
            CompilerResults retval = new CompilerResults(new TempFileCollection());

            string outputName = Path.GetFileNameWithoutExtension(options.OutputAssembly);
            string tempDir = Path.Combine(Path.GetTempPath(), "CSSCRIPT\\CPP\\" + System.Guid.NewGuid().ToString());
            string tempProj = Path.Combine(tempDir, outputName + ".csproj");
            string outputFile = Path.Combine(tempDir, outputName + (options.GenerateExecutable ? ".exe" : ".dll"));

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);

            Directory.CreateDirectory(tempDir);

            using (StreamReader sr = new StreamReader(ProjTemplateFile))
            using (StreamWriter sw = new StreamWriter(tempProj))
            {
                string content = sr.ReadToEnd();
                content = content.Replace("$NAME$", outputName);
                content = content.Replace("$DEBUG_TYPE$", options.IncludeDebugInformation ? "<DebugType>full</DebugType>" : "");
                content = content.Replace("$OPTIMIZE$", options.IncludeDebugInformation ? "false" : "true");
                content = content.Replace("$DEBUG_CONST$", options.IncludeDebugInformation ? "DEBUG;" : "");
                content = content.Replace("$DEBUG$", options.IncludeDebugInformation ? "true" : "false");
                content = content.Replace("$OUPTUT_DIR$", tempDir);

                //Exe/WinExe/Library
                if (options.GenerateExecutable) //exe
                {
                    if (options.CompilerOptions != null && options.CompilerOptions.IndexOf("/target:winexe") != -1)
                    {
                        content = content.Replace("$TYPE$", "WinExe");	//WinForm
                    }
                    else
                    {
                        content = content.Replace("$TYPE$", "Exe");	//console
                    }
                }
                else //dll
                {
                    content = content.Replace("$TYPE$", "Library");	//dll
                    //content = content.Replace("$TYPE$", "Exe");	//dll
                }

                string references = "";
                foreach (string file in options.ReferencedAssemblies)
                    references += string.Format(reference_template, Path.GetFileName(file), file);
                content = content.Replace("$REFERENCES$", references);

                content = content.Replace("$MIN_CLR_VER$", "<MinFrameworkVersionRequired>3.0</MinFrameworkVersionRequired>");
                content = content.Replace("$IMPORT_PROJECT$", "<Import Project=\"$(MSBuildBinPath)\\Microsoft.WinFX.targets\" />");

                string sources = "";

                foreach (string file in fileNames)
                {
                    if (file.ToLower().EndsWith(".xaml"))
                    {
                        if (IsAppXAML(file))
                            sources += "<ApplicationDefinition Include=\"" + file + "\" />\n";
                        else
                            sources += "<Page Include=\"" + file + "\" />\n";
                    }
                    else
                        sources += string.Format(include_template, file);
                }
                content = content.Replace("$SOURCE_FILES$", sources);

                sw.Write(content);
            }

            string compileLog = "";
            //Stopwatch sw1 = new Stopwatch();
            //sw1.Start();

            string msbuild = Path.Combine(Path.GetDirectoryName("".GetType().Assembly.Location), "MSBuild.exe");

            compileLog = RunApp(Path.GetDirectoryName(tempProj), msbuild, "\"" + tempProj + "\" /p:Configuration=\"CSSBuild\" /nologo /verbosity:m").Trim();

            //sw1.Stop();

            if (compileLog.EndsWith("-- FAILED."))
            {
                using (StringReader sr = new StringReader(compileLog))
                {
                    string line = "";
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line == "")
                            continue;

                        if (line.EndsWith("Done building project"))
                            break;

                        int lineNumber = 0;
                        int colNumber = 0;
                        string fileName = "";
                        string errorNumber = "";
                        string errorText = "";
                        bool isWarning = false;

                        int fileEnd = line.IndexOf(": warning ");
                        if (fileEnd == -1)
                            fileEnd = line.IndexOf(": error ");
                        else
                            isWarning = true;

                        if (fileEnd == -1)
                            continue;

                        string filePart = line.Substring(0, fileEnd);
                        string errorPart = line.Substring(fileEnd + 2); //" :" == 2
                        int lineNumberStart = filePart.LastIndexOf("(");
                        int errorDescrStart = errorPart.IndexOf(":");

                        string[] erorLocation = filePart.Substring(lineNumberStart).Replace("(", "").Replace(")", "").Split(',');
                        lineNumber = filePart.EndsWith(")") ? int.Parse(erorLocation[0]) : -1;
                        colNumber = filePart.EndsWith(")") ? int.Parse(erorLocation[1]) : -1;
                        fileName = Path.GetFullPath(lineNumber == -1 ? filePart : filePart.Substring(0, lineNumberStart).Trim());
                        errorNumber = errorPart.Substring(0, errorDescrStart).Trim();
                        errorText = errorPart.Substring(errorDescrStart + 1).Trim();

                        CompilerError error = new CompilerError(fileName, lineNumber, colNumber, errorNumber, errorText);
                        error.IsWarning = isWarning;

                        retval.Errors.Add(error);
                    }
                }
            }

            if (File.Exists(outputFile))
            {
                if (File.Exists(options.OutputAssembly))
                    File.Copy(outputFile, options.OutputAssembly, true);
                else
                    File.Move(outputFile, options.OutputAssembly);
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch { }
            }

            if (options.IncludeDebugInformation)
            {
                string pdbSrcFile = Path.ChangeExtension(outputFile, ".pdb");
                string pdbDestFile = Path.ChangeExtension(options.OutputAssembly, ".pdb");
                if (File.Exists(pdbSrcFile))
                {
                    if (File.Exists(pdbDestFile))
                        File.Copy(pdbSrcFile, pdbDestFile, true);
                    else
                        File.Move(pdbSrcFile, pdbDestFile);
                }
            }
            return retval;
        }

        static string ProjTemplateFile
        {
            get
            {
                //return @"C:\cs-script\Dev\WPF\VS\xaml.template";
                if (Environment.GetEnvironmentVariable("CSScriptDebugging") != null || Environment.GetEnvironmentVariable("CSScriptDebugging") == null)
                    return Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\xaml.template");
                else
                    return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"xaml.template");
            }
        }
        static string RunApp(string workingDir, string app, string args)
        {
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = app;
            myProcess.StartInfo.Arguments = args;
            myProcess.StartInfo.WorkingDirectory = workingDir;
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.Start();

            StringBuilder builder = new StringBuilder();

            string line = null;
            while (null != (line = myProcess.StandardOutput.ReadLine()))
            {
                builder.Append(line + "\n");
            }
            myProcess.WaitForExit();

            return builder.ToString();
        }

    }

    class XAMLTest
    {
        static void _Main()
        {
            bool dll = false;
            string source1 = Environment.ExpandEnvironmentVariables(@"C:\cs-script\Dev\WPF\vs\Window1.cs");
            string source2 = Environment.ExpandEnvironmentVariables(@"C:\cs-script\Dev\WPF\vs\Window1.xaml");
            string source3 = Environment.ExpandEnvironmentVariables(@"C:\cs-script\Dev\WPF\vs\App.xaml");

            CompilerParameters options = new CompilerParameters(
                new string[]
				{
					@"C:\WINDOWS\assembly\GAC_MSIL\System\2.0.0.0__b77a5c561934e089\System.dll",
					@"C:\WINDOWS\assembly\GAC_32\System.Data\2.0.0.0__b77a5c561934e089\System.Data.dll",
					@"C:\Program Files\Reference Assemblies\Microsoft\Framework\v3.0\WindowsBase.dll",
					@"C:\Program Files\Reference Assemblies\Microsoft\Framework\v3.0\PresentationCore.dll",
					@"C:\Program Files\Reference Assemblies\Microsoft\Framework\v3.0\PresentationFramework.dll"
				},
                Path.ChangeExtension(source1, dll ? ".dll" : ".exe"),
                false);

            options.GenerateExecutable = !dll;
            options.CompilerOptions += "/target:winexe ";
            options.IncludeDebugInformation = true;

            CompilerResults result = new CSCompiler("v3.5").CompileAssemblyFromFileBatch(options, new string[]
				{
					//source3, 
					source2, source1 
				});
        }
    }
}
