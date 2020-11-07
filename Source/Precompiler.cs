using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace csscript
{
    /// <summary>
    /// This class is a container for information related to the script pre-compilation.
    /// <para>It is used to pass an input information from the script engine to the <c>precompiler</c> instance as well as to pass back
    /// to the script engine some output information (e.g. added referenced assemblies)</para> .
    /// </summary>
    internal class PrecompilationContext
    {
        /// <summary>
        /// Collection of the referenced assemblies to be added to the process script referenced assemblies.
        /// <para>You may want to add new items to the referenced assemblies because of the pre-compilation logic (e.g. some code using assemblies not referenced by the primary script
        /// is injected in the script).</para>
        /// </summary>
        public List<string> NewReferences = new List<string>();

        /// <summary>
        /// Collection of the new dependency items (files).
        /// <para>Dependency files are checked before the script execution in order to understand if the script should be recompiled or it can be loaded from
        /// the cache. If any of the dependency files is changed since the last script execution the script engine will recompile the script. In the simple execution
        /// scenarios the script file is a dependency file.</para>
        /// </summary>
        public List<string> NewDependencies = new List<string>();

        /// <summary>
        /// Collection of the new 'include' items (dependency source files).
        /// </summary>
        public List<string> NewIncludes = new List<string>();

        /// <summary>
        /// Collection of the assembly and script probing directories to be added to the process search directories.
        /// <para>You may want to add new items to the process search directories because of the pre-compilation logic.</para>
        /// </summary>
        public List<string> NewSearchDirs = new List<string>();

        /// <summary>
        /// Additional compiler options to be passed to the script compiler
        /// </summary>
        public string NewCompilerOptions;

        /// <summary>
        /// Collection of the process assembly and script probing directories.
        /// </summary>
        public string[] SearchDirs = new string[0];

        public string Content;
        public string scriptFile;
        public bool IsPrimaryScript;
    }

    internal class DefaultPrecompiler
    {
        public static bool Compile(ref string code, string scriptFile, bool IsPrimaryScript, Hashtable context)
        {
            if (code.StartsWith("#!"))
            {
                code = "//" + code; //comment the Linux hashbang line to keep C# compiler happy
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// AutoclassGenerator to be used by external applications for generating the auto-class from the classless scripts
    /// </summary>
    public class AutoclassGenerator
    {
        /// <summary>
        /// Processes the specified code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="injectionPos">The injection position.</param>
        /// <param name="injectionLength">Length of the injection.</param>
        /// <returns></returns>
        static public string Process(string code, out int injectionPos, out int injectionLength)
        {
            AutoclassPrecompiler.Result result = AutoclassPrecompiler.Process(code, ConsoleEncoding);
            injectionPos = result.InjectionPos;
            injectionLength = result.InjectionLength;
            return result.Content;
        }

        //Returns 0-based line and column of the position in the text
        static int[] GetLineCol(string text, int pos)
        {
            int line = -1;
            int col = -1;
            using (var sr = new StringReader(text.Substring(0, pos)))
            {
                string line_str;
                while ((line_str = sr.ReadLine()) != null)
                {
                    line++;
                    col = line_str.Length;
                }
                if (line == -1) line = 0;
                if (col == -1) col = 0;
            }

            return new[] { line, col };
        }

        //Returns text position of 0-based line and column pair
        static int GetPos(string text, int line, int col)
        {
            bool isInLine = false;
            int lineCount = -1;
            int colCount = -1;
            int lastNonWhiteCharPos = -1;

            string line_s = null;
            var lines = new List<string>();

            for (int i = 0; i < text.Length; i++)
            {
                var isNewLineStart = (i == 0);

                if (text[i] != '\n' && text[i] != '\r')
                {
                    if (!isInLine)
                    {
                        colCount = 0;
                        lineCount++;
                        if (lineCount > line)
                            return lastNonWhiteCharPos + 1; //end of the line

                        if (line_s != null)
                            lines.Add(line_s);
                        line_s = "";
                    }
                    isInLine = true;

                    if (lineCount == line && colCount == col)
                        return i;

                    colCount++;

                    line_s += text[i];
                    lastNonWhiteCharPos = i;
                }
                else
                {
                    if (!isInLine)
                    {
                        if (i == 0)
                        {
                            // start of the text
                            if (text[i] == '\r' || text[i] == '\n')
                                lineCount++;
                        }
                        else
                        {
                            if (text[i] == '\n')
                            {
                                if (text[i - 1] == '\n') // /n[/n]
                                    lineCount++;
                            }
                            else if (text[i] == '\r')
                            {
                                if (text[i - 1] == '\r')  // /r[/r]
                                    lineCount++;
                                else if (text[i - 1] == '\n') // /r/n/[/r]/n
                                    lineCount++;
                            }
                        }
                    }

                    isInLine = false;
                }
            }
            return -1;
        }

        /// <summary>
        /// Processes the specified code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        static public string Process(string code, ref int position)
        {
            int[] line_col = GetLineCol(code, position);
            int originalLine = line_col[0];
            int originalCol = line_col[1];

            var result = AutoclassPrecompiler.Process(code, ConsoleEncoding);

            if (result.InjectionPos != -1)
            {
                int line = originalLine;
                if (result.BodyInjectedLine != -1 && result.BodyInjectedLine <= originalLine)
                {
                    line += result.BodyInjectedLineCount;
                    if (result.FooterInjectedLine != -1 && result.FooterInjectedLine <= originalLine)
                        line += result.FooterInjectedLineCount;
                }
                position = GetPos(result.Content, line, originalCol);
            }
            return result.Content;
        }

        /// <summary>
        /// The console encoding to be set for at the script initialization.
        /// </summary>
        static public string ConsoleEncoding = "utf-8";
    }

    internal class AutoclassPrecompiler
    {
        internal static bool decorateAutoClassAsCS6 = false;
        internal static bool injectBreakPoint = false;
        internal static string scriptFile;

        public static bool Compile(ref string content, string scriptFile, bool IsPrimaryScript, Hashtable context)
        {
            AutoclassPrecompiler.scriptFile = scriptFile;

            if (!IsPrimaryScript)
                return false;

            var result = Process(content, (string)context["ConsoleEncoding"]);
            content = result.Content;
            return result.InjectionLength > 0;
        }

        internal static string Process(string content)
        {
            return Process(content, Settings.DefaultEncodingName).Content;
        }

        internal class Result
        {
            public Result(string content)
            {
                Content = content;
                Reset();
            }

            public string Content;
            public int InjectionPos;
            public int InjectionLength;
            public int BodyInjectedLine;
            public int BodyInjectedLineCount;
            public int FooterInjectedLine;
            public int FooterInjectedLineCount;

            public Result Reset()
            {
                InjectionPos = -1;
                InjectionLength = 0;
                BodyInjectedLine = -1;
                BodyInjectedLineCount = 0;
                FooterInjectedLine = -1;
                FooterInjectedLineCount = 0;
                return this;
            }
        }

        internal static Result Process(string content, string consoleEncoding)
        {
            int entryPointInjectionPos = -1;
            var result = new Result(content);
            string autoClassMode = null;

            // Debug.Assert(false);
            // we will be effectively normalizing the line ends but the input file may no be

            var code = new StringBuilder(4096);
            var footer = new StringBuilder();

            //css_ac_end
            bool headerProcessed = false;
            int bracket_count = 0;

            bool stopDecoratingDetected = false;
            // int insertBreakpointAtLine = -1;

            string line;
            using (var sr = new StringReader(content))
            {
                bool autoCodeInjected = false;
                int lineCount = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    string lineText = line.TrimStart();

                    if (!stopDecoratingDetected && (line.Trim() == "//css_ac_end" || line.Trim() == "//css_autoclass_end"))
                    {
                        stopDecoratingDetected = true;
                        footer.AppendLine("#line " + (lineCount + 1) + " \"" + scriptFile + "\"");
                        result.FooterInjectedLine = lineCount;

                        // One is "} ///CS-Script auto-class generation"
                        // And another one "#line "
                        result.FooterInjectedLineCount = 2;
                    }

                    if (stopDecoratingDetected)
                    {
                        footer.AppendLine(line);
                        continue;
                    }

                    if (!headerProcessed && (lineText.StartsWith("//css_autoclass") || lineText.StartsWith("//css_ac")))
                    {
                        autoClassMode = lineText.Replace("//css_ac", "")
                                                .Replace("//css_autoclass", "")
                                                .Trim();
                    }

                    if (!headerProcessed && !line.TrimStart().StartsWith("using ")) //not using...; statement of the file header
                    {
                        if (!line.StartsWith("//") && line.Trim() != "") //not comments or empty line
                        {
                            headerProcessed = true;

                            result.InjectionPos = code.Length;
                            string tempText = "public class ScriptClass { public ";

                            if (autoClassMode == "freestyle")
                            {
                                var setConsoleEncoding = "";
                                if (string.Compare(consoleEncoding, Settings.DefaultEncodingName, true) != 0)
                                    setConsoleEncoding = "try { System.Console.OutputEncoding = System.Text.Encoding.GetEncoding(\"" + consoleEncoding + "\"); } catch {} ";

                                tempText += "static void Main(string[] args) { " + setConsoleEncoding + " main_impl(args); } ///CS-Script auto-class generation" + Environment.NewLine;

                                //"#line" must be injected before the method name. Injecting into the first line in body does not work. probably related to JIT
                                tempText += "#line " + (lineCount) + " \"" + scriptFile + "\"" + Environment.NewLine;
                                tempText += "static public void main_impl(string[] args) { ///CS-Script auto-class generation" + Environment.NewLine;
                                result.BodyInjectedLine = lineCount;
                                result.InjectionLength += tempText.Length;
                                result.BodyInjectedLineCount = 3;

                                autoCodeInjected = true;
                            }
#if net4
#if !InterfaceAssembly
                            if (decorateAutoClassAsCS6)
                                code.Append("using static dbg; ");
#endif
#endif
                            code.Append(tempText);

                            result.InjectionLength += tempText.Length;
                            entryPointInjectionPos = code.Length;
                        }
                    }

                    if (!autoCodeInjected && entryPointInjectionPos != -1 && !Utils.IsNullOrWhiteSpace(line))
                    {
                        bracket_count += lineText.Split('{').Length - 1;
                        bracket_count -= lineText.Split('}').Length - 1;

                        if (!lineText.StartsWith("//"))
                        {
                            // static void Main(string[] args)
                            // or
                            // int main(string[] args)

                            MatchCollection matches = Regex.Matches(lineText, @"\s+main\s*\(", RegexOptions.IgnoreCase);
                            foreach (Match match in matches)
                            {
                                // Ignore VB entry point
                                if (lineText.TrimStart().StartsWith("Sub Main", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                // Ignore assembly pseudo entry point "instance main"
                                if (match.Value.Contains("main"))
                                {
                                    bool noargs = Regex.Matches(lineText, @"\s+main\s*\(\s*\)").Count != 0;
                                    bool noReturn = Regex.Matches(lineText, @"void\s+main\s*\(").Count != 0;

                                    string actualArgs = (noargs ? "" : "args");

                                    string entryPointDefinition = "static int Main(string[] args) { ";

                                    if (string.Compare(consoleEncoding, Settings.DefaultEncodingName, true) != 0)
                                        entryPointDefinition += "try { System.Console.OutputEncoding = System.Text.Encoding.GetEncoding(\"" + consoleEncoding + "\"); } catch {} ";

                                    if (noReturn)
                                        entryPointDefinition += "new ScriptClass().main(" + actualArgs + "); return 0; } ";
                                    else
                                        entryPointDefinition += "return (int)new ScriptClass().main(" + actualArgs + "); } ";

                                    // point to the next line
                                    entryPointDefinition += "///CS-Script auto-class generation" + Environment.NewLine +
                                                             "#line " + (lineCount + 1) + " \"" + scriptFile + "\"";
                                    // if (injectBreakPoint)
                                    //     insertBreakpointAtLine = lineCount + 1;
                                    result.BodyInjectedLine = lineCount;
                                    result.InjectionLength += entryPointDefinition.Length;
                                    result.BodyInjectedLineCount = 2;
                                    code.Insert(entryPointInjectionPos, entryPointDefinition + Environment.NewLine);
                                }
                                else if (match.Value.Contains("Main")) //assembly entry point "static Main"
                                {
                                    if (lineText.Contains("static"))
                                    {
                                        if (bracket_count > 0) //not classless but a complete class with static Main
                                        {
                                            return result.Reset();
                                        }
                                    }

                                    string entryPointDefinition = "///CS-Script auto-class generation" + Environment.NewLine +
                                               "#line " + (lineCount + 1) + " \"" + scriptFile + "\"";

                                    result.BodyInjectedLine = lineCount;
                                    result.BodyInjectedLineCount = 2;
                                    code.AppendLine(entryPointDefinition);
                                    result.InjectionLength += entryPointDefinition.Length;
                                    // insertBreakpointAtLine = lineCount + 1;
                                }
                                autoCodeInjected = true;
                                break;
                            }
                        }
                    }
                    code.Append(line);

                    // if (insertBreakpointAtLine == lineCount)
                    // {
                    //     insertBreakpointAtLine = -1;
                    //     // if (injectBreakPoint)
                    //     //     code.Append("System.Diagnostics.Debugger.Break();");
                    // }

                    code.Append(Environment.NewLine);
                    lineCount++;
                }

                if (!autoCodeInjected)
                {
                    return result.Reset();
                }
            }

            if (autoClassMode == "freestyle")
                code.Append("}");

            code.Append("} ///CS-Script auto-class generation" + Environment.NewLine);

            result.Content = code.ToString() + footer.ToString();

            return result;
        }
    }
}