using CSScripting;
using System;
using System.IO;
using System.Text;

namespace Scripting
{
    class CSScriptException : ApplicationException
    {
        public CSScriptException(string message = null) : base(message)
        {
        }
    }
}

namespace CSScriptLib
{
    /// <summary>
    /// CSScriptLib is compiled as nets standard so some .NETCore API is not available.
    /// So filling the gaps...
    /// </summary>
    public static partial class CoreExtensions
    {
        /// <summary>
        /// Escapes the CS-Script directive (e.g. //css_*) delimiters.
        /// <para>All //css_* directives should escape any internal CS-Script delimiters by doubling the delimiter character.
        /// For example //css_include for 'script(today).cs' should escape brackets as they are the directive delimiters.
        /// The correct syntax would be as follows '//css_include script((today)).cs;'</para>
        /// <remarks>The delimiters characters are ';,(){}'.
        /// <para>However you should check <see cref="CSharpParser.DirectiveDelimiters"/> for the accurate list of all delimiters.
        /// </para>
        /// </remarks>
        /// </summary>
        /// <param name="text">The text to be processed.</param>
        /// <returns>The escaped string.</returns>
        public static string EscapeDirectiveDelimiters(this string text)
        {
            foreach (char c in CSharpParser.DirectiveDelimiters)
                text = text.Replace(c.ToString(), new string(c, 2)); //very unoptimized but it is intended only for troubleshooting.
            return text;
        }

        internal static string GetScriptedCodeAttributeInjectionCode(string scriptFileName, string assemblyFileName = null)
        {
            // using SystemWideLock fileLock = new SystemWideLock(scriptFileName, "attr");

            //Infinite timeout is not good choice here as it may block forever but continuing while the file is still locked will
            //throw a nice informative exception.
            // if (Runtime.IsWin)
            //     fileLock.Wait(1000);

            string code = $"[assembly: System.Reflection.AssemblyDescriptionAttribute(@\"{scriptFileName}\")]";

            if (assemblyFileName != null)
                code += $"\n[assembly: System.Reflection.AssemblyConfigurationAttribute(@\"{assemblyFileName}\")]";


            if (scriptFileName.GetExtension().SameAs(".vb"))
            {
                code = $"<Assembly: System.Reflection.AssemblyDescriptionAttribute(\"{scriptFileName.Replace(@"\", @"\\")}\")>";
                if (assemblyFileName != null)
                    code += $"<Assembly: System.Reflection.AssemblyConfigurationAttribute(\"{assemblyFileName.Replace(@"\", @"\\")}\")>";
            }
            string currentCode = "";

            string file = Path.Combine(CSExecutor.GetCacheDirectory(scriptFileName), scriptFileName.GetFileNameWithoutExtension() + $".attr.g{scriptFileName.GetExtension()}");

            Exception lastError = null;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (File.Exists(file))
                        using (var sr = new StreamReader(file))
                            currentCode = sr.ReadToEnd();

                    if (currentCode != code)
                    {
                        string dir = Path.GetDirectoryName(file);

                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        using (var sw = new StreamWriter(file)) //there were reports about the files being locked. Possibly by csc.exe so allow retry

                            sw.Write(code);
                    }
                    break;
                }
                catch (Exception e)
                {
                    lastError = e;
                }
                // Thread.Sleep(200);
            }

            if (!File.Exists(file))
                throw new ApplicationException("Failed to create AttributeInjection file", lastError);

            return file;
        }

        internal static bool Contains(this string text, string value, StringComparison comparisonType)
            => text.IndexOf(value, comparisonType) != -1;

        internal static string Replace(this string text, string oldValue, string newValue, StringComparison comparisonType)
        {
            var result = new StringBuilder();

            var pos = 0;
            var prevPos = 0;

            while ((pos = text.IndexOf(oldValue, pos, comparisonType)) != -1)
            {
                result.Append(text.Substring(prevPos, pos - prevPos));
                result.Append(newValue);
                prevPos = pos;
                pos += oldValue.Length;
            }
            result.Append(text.Substring(prevPos, text.Length - prevPos));

            return result.ToString();
        }
    }
}