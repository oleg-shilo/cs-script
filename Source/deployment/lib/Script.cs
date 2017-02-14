//css_args -c:0
//css_pre wsdl_t()
using System;
using System.Windows.Forms;

class Script
{
    [STAThread]
    static public void Main(string[] args)
    {
        string primaryScriptFile = Environment.GetEnvironmentVariable("EntryScript");
        Console.WriteLine("primaryScriptFile: " + primaryScriptFile);
        MessageBox.Show("Just a test!");

        for (int i = 0; i < args.Length; i++)
        {
            Console.WriteLine(args[i]);
        }
    }
}