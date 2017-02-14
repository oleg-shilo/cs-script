using System.IO;
using System;
using static dbg;

class Script
{
    static public void Main(string[] args)
    {
        print(new DirectoryInfo(Environment.CurrentDirectory));
    }
}