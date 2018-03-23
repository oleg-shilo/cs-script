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
using System.Runtime.Serialization.Formatters.Binary;

namespace CSScriptLib.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CSScript.EvaluatorConfig.DebugBuild = true;

            Test.ReferencingPackagesCode(); //return;
            Test.CompileCode();
            Test.CompileMethod();
            Test.CompileCSharp_7();
            Test.CompileDelegate();
            Test.CompileDelegate1();
            Test.LoadCode();
            Test.LoadCode2();
            Test.CrossReferenceCode();
        }
    }
}