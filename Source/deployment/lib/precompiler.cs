using System;
using System.IO;
using System.Text;
using CSScriptLibrary;
using csscript;

public class CSScriptPrecompiler
{
    public static bool Precompile(ref string code)
    {

        return false;
    }


    static public int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
                throw new Exception("No script were passed for execution.");

            if (!File.Exists(args[0]))
                throw new Exception("The command line first argument is not a valid file location.");

            string rawScript = args[0];
            string scriptExtension = Path.GetExtension(rawScript).ToLower();
            string reconstructedScript = Path.Combine(CSSEnvironment.GetCacheDirectory(rawScript),
                                                      Path.GetFileName(rawScript) + ".reconstructed" + scriptExtension);

            using (StreamReader sr = new StreamReader(rawScript))
            {
                string firstLine = sr.ReadLine();
                if (firstLine.StartsWith("#!"))
                {
                    if (File.GetLastWriteTimeUtc(rawScript) != File.GetLastWriteTimeUtc(reconstructedScript))
                        using (StreamWriter sw = new StreamWriter(reconstructedScript, false, Encoding.Unicode))
                        {
                            if (scriptExtension == ".vb")
                                sw.Write("'");

                            sw.WriteLine("//css_searchdir " + Path.GetDirectoryName(rawScript) + ";");

                            string line;
                            while (null != (line = sr.ReadLine()))
                                sw.WriteLine(line);
                        }

                    File.SetLastWriteTimeUtc(reconstructedScript, File.GetLastWriteTimeUtc(rawScript));

                    args[0] = ToRelativePath(reconstructedScript, Environment.CurrentDirectory);
                }
            }

            AppDomain.CurrentDomain.ExecuteAssembly(scriptEngineFile, null, args);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return 1;
        }
        return 0;
    }

    static string ToRelativePath(string path, string baseDir)
    {
        if (baseDir[baseDir.Length - 1] == Path.DirectorySeparatorChar)
            baseDir = baseDir.Remove(baseDir.Length - 1);

        var baseDirs = baseDir.Split(Path.DirectorySeparatorChar);
        var pathDirs = path.Split(Path.DirectorySeparatorChar);
        int lastCommonToken = -1;
        for (int i = 0; i < baseDirs.Length; i++)
        {
            if (i >= pathDirs.Length || baseDirs[i] != pathDirs[i])
                break;

            lastCommonToken = i;
        }

        if (lastCommonToken == -1)
            return path; //different drive

        var commonBaseLength = 0;
        for (int i = 0; i <= lastCommonToken; i++)
        {
            commonBaseLength += pathDirs[i].Length + 1;
        }

        string retval = "";
        for (int i = 0; i < baseDirs.Length - lastCommonToken - 1; i++)
        {
            retval += ".." + Path.DirectorySeparatorChar;
        }
        return Path.Combine(retval, path.Substring(commonBaseLength));

    }

    static string scriptEngineFile
    {
        get
        {
            return Path.Combine(Environment.ExpandEnvironmentVariables("%CSSCRIPT_DIR%"), "cscs.exe");
        }
    }
}

