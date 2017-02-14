using System;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Diagnostics;

class Script
{
    static public void i_Main(string[] args)
    {
        Install(new VSToolbar());
        Install(new VSEToolbar());
        //Restore(new VSToolbar());
        //Restore(new VSEToolbar());
        Console.WriteLine("Press <Enter> to continue...");
        Console.ReadLine();
    }

    static void Install(CSSToolbar toolbar)
    {
        try
        {
            if (toolbar.IsIdeInstalled && !toolbar.IsInstalled)
                toolbar.Install();
        }
        catch (Exception e)
        {
            Console.WriteLine("VS toolbar cannot be created:\n" + e.Message);
        }
    }
    static void Restore(CSSToolbar toolbar)
    {
        try
        {
            if (toolbar.IsIdeInstalled && toolbar.IsInstalled && toolbar.IsRestoreAvailable)
                toolbar.RestoreOldSettings();
        }
        catch (Exception e)
        {
            Console.WriteLine("VS toolbar cannot be created:\n" + e.Message);
        }
    }
}

public class VSToolbar : CSSToolbar
{
    public VSToolbar()
    {
        string userVSDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Visual Studio 2005");

        RegistryKey profile = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio\8.0\Profile");
        isIdeInstalled = (profile != null);

        if (isIdeInstalled)
        {
            //C:\Documents and Settings\<user>\My Documents\Visual Studio 2005\Settings\CurrentSettings.vssettings
            string file = profile.GetValue("AutoSaveFile").ToString();
            settingsFile = file.Replace("%vsspv_visualstudio_dir%", userVSDir);

            RegistryKey IDE = Registry.ClassesRoot.OpenSubKey(@"VisualStudio.cs.8.0\shell\Open\Command");
            if (IDE != null)
                ideFile = IDE.GetValue("").ToString().TrimStart('"').Split('\"')[0];
            else
                isIdeInstalled = false;
            //ideFile = "devenv.exe";
        }
    }
}
public class VSEToolbar : CSSToolbar
{
    public VSEToolbar()
    {
        string userVSDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Visual Studio 2005");
        RegistryKey profile = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VCSExpress\8.0\Profile");
        isIdeInstalled = (profile != null);

        if (isIdeInstalled)
        {
            //C:\Documents and Settings\<user>\My Documents\Visual Studio 2005\Settings\C# Express\CurrentSettings.vssettings
            string file = profile.GetValue("AutoSaveFile").ToString();
            settingsFile = file.Replace("%vsspv_visualstudio_dir%", userVSDir);

            RegistryKey IDE = Registry.ClassesRoot.OpenSubKey(@"VCSExpress.cs.8.0\shell\Open\Command");
            if (IDE != null)
                ideFile = IDE.GetValue("").ToString().TrimStart('"').Split('\"')[0];
            else
                isIdeInstalled = false;
            //ideFile = "VCSExpress.exe";
        }
    }
}

public class VS9Toolbar : CSSToolbar
{
    public VS9Toolbar()
    {
        string userVSDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Visual Studio 2008");
        RegistryKey profile = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio\9.0\Profile");
        isIdeInstalled = (profile != null);

        if (isIdeInstalled)
        {
            //C:\Documents and Settings\<user>\My Documents\Visual Studio 2005\Settings\CurrentSettings.vssettings
            string file = profile.GetValue("AutoSaveFile").ToString();
            settingsFile = file.Replace("%vsspv_visualstudio_dir%", userVSDir);
            if (File.Exists(settingsFile)) //even VS2008E creates non-Express settings in registry
            {
                RegistryKey IDE = Registry.ClassesRoot.OpenSubKey(@"VisualStudio.cs.9.0\shell\Open\Command");
                if (IDE != null)
                {
                    ideFile = IDE.GetValue("").ToString().TrimStart('"').Split('\"')[0];
                    //ideFile = "devenv.exe";
                    return;
                }
            }
        }

        settingsFile = "";
        isIdeInstalled = false;
    }
}

