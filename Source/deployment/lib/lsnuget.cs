//css_args /nl, /ac 
//css_nuget -ng:-Prerelease cs-script
using System;
using System.Linq;
using System.IO;

void main()
{
    string rootDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"cs-script\nuget");
    Console.WriteLine("NuGet dir: "+rootDir);
    foreach (var dir in Directory.GetDirectories(rootDir))
        Console.WriteLine(" " + Path.GetFileName(dir));
}