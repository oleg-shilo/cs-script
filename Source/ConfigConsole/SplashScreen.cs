using System.Threading;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

public partial class SplashScreen : Form
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.notification = new System.Windows.Forms.TextBox();
            this.OK = new System.Windows.Forms.Button();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(9, 25);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(400, 17);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(6, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Please wait...";
            // 
            // notification
            // 
            this.notification.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.notification.Location = new System.Drawing.Point(9, 49);
            this.notification.Multiline = true;
            this.notification.Name = "notification";
            this.notification.ReadOnly = true;
            this.notification.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.notification.Size = new System.Drawing.Size(400, 139);
            this.notification.TabIndex = 3;
            // 
            // OK
            // 
            this.OK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OK.Location = new System.Drawing.Point(145, 201);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(117, 23);
            this.OK.TabIndex = 5;
            this.OK.Text = "&OK";
            // 
            // linkLabel1
            // 
            this.linkLabel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabel1.Location = new System.Drawing.Point(45, 213);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(364, 23);
            this.linkLabel1.TabIndex = 4;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "linkLabel1";
            this.linkLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.linkLabel1.Visible = false;
            this.linkLabel1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.linkLabel1_MouseClick);
            // 
            // SplashScreen
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(417, 236);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.notification);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.progressBar1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SplashScreen";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CS-Script";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SplashScreen_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ProgressBar progressBar1;
    private TextBox notification;
    private Button OK;
    private LinkLabel linkLabel1;
    private System.Windows.Forms.Label label1;

    public SplashScreen()
    {
        InitializeComponent();
    }
    public SplashScreen(string title, string message)
    {
        InitializeComponent();
        Text = title;
        label1.Text = message;
        Height = 74;
        OK.Visible =
        notification.Visible = false;
        progressBar1.Enabled = true;
        linkLabel1.Visible = false;
    }

    public void Terminate()
    {
        this.Invoke((MethodInvoker)delegate
        {
            this.Height = 74;
            OK.Visible =
            notification.Visible = false;
            progressBar1.Enabled = true;
            linkLabel1.Visible = false;
            this.Close();
        });
    }

    public void ShowNotificationMessage(string message, string progressText, bool stopProgress, string commadTitle, MethodInvoker command)
    {
        this.Invoke((MethodInvoker)delegate
        {
            Debug.Assert(this.Visible);

            this.Height = 250;
            OK.Visible =
            notification.Visible = true;
            notification.Text = message;

            label1.Text = progressText;
            progressBar1.Enabled = !stopProgress;
            progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Blocks;
            progressBar1.Value = progressBar1.Minimum;

            linkLabel1.Visible = true;
            linkLabel1.Text = commadTitle;
            this.notificationCommand = command;
        });
    }

    MethodInvoker notificationCommand = null;

    private static SplashScreen instance;

    public static void ShowSplash(string title, string message)
    {
        Thread t = new Thread(
            delegate(object obj)
            {
                instance = new SplashScreen(title, message);
                instance.ShowDialog();
            });
        t.IsBackground = true;
        t.Start();
    }

    public static void ShowSplash()
    {
        Thread t = new Thread(
            delegate(object obj)
            {
                instance = new SplashScreen();
                instance.ShowDialog();
            });
        t.IsBackground = true;
        t.Start();
    }

    public static void ShowNotification(string message, string progressLabel, bool stopProgressbar, string commadTitle, MethodInvoker command)
    {
        try
        {
            //instance.Invoke((MethodInvoker)delegate { Application.ExitThread(); });
            instance.ShowNotificationMessage(message, progressLabel, stopProgressbar, commadTitle, command);
        }
        catch { }
    }

    public static bool IsClosed
    {
        get
        {
            return instance == null;
        }
    }

    public static void HideSplash()
    {
        try
        {
            //instance.Invoke((MethodInvoker)delegate { Application.ExitThread(); });
            instance.Terminate();
        }
        catch { }
    }

    private void SplashScreen_FormClosed(object sender, FormClosedEventArgs e)
    {
        instance = null;
    }

    private void linkLabel1_MouseClick(object sender, MouseEventArgs e)
    {
        if (notificationCommand != null)
            notificationCommand();
    }
}
