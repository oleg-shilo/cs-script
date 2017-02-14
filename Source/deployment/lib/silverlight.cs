//css_dbg /t:winexe, /platform:x86, /args:BookPages.xap;
//css_host /platform:x86;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using Microsoft.Win32;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using CSScriptLibrary;
using csscript;
using System.Resources;
using System.Reflection;
using System.Diagnostics;

//Inspired by:
//http://blogs.microsoft.co.il/blogs/tamir/archive/2008/05/02/stand-alone-multiplatform-silverlight-application.aspx

//Silverlight BookPages XAP is based on http://www.switchonthecode.com/tutorials/silverlight-3-tutorial-planeprojection-and-perspective-3d

//Because of the problems with the IE COM hosting as an ActiveX control the host application must be of a x86 CPU architecture. 
class Script
{
    const string usage =
            "Usage: cscs silverlight [/i]|[/u] [/exe] xapFile\n" +
            "Executes silverlight XAP file without launching the browser or cretaing Web page.\n" +
            " /i - install Windows Explorer shell extensions.\n" +
            " /u - uninstall Windows Explorer shell extensions.\n" +
            " /exe - generates self contained executable for executing silverlight XAP.\n";

    [STAThread]
    static void Main(string[] args)
    {
        if (!Utils.TryRenderXAPFromResources()) //try to runas a self-contained executable
        {
            //run as a script
            if (args.Length == 0)
            {
                Console.WriteLine("You must specify the XAP file to execute.\n" + usage);
            }
            else if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
            {
                Console.WriteLine(usage);
            }
            else
            {
                try
                {
                    if (args.Length == 1)
                    {
                        if (args[0].ToLower() == "/u")
                        {
                            UnInstallShellExtension();
                        }
                        else if (args[0].ToLower() == "/i")
                        {
                            InstallShellExtension();
                        }
                        else
                        {
                            var xapFile = Path.GetFullPath(args[0]);
                            Utils.RenderXAP(xapFile);
                        }
                    }
                    else
                    {
                        if (args[0] == "/exe")
                        {
                            var xapFile = Path.GetFullPath(args[1]);
                            Utils.GenerateExecutable(xapFile);
                        }
                        else
                        {
                            Console.WriteLine(string.Format("Unexpected command line parameter {0}\n(1)", args[0], usage));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    MessageBox.Show(ex.ToString());
                }
            }
        }
    }

    static void UnInstallShellExtension()
    {
        try
        {
            Registry.ClassesRoot.DeleteSubKeyTree(@".xap\shell");
            Console.WriteLine("Silverlight Player (XAP viewer) shell extensions have been removed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    static void InstallShellExtension()
    {
        if (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") == null)
        {
            Console.WriteLine("You must install CS-Script first.");
            return;
        }

        using (var shell = Registry.ClassesRoot.CreateSubKey(@".xap\shell\Veiw\command"))
        {
            if (shell != null)
                shell.SetValue("", Environment.ExpandEnvironmentVariables("\"%CSSCRIPT_DIR%\\csws.exe\" silverlight.cs \"%1\""));
        }

        using (var shell = Registry.ClassesRoot.CreateSubKey(@".xap\shell\Convert to EXE\command"))
        {
            if (shell != null)
                shell.SetValue("", Environment.ExpandEnvironmentVariables("\"%CSSCRIPT_DIR%\\csws.exe\" silverlight.cs /exe \"%1\""));
        }
        Console.WriteLine("Silverlight Player (XAP viewer) shell extensions have been created.");
    }
}

public class Utils
{
    public static bool TryRenderXAPFromResources()
    {
        byte[] xapData = Utils.GetXAPFromResources();

        if (xapData != null)
        {
            Application.Run(new SLViewer(xapData, Assembly.GetExecutingAssembly().GetName().Name));
            return true;
        }
        else
            return false;
    }

    public static void RenderXAP(string xapFile)
    {
        Application.Run(new SLViewer(xapFile));
    }

    public static void GenerateExecutable(string xapFile)
    {
        string resource = "";
        string xapSource = "";

        try
        {
            var scriptFile = CSSEnvironment.PrimaryScriptFile;
            //scriptFile = @"E:\cs-script\Lib\silverlight.cs"; //if not executed as script (e.g. under IDE) then find the way to discover the location of this source code file

            if (scriptFile == null || !File.Exists(scriptFile))
                throw new Exception("In order to generate executable you must run 'silverlight.cs' as a script");

            string csscriptEngine = Assembly.GetEntryAssembly().Location;
            //csscriptEngine = @"E:\cs-script\cscs.exe"; //if not executed as script (e.g. under IDE) then find the way to discover CS-Script engine location

            resource = Utils.ToResource(xapFile);
            xapSource = Path.Combine(Path.GetTempPath(),
                                     Path.GetFileNameWithoutExtension(xapFile) + ".cs");

            using (StreamWriter sw = new StreamWriter(xapSource))
            {
                sw.WriteLine("//css_co /platform:x86;");
                sw.WriteLine("//css_res " + resource + ";");
                sw.WriteLine("//css_inc " + scriptFile + ";");
            }

            Process
                .Start(csscriptEngine, "/ew \"" + xapSource + "\"")
                .WaitForExit();

            var exe = Path.ChangeExtension(xapFile, ".exe");
            var compiledAssembly = Path.ChangeExtension(xapSource, ".exe");
            if (File.Exists(compiledAssembly))
            {
                if (File.Exists(exe))
                    File.Delete(exe);

                File.Move(compiledAssembly, exe);
                Console.WriteLine("The executable " + exe + " has been generated.");
            }
            else
            {
                Console.WriteLine("Could not generate executable...");
            }
        }
        finally
        {
            try
            {
                if (File.Exists(resource))
                    File.Delete(resource);
            }
            catch { }

            try
            {
                if (File.Exists(xapSource))
                    File.Delete(xapSource);
            }
            catch { }
        }
    }

    public static byte[] GetXAPFromResources()
    {
        try
        {
            var res = new ResourceManager("SLViewer", Assembly.GetExecutingAssembly());
            return (byte[])res.GetObject("content.xap"); ;
        }
        catch
        {
            return null;
        }
    }

    public static string ToResource(string xapFile)
    {
        var resourceFile = Path.Combine(Path.GetTempPath(), "SLViewer.resources");
        using (var resourceWriter = new ResourceWriter(resourceFile))
        {
            resourceWriter.AddResource("content.xap", File.ReadAllBytes(xapFile));
            resourceWriter.Generate();
        }
        return resourceFile;
    }
}

public class SLViewer : Form
{
    WebBrowser browser = new WebBrowser();
    TcpListener webServer;
    byte[] xapData;

    public SLViewer(string xapFile)
    {
        if (!File.Exists(xapFile))
            throw new Exception("Cannot find " + xapFile);

        xapData = File.ReadAllBytes(xapFile);
        Text = "Silverlight Viewer - " + Path.GetFileName(xapFile);
        Init();
    }

    public SLViewer(byte[] xapData, string xapName)
    {
        this.xapData = xapData;
        Text = xapName;
        Init();
    }

    void Init()
    {
        double offsetRatio = 0.8;

        Size = new Size((int)((double)Screen.PrimaryScreen.Bounds.Size.Width * offsetRatio),
                        (int)((double)Screen.PrimaryScreen.Bounds.Height * offsetRatio));

        StartPosition = FormStartPosition.CenterScreen;

        browser.Dock = DockStyle.Fill;
        Controls.Add(this.browser);

        string url = "http://localhost:" + InitWebServer();
        browser.Url = new Uri(url);
    }

    int InitWebServer()
    {
        webServer = new TcpListener(IPAddress.Any, 0);
        webServer.Start();

        BeginGetReponse(null);

        return ((IPEndPoint)webServer.LocalEndpoint).Port;
    }

    void BeginGetReponse(IAsyncResult result)
    {
        if (result == null)
        {
            webServer.BeginAcceptSocket(BeginGetReponse, null);
            return;
        }

        Socket socket = webServer.EndAcceptSocket(result);
        if (socket.Connected)
        {
            var request = new byte[1024];
            int count = socket.Receive(request);
            string requestType = Encoding.ASCII.GetString(request, 0, count)
                                               .Split(' ')[1];

            if (requestType == "/")
            {
                byte[] response = Encoding.ASCII.GetBytes(
                                   @"<HTML>
                                            <HEAD>
                                                <TITLE>DeskLight</TITLE>
                                            </HEAD>
                                            <BODY>
                                                <OBJECT TYPE=""application/x-silverlight"" Width=""100%"" Height=""100%"">
                                                    <param name=""Source"" value=""silverlight.xap"" />
                                                </OBJECT>
                                            </BODY>
                                        </HTML>");

                byte[] headerData = Encoding.ASCII.GetBytes(
                                        @"HTTP/1.1 200 OK\r\n" +
                                        "Server: WeirdThing1.1\r\n" +
                                        "Content-Type: text/html\r\n" +
                                        "Accept-Ranges: bytes\r\n" +
                                        "Content-Length: " + response.Length + "\r\n" +
                                        "Connection: Close" + "\r\n\r\n");

                socket.Send(headerData);
                socket.Send(response);
            }
            else if (requestType.Contains("xap"))
            {
                var httpHeaderData = Encoding.ASCII.GetBytes(
                                        @"HTTP/1.1 200 OK\r\n" +
                                        "Server: WeirdThing1.1\r\n" +
                                        "Content-Type: application/xap\r\n" +
                                        "Accept-Ranges: bytes\r\n" +
                                        "Content-Length: " + xapData.Length + "\r\n" +
                                        "Connection: Close" + "\r\n\r\n");

                socket.Send(httpHeaderData);
                socket.Send(xapData);
            }
            else
            {
                Console.WriteLine("Unknown request received...");
            }
        }

        webServer.BeginAcceptSocket(BeginGetReponse, null);
    }
}
