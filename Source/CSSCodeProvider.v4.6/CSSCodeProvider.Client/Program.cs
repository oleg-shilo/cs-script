using System;
using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        //Environment.SetEnvironmentVariable("CSSCRIPT_ROSLYN", @"E:\cs-script\lib\Bin\Roslyn");
        //Environment.SetEnvironmentVariable("CSSCRIPT_ROSLYN", @"C:\Program Files (x86)\Mono\lib\mono\4.5");

        // CSSCodeProvider.CompilerPath = @"C:\Program Files (x86)\Mono\lib\mono\4.5\csc.exe";
        // CSSCodeProvider.CompilerPath = @"E:\cs-script\lib\Bin\Roslyn\csc.exe";
        //CSSCodeProvider.CompilerPath = @"/home/user/Desktop/Untitled Folder/roslyn_original/csc.exe";
        CSSCodeProvider.CompilerServerTimeToLive = 600;

        Environment.SetEnvironmentVariable("CSS_PROVIDER_TRACE", "true");

        Load();
    }

    static void testVb()
    {
        try
        {
            var baseDir = @"C:\Program Files (x86)\Notepad++\plugins\CSScriptNpp\Roslyn\";
            CSSCodeProvider.CompilerPath = baseDir + "vbc.exe";
            CSSCodeProvider.CompilerServerTimeToLive = 600;
            CSSCodeProvider.CompilerServerTimeToLive = 6;

            ICodeCompiler compiler = CSSCodeProvider.CreateCompiler("code.vb");
            var compilerParams = new CompilerParameters();
            //var file = @"E:\cs-script\samples\Hello.vb";
            //compilerParams.ReferencedAssemblies.Add(@"System.Net.Http.Formatting.dll");
            //compilerParams.ReferencedAssemblies.Add(@"System.dll");
            //var result = compiler.CompileAssemblyFromFile(compilerParams, file);

            var result = compiler.CompileAssemblyFromSource(compilerParams, @"
Imports System
Imports System.Windows.Forms

Module Module1
    Sub Main()
        Console.WriteLine(""Hello World!(VB)"")
    End Sub
End Module");
            bool success = !result.Errors.HasErrors;
        }
        catch { }
        Console.WriteLine("done");
    }

    static void Load()
    {
        var start = Stopwatch.StartNew();
        try
        {
            ICodeCompiler compiler = CSSCodeProvider.CreateCompiler("");
            var compilerParams = new CompilerParameters();
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");

            var script = Path.GetFullPath("roslyn_test.cs");
            if (!File.Exists(script))
                File.WriteAllText(script, @"
using System;
class Program
{
    static void Main()
    {
        void print()
        {
            Console.WriteLine(""Hello C#7!"");
        }

        print();
    }
}".Trim());

            var result = compiler.CompileAssemblyFromFile(compilerParams, script);
            foreach (CompilerError err in result.Errors)
                Console.WriteLine(err.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        Console.WriteLine($"done ({start.Elapsed})");
    }
}