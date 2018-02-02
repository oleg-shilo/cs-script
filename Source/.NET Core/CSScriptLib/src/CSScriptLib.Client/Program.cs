using CSScriptLib;
using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System.Runtime.Loader;
using System.Net;
using Newtonsoft.Json.Linq;

namespace CSScriptLib.Client
{
    public class Program
    {
        static void LoadNewtonsoftJson()
        {
            // var nj = typeof(JArray);
            AssemblyLoader.LoadByName("Newtonsoft.Json");
        }

        public static void Main(string[] args)
        {
            // CSScript.EvaluatorConfig.DebugBuild = true;

            var befor = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            LoadNewtonsoftJson();
            var after = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

            var ttt = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            var asms = Assembly.GetExecutingAssembly().GetTypes().Select(t => t.Assembly).Distinct().ToArray();

            Test.ReferencingPackagesCode(); //return;
            Test.CompileCode();
            Test.CompileMethod();
            Test.CompileCSharp_7();
            Test.CompileDelegate();
            Test.CompileDelegate1();
            Test.LoadCode();
            Test.LoadCode2();
            Test.CrossReferenceCode();
            asms = Assembly.GetExecutingAssembly().GetTypes().Select(t => t.Assembly).Distinct().ToArray();
        }
    }
}