using System;
using System.CodeDom.Compiler;
using System.Linq;
using CSScriptCompilers;

namespace CSSCodeProvider.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] files = new[] { @"E:\cs-script\Samples\hello.cpp" };

            var cParams = new CompilerParameters();
            cParams.OutputAssembly = @"C:\Users\osh\AppData\Local\Temp\CSSCRIPT\Cache\-727015236\hello.cpp.dll";
            cParams.ReferencedAssemblies.Add(@"C:\WINDOWS\Microsoft.Net\assembly\GAC_MSIL\System.Windows.Forms\v4.0_4.0.0.0__b77a5c561934e089\System.Windows.Forms.dll");
            cParams.ReferencedAssemblies.Add(@"C:\WINDOWS\Microsoft.Net\assembly\GAC_MSIL\System.Linq\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Linq.dll");
            cParams.ReferencedAssemblies.Add(@"C:\WINDOWS\Microsoft.Net\assembly\GAC_MSIL\System.Core\v4.0_4.0.0.0__b77a5c561934e089\System.Core.dll");

            var compiler = new CPPCompiler();
            compiler.CompileAssemblyFromFileBatch(cParams, files);


            var parser = new CCSharpParser(@"E:\cs-script\engine\CSSCodeProvider.v3.5\Script.cs");
            parser.ToTempFile(false);
        }
    }
}
