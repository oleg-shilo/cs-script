//css_args -ac, -inmem 
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

static string rootUrl = "http://oleg-shilo.github.io/cs-script/linux/ubuntu/";
static string versionUrl = rootUrl + "/version.txt";
static string help = @"Install/Update CS-Script with the latest Ubuntu package available from product GitHub repository.
 -i (or no args) - installs the latest version of cs-script package
 -u              - uninstalls the cs-script package from the system
 -?              - show command's help
 -ver            - print latest available version
 -check          - check if any updates are available";

void main(string[] args)
{
    if (args.LastOrDefault() == "-?")
    {
        Console.WriteLine(help);
    }
    else if (args.LastOrDefault() == "-ver")
    {
        Console.WriteLine("Checking the latest available version...");
        Console.WriteLine("Latest available version: " + versionUrl.DownloadString());
    }
    else if (args.LastOrDefault() == "-check")
    {
        Console.WriteLine("Checking the latest available version...");
        Console.WriteLine("Latest available version: " + versionUrl.DownloadString());
        Console.WriteLine("You have version: " + Environment.GetEnvironmentVariable("CSScriptRuntime"));
    }
    else if (args.LastOrDefault() == "-u")
    {
        Console.WriteLine("Uninstalling package: 'sudo dpkg --purge cs-script'");
        Process.Start("dpkg", "--purge cs-script")
               .WaitForExit();
    }
    else if (args.FirstOrDefault() == "-i" || args.Count() == 0)
    {
        if (Environment.UserName != "root")
        {
            Console.WriteLine("error: requested operation requires superuser privilege");
        }
        else
        {
            Console.WriteLine("Checking the latest available version...");

            var version = versionUrl.DownloadString();

            if (version == Environment.GetEnvironmentVariable("CSScriptRuntime"))
            {
                Console.WriteLine("You already have the latest version. Do you want to reinstall it?\n   [Y]es/[N]o");

                var response = Console.ReadLine();

                if (response != "Y" && response != "y")
                    return;
            }

            var packageUrl = rootUrl + "/cs-script_" + version + "_all.deb";
            Console.WriteLine("Downloading " + packageUrl + " ...");

            // install: sodo dpkg -i cs-script_3.27.2.1.deb
            // uninstall: sudo dpkg --purge cs-script

            string package = packageUrl.DownloadFile();
            var cmd = "-i " + Path.GetFileName(package);

            Console.WriteLine("Installing package: 'dpkg" + cmd + "'");
            Process.Start("dpkg", cmd)
                   .WaitForExit();
        }
    }
    else
        Console.WriteLine("Unknown command");
}

//css_ac_end
static class Web
{
    public static string DownloadString(this string url)
    {
        var temp = Path.GetTempFileName();

        try
        {
            new WebClient().DownloadFile(url, temp);
            return File.ReadAllText(temp);
        }
        finally { File.Delete(temp); }
    }

    public static string DownloadFile(this string url, string outFile = null)
    {
        outFile = outFile ?? (Path.GetFileName(url));
        new WebClient().DownloadFile(url, outFile);
        return Path.GetFullPath(outFile);
    }
}