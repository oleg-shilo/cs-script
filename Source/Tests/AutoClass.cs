using System.Linq;
using System.Reflection;
using Xunit;

public class AutoClass : TestBase
{
    class AutoclassGenerator
    {
        static public string Process(string text, ref int position)
        {
            try
            {
                var type = typeof(csscript.CSharpParser).Assembly
                    .GetLoadableTypes()
                    .Where(t => t.Name == "AutoclassGenerator")
                    .FirstOrDefault();

                MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == "Process" && m.GetParameters().Length == 2);
                object[] args = new object[] { text, position };
                var result = (string)method.Invoke(null, args);
                position = (int)args[1];
                return result;
            }
            catch { }
            return null;
        }
    }

    static string code_template = @"
using System;

$main$
{
    int t = 1;
    Console.WriteLine(1);
    Console.WriteLine(2);
    Console.WriteLine(3);
    Console.WriteLine(""Hello..."");
}

//css_ac_end

static class Extensions
{
    static public void Convert(this string text)
    {
        Console.WriteLine(""converting"");
    }
}
";

    [Fact]
    public void DecorateAndTrackPosition_InExtensionClass ()
    {
        string raw_code = code_template.Replace("$main$", "void main()");

        int position = raw_code.IndexOf("converting");

        string word1 = raw_code.WordAfter(position);

        string generated_code = AutoclassGenerator.Process(raw_code, ref position);

        string word2 = generated_code.WordAfter(position);

        Assert.Equal(word1, word2);
    }

    [Fact]
    public void DecorateAndTrackPosition_LowerCaseMain()
    {
        string raw_code = code_template.Replace("$main$", "void main()");

        int position = raw_code.IndexOf("WriteLine(1)");

        string word1 = raw_code.WordAfter(position);

        string generated_code = AutoclassGenerator.Process(raw_code, ref position);

        string word2 = generated_code.WordAfter(position);

        Assert.Equal(word1, word2);
    }

    [Fact]
    public void DecorateAndTrackPosition_UpperCaseStaticMain()
    {
        string raw_code = code_template.Replace("$main$", "static void Main()");

        int position = raw_code.IndexOf("WriteLine(1)");


        string word1 = raw_code.WordAfter(position);

        string generated_code = AutoclassGenerator.Process(raw_code, ref position);

        string word2 = generated_code.WordAfter(position);

        Assert.Equal(word1, word2);
    }

    [Fact]
    public void DecorateAndTrackPosition_UpperCaseMain()
    {
        string raw_code = code_template.Replace("$main$", "void Main()");

        int position = raw_code.IndexOf("WriteLine(1)");


        string word1 = raw_code.WordAfter(position);

        string generated_code = AutoclassGenerator.Process(raw_code, ref position);

        string word2 = generated_code.WordAfter(position);

        Assert.Equal(word1, word2);
    }

    [Fact]
    public void DecorateFreestyleAutoclass()
    {
        string raw_code = @"
using System;

Console.WriteLine(1);
Console.WriteLine(2);
Console.WriteLine(3);
";

        string expected_generated_code = @"
using System;

void main()
public class ScriptClass { public static int Main(string[] args) { try { Console.OutputEncoding = System.Text.Encoding.GetEncoding(""utf-8""); } catch {} new ScriptClass().main(args); return 0; } void main() {///CS-Script auto-class generation
#line 4 """"
Console.WriteLine(1);
Console.WriteLine(2);
Console.WriteLine(3);
}} ///CS-Script auto-class generation
";

        int position = raw_code.IndexOf("WriteLine(1)");
        string word1 = raw_code.WordAfter(position);

        string generated_code = AutoclassGenerator.Process(raw_code, ref position);

        string word2 = generated_code.WordAfter(position);
        Assert.Equal(expected_generated_code, generated_code);
    }


    [Fact]
    public void DecorateGenericAutoclassIntVoid()
    {
        string raw_code = code_template.Replace("$main$", "int main()");
        int position = 0;

        string generated_code = AutoclassGenerator.Process(raw_code, ref position);

        Assert.True(generated_code.Contains("(int)new ScriptClass().main()"));
    }

    [Fact]
    public void DecorateGenericAutoclassIntArgs()
    {
        string raw_code = code_template.Replace("$main$", "int main(string[] args)");
        int position = 0;

        string generated_code = AutoclassGenerator.Process(raw_code, ref position);

        Assert.True(generated_code.Contains("(int)new ScriptClass().main(args)"));
    }

    [Fact]
    public void DecorateGenericAutoclass()
    {
        string raw_code = code_template.Replace("$main$", "void main()");

        string expected_generated_code = @"
using System;

public class ScriptClass { public static int Main(string[] args) { try { Console.OutputEncoding = System.Text.Encoding.GetEncoding(""utf-8""); } catch {} new ScriptClass().main(); return 0; } ///CS-Script auto-class generation
#line 4 """"
void main()
{
    int t = 1;
    Console.WriteLine(1);
    Console.WriteLine(2);
    Console.WriteLine(3);
    Console.WriteLine(""Hello..."");
}

} ///CS-Script auto-class generation
#line 13 """"
//css_ac_end

static class Extensions
{
    static public void Convert(this string text)
    {
        Console.WriteLine(""converting"");
    }
}
";

        int position = raw_code.IndexOf("WriteLine(1)");
        string word1 = raw_code.WordAfter(position);

        string generated_code = AutoclassGenerator.Process(raw_code, ref position);

        string word2 = generated_code.WordAfter(position);
        Assert.Equal(expected_generated_code, generated_code);
    }

}