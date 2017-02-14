//css_ref System.Core;
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Xml;

class Script
{
    const string usage = "Usage: cscs nocache [/ns:[namsspaceN1;]...[namsspaceN;]][statement] ...\nExecutes script without using script cache even if it is available.\n";

    static public void Main(string[] args)
    {
        if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
        {
            Console.WriteLine(usage);
        }
        else
        {
            string oldConfigFile = null;
            if (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") != null)
                oldConfigFile = Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\css_config.xml");

            if (oldConfigFile != null)
            {
                var newConfigFile = GenerateConfigFile(oldConfigFile);

                string cssExe = Assembly.GetEntryAssembly().Location;
                var newArgs = new List<string>();
                newArgs.Add("/nl"); //suppress printing logo
                newArgs.Add("/noconfig:" + newConfigFile); //suppress using default config file and use custom one instead
                newArgs.AddRange(args);
                RunApp(cssExe, ToArgsString(newArgs.ToArray()));
            }
        }
    }
    static string GenerateConfigFile(string templateConfigFile)
    {
        var newConfigFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "css_config.xml");
        if (File.GetLastWriteTimeUtc(newConfigFile) != File.GetLastWriteTimeUtc(templateConfigFile))
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(templateConfigFile);            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                if (node.Name == "defaultArguments")
                {
                    node.InnerText = node.InnerText.Replace("/c ", "")
                                                   .Replace(" /c", "")
                                                   .Trim();
                    break;
                }
            doc.Save(newConfigFile);
            File.SetLastWriteTimeUtc(newConfigFile, File.GetLastWriteTimeUtc(templateConfigFile));
        }
        return newConfigFile;
    }

    static string ToArgsString(string[] args)
    {
        var builder = new StringBuilder();
        foreach (string item in args)
        {
            builder.Append('"');
            builder.Append(item);
            builder.Append('"');
            builder.Append(' ');
        }
        return builder.ToString().TrimEnd();
    }

    static void RunApp(string app, string args)
    {
        //Console.WriteLine(args);
        
        Process p = new Process();
        p.StartInfo.FileName = app;
        p.StartInfo.Arguments = args;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.CreateNoWindow = true;
        p.Start();

        string line = null;
        while (null != (line = p.StandardOutput.ReadLine()))
        {
            Console.WriteLine(line);
        }

        p.WaitForExit();
    }
}
