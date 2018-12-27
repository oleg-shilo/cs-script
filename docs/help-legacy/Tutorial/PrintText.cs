//////////////////////////////////////////////////////////////////////////
//PrintText.cs - script file from 'Script importing' tutorial
using System;
using System.Text;
using Scripting;

//css_import print;

class Script
{
    static string usage = "Usage: csc printtext <text> ...\nThis script will print (with print preview) specified text on the system default printer.\n";

    static public void Main(string[] args)
    {
        if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
        {
            Console.WriteLine(usage);
        }
        else
        {
            SimplePrinting printer = new SimplePrinting();
            printer.Print(args[0], true);
        }
    }
}

