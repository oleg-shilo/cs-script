using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CSScriptLibrary;
using csscript;

public class Script
{
    static void Print(string algorithmName, Func<string> action)
    {
        try
        {
            Console.WriteLine(string.Format(" by {0}: {1}", algorithmName, action()));
        }
        catch
        {
            Console.WriteLine(string.Format(" by {0}: error", algorithmName));
        }
    }

    public static void Main()
    {
        var entryAsm = Assembly.GetEntryAssembly();

        var thisAsm = Assembly.GetExecutingAssembly();

        var hostAsm = Assembly.GetCallingAssembly();

        if (thisAsm == entryAsm)
            Console.WriteLine("It is not a script assembly but a fully compiled stand alone application.");

        if (hostAsm == null)
            Console.WriteLine("It is an assembly hosted by " + hostAsm.GetName().Name);

        Console.WriteLine("Script source file");
        Print("reflection #1    ", () => CSScript.GetScriptName(Assembly.GetExecutingAssembly()));
        Print("reflection #2    ", () => Assembly.GetExecutingAssembly().GetScriptName());
        Print("reflection #3    ", () => GetScriptName(thisAsm));
        Print("EnvVar           ", () => Environment.GetEnvironmentVariable("EntryScript"));

        Console.WriteLine("\nScript assembly file");
        Print("reflection", () => Assembly.GetExecutingAssembly().Location);
        Print("EnvVar #1 ", () => Environment.GetEnvironmentVariable("EntryScriptAssembly"));
        Print("EnvVar #2 ", () => Environment.GetEnvironmentVariable("location:" + Assembly.GetExecutingAssembly().GetHashCode()));
    }

    static public string GetScriptName(Assembly assembly)
    {
        //You may need to change C#6 statements you want to run the script against the engine not configured for C#6.
        return assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
                           .Cast<AssemblyDescriptionAttribute>()
                           .FirstOrDefault()?.Description;
    }
}