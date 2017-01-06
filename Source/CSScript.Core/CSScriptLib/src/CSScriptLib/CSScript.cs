using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Scripting;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Text;

namespace CSScriptLibrary
{
    public class Settings
    {
        public string SearchDirs { get; set; } = "";
        //public List<string> SearchDirs { get; set; } = new List<string>();
    }

    public partial class CSScript
    {
        static public  Settings GlobalSettings = new Settings();

        /// <summary>
        /// Global instance of <see cref="CSScriptLibrary.RoslynEvaluator"/>. This object is to be used for
        /// dynamic loading of the  C# code by using Roslyn "compiler as service".
        /// <para>If you need to use multiple instances of th evaluator then you will need to call 
        /// <see cref="CSScriptLibrary.IEvaluator"/>.Clone().
        /// </para>
        /// </summary>
        /// <value> The <see cref="CSScriptLibrary.RoslynEvaluator"/> instance.</value>
        static public RoslynEvaluator RoslynEvaluator
        {
            get
            {
                if (EvaluatorConfig.Access == EvaluatorAccess.AlwaysCreate)
                    return (RoslynEvaluator) roslynEvaluator.Value.Clone();
                else
                    return roslynEvaluator.Value;
            }
        }
        static Lazy<RoslynEvaluator> roslynEvaluator = new Lazy<RoslynEvaluator>();

        static internal string WrapMethodToAutoClass(string methodCode, bool injectStatic, bool injectNamespace, string inheritFrom = null)
        {
            var code = new StringBuilder(4096);
            code.Append("//Auto-generated file\r\n"); //cannot use AppendLine as it is not available in StringBuilder v1.1
            code.Append("using System;\r\n");

            bool headerProcessed = false;

            string line;

            using (StringReader sr = new StringReader(methodCode))
                while ((line = sr.ReadLine()) != null)
                {
                    if (!headerProcessed && !line.TrimStart().StartsWith("using ")) //not using...; statement of the file header
                    {
                        string trimmed = line.Trim();
                        if (!trimmed.StartsWith("//") && trimmed != "") //not comments or empty line
                        {
                            headerProcessed = true;

                            if (injectNamespace)
                            {
                                code.Append("namespace Scripting\r\n");
                                code.Append("{\r\n");
                            }

                            if (inheritFrom != null)
                                code.Append("   public class DynamicClass : " + inheritFrom + "\r\n");
                            else
                                code.Append("   public class DynamicClass\r\n");

                            code.Append("   {\r\n");
                            string[] tokens = line.Split("\t ".ToCharArray(), 3, StringSplitOptions.RemoveEmptyEntries);

                            if (injectStatic)
                            {
                                if (tokens[0] != "static" && tokens[1] != "static" && tokens[2] != "static") //unsafe public static
                                    code.Append("   static\r\n");
                            }

                            if (tokens[0] != "public" && tokens[1] != "public" && tokens[2] != "public")
                                code.Append("   public\r\n");
                        }
                    }

                    code.Append(line);
                    code.Append("\r\n");
                }

            code.Append("   }\r\n");
            if (injectNamespace)
                code.Append("}\r\n");

            return code.ToString();
        }
    }

}