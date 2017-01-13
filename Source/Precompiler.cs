using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace csscript
{
    /// <summary>
    /// This class is a container for information related to the script precompilation.
    /// <para>It is used to pass an input information from the script engine to the <c>precompiler</c> instance as well as to pass back
    /// to the script engine some output information (e.g. added referenced assemblies)</para> .
    /// </summary>
    internal class PrecompilationContext
    {
        ///// <summary>
        ///// Full path of the script being passed for precompilation.
        ///// </summary>
        //public string ScriptFileName;
        ///// <summary>
        ///// Flag, which indicates if the script passed for precompilation is an entry script (primary script).
        ///// <para>This field can be used to determine the precompilation algorithm based on the entry script. For example
        ///// generating the <c>static Main()</c> wrapper for classless scripts should be done only for an entry script but not for other included/imported script. </para>
        ///// </summary>
        //public bool IsPrimaryScript = true;
        /// <summary>
        /// Collection of the referenced assemblies to be added to the process script referenced assemblies.
        /// <para>You may want to add new items to the referenced assemblies because of the precompilation logic (e.g. some code using assemblies not referenced by the primary script 
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
        /// <para>You may want to add new items to the process search directories because of the precompilation logic.</para>
        /// </summary>
        public List<string> NewSearchDirs = new List<string>();
        /// <summary>
        /// Collection of the process assembly and script probing directories.
        /// </summary>
        public string[] SearchDirs = new string[0];
    }

    ///// <summary>
    ///// The interface that all CS-Script precompilers need to implement.
    ///// <para>
    ///// The following is an example of the precompiler for sanitizing the script containing hashbang string on Linux.
    ///// </para>
    ///// <code>
    /////  public class HashBangPrecompiler : IPrecompiler
    /////  {
    /////     public bool Compile(ref string code, PrecompilationContext context)
    /////     {
    /////         if (code.StartsWith("#!"))
    /////         {
    /////              code = "//" + code; //comment the Linux hashbang line to keep C# compiler happy
    /////              return true;
    /////         }
    /////              
    /////         return false;
    /////     }
    ///// }
    ///// </code>
    ///// </summary>
    //public interface IPrecompiler
    //{
    //    /// <summary>
    //    /// Compiles/modifies the specified code.
    //    /// <para>
    //    /// </para>
    //    /// </summary>
    //    /// <param name="code">The code to be compiled.</param>
    //    /// <param name="context">The context.</param>
    //    /// <returns><c>True</c> if the <paramref name="code"/> code has been modified and <c>False</c> </returns>
    //    bool Compile(ref string code, PrecompilationContext context);
    //}

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
            return AutoclassPrecompiler.Process(code, out injectionPos, out injectionLength, ConsoleEncoding);
        }

        /// <summary>
        /// Processes the specified code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        static public string Process(string code, ref int position)
        {
            int injectionPos;
            int injectionLength;
            string retval = Process(code, out injectionPos, out injectionLength);
            if (position > injectionPos)
                position += injectionLength;
            return retval;
        }

        /// <summary>
        /// The console encoding to be set for at the script initialization.
        /// </summary>
        static public string ConsoleEncoding = "utf-8";
    }

    internal class AutoclassPrecompiler// : IPrecompiler
    {
        //static string FileToClassName(string text)
        //{
        //    return Path.GetFileNameWithoutExtension(text).Replace("_", ""); //double '_' are not allowed for class names
        //}

        internal static bool decorateAutoClassAsCS6 = false;

        public static bool Compile(ref string content, string scriptFile, bool IsPrimaryScript, Hashtable context)
        {
            if (!IsPrimaryScript)
                return false;

            int injectionPos;
            int injectionLength;
            content = Process(content, out injectionPos, out injectionLength, (string) context["ConsoleEncoding"]);
            return true;
        }

        internal static string Process(string content, out int injectionPos, out int injectionLength, string consoleEncoding)
        {
            int entryPointInjectionPos = -1;
            injectionPos = -1;
            injectionLength = 0;

            var code = new StringBuilder(4096);
            //code.Append("//Auto-generated file" + Environment.NewLine); //cannot use AppendLine as it is not available in StringBuilder v1.1
            //code.Append("using System;\r\n");

            bool headerProcessed = false;
            int bracket_count = 0;

            string line;
            using (var sr = new StringReader(content))
            {
                bool autoCodeInjected = false;

                while ((line = sr.ReadLine()) != null)
                {
                    if (!headerProcessed && !line.TrimStart().StartsWith("using ")) //not using...; statement of the file header
                        if (!line.StartsWith("//") && line.Trim() != "") //not comments or empty line
                        {
                            headerProcessed = true;

                            injectionPos = code.Length;
                            string tempText = "public class ScriptClass { public ";
                            if (decorateAutoClassAsCS6)
                                code.Append("using static dbg; ");
                            code.Append(tempText);
                            injectionLength += tempText.Length;
                            entryPointInjectionPos = code.Length;
                        }

                    if (!autoCodeInjected && entryPointInjectionPos != -1 && !Utils.IsNullOrWhiteSpace(line))
                    {
                        string text = line.TrimStart();

                        bracket_count += text.Split('{').Length - 1;
                        bracket_count -= text.Split('}').Length - 1;

                        if (!text.StartsWith("//"))
                        {
                            //static void Main(string[] args)
                            //or
                            //int main(string[] args)

                            MatchCollection matches = Regex.Matches(text, @"\s+main\s*\(", RegexOptions.IgnoreCase);
                            foreach (Match match in matches)
                            {
                                if (match.Value.Contains("main")) //assembly pseudo entry point "instance main"
                                {
                                    bool noargs = Regex.Matches(text, @"\s+main\s*\(\s*\)").Count != 0;
                                    bool noReturn = Regex.Matches(text, @"void\s+main\s*\(").Count != 0;

                                    string actualArgs = (noargs ? "" : "args");

                                    string entryPointDefinition = "static int Main(string[] args) { ";

                                    if (decorateAutoClassAsCS6)
                                        entryPointDefinition = "using static dbg; " + entryPointDefinition;

                                    if (string.Compare(consoleEncoding, Settings.DefaultEncodingName, true) != 0)
                                        entryPointDefinition += "try { Console.OutputEncoding = System.Text.Encoding.GetEncoding(\"" + consoleEncoding + "\"); } catch {} ";

                                    if (noReturn)
                                    {
                                        entryPointDefinition += "new ScriptClass().main(" + actualArgs + "); return 0;";
                                    }
                                    else
                                    {
                                        entryPointDefinition += "return (int)new ScriptClass().main(" + actualArgs + ");";
                                    }
                                    entryPointDefinition += "} ///CS-Script auto-class generation" + Environment.NewLine;

                                    injectionLength += entryPointDefinition.Length;
                                    code.Insert(entryPointInjectionPos, entryPointDefinition);
                                }
                                else if (match.Value.Contains("Main")) //assembly entry point "static Main"
                                {
                                    if (!text.Contains("static"))
                                    {
                                        string tempText = "static ";

                                        injectionLength += tempText.Length;
                                        bool allow_member_declarations_before_entry_point = true;
                                        if (allow_member_declarations_before_entry_point)
                                            code.Append(tempText);
                                        else
                                            code.Insert(entryPointInjectionPos, tempText);
                                    }
                                    else if (bracket_count > 0) //not classless but a complete class with static Main
                                    {
                                        return content;
                                    }
                                }
                                autoCodeInjected = true;
                                break;
                            }
                        }
                    }
                    code.Append(line);
                    code.Append(Environment.NewLine);
                }
            }
            code.Append("} ///CS-Script auto-class generation" + Environment.NewLine);

            var result = code.ToString();
            return result;
        }
    }
}