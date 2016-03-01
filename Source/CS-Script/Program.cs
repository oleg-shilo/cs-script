using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace CS_Script
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                MessageBox.Show("This application is not intended to be run on its own.\nIt is meant to be configured as a default 'Open With...' application for the *.cs files.", "CS-Script");
            }
            else
                try
                {
                    var cmdTemplate = (string)Registry.GetValue(@"HKEY_CLASSES_ROOT\CsScript\Shell\Open\command", "App", "");

                    char separator = ' ';

                    if (cmdTemplate.StartsWith("\""))
                        separator = '\"';

                    string[] parts = cmdTemplate.Split(new[] { separator }, 2, StringSplitOptions.RemoveEmptyEntries);

                    string handlerApp = parts.First();
                    string handlerArgs = parts.Last();

                    for (int i = 0; i < args.Length; i++)
                        handlerArgs = handlerArgs.Replace("%" + (i + 1), args[i]);

                    Process.Start(handlerApp, handlerArgs);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
        }
    }
}