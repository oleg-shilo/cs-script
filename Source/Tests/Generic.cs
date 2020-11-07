extern alias css;

using csscript = css::csscript;
using CSSUtils = css::csscript.CSSUtils;
using CSharpParser = css::csscript.CSharpParser;
using System;
using System.Linq;
using Xunit;
using csscript;

public class GenericTest
{
    // [DebugBuildFact]
    [Fact(Skip = "dev only")]
    public void Test_PathSimpleExpression()
    {
        Assert.True(System.IO.Directory.Exists(@"C:\ProgramData\CS-Script\nuget\ServiceStack.Interfaces"));

        var dirs = CSSUtils.GetDirectories("", @"C:\ProgramData\CS-Script\nuget\ServiceStack.Interfaces\ServiceStack.Interfaces.*");
        Assert.Equal(3, dirs.Length);

        dirs = CSSUtils.GetDirectories("", @"C:\ProgramData\CS-Script\nuget\ServiceStack.Interfaces\ServiceStack.Interfaces.*\lib");
        Assert.Equal(1, dirs.Length);

        dirs = CSSUtils.GetDirectories("", @"C:\ProgramData\CS-Script\nuget\ServiceStack.Interfaces\ServiceStack.Interfaces.*\lib\*");
        Assert.Equal(1, dirs.Length);

        dirs = CSSUtils.GetDirectories("", @"C:\ProgramData\CS-Script\nuget\ServiceStack.Interfaces\ServiceStack.Interfaces.*\lib\**");
        Assert.Equal(1, dirs.Length);
    }

    [Fact]
    public void Test()
    {
        try
        {
            var code = @"//css_inc test (script, which has ';' chars).cs;
                         using System;
                         static void SayHello(string greeting)
                         {
                             Console.WriteLine(greeting);
                         }";

            var parser = new CSharpParser(code, false, null, null);
        }
        catch
        {
            // because of te multiple assemblies with the same type loaded
            // it's impossible to use Assert.Throw<SpecificAssemblyType> reliably
        }
    }

    // [Fact]
    // public void Test_ConsoleLines()
    // {
    //     var text = "but also injects break point enables at the start of the user code (useful for IDEs). " +
    //                "but also injects break point enables at the start of the user code (useful for IDEs). " +
    //                "but also injects break point enables at the start of the user code (useful for IDEs). " +
    //                "but also injects break point enables at the start of the user code (useful for IDEs). " +
    //                "but also injects break point enables at the start of the user code (useful for IDEs). ";

    //     var formatted = text.SplitIntoLines(120, 12);
    // }

    [Fact]
    public void Test_CLI_Help()
    {
        // var sw = new System.Diagnostics.Stopwatch();
        // sw.Restart();
        // HelpProvider.BuildCommandInterfaceHelp(null);
        // sw.Stop();
    }

    [Fact]
    public void Test_CSS_Init()
    {
        CSharpParser.InitInfo info;

        info = Parse("//css_init CoInitializeSecurity;");
        Assert.NotNull(info);
        Assert.Equal(3, info.RpcImpLevel);
        Assert.Equal(0x40, info.EoAuthnCap);

        info = Parse("//css_init CoInitializeSecurity();");
        Assert.NotNull(info);
        Assert.Equal(3, info.RpcImpLevel);
        Assert.Equal(0x40, info.EoAuthnCap);

        info = Parse("//css_init CoInitializeSecurity( 1 ,    0x15);");
        Assert.NotNull(info);
        Assert.Equal(1, info.RpcImpLevel);
        Assert.Equal(0x15, info.EoAuthnCap);

        info = Parse("//css_init CoInitializeSecurity(1,0x15);");
        Assert.NotNull(info);
        Assert.Equal(1, info.RpcImpLevel);
        Assert.Equal(0x15, info.EoAuthnCap);

        info = Parse("//css_inissst CoInitializeSecurity;");
        Assert.Null(info);

        Assert.Throws<ApplicationException>(() => Parse("//css_init CoInitializeSecurity 1, 2;"));
        Assert.Throws<ApplicationException>(() => Parse("//css_init CoInitializeSecurity( 1 );"));
        Assert.Throws<ApplicationException>(() => Parse("//css_init CoasdsaInitializeSecurity( 1 );"));
    }

    CSharpParser.InitInfo Parse(string code)
    {
        return new CSharpParser(code + "\n", false, null, null).Inits.FirstOrDefault();
    }
}