public class VSE9Toolbar : CSSToolbar
{
    public VSE9Toolbar()
    {
        string userVSDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Visual Studio 2008");
        RegistryKey profile = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VCSExpress\9.0\Profile");
        isIdeInstalled = (profile != null);

        if (isIdeInstalled)
        {
            //C:\Documents and Settings\<user>\My Documents\Visual Studio 2005\Settings\C# Express\CurrentSettings.vssettings
            string file = profile.GetValue("AutoSaveFile").ToString();
            settingsFile = file.Replace("%vsspv_visualstudio_dir%", userVSDir);

            RegistryKey IDE = Registry.ClassesRoot.OpenSubKey(@"VCSExpress.cs.9.0\shell\Open\Command");
            if (IDE != null)
            {
                ideFile = IDE.GetValue("").ToString().TrimStart('"').Split('\"')[0];
                //ideFile = "VCSExpress.exe";
            }
        }
        settingsFile = "";
        isIdeInstalled = false;
    }
}

public class VS10Toolbar : CSSToolbar
{
    public VS10Toolbar()
    {
        string userVSDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Visual Studio 2010");
        RegistryKey profile = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio\10.0\Profile");
        isIdeInstalled = (profile != null);

        if (isIdeInstalled)
        {
            //C:\Documents and Settings\<user>\My Documents\Visual Studio 2005\Settings\CurrentSettings.vssettings
            string file = profile.GetValue("AutoSaveFile").ToString();
            settingsFile = file.Replace("%vsspv_visualstudio_dir%", userVSDir);
            if (File.Exists(settingsFile)) //even VS2008E creates non-Express settings in registry
            {
                RegistryKey IDE = Registry.ClassesRoot.OpenSubKey(@"VisualStudio.cs.10.0\shell\Open\Command");
                if (IDE != null)
                {
                    ideFile = IDE.GetValue("").ToString().TrimStart('"').Split('\"')[0];
                    //ideFile = "devenv.exe";
                    return;
                }
            }

            settingsFile = "";
            isIdeInstalled = false;
        }
    }

    override public bool IsInstalled
    {
        get
        {
            return false;
        }
    }

    override public void Install()
    {
        string vsxFile = Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\CSScriptVSX.vsix");
        string vsxUrl = "http://visualstudiogallery.msdn.microsoft.com/e457e0bb-e89c-4775-8350-bce4ed533148";

        if (File.Exists(vsxFile))
        {
            DialogResult response = MessageBox.Show(
                "The latest version of the extension can be downloaded from the Visual Studio Gallery?\n\n" +
                "Press Yes if you would like to download the extension or press No to install the extension from the CS-Script package.", 
                "VSX Installation", 
                MessageBoxButtons.YesNoCancel);
            
            if (response == DialogResult.Cancel ) 
                 return;  
            else if (response == DialogResult.Yes ) 
                Process.Start(vsxUrl);
            else
                Process.Start(vsxFile);
        }
        else
            Process.Start(vsxUrl);
    }
}

public class VSE10Toolbar : CSSToolbar
{
    public VSE10Toolbar()
    {
        string userVSDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Visual Studio 2010");
        RegistryKey profile = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VCSExpress\10.0\Profile");
        isIdeInstalled = (profile != null);

        if (isIdeInstalled)
        {
            //C:\Documents and Settings\<user>\My Documents\Visual Studio 2005\Settings\C# Express\CurrentSettings.vssettings
            string file = profile.GetValue("AutoSaveFile").ToString();
            settingsFile = file.Replace("%vsspv_visualstudio_dir%", userVSDir);

            RegistryKey IDE = Registry.ClassesRoot.OpenSubKey(@"VCSExpress.cs.10.0\shell\Open\Command");
            if (IDE != null)
            {
                ideFile = IDE.GetValue("").ToString().TrimStart('"').Split('\"')[0];
                //ideFile = "VCSExpress.exe";
                return;
            }
        }

        settingsFile = "";
        isIdeInstalled = false;
    }
}

