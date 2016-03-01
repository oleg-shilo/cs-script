//css_co /platform:x86;
using System;
using System.Diagnostics;

class Script
{
    [STAThread]
    static public void Main(string[] args)
    {
        Debug.Assert(false);
        string[] newArgs = new string[args.Length - 1];
        Array.Copy(args, 1, newArgs, 0, newArgs.Length);
        AppDomain.CurrentDomain.ExecuteAssembly(args[0], newArgs);
    }
}

