using System;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;
using System.IO;
using CSScriptLibrary;
using csscript;

namespace Scripting
{
    class UpdateScript
    {
        static public void Main(string[] args)
        {
            string primaryScriptFile = Environment.GetEnvironmentVariable("EntryScript");
            Console.WriteLine("primaryScriptFile: " + primaryScriptFile);
        }
    }

}