public class VS12Toolbar : CSSToolbar
{
    public VS12Toolbar()
    {
        string userVSDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Visual Studio 2012");
        RegistryKey profile = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio\11.0\Profile");
        isIdeInstalled = (profile != null);

        if (isIdeInstalled)
        {
            //C:\Documents and Settings\<user>\My Documents\Visual Studio 2005\Settings\CurrentSettings.vssettings
            string file = profile.GetValue("AutoSaveFile").ToString();
            settingsFile = file.Replace("%vsspv_visualstudio_dir%", userVSDir);
            if (File.Exists(settingsFile)) //even VS2008E creates non-Express settings in registry
            {
                RegistryKey IDE = Registry.ClassesRoot.OpenSubKey(@"VisualStudio.cs.11.0\shell\Open\Command");
                if (IDE != null)
                {
                    ideFile = IDE.GetValue("").ToString().TrimStart('"').Split('\"')[0];
                    //ideFile = "devenv.exe";
                    return;
                }
            }

            settingsFile = "";
            isIdeInstalled = false;
        }
    }

    override public bool IsInstalled
    {
        get
        {
            return false;
        }
    }

    override public void Install()
    {
        string vsxFile = Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\CSScript.vsix");
        string vsxUrl = "http://visualstudiogallery.msdn.microsoft.com/7ca14f55-1b6e-4390-bfa0-7eda7b1bb1a7";

        if (File.Exists(vsxFile))
        {
            DialogResult response = MessageBox.Show(
                "The latest version of the extension can be downloaded from the Visual Studio Gallery?\n\n" +
                "Press Yes if you would like to download the extension or press No to install the extension from the CS-Script package.",
                "VSX Installation",
                MessageBoxButtons.YesNoCancel);

            if (response == DialogResult.Cancel)
                return;
            else if (response == DialogResult.Yes)
                Process.Start(vsxUrl);
            else
                Process.Start(vsxFile);
        }
        else
            Process.Start(vsxUrl);
    }
}

public class VSE12Toolbar : CSSToolbar
{
    public VSE12Toolbar()
    {
        string userVSDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Visual Studio 2012");
        RegistryKey profile = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VCSExpress\11.0\Profile");
        isIdeInstalled = (profile != null);

        if (isIdeInstalled)
        {
            //C:\Documents and Settings\<user>\My Documents\Visual Studio 2005\Settings\C# Express\CurrentSettings.vssettings
            string file = profile.GetValue("AutoSaveFile").ToString();
            settingsFile = file.Replace("%vsspv_visualstudio_dir%", userVSDir);

            RegistryKey IDE = Registry.ClassesRoot.OpenSubKey(@"VCSExpress.cs.11.0\shell\Open\Command");
            if (IDE != null)
            {
                ideFile = IDE.GetValue("").ToString().TrimStart('"').Split('\"')[0];
                //ideFile = "VCSExpress.exe";
                return;
            }
        }

        settingsFile = "";
        isIdeInstalled = false;
    }
}

