using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.Resources;

class Script
{
    [STAThread]
    static public void Main(string[] args)
    {
        string sourceDir = Path.GetDirectoryName(args[0]);

        string[] files = new string[]
        {
            Path.Combine(sourceDir, "css_logo_256x256.png"),
            Path.Combine(sourceDir, "donate.png"),
            Path.Combine(sourceDir, "css_logo.ico")
        };

        var resFile = Path.Combine(sourceDir, "images.resources");

        using (var resourceWriter = new ResourceWriter(resFile))
        {
            foreach (string file in files)
            {
                if (file.EndsWith(".ico"))
                    resourceWriter.AddResource(Path.GetFileName(file), new Icon(file));
                else
                    resourceWriter.AddResource(Path.GetFileName(file), new Bitmap(file));
            }

            resourceWriter.Generate();
        }
    }
}