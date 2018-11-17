using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

//http://dotnet.github.io/port-to-core/Moq4_ApiPortabilityAnalysis.htm

/// <summary>
/// Common extensions class.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Safely retrieves parent directory of the assembly. Returns empty string if the assembly is
    /// a dynamic or in-memory assembly.
    /// </summary>
    /// <param name="asm">The asm.</param>
    /// <returns></returns>
    public static string Directory(this Assembly asm)
    {
        var file = asm.Location();
        if (file.HasText())
            return Path.GetDirectoryName(file);
        else
            return "";
    }

    internal static Type FirstUserTypeAssignableFrom<T>(this Assembly asm)
    {
        // exclude Roslyn internal types
        return asm
            .ExportedTypes
            .Where(t => t.FullName.None(char.IsDigit)           // 1 (yes Roslyn can generate class with this name)
                     && t.FullName.StartsWith("Submission#0+")  // Submission#0+Script
                     && !t.FullName.Contains("<<Initialize>>")) // Submission#0+<<Initialize>>d__0
            .FirstOrDefault(x => typeof(T).IsAssignableFrom(x));
    }

    /// <summary>
    /// Safely retrieves parent location of the assembly. Returns empty string if the assembly is
    /// a dynamic or in-memory assembly.
    /// </summary>
    /// <param name="asm">The asm.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Determines whether the assembly is dynamic.
    /// </summary>
    /// <param name="asm">The asm.</param>
    /// <returns>
    ///   <c>true</c> if the specified asm is dynamic; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsDynamic(this Assembly asm)
    {
        //http://bloggingabout.net/blogs/vagif/archive/2010/07/02/net-4-0-and-notsupportedexception-complaining-about-dynamic-assemblies.aspx
        //Will cover both System.Reflection.Emit.AssemblyBuilder and System.Reflection.Emit.InternalAssemblyBuilder
        return asm.GetType().FullName.EndsWith("AssemblyBuilder") || asm.Location == null || asm.Location == "";
    }

    /// <summary>
    /// returns an assembly of a specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    public static Assembly Assembly(this Type type)
    {
        return type.GetTypeInfo().Assembly;
    }

    /// <summary>
    /// Determines whether string has text.
    /// </summary>
    /// <param name="txt">The text.</param>
    /// <returns>
    ///   <c>true</c> if the specified text has text; otherwise, <c>false</c>.
    /// </returns>
    public static bool HasText(this string txt)
    {
        return !string.IsNullOrEmpty(txt);
    }

    /// <summary>
    /// Casts the specified object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj">The object.</param>
    /// <returns></returns>
    public static T Cast<T>(this object @obj)
    {
        return (T)@obj;
    }

    /// <summary>
    /// Determines whether the text is empty.
    /// </summary>
    /// <param name="txt">The text.</param>
    /// <returns>
    ///   <c>true</c> if the specified text is empty; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsEmpty(this string txt)
    {
        return string.IsNullOrEmpty(txt);
    }

    /// <summary>
    /// Determines whether the hosting OS is Linux.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if hosting OS is Linux; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLinux()
    {
        return (RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
    }

    /// <summary>
    /// Files the delete.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="rethrow">if set to <c>true</c> [rethrow].</param>
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

    /// <summary>
    /// Compare paths using case sensitivity based on the OS type.
    /// </summary>
    /// <param name="path1">The path1.</param>
    /// <param name="path2">The path2.</param>
    /// <returns></returns>
    public static int PathCompare(string path1, string path2)
    {
        if (IsLinux())
            return string.Compare(path1, path2);
        else
            return string.Compare(path1, path2, true);
    }

    /// <summary>
    /// Determines whether two paths are same. OS/FS neutral algorithm.
    /// </summary>
    /// <param name="path1">The path1.</param>
    /// <param name="path2">The path2.</param>
    /// <returns>
    ///   <c>true</c> if [is same path] [the specified path2]; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsSamePath(this string path1, string path2)
    {
        return PathCompare(path1, path2) == 0;
    }

    /// <summary>
    /// Gets the methods of the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    public static MethodInfo[] GetMethods(this Type type)
    {
        return type.GetTypeInfo().GetMethods();
    }

    /// <summary>
    /// Gets the name of the type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    public static string GetName(this Type type)
    {
        return type.GetTypeInfo().Name;
    }

    /// <summary>
    /// Creates instance of a class from underlying assembly.
    /// </summary>
    /// <param name="asm">The asm.</param>
    /// <param name="typeName">The 'Type' full name of the type to create. (see Assembly.CreateInstance()).
    /// You can use wild card meaning the first type found. However only full wild card "*" is supported.</param>
    /// <param name="args">The non default constructor arguments.</param>
    /// <returns>
    /// Instance of the 'Type'. Throws an ApplicationException if the instance cannot be created.
    /// </returns>
    public static object CreateObject(this Assembly asm, string typeName, params object[] args)
    {
        return CreateInstance(asm, typeName, args);
    }

    /// <summary>
    /// Checks if none of the specified satisfies the filter condition.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="filter">The filter.</param>
    /// <returns></returns>
    public static bool None<T>(this IEnumerable<T> collection, Predicate<T> filter)
    {
        return !collection.All(ch => !filter(ch));
    }

    /// <summary>
    /// Creates instance of a Type from underlying assembly.
    /// </summary>
    /// <param name="asm">The asm.</param>
    /// <param name="typeName">Name of the type to be instantiated. Allows wild card character (e.g. *.MyClass can be used to instantiate MyNamespace.MyClass).</param>
    /// <param name="args">The non default constructor arguments.</param>
    /// <returns>
    /// Created instance of the type.
    /// </returns>
    /// <exception cref="System.Exception">Type " + typeName + " cannot be found.</exception>
    private static object CreateInstance(Assembly asm, string typeName, params object[] args)
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
            var name = typeName.Replace("*.", "");

            Type[] types = asm.GetTypes()
                              .Where(t => t.FullName.None(char.IsDigit)
                                          && (t.FullName == name
                                               || t.FullName == ("Submission#0+" + name)
                                               || t.Name == name))
                                 .ToArray();

            if (types.Length == 0)
                throw new Exception("Type " + typeName + " cannot be found.");

            return Activator.CreateInstance(types.First(), args);
        }
    }
}

internal class NuGet
{
    static public string NuGetCacheView => "<not defined>";

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

internal class Utils
{
    public static string RemoveAssemblyExtension(string asmName)
    {
        if (asmName.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) || asmName.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
            return asmName.Substring(0, asmName.Length - 4);
        else
            return asmName;
    }
}

internal class CSSUtils
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