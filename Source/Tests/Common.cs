using CSScriptLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

public class TestBase
{
    static SimpleAsmProbing probing = null;

    public TestBase()
    {
        if (probing == null)
        {
            probing = SimpleAsmProbing.For(".", @"..\..\..\Roslyn.Scripting");
        }
        Environment.SetEnvironmentVariable("CSSCRIPT_DIR", Environment.CurrentDirectory);
    }
}

internal static class Extensions
{
    public static string WordAfter(this string text, int position)
    {
        return text.Substring(position).Split(" \t\n\r".ToCharArray()).FirstOrDefault();
    }

    public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }
}

public class DebugBuildFactAttribute : FactAttribute
{
    public DebugBuildFactAttribute()
    {
        // some intense scenarios do not play nice under xUnit runtime when "Run All Tests"
        // Though it runs OK when running individual test one by one.
        // This is particularly the case with Mono engine evaluator.
#if !DEBUG
        Skip = "Ignored in Release mode";
#endif
    }
}

class As
{
    public static object BlockingTest = typeof(string);
}

namespace Tests
{
    public interface ICalc
    {
        int Sum(int a, int b);
    }

    public class InputData
    {
        public int Index = 0;
    }

    internal static class StringExtensions
    {
        public static string GetDirectoryName(this string path)
        {
            return Path.GetDirectoryName(path);
        }
    }
}