// single step conversion of *.resx into *.resources
//css_res Resources.resx, Resources.resources; 

using System;
using System.IO;
using System.Reflection;
using System.Resources;

class Script
{
    static public void Main(string[] args)
    {
        Console.WriteLine("Extracting data from resources");

        var res = new ResourceManager("Resources", Assembly.GetExecutingAssembly());

        Console.WriteLine("String1=\"{0}\"", res.GetObject("String1"));
    }
}