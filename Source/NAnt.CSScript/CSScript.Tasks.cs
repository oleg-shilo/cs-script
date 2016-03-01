using System;
using System.IO;
using CSScriptLibrary;
using NAnt.Core;
using NAnt.Core.Attributes;
using System.Collections.Generic;
using System.Diagnostics;

public class NAntRuntime
{
    static public NAnt.Core.Project Project { get; internal set; }
    static public CSScriptTask Task { get; internal set; }
}

public static class CSScriptTaskExtensions
{
    public static string Expand(this string obj)
    {
        return NAntRuntime.Task.Expand(obj);
    }
}

[TaskName("CSScript")]
public class CSScriptTask : Task
{
    [TaskAttribute("file", Required = false)]
    [StringValidator(AllowEmpty = true)]
    public string ScriptFile { get; set; }

    //[TaskAttribute("assemblies", Required = false)]
    //[StringValidator(AllowEmpty = true)]
    //public string Assemblies { get; set; }

    [TaskAttribute("method", Required = false)]
    [StringValidator(AllowEmpty = true)]
    public bool Method { get; set; }

    [TaskAttribute("verbose", Required = false)]
    [StringValidator(AllowEmpty = true)]
    public new bool Verbose { get; set; }

    [TaskAttribute("entryPoint", Required = false)]
    [StringValidator(AllowEmpty = true)]
    public string EntryPoint { get; set; }

    string ScriptCode;

    protected override void Initialize()
    {
        ScriptCode = this.Project.ExpandProperties(this.XmlNode.InnerText, this.Location);
    }

    public string Expand(string data)
    {
        return this.Project.ExpandProperties(data, this.Location);
    }

    protected override void ExecuteTask()
    {
        try
        {
            NAntRuntime.Project = Project;
            NAntRuntime.Task = this;

            Debug.Assert(false);

            if (!string.IsNullOrEmpty(ScriptCode))
            {
                string code = ScriptCode.Expand();

                //List<string> refAssemblies = new List<string>();

                //if (!string.IsNullOrEmpty(Assemblies))
                //{
                //    foreach (string name in Assemblies.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                //        foreach (string file in CSScriptLibrary.AssemblyResolver.FindAssembly(name, new string[0]))
                //            if (!refAssemblies.Contains(file))
                //                refAssemblies.Add(file);
                //}

                MethodDelegate method;
                if (Method)
                {
                    method = CSScript.LoadMethod(code, null, true).GetStaticMethod();
                }
                else
                {
                    if (!string.IsNullOrEmpty(EntryPoint))
                        method = CSScript.LoadCode(code, null, true).GetStaticMethod("*." + EntryPoint);
                    else
                        method = CSScript.LoadCode(code, null, true).GetStaticMethod("*.Main");
                }
                method();
            }
            else if (!string.IsNullOrEmpty(ScriptFile))
            {
                string file = ScriptFile.Expand();

                if (File.Exists(file) == false)
                    throw new BuildException("The script file does not exist");

                CSScript.Execute(x => Console.WriteLine(x),
                                 new[] { "/nl", file });
            }
            else
                throw new BuildException("You have to specify either script file or script code.");
        }
        catch (Exception e)
        {
            if (Verbose)
                Log(Level.Error, e.ToString());
            else
                Log(Level.Error, e.Message);
        }
    }
}

