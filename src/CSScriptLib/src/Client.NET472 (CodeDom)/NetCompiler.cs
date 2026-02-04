using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using CSScripting;

namespace CSScripting
{
    class NetCompiler
    {
        public static string DownloadCompiler()
        {
            var packageFile = Path.GetFullPath("roslyn.zip");
            var contentDir = Path.GetFullPath("compilers");
            var packageUrl = "https://www.nuget.org/api/v2/package/Microsoft.Net.Compilers/3.8.0";
            var compilerFile = Path.Combine(contentDir, "tools", "csc.exe");

            if (File.Exists(compilerFile))
                return compilerFile;

            try
            {
                Console.WriteLine("Downloading latest C# compiler from:");
                Console.WriteLine("  " + packageUrl);
                Console.WriteLine("\nNote, without latest C# compiler you can only execute scripts written in C#5 syntax.\n");

                new WebClient()
                    .DownloadFile(packageUrl, packageFile);

                ZipFile.ExtractToDirectory(packageFile, contentDir);

                return compilerFile;
            }
            catch
            {
                Console.WriteLine($"Cannot download '{packageUrl}' ...");
                return null;
            }
        }

        public static void EnableLatestSyntax()
        {
            var vs_professional_csc = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\Roslyn\csc.exe";
            var vs_community_csc = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe";

            if (File.Exists(vs_professional_csc))
                Globals.csc = vs_professional_csc;
            else if (File.Exists(vs_community_csc))
                Globals.csc = vs_community_csc;
            else
            {
                var latest_csc = DownloadCompiler();

                if (latest_csc.HasText())
                    Globals.csc = latest_csc;
                else
                    Console.WriteLine("Using default C#5 compiler");
            }
        }
    }
}