public interface IVSCodeSnippet
{
    bool IsInstalled { get; }
    void Install();
    void Remove();
}
public class VSCodeSnippet : IVSCodeSnippet
{
    protected string cssSnippetDir = Environment.ExpandEnvironmentVariables("%CSSCRIPT_DIR%\\Lib\\Code Snippets\\Visual C# (CS-Script)");
    protected string regSnippetsPath
    {
        get { return regSnippetsPathBase + @"\Visual C#"; }
    }
    protected string regSnippetsPathBase = @"Software\Microsoft\VisualStudio\8.0\Languages\CodeExpansions";
    public bool IsInstalled
    {
        get
        {
            RegistryKey snippetsPath = Registry.CurrentUser.OpenSubKey(regSnippetsPath);

            if (snippetsPath != null)
            {
                string path = snippetsPath.GetValue("Path").ToString();
                return path.Contains(cssSnippetDir) || path.Contains(cssSnippetDir + "\\");
            }
            return false;
        }
    }
    public void Install()
    {
        RegistryKey snippetsPath = Registry.CurrentUser.OpenSubKey(regSnippetsPath, true);

        if (snippetsPath == null)
        {
            Customize();
            snippetsPath = Registry.CurrentUser.OpenSubKey(regSnippetsPath, true);
        }

        string path = snippetsPath.GetValue("Path").ToString();
        snippetsPath.SetValue("Path", path + ";" + cssSnippetDir);

    }
    public virtual void Remove()
    {
        RegistryKey snippetsPath = Registry.CurrentUser.OpenSubKey(regSnippetsPath, true);

        if (snippetsPath != null)
        {
            string path = snippetsPath.GetValue("Path").ToString();
            snippetsPath.SetValue("Path", path.Replace(";" + cssSnippetDir, ""));
        }
    }
    //This is an equivalent of the customize code snippets action from the tools VS menu.
    //It creates initial registry structure.
    void Customize()
    {
        RegistryKey snippetsBasePath = Registry.CurrentUser.OpenSubKey(regSnippetsPathBase, true);
        RegistryKey snippetsPath = snippetsBasePath.CreateSubKey("Visual C#");
        snippetsPath.SetValue("", "{694DD9B6-B865-4C5B-AD85-86356E9C88DC}");
        snippetsPath.SetValue("FirstLoad", 1);
        snippetsPath.SetValue("Path", "%InstallRoot%\\VC#\\Snippets\\%LCID%\\Visual C#\\;%InstallRoot%\\VC#\\Snippets\\%LCID%\\Refactoring\\;%MyDocs%\\Code Snippets\\Visual C#\\My Code Snippets\\");
        snippetsPath.CreateSubKey("Paths").SetValue("Microsoft Visual CSharp", "%InstallRoot%\\VC#\\Snippets\\%LCID%\\Visual C#\\;%InstallRoot%\\VC#\\Snippets\\%LCID%\\Refactoring\\;%MyDocs%\\Code Snippets\\Visual C#\\My Code Snippets\\");
        snippetsBasePath.DeleteSubKey("CSharp");
    }
}
public class VSECodeSnippet : VSCodeSnippet
{
    public VSECodeSnippet()
    {
        base.regSnippetsPathBase = @"Software\Microsoft\VCSExpress\8.0\Languages\CodeExpansions";
    }
    public override void Remove()
    {
        MessageBox.Show(
            "The Express edition of Visual Studio 2005 has problem with deleting the custom Code Snippets path(s).\n\n" +
            "Thus you need to remove CS-Script snippets manually:Tools->Code Snippets Manager->Remove.");
    }
}
public class VS9CodeSnippet : VSCodeSnippet
{
    public VS9CodeSnippet()
    {
        base.regSnippetsPathBase = @"Software\Microsoft\VisualStudio\9.0\Languages\CodeExpansions";
    }
}
public class VSE9CodeSnippet : VSCodeSnippet
{
    public VSE9CodeSnippet()
    {
        base.regSnippetsPathBase = @"Software\Microsoft\VCSExpress\9.0\Languages\CodeExpansions";
    }
}

public class VS10CodeSnippet : VSCodeSnippet
{
    public VS10CodeSnippet()
    {
        base.regSnippetsPathBase = @"Software\Microsoft\VisualStudio\10.0\Languages\CodeExpansions";
    }
}
public class VSE10CodeSnippet : VSCodeSnippet
{
    public VSE10CodeSnippet()
    {
        base.regSnippetsPathBase = @"Software\Microsoft\VCSExpress\10.0\Languages\CodeExpansions";
    }
}


public class CSSToolbar
{
    XmlDocument doc;
    public struct ToolData
    {
        public ToolData(string name, string command, string args)
        {
            this.name = name;
            this.command = command;
            this.args = args;
        }

