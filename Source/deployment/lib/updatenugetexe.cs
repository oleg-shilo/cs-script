//css_args /ac;
using System.Diagnostics;
using System;

void main()
{
    run(Environment.ExpandEnvironmentVariables(@"%csscript_dir%\lib\nuget.exe"), "Update -self");
}

void run(string app, string args)
{
    var proc = new Process();
    proc.StartInfo.FileName = app;
    proc.StartInfo.Arguments = args;
    proc.StartInfo.UseShellExecute = false;
    proc.StartInfo.RedirectStandardOutput = true;
    proc.StartInfo.CreateNoWindow = true;
    proc.Start();

    string line = null;

    while (null != (line = proc.StandardOutput.ReadLine()))
        Console.WriteLine(line);

    proc.WaitForExit();
}