using System;
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


namespace CSScriptCompilers //CS-Script
{
	/// <summary>
	/// C++ compiler. 
	/// This class is capable of compiling (with MSBuild) dynamically created VS C++ project based on cpp file(s) 
	/// </summary>
	public class CPPCompiler : ICodeCompiler
	{
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

		static string cppEntryPointImpl =
			//"using namespace System;\r\n" +
			"\r\n" +
			"int main(array<System::String ^> ^args) \r\n" +
			"{ \r\n" +
			"	 <CLR_ENTRY>(); \r\n" +
			"    return 0; \r\n" +
			"}";

		public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
		{
			if (!options.GenerateExecutable)
			{
				CompilerResults retval = CompileAssemblyFromFileBatchImpl(options, fileNames);
				return retval;
			}
			else
			{
				//compile dll
				string newSource = Path.ChangeExtension(Path.GetTempFileName(), ".cpp");

				string originatOutFile = options.OutputAssembly;
				options.OutputAssembly = Path.GetTempFileName();
				options.GenerateExecutable = false;

				CompilerResults retval = CompileAssemblyFromFileBatchImpl(options, fileNames);
				if (retval.Errors.Count == 0 && File.Exists(options.OutputAssembly))
				{
					File.Copy(fileNames[0], newSource);
					fileNames[0] = newSource;

					string clrEntryPoint = GetCLREntryPointName(options.OutputAssembly);
					using (StreamWriter sw = new StreamWriter(newSource, true))
					{
						sw.Write(cppEntryPointImpl.Replace("<CLR_ENTRY>", clrEntryPoint));
					}
					try
					{
						File.Delete(options.OutputAssembly);
					}
					catch { }
				}
				else
					return retval;

				//compile exe
				options.OutputAssembly = originatOutFile;
				options.GenerateExecutable = true;

				ArrayList files = new ArrayList();

				files.AddRange(fileNames);
				//files.Add(extraSource);

				return CompileAssemblyFromFileBatchImpl(options, (string[])files.ToArray(typeof(string)));
			}
		}

		public static string GetCLREntryPointName(string asm)
		{
			string autoCleanLocation = Path.Combine(Path.GetTempPath(), "CSSCRIPT\\CPP\\" + Path.GetFileNameWithoutExtension(asm));
			File.Copy(asm, autoCleanLocation);
			Assembly compiledAssembly = Assembly.LoadFile(asm);

			foreach (Module m in compiledAssembly.GetModules())
			{
				foreach (Type t in m.GetTypes())
				{
					BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Static;
					foreach (MemberInfo mi in t.GetMembers(bf))
						if (mi.Name == "Main")
							return (mi.ReflectedType.FullName + ".Main").Replace(".", "::");

				}
			}
			return "";
		}


