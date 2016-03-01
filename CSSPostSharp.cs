//css_ref System.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

public class CSSPostProcessor
{
    /// <summary>
    /// Processes the specified script assembly before its execution.
    /// </summary>
    /// <param name="assemblyIn">The compiled script assembly to be processed .</param>
    /// <param name="refAssemblies">The assemblies referenced by the script.</param>
    /// <param name="probingDirs">The assembly probing directories.</param>
    public static void Process(string assemblyIn, string[] refAssemblies, string[] probingDirs)
    {
        var refPostSharpAsms = (from a in refAssemblies
                                where a.ToLower().EndsWith("postsharp.laos.dll")
                                select a).Count();

        if (refPostSharpAsms != 0) //Aspects need to be injected as this assembly references PostSharp.Laos
        {
            var tempDir = Path.Combine(Path.GetTempPath(), @"CSSCRIPT\PostSharp\" + Guid.NewGuid().ToString());
            var postSharpExe = Path.Combine(PostSharpDir, "PostSharp.exe");
            var rawAssembly = assemblyIn;
            var processedAssembly = Path.Combine(tempDir, Path.GetFileName(assemblyIn));

            var args = string.Format("\"{0}\\Default.psproj\" \"{1}\" \"/p:Output={2}\" /p:IntermediateDirectory=. /p:CleanIntermediate=true /p:ResolvedReferences= /P:SignAssembly=false /P:PrivateKeyLocation= /P:SearchPath=",
                              PostSharpDir, rawAssembly, processedAssembly);

            Directory.CreateDirectory(tempDir);

            try
            {
                string stdOut = Execute(postSharpExe, args);

                if (!File.Exists(processedAssembly))
                    throw new ApplicationException(stdOut);
                else
                    File.Copy(processedAssembly, rawAssembly, true);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    static string Execute(string app, string args)
    {
        var proc = new Process();
        proc.StartInfo.FileName = app;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.CreateNoWindow = true;
        proc.Start();

        var stdOut = new StringBuilder();
        string line = null;
        while (null != (line = proc.StandardOutput.ReadLine()))
        {
            stdOut.AppendLine(line);
        }
        proc.WaitForExit();

        return stdOut.ToString();
    }

    static string postSharpDir;
    static string PostSharpDir
    {
        get
        {
            if (postSharpDir == null)
            {
                postSharpDir = Environment.GetEnvironmentVariable("CSS_POSTSHARP");
                if (postSharpDir == null)
                {
                    var progFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

                    var dirs = (from dir in Directory.GetDirectories(progFilesDir, "PostSharp*")
                                where File.Exists(Path.Combine(dir, "PostSharp.exe"))
                                orderby dir
                                select dir);
                    if (dirs.Count() != 0)
                        return dirs.Last();
                    else
                        throw new ApplicationException("Cannot find PostSharp binaries.\nEither install PostSharp or set EnvVar CSS_POSTSHARP to the location of the PostSharp binaries");
                }

            }
            return postSharpDir;
        }
    }

}