        public string name;
        public string command;
        public string args;
    }
    ToolData[] data = new ToolData[] 
        {
            new ToolData("CS-Script Refresh", "csws.exe", "refreshVSProj \"$(ProjectDir)\\$(ProjectFileName)\""),
            new ToolData("CS-Script Run precompiler", "csws.exe", "debug2005Precompiler \"$(ProjectDir)\\$(ProjectFileName)\""),
            new ToolData("CS-Script Explore", "explorer.exe", " /e, /select, \"$(ItemPath)\"")
        };
    protected string settingsFile = "";
    protected string ideFile = "";

    protected CSSToolbar()
    {
    }
    protected bool isIdeInstalled = false;
    public bool IsIdeInstalled
    {
        get
        {
            return isIdeInstalled;
        }
    }

    virtual public bool IsInstalled
    {
        get
        {
            if (settingsFile == "")
                return false;

            using (StreamReader sr = new StreamReader(settingsFile))
                return (sr.ReadToEnd().IndexOf("CS-Script Refresh") != -1);
        }
    }

    virtual public void Install()
    {
        File.Copy(settingsFile, settingsFile + ".css.bak", true);
        //ProcessAsXml(); //does not work well with VS: even XmlDocument.Load() + XmlDocument.Save() make .settings file unrecognizable by VS
        ProcessAsTxt(settingsFile);
    }

