using System;
using System.Reflection;
using System.Linq;
using System.IO;

class Script
{
    [STAThread]
    static public void Main(string[] args)
    {
        string thisScriptFile = GetScriptName(Assembly.GetExecutingAssembly()); 
        string configConsole = @"ConfigConsole\ConfigConsole.exe";

        if(thisScriptFile != null)
            configConsole = Path.Combine(Path.GetDirectoryName(thisScriptFile), configConsole);
        
	    AppDomain.CurrentDomain.ExecuteAssembly(Path.GetFullPath(configConsole), args);
    }
	
    static string GetScriptName(Assembly assembly)
    {
        var attr =  (from item in assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true)
                     select (AssemblyDescriptionAttribute)item)
                    .FirstOrDefault();

        return attr == null ? "" : attr.Description;
    }
}

