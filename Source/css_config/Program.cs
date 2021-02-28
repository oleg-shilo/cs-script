using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

class Global
{
    public static string AssemblyVersion
    {
        get
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return v.Major + "." + v.Minor + "." + v.Build;
        }
    }

    public static string AssemblyProduct
    {
        get
        {
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if (attributes.Length == 0)
                return "";
            else
                return ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    public static string AssemblyCopyright
    {
        get
        {
            // Get all Copyright attributes on this assembly
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);

            // If there aren't any Copyright attributes, return an empty string
            if (attributes.Length == 0)
                return "";

            // If there is a Copyright attribute, return its value
            return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }
}

class Program
{
    [STAThread]
    static void Main()
    {
        //Debug.Assert(false);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (!IsValidCLR())
        {
            MessageBox.Show("CS-Script requires .NET v" + RequiredCLRVersion + ".\nPlease install the required version and start the installation/configuration again.", "CS-Script");
        }
        else
        {
            //SplashScreen.ShowSplash();
            //System.Threading.Thread.Sleep(5000);
            //return;

            if (VistaSecurity.IsAdmin())
            {
                bool environmentIsValid = PathchAndValidateEnvironment();
                if (environmentIsValid)
                    AppForm.StartConfig();
            }
            else
            {
                //Application.EnableVisualStyles();
                //Application.SetCompatibleTextRenderingDefault(false);

                //embedded manifest will force it start elevated
                // VistaSecurity.RestartElevated();
                //MessageBox.Show("non admin");
            }
        }
    }    static string RequiredCLRVersion = "4.0";

    static bool IsValidCLR()
    {
        using (RegistryKey subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v" + RequiredCLRVersion, false))
            return subKey != null;

        //Application.Run(new AppForm());

        //VistaSecurity.RestartElevated();
    }

    static bool PathchAndValidateEnvironment()
    {
        if (IsNet40OrNewer() && !IsNet45OrNewer())
        {
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string sourceDll = Path.Combine(baseDir, @"Lib\Bin\NET 4.0\CSScriptLibrary.dll");
            string destDll = Path.Combine(baseDir, @"Lib\CSScriptLibrary.dll");
            string backupDll = Path.Combine(baseDir, @"Lib\CSScriptLibrary.v4.5.dll");

            if (File.Exists(backupDll))
                return true; //already patched

            var response = MessageBox.Show(
                "CS-Script requires .NET minimum v4.5 and you have only v4.0 installed.\n" +
                "Please install the required version and start the installation/configuration again.\n\n" +

                "Alternatively you can retarget the script engine (CSScriptLibrary.dll) to .NET v4.0.\n" +

                //"Note: you will need to repeat the CS-Script setup if you install .NET v4.5 in the future.\n\n" +

                "Would you like to retarget the script engine?", "CS-Script",
                MessageBoxButtons.YesNo);

            if (response == DialogResult.Yes)
            {
                try
                {
                    File.Move(destDll, backupDll);
                    File.Copy(sourceDll, destDll);
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Script engine assembly substitution failed.\n\n" + e.Message, "CS-Script");
                    return false;
                }
            }
            else
                return false;
        }
        else
        {
            return true;
        }
    }

    public static bool IsNet45OrNewer()
    {
        // Class "ReflectionContext" exists from .NET 4.5 onwards.
        return Type.GetType("System.Reflection.ReflectionContext", false) != null;
    }

    public static bool IsNet40OrNewer()
    {
        return Environment.Version.Major >= 4;
    }
}