using System;
using System.Windows.Forms;

public class Script
{
    public static void Execute(Host host)
    {
        Console.WriteLine("Script: working with " + host.Name);
        host.CreateDocument();
        host.CloseDocument();
        host.OpenDocument("document.txt");
        host.SaveDocument("document1.txt");
    }
}