		CompilerResults CompileAssemblyFromFileBatchImpl(CompilerParameters options, string[] fileNames)
		{
			//System.Diagnostics.Debug.Assert(false);

			CompilerResults retval = new CompilerResults(new TempFileCollection());

			if (Environment.Version.Major < 2)
			{
				throw new ApplicationException("C++/CLI scripts are only supported on .NET 2.0 and higher. Please ensure the script engine is running under the appropriate version of CLR.");
			}

			string outputName = Path.GetFileNameWithoutExtension(options.OutputAssembly);
			string tempDir = Path.Combine(Path.GetTempPath(), "CSSCRIPT\\CPP\\" + System.Guid.NewGuid().ToString());
			string tempProj = Path.Combine(tempDir, outputName + ".vcproj");
			string outputFile = Path.Combine(tempDir, outputName + (options.GenerateExecutable ? ".exe" : ".dll"));

			if (Directory.Exists(tempDir))
				Directory.Delete(tempDir, true);

			Directory.CreateDirectory(tempDir);

			using (StreamReader sr = new StreamReader(ProjTemplateFile))
			using (StreamWriter sw = new StreamWriter(tempProj))
			{
				string content = sr.ReadToEnd();
				content = content.Replace("$NAME$", outputName);
				content = content.Replace("$TEMP_DIR$", tempDir);
				content = content.Replace("$INTER_DIR$", tempDir);
				content = content.Replace("$PREPR_DEBUG$", options.IncludeDebugInformation ? "_DEBUG" : "");
				content = content.Replace("$DEBUG$", options.IncludeDebugInformation ? "true" : "false");

				if (options.GenerateExecutable) //exe
				{
					if (options.CompilerOptions != null && options.CompilerOptions.IndexOf("/target:winexe") != -1)	//WinForm
					{
						content = content.Replace("$MANAGED_EXT$", "2");
						content = content.Replace("$LINK_WINFORM$", "SubSystem=\"2\"\r\nEntryPointSymbol=\"main\"");
					}
					else
					{
						content = content.Replace("$MANAGED_EXT$", "1");	//console
						content = content.Replace("$LINK_WINFORM$", "");
					}
					
					content = content.Replace("$TYPE$", "1");
				}
				else //dll
				{
					content = content.Replace("$MANAGED_EXT$", "1");
					content = content.Replace("$LINK_WINFORM$", "");
					content = content.Replace("$TYPE$", "2");
				}

				string references = "";
				foreach (string file in options.ReferencedAssemblies)
					references += "<AssemblyReference RelativePath=\"" + file + "\"/>\n";
				content = content.Replace("$REFERENCES$", references);

				string sources = "";
				foreach (string file in fileNames)
					sources += "<File RelativePath=\"" + file + "\"></File>\n";
				content = content.Replace("$SOURCE_FILES$", sources);

				sw.Write(content);
			}

			string compileLog = "";
			//Stopwatch sw1 = new Stopwatch();
			//sw1.Start();

			string msbuild = Path.Combine(Path.GetDirectoryName("".GetType().Assembly.Location), "MSBuild.exe");
			string configuration = "CSSBuild";
			if (msbuild.IndexOf("Framework64") != -1) //x64
			{
				msbuild = msbuild.Replace("Framework64", "Framework"); //VS2005 does not support x64 MSBuild/VCBuild
				configuration += "|x64";
			}
			else
			{
				configuration += "|Win32";
			}

			compileLog = RunApp(msbuild, "\"" + tempProj + "\" /p:Configuration=\"" + configuration + "\" /nologo /verbosity:m");
			compileLog = compileLog.TrimEnd();
			//sw1.Stop();

			if (compileLog.EndsWith(" -- FAILED."))
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

						int lineNumber = -1;
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

						lineNumber = filePart.EndsWith(")") ? int.Parse(filePart.Substring(lineNumberStart).Replace("(", "").Replace(")", "")) : -1;
						fileName = Path.GetFullPath(lineNumber == -1 ? filePart : filePart.Substring(0, lineNumberStart).Trim());
						errorNumber = errorPart.Substring(0, errorDescrStart).Trim();
						errorText = errorPart.Substring(errorDescrStart + 1).Trim();

						CompilerError error = new CompilerError(fileName, lineNumber == -1 ? 0 : lineNumber, 0, errorNumber, errorText);
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
				if (Environment.GetEnvironmentVariable("CSScriptDebugging") != null || Environment.GetEnvironmentVariable("CSScriptDebugging") == null)
					return Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\cpp.template");
				else
					return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"cpp.template");
			}
		}
		static string RunApp(string app, string args)
		{
			Process myProcess = new Process();
			myProcess.StartInfo.FileName = app;
			myProcess.StartInfo.Arguments = args;
			myProcess.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
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

	class CPPTest
	{
		static void _Main()
		{
			bool dll = false;
			string source = Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Samples\hello.cpp");
			
			CompilerParameters options = new CompilerParameters(
				new string[]
				{
					@"C:\WINDOWS\assembly\GAC_MSIL\System\2.0.0.0__b77a5c561934e089\System.dll",
					@"C:\WINDOWS\assembly\GAC_32\System.Data\2.0.0.0__b77a5c561934e089\System.Data.dll",
					@"C:\WINDOWS\assembly\GAC_MSIL\System.Xml\2.0.0.0__b77a5c561934e089\System.Xml.dll",
					@"C:\WINDOWS\assembly\GAC_MSIL\System.Windows.Forms\2.0.0.0__b77a5c561934e089\System.Windows.Forms.dll"
				},
				Path.ChangeExtension(source, dll ? ".dll" : ".exe"),
				false);

			options.GenerateExecutable = !dll;
			//options.CompilerOptions += "/target:winexe ";
			options.IncludeDebugInformation = true;

			CompilerResults result = new CPPCompiler().CompileAssemblyFromFileBatch(options, new string[]
				{
					source
				});
		}
	}
}
