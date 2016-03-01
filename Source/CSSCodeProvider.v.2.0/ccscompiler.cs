using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.CodeDom.Compiler;
using Microsoft.JScript;
using System.CodeDom;

namespace CSScriptCompilers //CS-Script
{
	/// <summary>
	/// C# compiler. 
	/// This calss is capable to distinguish strandard and classless C# (CCS|CC#) syntax. It will always normalise 
	/// CC# syntax to the standard C# when CSharpCode|CSharpScriptCode properties accessed.
	/// The parser for CC# is a line-based (but not token-based) parser. And as such it cannot garantee
	/// success in 100% cases of parsing valid CC# code.
	/// Limitations:
	///		At this moment there are no known problems but they may be
	///		discovered in the future.
	/// </summary>
	public class CSharpCompiler : ICodeCompiler  
	{
        object options;
        public CSharpCompiler(object options)
        {
            this.options = options;
        }
		#region Dummy interface implementations 
		public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit)
		{
			throw new Exception("CompileAssemblyFromDom is not implemented");
		}
		public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits)
		{
			throw new Exception("CompileAssemblyFromDomBatch is not implemented"); 
		}
		public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName)
		{
			throw new Exception("CompileAssemblyFromFile is not implemented");
		}
		
		public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
		{
			throw new Exception("CompileAssemblyFromSource is not implemented");
		}
		public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
		{
			throw new Exception("CompileAssemblyFromSourceBatch is not implemented");
		}
		#endregion

		public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
		{
			string tempFile;
			ArrayList classlessFiles = new ArrayList(); 
			ArrayList files = new ArrayList();
			int count = 0;
			CCSharpParser ccs;
			foreach (string file in fileNames)
			{
				ccs = new CCSharpParser(file);
				if (!ccs.isClassless)
				{
					files.Add(file);
				}
				else
				{
					tempFile = ccs.ToTempFile(count > 0 );
					classlessFiles.Add(tempFile);
					files.Add(tempFile);
				}
				count++;
			}
			
            Microsoft.CSharp.CSharpCodeProvider provider;

            if (options.ToString() == "" || options == null)
                return new Microsoft.CSharp.CSharpCodeProvider().CreateCompiler().CompileAssemblyFromFileBatch(options, fileNames);
            else
                return new Microsoft.CSharp.CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", options.ToString() } }).CreateCompiler().CompileAssemblyFromFileBatch(options, fileNames);

            CompilerResults retval;
            retval = provider.CreateCompiler().CompileAssemblyFromFileBatch(options, (string[])files.ToArray(typeof(string)));
			
			
			if (!retval.Errors.HasErrors)
				foreach (string file in classlessFiles)
					try
					{ 
						File.Delete(file);
					} 
					catch{}

			return retval;
		}
	}

	/// <summary>
	/// Extremely light/primitive parser
	/// </summary>
	/// <param name="file"></param>
	/// <returns>true if the file comntains classless C# code. Otherwise returns false.</returns>
	class CCSharpParser
	{	
		public CCSharpParser(string file)
		{
			if (!File.Exists(file))
				throw new FileLoadException("Cannot find "+file+" file.");
			
			using (StreamReader sr = new StreamReader(file))
				isClassless = IsClasslessCode(sr.ReadToEnd());

			this.file = file;
		}
		public bool isClassless = false;

		string FixString(string text)
		{
			string retval = text;

			foreach (char c in @" !@#$%^&*()+|\~`,./'"":;{[}]".ToCharArray())
				retval = retval.Replace(c, '_');

			return retval.Insert(0, "_"); //to ensure the string does not starte with a numeric character as C# compiler does not like it when string used as namespace
		}

		public string CSharpCode //C# code without static Main defined
		{
			get
			{
				if (!isClassless)
					throw new Exception("Attempting to process standard C# code as CC# syntax.");
				StringBuilder sb = new StringBuilder();
				if (!bodyStart0.TrimStart().StartsWith("using System;"))
					sb.Append("using System;\r\n"); //always insert default namespace
				sb.Append(header.ToString());
				sb.Append(bodyStart0);
				sb.Append(FixString(Path.GetFileNameWithoutExtension(file)));
				sb.Append(bodyStart1);
				sb.Append("i_Main"); 
				sb.Append(bodyStart2);
				sb.Append(body.ToString());
				sb.Append(bodyEnd);
				sb.Append(footer.ToString());
				sb.Append(footerEnd);
				return sb.ToString();
			}
		}
		public string CSharpScriptCode //C# code with static Main defined
		{
			get
			{
				if (!isClassless)
					throw new Exception("Attempting to process standard C# code as CC# syntax.");
				StringBuilder sb = new StringBuilder();
				sb.Append("using System;\r\n"); //always insert default namespace
				sb.Append(header.ToString());
				sb.Append(bodyStart0);
				sb.Append(FixString(Path.GetFileNameWithoutExtension(file)));
				sb.Append(bodyStart1);
				sb.Append("Main");
				sb.Append(bodyStart2);
				sb.Append(body.ToString());
				sb.Append(bodyEnd);
				sb.Append(footer.ToString());
				sb.Append(footerEnd);
				return sb.ToString();
			}
		}
		public string ToTempFile(bool imported)
		{
			if (!isClassless)
				throw new Exception("ClasslessCSharpParser.ToTempFile should not be called for standard C# code");

			string file = Path.GetTempFileName();

			if (File.Exists(file))
				File.Delete(file);

			string dir = Path.Combine(Path.GetTempPath(), "CSSCRIPT//Classless");
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			
			file = Path.Combine(dir, Path.GetFileNameWithoutExtension(file)+".cs");

			using (StreamWriter sw = new StreamWriter(file))
				sw.Write(imported ? CSharpCode : CSharpScriptCode);
			
			return file;
		}

		#region CC# Parser
		static string bodyStart0 =
			"namespace ";
		static string bodyStart1 = 
			"\r\n" +
			"{\r\n" +
			"	public class Script\r\n" +
			"	{\r\n" +
			"		static public void ";
		static string bodyStart2 = 
			"(string[] args)\r\n" +
			"		{\r\n";
		static string bodyEnd =
			"		}\r\n";
		static string footerEnd =
			"	}\r\n"+
			"}\r\n";

		//const string usage = "Usage: cscscript noClass [/o]|[/f] <file>...\nExecutes C# script file that contains standard or free standing C# code without any class declarations (classless).\n"+
		//	"/o - output processed file (ext_<file>) only.\n"+
		//	"/f - force the file to be recognized as containing classless C# code.\n"+
		//	" Example: cscscript noClass script.cs\n";

		StringBuilder header = new StringBuilder();
		StringBuilder body = new StringBuilder();
		StringBuilder footer = new StringBuilder();
		string file;

		enum CodeArea
		{
			unknown,
			header,
			body,
			footer
		}

		bool ProcessIfMarkedAsClassless(string rawCode)
		{
			int beginPos = -1;
			while (-1 != (beginPos = rawCode.IndexOf("//css_begin;", beginPos+1)))
			{
				if (beginPos != 0)
					if (rawCode[beginPos - 1] != '\n' && rawCode[beginPos - 1] != '\r')
						continue;

				if ((beginPos + "//css_begin;".Length) < rawCode.Length)
					if (rawCode[beginPos + "//css_begin;".Length + 1] != '\n' && rawCode[beginPos + "//css_begin;".Length + 1] != '\r')
						continue;
				break;
			}
			int endPos = -1;
			while (-1 != (endPos = rawCode.IndexOf("//css_end;", endPos + 1)))
			{
				if (endPos != 0)
					if (rawCode[endPos - 1] != '\n' && rawCode[endPos - 1] != '\r')
						continue;

				if ((endPos + "//css_end;".Length) < rawCode.Length)
					if (rawCode[endPos + "//css_end;".Length + 1] != '\n' && rawCode[endPos + "//css_end;".Length + 1] != '\r')
						continue;
				break;
			}
			if (beginPos == -1 && endPos == -1)
				return false;

			if (endPos == -1)
				endPos = rawCode.Length-1;
			if (beginPos == -1)
				beginPos = 0;

			header.Append(rawCode.Substring(0, beginPos));
			body.Append(rawCode.Substring(beginPos, endPos - beginPos));
			footer.Append(rawCode.Substring(endPos, rawCode.Length-endPos));

			return true;
		}

		bool IsClasslessCode(string rawCode)
		{
			if (ProcessIfMarkedAsClassless(rawCode))
				return true;

			//ensure "one statement - one line" rule
			string code = new CSharpParser(rawCode).PureCode.Replace("\n", " ").Replace("\r", " ").Replace(";", ";\n"); 
			
			string line;
			StringReader sr = new StringReader(code);

			CodeArea area = CodeArea.unknown;
			while ((line = sr.ReadLine()) != null)
			{
				string clearLine = line.Trim();

				if (clearLine.Length == 0)
					continue;
				while (true)
				{
					switch (area)
					{
						case CodeArea.unknown:
						{
							if (IsUsingDirective(clearLine))
							{
								area = CodeArea.header;
								continue;
							}
							else if (!IsDeclaration(clearLine))
							{
								area = CodeArea.body;
								continue;
							}
							else
							{
								area = CodeArea.footer;
								continue;
							}
						}
						case CodeArea.header:
						{
							if (IsUsingDirective(clearLine))
							{
								header.Append(line);
								header.Append(Environment.NewLine);
							}
							else
							{
								area = CodeArea.body;
								continue;
							}
							break;
						}
						case CodeArea.body:
						{
							if (!IsDeclaration(clearLine))
							{
								body.Append(line);
								body.Append(Environment.NewLine);
							}
							else
							{
								area = CodeArea.footer;
								continue;
							}
							break;
						}
						case CodeArea.footer:
						{
							footer.Append(line);
							footer.Append(Environment.NewLine);
							break;
						}
					}
					break;
				}
			}
			if (body.Length != 0)
				return true;
			else
			{
				if (footer.Length == 0)
					return false;
				else
				{
					//it is either C# or CC# without body
					string fotterStr = footer.ToString().TrimStart();
					if (fotterStr.StartsWith("namespace") && IsToken(fotterStr, 0, "namespace".Length))
						return false;
					else
					{
						foreach (string classToken in new string[] { "class", "enum", "struct" })
						{
							int classStart = -1;
							while ((classStart = fotterStr.IndexOf(classToken, classStart + 1)) != -1)
							{
								if (IsToken(fotterStr, classStart, classToken.Length))
								{
									string decorations = fotterStr.Substring(0, classStart);
									if (decorations.IndexOfAny(";{}()".ToCharArray()) != -1)
										break;
									else
										return false;
								}
							}
						}
						return true;
					}
				}
			}
		}
		bool IsToken(string text, int start, int length)
		{
			if (start != 0)
				if (text.Substring(start - 1, 1).LastIndexOfAny("\n\r\t {}()".ToCharArray()) == -1)
					return false;

			if ((start + length) != text.Length)
				if (text.Substring(start + length, 1).IndexOfAny("\n\r\t {}()".ToCharArray()) == -1)
					return false;

			return true;
		}
		struct sd
		{
		}
		bool IsDeclaration(string code)
		{
			if (IsMethodDeclaration(code) || code.StartsWith("class ") || code.StartsWith("enum ") || code.StartsWith("struct ") || code.StartsWith("namespace ") ||
				code.StartsWith("private ") || code.StartsWith("public ") || code.StartsWith("struct ") || code.StartsWith("internal ") || code.StartsWith("struct ") || code.StartsWith("protected "))
				return true;
			else
				return false;
		}
		bool IsMethodDeclaration(string code)
		{	
			//RE would work here very well too (for whole text not for line) and it might be introduced in the future.
			//The following RE will return all matches for patern: "word ([word word[,word word]]) {"
			//\s+ \w+ \s* \(  [\s* \w+ \s+ \w+ \s*]* [,\s* \w+ \s+ \w+ \s*]* \)   \s* { 

			int lBracket = code.IndexOf("(");
			if (lBracket != -1)
			{
				int rBracket = code.IndexOf(")", lBracket);
				if (rBracket != -1)
				{
					if (code.Length-1 != rBracket) //rBracket must be the last character in the trimmed line (to ignore 'void methods' calls)
					{
						string leftOvers = code.Substring(rBracket+1).Trim();
						if (!leftOvers.StartsWith("{"))
							return false;
					}
					
					string[] betweenBracketsContent = code.Substring(lBracket, rBracket-lBracket).Trim("()".ToCharArray()).Split(",".ToCharArray());
					if (betweenBracketsContent.Length != 0)
						foreach(string declaration in betweenBracketsContent)
							if (declaration.Length != 0)
							{
								int tokenCount = 0;
								foreach(string token in declaration.Trim().Split("\t\n\r ".ToCharArray()))
									if (token != "" && token != "in" && token != "out" && token != "ref")
										tokenCount++;
		
								if (tokenCount != 2 && tokenCount != 0)
									return false;
							}
						
					return true;
				}
			}
			return false;
		}
		bool IsUsingDirective(string code)
		{
			if (code.StartsWith("using ") || code.StartsWith("//css_"))
				return true;
			else
				return false;
		}
		#endregion
		#region C# Parser
		/// <summary>
		/// CSharpParser is a cut down edition of the CS-Script engine C# parser implementation.
		/// It only parses C# code with respect to strings and comments.
		/// </summary>
		class CSharpParser
		{
			public CSharpParser(string code)
			{
				this.code = code;
				NoteCommentsAndStrings();
			}
			public string PureCode
			{
				get
				{
					if (commentRegions.Count == 0)
						return code;

					StringBuilder sb = new StringBuilder();
					int start = -1;
					int end = -1;
					int lastCommentEnd = -1;
					
					foreach (int[] positions in commentRegions)
					{
						start = lastCommentEnd + 1;
						end = positions[0];
						lastCommentEnd = positions[1];
						sb.Append(code.Substring(start, end - start));

						if (code[lastCommentEnd] == '\n' || code[lastCommentEnd] == '\r')
						{
							if (code[lastCommentEnd - 1] == '\n' || code[lastCommentEnd - 1] == '\r')
								sb.Append(code[lastCommentEnd - 1]);
							sb.Append(code[lastCommentEnd]);
						}
					}
					if (lastCommentEnd + 1 < code.Length - 1)
						sb.Append(code.Substring(lastCommentEnd + 1));
					
					return sb.ToString();
				}
			}
			string code = "";
			void NoteCommentsAndStrings()
			{
				ArrayList quotationChars = new ArrayList();
				int startPos = -1;
				int startSLC = -1; //single line comment
				int startMLC = -1; //multiple line comment
				int searchOffset = 0;
				string endToken = "";
				string startToken = "";
				int endPos = -1;
				int lastEndPos = -1;
				do
				{
					startSLC = code.IndexOf("//", searchOffset);
					startMLC = code.IndexOf("/*", searchOffset);

					if (startSLC == Math.Min(startSLC != -1 ? startSLC : Int16.MaxValue,
						startMLC != -1 ? startMLC : Int16.MaxValue))
					{
						startPos = startSLC;
						startToken = "//";
						endToken = "\n";
					}
					else
					{
						startPos = startMLC;
						startToken = "/*";
						endToken = "*/";
					}

					if (startPos != -1)
						endPos = code.IndexOf(endToken, startPos + startToken.Length);

					if (startPos != -1 && endPos != -1)
					{
						int startCode = commentRegions.Count == 0 ? 0 : ((int[])commentRegions[commentRegions.Count - 1])[1] + 1;

						int[] quotationIndexes = AllRawIndexOf("\"", startCode, startPos);
						if ((quotationIndexes.Length % 2) != 0)
						{
							searchOffset = startPos + startToken.Length;
							continue;
						}

						commentRegions.Add(new int[2] { startPos, endPos });
						quotationChars.AddRange(quotationIndexes);

						searchOffset = endPos + endToken.Length;
					}
				}
				while (startPos != -1 && endPos != -1);

				if (lastEndPos != 0 && searchOffset < code.Length)
				{
					quotationChars.AddRange(AllRawIndexOf("\"", searchOffset, code.Length));
				}

				for (int i = 0; i < quotationChars.Count; i++)
				{
					if (i + 1 < stringRegions.Count)
						stringRegions.Add(new int[] { (int)quotationChars[i], (int)quotationChars[i + 1] });
					else
						stringRegions.Add(new int[] { (int)quotationChars[i], -1 });
					i++;
				}
			}
			int[] AllRawIndexOf(string pattern, int startIndex, int endIndex) //all raw matches
			{
				ArrayList retval = new ArrayList();
				int pos = code.IndexOf(pattern, startIndex, endIndex - startIndex);
				while (pos != -1)
				{
					retval.Add(pos);
					pos = code.IndexOf(pattern, pos + 1, endIndex - (pos + 1));
				}
				return (int[])retval.ToArray(typeof(int));
			}
			ArrayList stringRegions = new ArrayList();
			ArrayList commentRegions = new ArrayList();
		}
		#endregion
	}

	class Test
	{
		static void i_Main()
		{
			CCSharpParser ccs = new CCSharpParser(@"C:\cs-script\Dev\CC#\using.ccs");
			if (ccs.isClassless)
			{
				Console.WriteLine("CC#");
				Console.WriteLine(ccs.CSharpCode);
			}
			else
				Console.WriteLine("C#");
		}
	}
}
