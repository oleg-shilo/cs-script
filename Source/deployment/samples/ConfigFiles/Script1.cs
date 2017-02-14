using System;
using System.Configuration;


class Script
{
    static public void Main(string[] args)
    {
        Console.WriteLine(ConfigurationManager.AppSettings["greeting"]);
    }
}
