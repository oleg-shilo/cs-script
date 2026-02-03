using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Reflection.BindingFlags;
using System.Threading;
using Mono.Reflection;

public static class UnloadingTest
{
}

class Profiler
{
    static public Stopwatch Stopwatch = new Stopwatch();
}

public static class CSScriptPolyfills
{
    internal static string[] GetRawStatements(this CSScriptLib.CSharpParser parser, string codeToAnalyze, string pattern, int endIndex, bool ignoreComments)
    {
        MethodInfo method = typeof(CSScriptLib.CSharpParser).GetMethod(
            "GetRawStatements",
            NonPublic | Instance,
            null,
            new Type[] { typeof(string), typeof(string), typeof(int), typeof(bool) },
            null
);
        if (method != null)
            return (string[])method.Invoke(parser, new object[] { codeToAnalyze, pattern, endIndex, ignoreComments });
        throw new MissingMethodException("GetRawStatements method is not found in CSharpParser class.");
    }

    internal static void FileDelete(this string filePath, bool rethrow)
    {
        if (filePath?.Any() == true)
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
    }
}

public static class StaticAnalyzer
{
    public static string FullName(this MethodInfo method)
        => $"{method.DeclaringType.FullName}.{method.Name}";

    public static void VisitMethodCalls(this Assembly asm, Func<MethodInfo, MethodInfo, bool> visit)
    {
        var methods = asm.GetTypes()
                         .SelectMany(x => x.GetMethods(Public | NonPublic | Instance | Static)
                                           .Where(y => y.DeclaringType.Assembly == asm));

        foreach (MethodInfo caller in methods)
            foreach (Instruction instruction in caller.GetInstructions())
            {
                var calledMethod = instruction.Operand as MethodInfo;
                if (calledMethod != null)
                {
                    if (!visit(caller, calledMethod))
                        break;
                }
            }
    }

    public static IEnumerable<MethodInfo> GetUnReferencedMethods(this Assembly asm)
    {
        var methods = asm.GetTypes()
                         .Where(x => !x.Name.Contains("_AnonymousType") &&
                                     !x.Name.Contains("d_") &&
                                     !x.Name.Contains("b_") &&
                                     !x.Name.Contains("_DisplayClass"))
                         .Where(x => !x.IsPublic)
                         .SelectMany(x => x.GetMethods(Public | NonPublic | Instance | Static)
                                           .Where(y => y.DeclaringType.Assembly == asm))
                         .Where(x => !x.Name.Contains("b_") &&
                                     !x.Name.Contains("g_"))
                         // .Select(x => new { Name = x.FullName, Method = x })
                         .ToArray();
        var notCalled = methods.ToList();

        foreach (MethodInfo caller in methods)
        {
            if (notCalled.Any() == false)
                break;
            try
            {
                foreach (Instruction instruction in caller.GetInstructions())
                {
                    var calledMethod = instruction.Operand as MethodInfo;
                    if (calledMethod != null)
                    {
                        if (notCalled.Contains(calledMethod))
                            notCalled.Remove(calledMethod);
                    }
                }
            }
            catch { }
        }

        return notCalled;
    }
}