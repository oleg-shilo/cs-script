//css_inc GACHelper.cs;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Diagnostics;
using System.Reflection;
using System.Collections;

//Microsoft.CLRAdmin.Fusion

//http://www.dotnet247.com/247reference/a.aspx?u=http://blogs.msdn.com/junfeng/archive/2004/09/14/229653.aspx
//http://www.codeproject.com/KB/dotnet/undocumentedfusion.aspx
//http://www.codeguru.com/columns/kate/article.php/c12793/
//http://blogs.msdn.com/junfeng/articles/229649.aspx
//http://www.google.com.au/#hl=en&source=hp&q=Fusion+Wrapper+GAC&aq=f&aqi=&aql=&oq=&gs_rfai=&fp=b694f344a6ac6cb7
//http://dotnetkicks.com/linq/Linq_to_Gac_Use_Linq_to_Power_Query_your_Gac_via_C_to_Fusion

class Script
{
    [STAThread]
    static public void Main(string[] args)
    {
        Action<Action> profile = (action) =>
        {
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 100; i++)
            {
                action();
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        };

        profile(() => TestWithFusion("System.Core"));
        profile(() => TestWithExternalCompiler("System.Core.dll"));
        profile(() => TestWithInProcCompiler("System.Core.dll"));
        profile(() => TestWithGacUtil("System.Core"));

        Console.ReadKey();
    }

    static void TestWithFusion(string asm)
    {
        var e = new csscript.AssemblyEnum(asm);
        var name = e.GetNextAssembly();
        var file = csscript.AssemblyCache.QueryAssemblyInfo(name);
    }

    static void TestWithInProcCompiler(string asm)
    {
        IDictionary<string, string> providerOptions = new Dictionary<string, string>();
        providerOptions["CompilerVersion"] = "3.5";
        CompilerParameters compilerParams = new CompilerParameters();
        compilerParams.ReferencedAssemblies.Add(asm);
        ICodeCompiler compiler = CodeDomProvider.CreateProvider("C#").CreateCompiler();
        var result = compiler.CompileAssemblyFromSource(compilerParams, "");

        var erorr = result.Errors[0];
    }
    
    static bool TestWithExternalCompiler(string asm)
    {
        string compiler = Environment.ExpandEnvironmentVariables(@"%windir%\Microsoft.NET\Framework\v3.5\csc.exe");
        string args = @"/nologo  ""/out:asm.dll /t:library ""G:\cs-script\engine\Build\asm.cs"" /r:System.dll /r:"+asm;
        var p = new Process();
        p.StartInfo.FileName = compiler;
        p.StartInfo.Arguments = args;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        p.WaitForExit();
        int code = p.ExitCode;
        return code == 0;
    }

    static bool TestWithGacUtil(string asm)
    {
        string compiler = @"D:\Program Files\Microsoft SDKs\Windows\v6.0A\bin\gacutil.exe";
        string args = "/nologo /l "+asm;
        var p = new Process();
        p.StartInfo.FileName = compiler;
        p.StartInfo.Arguments = args;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        p.WaitForExit();
        int code = p.ExitCode;
        return code == 0;
    }

    static bool ProcessStart(string app, string args)
    {
        Process myProcess = new Process();
        myProcess.StartInfo.FileName = app;
        myProcess.StartInfo.Arguments = args;
        myProcess.StartInfo.UseShellExecute = false;
        myProcess.StartInfo.RedirectStandardOutput = true;
        myProcess.StartInfo.CreateNoWindow = true;
        myProcess.Start();

        string line = null;
        while (null != (line = myProcess.StandardOutput.ReadLine()))
        {
            Console.WriteLine(line);
        }
        myProcess.WaitForExit();
        int code = myProcess.ExitCode;
        return code == 0;
    }
}

public class Fusion
{
    public enum CacheType
    {
        Zap = 0x1,
        GAC = 0x2,
        Download = 0x4
    }

    static Type FusionType;

    static Fusion()
    {
        Assembly a = Assembly.Load("mscorcfg, "
          + "Version=1.0.5000.0, "
          + "Culture=neutral, "
          + "PublicKeyToken=b03f5f7f11d50a3a");
        FusionType = a.GetType("Microsoft.CLRAdmin.Fusion");
    }

    public static String GetCacheTypeString(UInt32 nFlag)
    {
        object[] args = new object[] { nFlag };
        BindingFlags bindingFlags = (BindingFlags)314;
        return ((String)
          (FusionType.InvokeMember("GetCacheTypeString",
            bindingFlags,
            null,
            null,
            args)));
    }

    public static void ReadCache(ArrayList alAssems, UInt32 nFlag)
    {
        object[] args = new object[] { alAssems, nFlag };
        BindingFlags bindingFlags = (BindingFlags)314;
        FusionType.InvokeMember("ReadCache",
          bindingFlags,
          null,
          null,
          args);
    }

    public static StringCollection GetKnownFusionApps()
    {
        object[] args = new object[0];
        BindingFlags bindingFlags = (BindingFlags)314;
        return ((StringCollection)
          (FusionType.InvokeMember("GetKnownFusionApps",
            bindingFlags,
            null,
            null,
            args)));
    }
}

