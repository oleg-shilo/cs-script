using System;
using System.Windows.Forms;

class Script
{
    const string usage = "Usage: cscscript RuntimeInfo ...\nPrints Runtime Environment information.\n";

    static public void Main(string[] args)
    {
        Console.WriteLine("OS CPU:      " + (Environment.Is64BitOperatingSystem ? "x64":"x86"));
        Console.WriteLine("Process CPU: " + (Environment.Is64BitProcess ? "x64":"x86"));
        Console.WriteLine("OS Version:  " + Environment.OSVersion);
        Console.WriteLine("Domain Name: " + Environment.UserDomainName);
        Console.WriteLine("User Name:   " + Environment.UserName);
    }
}