    public void Import()
    {
        string homeDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");
        if (homeDir != null)
        {
            File.Copy(settingsFile, settingsFile + ".css.bak", true);
            try
            {
                System.Diagnostics.Process.Start(ideFile, "/ResetSettings \"" + Path.Combine(homeDir, @"Lib\CSScript-Toolbar.vssettings") + "\"");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }

    public bool IsRestoreAvailable
    {
        get
        {
            return File.Exists(settingsFile + ".css.bak");
        }
    }

    public void RestoreOldSettings()
    {
        File.Copy(settingsFile + ".css.bak", settingsFile, true);
        File.Delete(settingsFile + ".css.bak");
    }

    void ProcessAsTxt(string settingsFile)
    {
        string startMarker = "<Category name=\"Environment_ExternalTools\" Category=\"{E8FAE9E8-FBA2-4474-B134-AB0FFCFB291D}\" Package=\"{DA9FB551-C724-11d0-AE1F-00A0C90FFFC3}\" RegisteredName=\"Environment_ExternalTools\" PackageName=\"Visual Studio Environment Package\">";
        string endMarker = "<PropertyValue name=\"ToolNames\">";
        string endNoToolsMarker = "<PropertyValue name=\"ToolNames\"/>";

        string newToolDataTemplate =
            "<PropertyValue name=\"{0}.Command\">{1}</PropertyValue>" +
            "<PropertyValue name=\"{0}.Arguments\">{2}</PropertyValue>" +
            "<PropertyValue name=\"{0}.InitialDirectory\"/>" +
            "<PropertyValue name=\"{0}.SourceKeyName\"/>" +
            "<PropertyValue name=\"{0}.UseOutputWindow\">false</PropertyValue>" +
            "<PropertyValue name=\"{0}.PromptForArguments\">false</PropertyValue>" +
            "<PropertyValue name=\"{0}.CloseOnExit\">false</PropertyValue>" +
            "<PropertyValue name=\"{0}.IsGUIapp\">true</PropertyValue>" +
            "<PropertyValue name=\"{0}.SaveAllDocs\">true</PropertyValue>" +
            "<PropertyValue name=\"{0}.UseTaskList\">false</PropertyValue>" +
            "<PropertyValue name=\"{0}.Unicode\">false</PropertyValue>" +
            "<PropertyValue name=\"{0}.Package\">{2}</PropertyValue>" +
            "<PropertyValue name=\"{0}.NameID\">0</PropertyValue>";


        string text = "";
        using (StreamReader sr = new StreamReader(settingsFile))
            text = sr.ReadToEnd();

        if (text.IndexOf("CS-Script Refresh") != -1)
            throw new Exception("CS-Script settings already exist.");


        string newStartMarker = startMarker;
        string newEndMarker = endMarker;
        foreach (ToolData item in data)
        {
            newStartMarker += string.Format(newToolDataTemplate, item.name, item.command, item.args, "{00000000-0000-0000-0000-000000000000}");
            newEndMarker += item.name + ", ";
        }

        using (StreamWriter sw = new StreamWriter(settingsFile))
        {
            int noToolsMarker = text.IndexOf(endNoToolsMarker);
            if (noToolsMarker != -1)
            {
                newEndMarker = newEndMarker.Substring(0, newEndMarker.Length - 2) + "</PropertyValue>";
                sw.Write(text.Replace(startMarker, newStartMarker).Replace(endNoToolsMarker, newEndMarker));
            }
            else
            {
                sw.Write(text.Replace(startMarker, newStartMarker).Replace(endMarker, newEndMarker));
            }
        }

        CreateToolbar(settingsFile);
    }

    void CreateToolbar(string settingsFile)
    {
        string startUserCustomizationsMarker = "<UserCustomizations>";
        string endUserCustomizationsMarker = "</UserCustomizations>";
        string endCommandBarsMarker = "</CommandBars>";

        string toolbarData =
            "<add_group Group=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000203\" GroupPri=\"40000001\" Menu=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000601\"/>" +
            "<add_toolbar Menu=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000601\" Name=\"CS-Script\" MenuType=\"toolbar\"/>" +
            "<modify_toolbar Menu=\"{9ADF33D0-8AAD-11D0-B606-00A0C922E851}:0000000e\" Visibility=\"auto\" FullScreen=\"hide\" Dock=\"top\" Row=\"2\" DockRectangle=\"308,49,717,75\"/>" +
            "<modify_toolbar Menu=\"{C9DD4A58-47FB-11D2-83E7-00C04F9902C1}:00000421\" Visibility=\"show\" FullScreen=\"hide\" Dock=\"top\" Row=\"2\" DockRectangle=\"0,49,308,75\"/>" +
            "<modify_toolbar Menu=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000601\" Name=\"CS-Script\" Visibility=\"show\" FullScreen=\"hide\" Floating=\"true\" Dock=\"top\" Row=\"4\" FloatRectangle=\"658,292,908,338\" DockRectangle=\"649,49,916,75\"/>" +
            "<add Cmd=\"{5EFC7975-14BC-11CF-9B2B-00AA00573819}:00000102\" CmdPri=\"07008001\" Group=\"{C9DD4A58-47FB-11D2-83E7-00C04F9902C1}:00000011\" GroupPri=\"03000000\" Menu=\"{C9DD4A58-47FB-11D2-83E7-00C04F9902C1}:00000421\"/>" +
            "<add Cmd=\"{5EFC7975-14BC-11CF-9B2B-00AA00573819}:00000276\" CmdPri=\"40000001\" Group=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000203\" GroupPri=\"40000001\" Menu=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000601\"/>" +
            "<add Cmd=\"{5EFC7975-14BC-11CF-9B2B-00AA00573819}:00000277\" CmdPri=\"40008001\" Group=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000203\" GroupPri=\"40000001\" Menu=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000601\"/>" +
            "<add Cmd=\"{5EFC7975-14BC-11CF-9B2B-00AA00573819}:00000278\" CmdPri=\"40010001\" Group=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000203\" GroupPri=\"40000001\" Menu=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000601\"/>" +
            "<modify Cmd=\"{5EFC7975-14BC-11CF-9B2B-00AA00573819}:00000276\" CmdPri=\"40000001\" Group=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000203\" GroupPri=\"40000001\" Menu=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000601\" Name=\"Refresh project\" CustomIcon=\"CCgEAAAoAAAAEAAQAAEgAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAcAAAACgAAAAQABAAAQEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD///8A//8AAP//AAD//wAA+DcAAPAXAADjhwAA58cAAP+HAADh/wAA4+cAAOHHAADoDwAA7B8AAP//AAD//wAA//8AAA\" Style=\"3\"/>" +
            "<modify Cmd=\"{5EFC7975-14BC-11CF-9B2B-00AA00573819}:00000277\" CmdPri=\"40008001\" Group=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000203\" GroupPri=\"40000001\" Menu=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000601\" Name=\"Precompiler\" Icon=\"{D309F794-903F-11D0-9EFC-00A0C911004F}:00000860\" Style=\"3\"/>" +
            "<modify Cmd=\"{5EFC7975-14BC-11CF-9B2B-00AA00573819}:00000278\" CmdPri=\"40010001\" Group=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000203\" GroupPri=\"40000001\" Menu=\"{F33C911F-81BB-4F43-8B60-5BA39E9A94A1}:00000601\" Name=\"Explore\" CustomIcon=\"COgAAAAoAAAAEAAQAAEEAAAAAAAAAAAAAAAAAAAAAAAAAAD/AAAAgICAAP///wAAAAAAgAAAAIAAgACAgAAAgICAAMDAwAAAAP8AAP8AAAD//wD/AAAA/wD/AP//AAD///8AERERERERERESMzoSMyMyMRIiIhIiIiIhEjMyEjMjMjESIiISIiIiIRIzMhIzIzIxEiIiEiIiIiESMzISMyMyMRIiIhIiIiIhEjMyEjMjMjESIiISIiIiIRERERERERERERIiIiIiIiEREREREREREQAAAAAAAAAAAAAAAAAAAABwAAAAKAAAABAAEAABAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP///wAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\" Style=\"3\"/>";

        string text = "";
        using (StreamReader sr = new StreamReader(settingsFile))
            text = sr.ReadToEnd();

        int endCommandBars = text.IndexOf(endCommandBarsMarker);
        int endUserCustomizations = text.LastIndexOf(endUserCustomizationsMarker);

        if (endCommandBars > endUserCustomizations &&
            text.Substring(endUserCustomizations + endUserCustomizationsMarker.Length, endCommandBars - endUserCustomizations).Trim() == "")
        {
            //</UserCustomizations> </CommandBars>
            text = text.Insert(endUserCustomizations, toolbarData);
        }
        else
        {
            //</CommandBars> 
            text = text.Insert(endCommandBars, startUserCustomizationsMarker + toolbarData + endUserCustomizationsMarker);
        }

        using (StreamWriter sw = new StreamWriter(settingsFile))
            sw.Write(text);

    }

    void ProcessAsXml(string settingsFile)
    {
        try
        {
            doc = new XmlDocument();
            doc.Load(settingsFile);
            string projFile1 = settingsFile + ".xml";

            XmlNode toolNames = doc.SelectNodes("UserSettings/Category[@name='Environment_Group']/Category[@name='Environment_ExternalTools']/PropertyValue[@name='ToolNames']")[0];

            XmlNode externalTools = toolNames.ParentNode;

            XmlNode firstTool = externalTools.FirstChild;

            string newTool = "CS-Script explore";

            externalTools.InsertBefore(CreatePropertyValue(newTool + ".Command", "explorer.exe"), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".Arguments", " /e, /select, $(ItemPath)"), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".InitialDirectory", ""), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".SourceKeyName", ""), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".UseOutputWindow", "false"), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".PromptForArguments", "false"), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".CloseOnExit", "false"), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".IsGUIapp", "true"), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".SaveAllDocs", "true"), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".UseTaskList", "false"), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".Unicode", "false"), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".Package", "{00000000-0000-0000-0000-000000000000}"), firstTool);
            externalTools.InsertBefore(CreatePropertyValue(newTool + ".NameID", "0"), firstTool);

            toolNames.InnerText = newTool + ", " + toolNames.InnerText;

            doc.Save(settingsFile);
        }
        catch (Exception e)
        {
            Console.WriteLine("VS environment cannot be configured:\n" + e.Message);
        }
    }

    XmlElement CreatePropertyValue(string name, string value)
    {
        XmlAttribute newAttr;
        newAttr = doc.CreateAttribute("name");
        newAttr.Value = name;

        XmlElement elem = doc.CreateElement("PropertyValue");
        elem.Attributes.Append(newAttr);
        elem.InnerText = value;

        return elem;
    }
}



