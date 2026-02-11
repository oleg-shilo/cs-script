using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace CSScripting
{
    class NetCompiler
    {
        public static string DownloadRecentCompiler()
        {
            var packageFile = Path.GetFullPath("roslyn.zip");
            var contentDir = Path.GetFullPath("compilers");
            var packageUrl = "https://www.nuget.org/api/v2/package/Microsoft.Net.Compilers.Toolset/5.0.0";
            var compilerFile = Path.Combine(contentDir, "tools", "csc.exe");

            if (File.Exists(compilerFile))
                return compilerFile;

            try
            {
                Console.WriteLine("Downloading C# recent compiler from:");
                Console.WriteLine("  " + packageUrl);
                Console.WriteLine();
                Console.WriteLine("Note, without recent C# compiler you can only execute scripts written in C# 5 syntax.");
                Console.WriteLine("With recent C# compiler on .NET Framework, you can execute scripts written in C# 7.3 syntax.");
                Console.WriteLine();

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

        public static void EnableCSharp73Syntax()
        {
            var vs_enterprise_csc = @"C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe";
            var vs_professional_csc = @"C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\Roslyn\csc.exe";
            var vs_community_csc = @"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe";

            if (File.Exists(vs_enterprise_csc))
                Globals.csc = vs_enterprise_csc;
            else if (File.Exists(vs_professional_csc))
                Globals.csc = vs_professional_csc;
            else if (File.Exists(vs_community_csc))
                Globals.csc = vs_community_csc;
            else
            {
                var latest_csc = DownloadRecentCompiler();

                if (latest_csc.HasText())
                    Globals.csc = latest_csc;
                else
                    Console.WriteLine("Using default C# 5 compiler");
            }
        }
    }
}