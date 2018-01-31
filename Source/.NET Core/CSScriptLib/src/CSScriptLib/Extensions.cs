using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Scripting;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

//http://dotnet.github.io/port-to-core/Moq4_ApiPortabilityAnalysis.htm

public static class Extensions
{
    public static string Directory(this Assembly asm)
    {
        var file = asm.Location();
        if (file.HasText())
            return Path.GetDirectoryName(file);
        else
            return "";
    }

    public static string Location(this Assembly asm)
    {
        if (asm.IsDynamic())
        {
            string location = Environment.GetEnvironmentVariable("location:" + asm.GetHashCode());
            if (location == null)
                return "";
            else
                return location ?? "";
        }
        else
            return asm.Location;
    }

    public static bool IsDynamic(this Assembly asm)
    {
        //http://bloggingabout.net/blogs/vagif/archive/2010/07/02/net-4-0-and-notsupportedexception-complaining-about-dynamic-assemblies.aspx
        //Will cover both System.Reflection.Emit.AssemblyBuilder and System.Reflection.Emit.InternalAssemblyBuilder
        return asm.GetType().FullName.EndsWith("AssemblyBuilder") || asm.Location == null || asm.Location == "";
    }

    public static Assembly Assembly(this Type t)
    {
        return t.GetTypeInfo().Assembly;
    }

    public static bool HasText(this string txt)
    {
        return !string.IsNullOrEmpty(txt);
    }

    public static bool IsEmpty(this string txt)
    {
        return string.IsNullOrEmpty(txt);
    }

    public static bool IsLinux()
    {
        return (RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
    }

    public static void FileDelete(this string filePath, bool rethrow)
    {
        //There are the reports about
        //anti viruses preventing file deletion
        //See 18 Feb message in this thread https://groups.google.com/forum/#!topic/cs-script/5Tn32RXBmRE

        for (int i = 0; i < 3; i++)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                break;
            }
            catch
            {
                if (rethrow && i == 2)
                    throw;
            }

            Thread.Sleep(300);
        }
    }

    public static int PathCompare(string path1, string path2)
    {
        if (IsLinux())
            return string.Compare(path1, path2);
        else
            return string.Compare(path1, path2, true);
    }

    public static bool IsSamePath(this string path1, string path2)
    {
        return PathCompare(path1, path2) == 0;
    }

    public static MethodInfo[] GetMethods(this Type t)
    {
        return t.GetTypeInfo().GetMethods();
    }

    public static string GetName(this Type t)
    {
        return t.GetTypeInfo().Name;
    }

    public static object CreateObject(this Assembly asm, string typeName, params object[] args)
    {
        return CreateInstance(asm, typeName, args);
    }

    /// <summary>
    /// Creates instance of a Type from underlying assembly.
    /// </summary>
    /// <param name="typeName">Name of the type to be instantiated. Allows wild card character (e.g. *.MyClass can be used to instantiate MyNamespace.MyClass).</param>
    /// <param name="args">The non default constructor arguments.</param>
    /// <returns>Created instance of the type.</returns>
    static object CreateInstance(Assembly asm, string typeName, params object[] args)
    {
        //note typeName for FindTypes does not include namespace
        if (typeName == "*")
        {
            //instantiate the first type found (but not auto-generated types)
            //Ignore Roslyn internal type: "Submission#N"; real script class will be Submission#0+Script
            foreach (Type type in asm.GetTypes())
            {
                bool isMonoInternalType = (type.FullName == "<InteractiveExpressionClass>");
                bool isRoslynInternalType = (type.FullName.StartsWith("Submission#") && !type.FullName.Contains("+"));

                if (!isMonoInternalType && !isRoslynInternalType)
                {
                    return Activator.CreateInstance(type, args);
                }
            }
            return null;
        }
        else
        {
            Type[] types = asm.GetTypes().Where(t => t.FullName == typeName || t.FullName == ("Submission#0+" + typeName)).ToArray();
            if (types.Length == 0)
                throw new Exception("Type " + typeName + " cannot be found.");
            return Activator.CreateInstance(types.First(), args);
        }
    }
}

public class AssemblyLoader : AssemblyLoadContext
{
    public static Assembly LoadFrom(string path)
    {
        return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    }

    public static Assembly LoadByName(string name)
    {
        return AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(name));
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
    }
}

class NuGet
{
    static public string NuGetCacheView
    {
        get { return "<not defined>"; }
        //get { return Directory.Exists(NuGetCache) ? NuGetCache : "<not found>"; }
    }

    //static string nuGetCache = null;
    //static string NuGetCache
    //{
    //    get
    //    {
    //        if (nuGetCache == null)
    //        {
    //            nuGetCache = Environment.GetEnvironmentVariable("css_nuget") ??
    //                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CS-Script" + Path.DirectorySeparatorChar + "nuget");

    //            if (!Directory.Exists(nuGetCache))
    //                Directory.CreateDirectory(nuGetCache);
    //        }
    //        return nuGetCache;
    //    }
    //}
}

class Utils
{
    public static string RemoveAssemblyExtension(string asmName)
    {
        if (asmName.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) || asmName.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
            return asmName.Substring(0, asmName.Length - 4);
        else
            return asmName;
    }
}

class CSSUtils
{
    static public string[] GetDirectories(string workingDir, string rootDir)
    {
        if (!Path.IsPathRooted(rootDir))
            rootDir = Path.Combine(workingDir, rootDir); //cannot use Path.GetFullPath as it crashes if '*' or '?' are present

        List<string> result = new List<string>();

        if (rootDir.Contains("*") || rootDir.Contains("?"))
        {
            bool useAllSubDirs = rootDir.EndsWith("**");

            string pattern = ConvertSimpleExpToRegExp(useAllSubDirs ? rootDir.Remove(rootDir.Length - 1) : rootDir);

            var wildcard = new Regex(pattern, RegexOptions.IgnoreCase);

            int pos = rootDir.IndexOfAny(new char[] { '*', '?' });

            string newRootDir = rootDir.Remove(pos);

            pos = newRootDir.LastIndexOf(Path.DirectorySeparatorChar);
            newRootDir = rootDir.Remove(pos);

            if (Directory.Exists(newRootDir))
            {
                foreach (string dir in Directory.GetDirectories(newRootDir, "*", SearchOption.AllDirectories))
                    if (wildcard.IsMatch(dir))
                    {
                        if (!result.Contains(dir))
                        {
                            result.Add(dir);

                            if (useAllSubDirs)
                                foreach (string subDir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
                                    //if (!result.Contains(subDir))
                                    result.Add(subDir);
                        }
                    }
            }
        }
        else
            result.Add(rootDir);

        return result.ToArray();
    }

    //Credit to MDbg team: https://github.com/SymbolSource/Microsoft.Samples.Debugging/blob/master/src/debugger/mdbg/mdbgCommands.cs
    public static string ConvertSimpleExpToRegExp(string simpleExp)
    {
        var sb = new StringBuilder();
        sb.Append("^");
        foreach (char c in simpleExp)
        {
            switch (c)
            {
                case '\\':
                case '{':
                case '|':
                case '+':
                case '[':
                case '(':
                case ')':
                case '^':
                case '$':
                case '.':
                case '#':
                case ' ':
                    sb.Append('\\').Append(c);
                    break;

                case '*':
                    sb.Append(".*");
                    break;

                case '?':
                    sb.Append(".");
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }

        sb.Append("$");
        return sb.ToString();
    }
}