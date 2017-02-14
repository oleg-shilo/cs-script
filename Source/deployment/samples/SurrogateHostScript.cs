//css_host /version:v3.5 /platform:x86;
//css_ref System 
//css_ignore_ns System.Runtime.InteropServices
using System;
using System.Runtime.InteropServices;
 
class Script   
{
    static public void Main(string [] args)
    {
        Console.WriteLine("TragetFramework: " + Environment.Version);
        Console.WriteLine("Platform: " + ((Marshal.SizeOf(typeof(IntPtr)) == 8) ? "x64" : "x32"));
    }
}
