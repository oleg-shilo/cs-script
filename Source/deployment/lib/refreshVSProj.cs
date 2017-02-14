using System;
using System.Xml;
using System.Windows.Forms;
using System.IO;

//css_import debugVS8.0.cs;
//css_import debugVS9.0.cs;
//css_import debugVS10.0.cs;
class Script
{
    static string usage = "Usage: cscscript refreshVSProj file...\nUpdates C# script project file loaded in VisualStudio.\n" +
                            "Can be used as a Visual Studio external tool macro:\n\t refreshVSProj \"$(ProjectDir)\\$(ProjectFileName)\"\n";

    static public void Main(string[] args)
    {
        if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
        {
            Console.WriteLine(usage);
        }
        else
        {
            System.Diagnostics.Debug.Assert(false);

            bool isVS8 = true;
            bool isVS9 = true;
            bool isVS10 = true;

            VS100.VSProjectDoc doc = new VS100.VSProjectDoc(args[0]);
            XmlNode node = doc.SelectFirstNode("//Project");
            XmlAttribute version = node.Attributes["ToolsVersion"];

            if (version != null)
            {
                isVS8 = false;
                if (version.Value == "4.0")
                {
                    isVS9 = false;
                    isVS10 = true;
                }
                else
                {
                    isVS9 = true;
                    isVS10 = false;
                }
            }
            else
            {
                isVS9 = false;
                isVS8 = true;
                isVS10 = false;
            }

            if (isVS8)
            {
                VS80.Script.i_Main(new string[] { "/r", args[0] });
            }
            else if (isVS9)
            {
                VS90.Script.i_Main(new string[] { "/r", args[0] });
            }
            else if (isVS10)
            {
                VS100.Script.i_Main(new string[] { "/r", args[0] });
            }
        }
    }
}