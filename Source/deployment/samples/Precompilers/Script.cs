//css_args a b
//css_autoclass freestyle
///css_precompiler freestyle

using System;
using System.IO;
using System.Windows.Forms;
 
Console.WriteLine(1);
Console.WriteLine(2); 
Console.WriteLine(3);
Console.WriteLine(4); 
 
"r".Convert();
var t2 = ""; 
 
for (int i = 0; i < args.Length; i++)  
{ 
	Console.WriteLine(args[i]);
}  
 

int t = 8;

Console.WriteLine(8.ToString("D6"));
t++;
   
MessageBox.Show(Path.GetDirectoryName(Environment.CurrentDirectory), "  Hello World!WW");

//css_ac_end

static class Extensions
{
    static public void Convert(this string text)
    {
        Console.WriteLine("converting");
    }
}