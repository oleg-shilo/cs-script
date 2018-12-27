//////////////////////////////////////////////////////////////////////////
//cmdShell.cs - script file from 'Creating shell extension' tutorial

using System;
using Microsoft.Win32;

class Script
{
    const string usage = "Usage: cscs cmdShell </i|/u> ...\nCreates shell extension 'Cmd'. This extension allows to open command-prompt pointing to the directory where 'right-clocked' file is.\n"+
                         "'/i' / '/u' - command switch to install/uninstall shell extension\n";

    static public void Main(string[] args)
    {
        if (args.Length == 1) 
        {
            if (args[0].ToLower() == "/u")
            {
                try
                {
                    Registry.ClassesRoot.DeleteSubKeyTree("*\\shell\\Cmd");
                    Console.WriteLine("Shell extension 'Cmd' has been removed.");
                }
                catch (Exception ex)
                {    
                    Console.WriteLine(ex);
                }
            }
            else if (args[0].ToLower() == "/i")
            {
                RegistryKey shell = Registry.ClassesRoot.CreateSubKey("*\\shell\\Cmd\\command");
                shell.SetValue("", "cmd.exe");
                shell.Close();
                Console.WriteLine("Shell extension 'Cmd' has been created.");
            }
            else
                Console.WriteLine(usage);
        }
        else
            Console.WriteLine(usage);
    }
}

