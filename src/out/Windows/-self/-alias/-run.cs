using static System.Reflection.Assembly;
using static System.IO.Path;
using static System.IO.File;
using static System.Environment;
using static System.Diagnostics.Process;

using System;
using System.IO;
using System.Diagnostics;

var engine_asm = GetEntryAssembly().Location;

// to ensure we are not picking cscs.dll
if (Environment.OSVersion.Platform != PlatformID.Win32NT)
{
    Console.WriteLine("Creating `css.exe` alias with this script is only useful on windows. On Linux you " +
                      "have a much better option `alias`. You can enable it as below: " + NewLine +
                      "alias css='dotnet /usr/local/bin/cs-script/cscs.dll'" + NewLine +
                      "After that you can invoke CS-Script engine from anywhere by just typing 'css'.");
    return;
}

var workingDir = Environment.ExpandEnvironmentVariables(Path.GetDirectoryName(engine_asm));
var alias = "css.exe";

if (File.Exists(Path.Combine(workingDir, alias)))
    File.Delete(Path.Combine(workingDir, alias));

var p = new Process();
p.StartInfo.FileName = "cmd";
p.StartInfo.Arguments = $"/C \"mklink /H {alias} cscs.exe\"";
p.StartInfo.WorkingDirectory = workingDir;
p.StartInfo.RedirectStandardOutput = true;
p.Start();
var result = p.StandardOutput.ReadToEnd();
p.WaitForExit();

if (File.Exists(Path.Combine(workingDir, alias)))
    Console.WriteLine($"Alias created for {alias} <<===>> cscs.exe");
else
    Console.WriteLine($"Failed to create {alias} alias.");