//css_dbg /t:winexe, /args:"C:\Documents and Settings\zos\Local Settings\Temp\CSSCRIPT\92332734\code (script).csproj"; 
using System;
using System.Xml;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

public class Form1 : Form
{
    class PrecompileCmd
    {
        public PrecompileCmd(string command)
        {
            this.args = command.Replace("cscs.exe ", "") + " //x";
            this.app = "cscs.exe";
            string[] tokens = command.Substring(0, command.IndexOf("\"/primary:")).Split('\\');
            script = tokens[tokens.Length - 1].Replace("\"", "");
        }
        public string args;
        public string app;
        public string script;
        public override string ToString()
        {
            return script;
        }
    }

    public Form1(string[] commands)
    {
        InitializeComponent();

        foreach (string item in commands)
            listBox2.Items.Add(new PrecompileCmd(item));

        if (listBox2.Items.Count != 0)
            listBox2.SelectedIndex = 0;
    }

    private ListBox listBox2;
    private Button ok1;
    private Button cancel1;

    private System.ComponentModel.IContainer components = null;
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.listBox2 = new System.Windows.Forms.ListBox();
        this.ok1 = new System.Windows.Forms.Button();
        this.cancel1 = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // listBox2
        // 
        this.listBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                    | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
        this.listBox2.FormattingEnabled = true;
        this.listBox2.Location = new System.Drawing.Point(2, 3);
        this.listBox2.Name = "listBox2";
        this.listBox2.Size = new System.Drawing.Size(258, 95);
        this.listBox2.TabIndex = 0;
        this.listBox2.DoubleClick += new System.EventHandler(this.listBox2_DoubleClick_1);
        // 
        // ok1
        // 
        this.ok1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
        this.ok1.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.ok1.Location = new System.Drawing.Point(43, 114);
        this.ok1.Name = "ok1";
        this.ok1.Size = new System.Drawing.Size(75, 23);
        this.ok1.TabIndex = 1;
        this.ok1.Text = "Ok";
        this.ok1.Click += new System.EventHandler(this.ok1_Click);
        // 
        // cancel1
        // 
        this.cancel1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
        this.cancel1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.cancel1.Location = new System.Drawing.Point(138, 114);
        this.cancel1.Name = "cancel1";
        this.cancel1.Size = new System.Drawing.Size(75, 23);
        this.cancel1.TabIndex = 2;
        this.cancel1.Text = "Cancel";
        // 
        // Form1
        // 
        this.AcceptButton = this.ok1;
        this.CancelButton = this.cancel1;
        this.ClientSize = new System.Drawing.Size(263, 149);
        this.Controls.Add(this.cancel1);
        this.Controls.Add(this.ok1);
        this.Controls.Add(this.listBox2);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
        this.Name = "Form1";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Files to precompile";
        this.ResumeLayout(false);

    }

    #endregion

    static void ProcessStart(string app, string args)
    {
        Process myProcess = new Process();
        myProcess.StartInfo.FileName = app;
        myProcess.StartInfo.Arguments = args;
        myProcess.StartInfo.UseShellExecute = false;
        myProcess.StartInfo.RedirectStandardOutput = true;
        myProcess.StartInfo.CreateNoWindow = true;
        myProcess.Start();
    }

    private void listBox2_DoubleClick_1(object sender, EventArgs e)
    {
        ok1_Click(sender, e);
    }

    private void ok1_Click(object sender, EventArgs e)
    {
        if (listBox2.SelectedItem != null)
        {
            PrecompileCmd cmd = (PrecompileCmd)listBox2.SelectedItem;
            ProcessStart(cmd.app, cmd.args);
            Close();
        }
    }
}

class Script
{
    static public void Main(string[] args)
    {
        new Form1(GetPreBuildEvent(args[0])).ShowDialog();
    }

    static string[] GetPreBuildEvent(string projFile)
    {
        //<Project>
        //<PropertyGroup>
        //<PreBuildEvent>
        //csws.exe "C:\cs-script\Dev\Macros C#\precompile.cs" "C:\cs-script\Dev\Macros C#\code.cs"
        //</PreBuildEvent>
        //</PropertyGroup>
        //</Project>
        XmlDocument doc = new XmlDocument();
        doc.Load(projFile);

        XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("ab", "http://schemas.microsoft.com/developer/msbuild/2003");

        List<string> retval = new List<string>(doc.SelectNodes("//ab:Project/ab:PropertyGroup/ab:PreBuildEvent", nsmgr)[0].InnerText.Split("\r\n".ToCharArray()));
        retval.RemoveAll(delegate(string item)
        {
            return item.Trim() == "" || !item.Trim().ToLower().StartsWith("cscs.exe \"");
        });
        return retval.ToArray();
    }

}

