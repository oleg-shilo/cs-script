using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SplashScreen));
            this.panel1 = new System.Windows.Forms.Panel();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel1.Location = new System.Drawing.Point(1, 8);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(43, 38);
            this.panel1.TabIndex = 0;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(47, 9);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(329, 17);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(44, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(356, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "CS-Script v0.0.0. - Copyright © 2004-2016 Oleg Shilo. All Rights Reserved";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // SplashScreen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(413, 50);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.panel1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SplashScreen";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CS-Script Config - Splash Screen";
            this.Deactivate += new System.EventHandler(this.SplashScreen_Deactivate);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.ProgressBar progressBar1;
    private System.Windows.Forms.Timer timer1;
    private System.Windows.Forms.Label label1;

    public SplashScreen()
    {
        InitializeComponent();
        label1.Text = "CS-Script v" + Global.AssemblyVersion + " - " + Global.AssemblyCopyright;
    }

    private static SplashScreen instance;

    public static void ShowSplash()
    {
        ThreadPool.QueueUserWorkItem(delegate(object obj)
        {
            instance = new SplashScreen();
            instance.ShowDialog();
        });
    }
    public static void HideSplash()
    {
        try
        {
            instance.Invoke((Action)delegate { instance.Close(); });
        }
        catch { }
    }

    private void SplashScreen_Deactivate(object sender, EventArgs e)
    {
        Application.ExitThread();
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        if (Environment.GetEnvironmentVariable("ConfigConsoleLoaded") != null)
        {
            timer1.Enabled = false;
            Close();
        }
    }

}
