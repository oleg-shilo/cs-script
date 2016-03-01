using CSScript;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

class ChooseDefaultProgram
{
    const string Usage =
@"Sets Default Program for a given file extension under the current user account.

Usage: ChooseDefaultProgram -e:<extension> -prog:<program_id> -hash:<program_hash>
 extension    - file extension (e.g. '.cs')
 program_id   - program Id to associate the file extension with (e.g. 'CsScript')
 program_hash - program Id hash (e.g. 'pA2ZT35FDyE=')";

    static public int Main(string[] args)
    {
        try
        {
            string fileExtension = GetArg("-e:");
            string programId = GetArg("-prog:");
            string hash = GetArg("-hash:");

            if (fileExtension == "" || programId == "" || hash == "")
            {
                Console.WriteLine(Usage);
            }
            else
            {
                bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) ? true : false;

                if (!isAdmin)
                    throw new Exception("Error: You need to run this application as administrator.");

                ChooseDefaultProgram.Set(fileExtension, programId, hash);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
            return 1;
        }

        return 0;
    }

    static public void Set(string fileExtension, string progId, string hash)
    {
        string sid = WindowsIdentity.GetCurrent().User.ToString();

        //S-1-5-21-873285455-4058847463-2957848332-1168
        string workingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChooseDefaultProgram");

        if (!Directory.Exists(workingDir))
            Directory.CreateDirectory(workingDir);

        string psExec = Path.Combine(workingDir, "PsExec.exe");

        if (!File.Exists(psExec))
            File.WriteAllBytes(psExec, Resources.PsExec);

        string regEdit = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "regedit.exe");

        string regFile = Path.GetTempFileName();

        string regFileContent = RegFileTemplate.Replace("{SID}", sid)
                                               .Replace("{EXTENSION}", fileExtension)
                                               .Replace("{PROGID}", progId)
                                               .Replace("{HASH}", hash);

        File.WriteAllText(regFile, regFileContent);

        string regEditSwitch = "/s";

        try
        {
            string output = Run(psExec, string.Format("-i -h -s \"{0}\" {1} \"{2}\"", regEdit, regEditSwitch, regFile));

            //It is a good idea to analyse the output for errors here. However PsExec does not pass the StdOut properly
        }
        finally
        {
            try
            {
                if (File.Exists(regFile))
                    File.Delete(regFile);
            }
            catch { }
        }
    }

    static string GetArg(string suffix)
    {
        return Environment.GetCommandLineArgs()
                          .Where(item => item.StartsWith(suffix))
                          .Select(item => item.Substring(suffix.Length))
                          .FirstOrDefault();
    }

    static string Run(string application, string args)
    {
        var proc = new Process();
        proc.StartInfo.FileName = application;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.CreateNoWindow = true;
        proc.Start();

        var builder = new StringBuilder();

        string line = null;
        while (null != (line = proc.StandardOutput.ReadLine()))
        {
            builder.AppendLine(line);
        }
        proc.WaitForExit();

        return builder.ToString();
    }

    const string RegFileTemplate =
@"Windows Registry Editor Version 5.00

[HKEY_USERS\{SID}\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{EXTENSION}\UserChoice]
""Hash""=""{HASH}""
""ProgId""=""{PROGID}""";
}