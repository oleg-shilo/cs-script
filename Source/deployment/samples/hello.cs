//css_args /c:0, /inmem
using System;
using System.Windows.Forms;

public class Script
{
    const string usage = "Usage: cscscript hello ...\nThe canonical \"Hello world!\" script.\n";

    static public void Main(string[] args)
    {
        //System.Diagnostics.Debug.Assert(false);

        if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
        {
            Console.WriteLine(usage);
        }
        else
        {
            Console.WriteLine("Hello World!");
            MessageBox.Show("Hello World!");
        }
    }
}