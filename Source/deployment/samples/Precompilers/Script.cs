//css_precompiler freestyle
using System.Windows.Forms;
 
for (int i = 0; i < args.Length; i++)
{
	Console.WriteLine(args[i]);
}

MessageBox.Show(Path.GetDirectoryName(Environment.CurrentDirectory), "Hello World!");


