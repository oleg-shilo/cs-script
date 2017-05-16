using CSScriptLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace CSScriptLib.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Test.CompileCode();
            Test.CompileMethod();
            Test.CompileCSharp_7();
            Test.CompileDelegate();
            Test.CompileDelegate1();
            Test.LoadCode();
            Test.LoadCode2();
        }
    }
}