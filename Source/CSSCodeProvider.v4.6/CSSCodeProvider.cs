using System;
using System.CodeDom.Compiler;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;

public class CSSCodeProvider
{
    static List<object> AsmList = new List<object>();

    static CSSCodeProvider()
    {
        //Debug.Assert(false);
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        if (ProviderPath != null && File.Exists(ProviderPath))
        {
            string name = Path.GetFileNameWithoutExtension(ProviderPath);
            if (args.Name.StartsWith(name))
                return Assembly.LoadFrom(ProviderPath);
        }
        return null;
    }

    //Roslyn Microsoft.CodeDom.Providers.DotNetCompilerPlatform.dll v1.0.20615.0 has hard-codded
    //(with respect AppDomain.ApplicationBase) location of the csc.exe.
    //To solve this problem need to use reflection to set a real location of the compiler
    //The same needs to be done for server "keepalive" (https://roslyn.codeplex.com/wikipage?title=Building,%20Testing%20and%20Debugging)

    static public string CompilerPath = null;
    static public string ProviderPath = null;
    static public int? CompilerServerTimeToLive = null;

    static string ExistingFile(string dir, params string[] paths)
    {
        var file = Path.Combine(new[] { dir }.Concat(paths).ToArray());
        if (File.Exists(file))
            return file;
        return
            null;
    }

    static string GetDefaultAssemblyPath(string file)
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        return ExistingFile(dir, file) ??
               ExistingFile(Environment.GetEnvironmentVariable("CSSCRIPT_ROSLYN") ??dir, file) ??
               ExistingFile(dir, "bin", file) ??
               ExistingFile(dir, "roslyn", file) ??
               ExistingFile(dir, "bin", "roslyn", file);
    }

    static bool inited = false;
    static void Init()
    {
        if (!inited)
        {
            inited = true;
            CompilerPath = CompilerPath ??
                           Environment.GetEnvironmentVariable("RoslynLocation") ??
                           GetDefaultAssemblyPath("csc.exe");

            ProviderPath = ProviderPath ??
                           Environment.GetEnvironmentVariable("CodeDomProviderLocation") ??
                           GetDefaultAssemblyPath("Microsoft.CodeDom.Providers.DotNetCompilerPlatform.dll");

            CompilerServerTimeToLive = DefaultCompilerServerTimeToLive();
        }
    }

    static int? DefaultCompilerServerTimeToLive()
    {
        int? result = null;
        var file = CompilerPath.PathChangeFileName("VBCSCompiler.exe.config");
        if (File.Exists(file))
        {
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = file }, ConfigurationUserLevel.None);
            int value = 0;
            if (int.TryParse(config.AppSettings.Settings["keepalive"]?.Value, out value))
                return value;
        }
        return result;
    }

    public static ICodeCompiler CreateCompiler(string sourceFile)
    {
        //MessageBox.Show("retgertrew");
        //System.Diagnostics.Debug.Assert(false);
        Init();
        return CreateCompilerImpl(sourceFile);
    }

    static ICodeCompiler CreateCompilerImpl(string sourceFile)
    {
        string compilerDefaultSyntax = Environment.GetEnvironmentVariable("CSS_CompilerDefaultSyntax") ?? ".cs";

        bool isVB = false;

        if (!string.IsNullOrEmpty(sourceFile) && !sourceFile.EndsWith(".tmp"))
            isVB = sourceFile.EndsWith(".vb", StringComparison.OrdinalIgnoreCase);
        else
            isVB = compilerDefaultSyntax.IsOneOf_IgnoreCase(".vb", "VB", "VB.NET");

        if (isVB)
            return new VBCodeProvider().SetCompilerSettings(CompilerPath, CompilerServerTimeToLive)
                                       .CreateCompiler();
        else
            return new CSharpCodeProvider().SetCompilerSettings(CompilerPath, CompilerServerTimeToLive)
                                           .CreateCompiler();
    }
}

static class RoslynExtensions
{
    internal static string PathChangeFileName(this string filePath, string fileName)
    {
        if (filePath != null)
            return Path.Combine(Path.GetDirectoryName(filePath), fileName);
        else
            return filePath;
    }

    internal static bool IsOneOf_IgnoreCase(this string text, params string[] patterns)
    {
        foreach (var item in patterns)
            if (string.Compare(text, item, true) == 0)
                return true;
        return false;
    }

    internal static VBCodeProvider SetCompilerSettings(this VBCodeProvider provider, string compilerFullPath, int? compilerServerTimeToLive = null)
    {
        return (VBCodeProvider) SetCompilerSettingsImpl(provider, compilerFullPath, compilerServerTimeToLive);
    }

    internal static CSharpCodeProvider SetCompilerSettings(this CSharpCodeProvider provider, string compilerFullPath, int? compilerServerTimeToLive = null)
    {
        return (CSharpCodeProvider) SetCompilerSettingsImpl(provider, compilerFullPath, compilerServerTimeToLive);
    }

    static object SetCompilerSettingsImpl(object provider, string compilerFullPath, int? compilerServerTimeToLive = null)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var settings = provider.GetType().GetField("_compilerSettings", flags).GetValue(provider);

        settings.GetType().GetField("_compilerFullPath", flags).SetValue(settings, compilerFullPath);

        if (compilerServerTimeToLive.HasValue)
            settings.GetType().GetField("_compilerServerTimeToLive", flags).SetValue(settings, compilerServerTimeToLive);

        return provider;
    }
}