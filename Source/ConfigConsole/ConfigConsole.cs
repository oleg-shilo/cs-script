//css_dbg /t:winexe;
//css_dir ..\
//css_dir %CS-S_DEV_ROOT%\lib
//css_ref System.Core;
//css_pre images($this)
//css_res images.resources;
//css_import searchDirs.cs;
//css_import SplashScreen.cs;
using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using csscript;

namespace Config
{
    public class ConfigForm : Form
    {
        private CSScriptInstaller installer;
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.Button closeBtn;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button deactivateBtn;
        private System.Windows.Forms.CheckedListBox checkedListBox1;
        private System.Windows.Forms.Label label4;
        private TabPage tabPage3;
        private LinkLabel linkLabel2;
        private Button button5;
        private TextBox textBox3;
        private Label label6;
        private ToolTip toolTip1;
        private GroupBox groupBox3;
        private LinkLabel linkLabel9;
        private LinkLabel linkLabel8;
        private LinkLabel linkLabel7;
        private LinkLabel linkLabel6;
        private TextBox textBox4;
        private bool quiet = false;
        private GroupBox groupBox4;
        private LinkLabel integrateVS;
        private LinkLabel manageSearchDirs;
        private LinkLabel configureShellExt;
        private TabControl tabControl2;
        private TabPage tabPage4;
        private Button repareBtn;
        private Button donateBtn;
        private ComboBox doubleClickAction;
        private PictureBox iconPictureBox;
        private Button changeIconButton;
        private PictureBox pictureBox1;
        private LinkLabel linkLabel11;
        private Button updateNuGetBtn;
        private LinkLabel integrateNpp;
        private LinkLabel integrateST3;
        private bool ignoreDirtyOnClose = false;

        public ConfigForm(bool quiet, bool update)
        {
            this.quiet = quiet;
            installer = new CSScriptInstaller(quiet, update);
            Init();
        }

        private void Init()
        {
            //System.Diagnostics.Debug.Assert(false);

            InitializeComponent();

            if (installer.restrictedMode)
            {
                //MessageBox.Show("Your user account dos not allow changing the system configuration.\n" +
                //  "Some functionality of the configuration console will be disabled.",
                //  "CS-Script Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Text += " (Restricted mode)";
                deactivateBtn.Enabled =
                comboBox1.Enabled =
                checkedListBox1.Enabled =
                doubleClickAction.Enabled = false;
            }

            textBox1.Text = installer.HomeDir;
            comboBox1.Items.AddRange(installer.AvailableCLRVersions);
            comboBox1.Text = installer.targetCLRVersion.Version;
            doubleClickAction.Text = installer.doubleClickAction;
            textBox3.Text = "<unknown>";

            foreach (CSScriptInstaller.ContextMenuInfo info in installer.ContextMenus)
            {
                if (CSScriptInstaller.justInstalled)
                {
                    checkedListBox1.Items.Add(info.name + info.hint, info.enabledDefault);
                    info.dirty = true;
                }
                else
                    checkedListBox1.Items.Add(info.name + info.hint, info.Enabled);
            }

            if (CSScriptInstaller.justInstalled)
            {
                Save();
                CSScriptInstaller.ResetCsDefaultProgram();
            }

            propertyGrid1.SelectedObject = installer.settings;

            textBox3.Text = Environment.GetEnvironmentVariable("CSScriptRuntime");
            this.Text += " - " + textBox3.Text;

            button2.Enabled = false;
            button3.Enabled = false;
        }

        static public string DoubleClickNotepadAction
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "notepad.exe") + " \"%1\"";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.integrateST3 = new System.Windows.Forms.LinkLabel();
            this.doubleClickAction = new System.Windows.Forms.ComboBox();
            this.configureShellExt = new System.Windows.Forms.LinkLabel();
            this.integrateNpp = new System.Windows.Forms.LinkLabel();
            this.integrateVS = new System.Windows.Forms.LinkLabel();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.iconPictureBox = new System.Windows.Forms.PictureBox();
            this.changeIconButton = new System.Windows.Forms.Button();
            this.updateNuGetBtn = new System.Windows.Forms.Button();
            this.repareBtn = new System.Windows.Forms.Button();
            this.deactivateBtn = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.manageSearchDirs = new System.Windows.Forms.LinkLabel();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.donateBtn = new System.Windows.Forms.Button();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.linkLabel11 = new System.Windows.Forms.LinkLabel();
            this.linkLabel9 = new System.Windows.Forms.LinkLabel();
            this.linkLabel8 = new System.Windows.Forms.LinkLabel();
            this.linkLabel7 = new System.Windows.Forms.LinkLabel();
            this.linkLabel6 = new System.Windows.Forms.LinkLabel();
            this.button5 = new System.Windows.Forms.Button();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.closeBtn = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.iconPictureBox)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            //
            // tabControl1
            //
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(1, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(433, 243);
            this.tabControl1.TabIndex = 1;
            //
            // tabPage1
            //
            this.tabPage1.Controls.Add(this.integrateST3);
            this.tabPage1.Controls.Add(this.doubleClickAction);
            this.tabPage1.Controls.Add(this.configureShellExt);
            this.tabPage1.Controls.Add(this.integrateNpp);
            this.tabPage1.Controls.Add(this.integrateVS);
            this.tabPage1.Controls.Add(this.groupBox4);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.comboBox1);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(425, 217);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            //
            // integrateST3
            //
            this.integrateST3.Location = new System.Drawing.Point(235, 183);
            this.integrateST3.Name = "integrateST3";
            this.integrateST3.Size = new System.Drawing.Size(165, 19);
            this.integrateST3.TabIndex = 15;
            this.integrateST3.TabStop = true;
            this.integrateST3.Text = "Sublime Text Integration";
            this.integrateST3.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.integrateST3_LinkClicked);
            //
            // doubleClickAction
            //
            this.doubleClickAction.FormattingEnabled = true;
            this.doubleClickAction.Location = new System.Drawing.Point(13, 119);
            this.doubleClickAction.Name = "doubleClickAction";
            this.doubleClickAction.Size = new System.Drawing.Size(396, 21);
            this.doubleClickAction.TabIndex = 14;
            this.doubleClickAction.SelectedIndexChanged += new System.EventHandler(this.doubleClickAction_SelectedIndexChanged);
            this.doubleClickAction.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            this.doubleClickAction.Click += new System.EventHandler(this.doubleClickAction_Click);
            //
            // configureShellExt
            //
            this.configureShellExt.Location = new System.Drawing.Point(13, 154);
            this.configureShellExt.Name = "configureShellExt";
            this.configureShellExt.Size = new System.Drawing.Size(146, 13);
            this.configureShellExt.TabIndex = 13;
            this.configureShellExt.TabStop = true;
            this.configureShellExt.Text = "Configure Context Menu";
            this.toolTip1.SetToolTip(this.configureShellExt, "Open Advanced Shell Extensions management console");
            this.configureShellExt.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.configureShellExt_LinkClicked);
            //
            // integrateNpp
            //
            this.integrateNpp.Location = new System.Drawing.Point(235, 154);
            this.integrateNpp.Name = "integrateNpp";
            this.integrateNpp.Size = new System.Drawing.Size(165, 19);
            this.integrateNpp.TabIndex = 13;
            this.integrateNpp.TabStop = true;
            this.integrateNpp.Text = "Notepad++ Integration";
            this.integrateNpp.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.integrateNpp_LinkClicked);
            //
            // integrateVS
            //
            this.integrateVS.Location = new System.Drawing.Point(13, 183);
            this.integrateVS.Name = "integrateVS";
            this.integrateVS.Size = new System.Drawing.Size(134, 19);
            this.integrateVS.TabIndex = 13;
            this.integrateVS.TabStop = true;
            this.integrateVS.Text = "Visual Studio Integration";
            this.toolTip1.SetToolTip(this.integrateVS, "Open VS integration console");
            this.integrateVS.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.integrateVS_LinkClicked);
            //
            // groupBox4
            //
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.tabControl2);
            this.groupBox4.Location = new System.Drawing.Point(198, 213);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(224, 0);
            this.groupBox4.TabIndex = 12;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Explorer context menu:";
            this.groupBox4.Visible = false;
            //
            // tabControl2
            //
            this.tabControl2.Controls.Add(this.tabPage4);
            this.tabControl2.Location = new System.Drawing.Point(56, 29);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(232, 82);
            this.tabControl2.TabIndex = 14;
            this.tabControl2.Visible = false;
            //
            // tabPage4
            //
            this.tabPage4.Controls.Add(this.checkedListBox1);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(224, 56);
            this.tabPage4.TabIndex = 0;
            this.tabPage4.Text = "Simplified Context Menu";
            //
            // checkedListBox1
            //
            this.checkedListBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBox1.CausesValidation = false;
            this.checkedListBox1.CheckOnClick = true;
            this.checkedListBox1.Location = new System.Drawing.Point(3, 3);
            this.checkedListBox1.Name = "checkedListBox1";
            this.checkedListBox1.Size = new System.Drawing.Size(215, 34);
            this.checkedListBox1.TabIndex = 10;
            this.toolTip1.SetToolTip(this.checkedListBox1, "Select menu items you wnt to appear in the Explorer context menu");
            this.checkedListBox1.Click += new System.EventHandler(this.checkedListBox1_Click);
            this.checkedListBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.checkedListBox1_KeyPress);
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.iconPictureBox);
            this.groupBox1.Controls.Add(this.changeIconButton);
            this.groupBox1.Controls.Add(this.updateNuGetBtn);
            this.groupBox1.Controls.Add(this.repareBtn);
            this.groupBox1.Controls.Add(this.deactivateBtn);
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(419, 88);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Active CS-Script installation";
            //
            // iconPictureBox
            //
            this.iconPictureBox.Location = new System.Drawing.Point(9, 46);
            this.iconPictureBox.Name = "iconPictureBox";
            this.iconPictureBox.Size = new System.Drawing.Size(41, 36);
            this.iconPictureBox.TabIndex = 7;
            this.iconPictureBox.TabStop = false;
            //
            // changeIconButton
            //
            this.changeIconButton.Location = new System.Drawing.Point(59, 52);
            this.changeIconButton.Name = "changeIconButton";
            this.changeIconButton.Size = new System.Drawing.Size(79, 23);
            this.changeIconButton.TabIndex = 6;
            this.changeIconButton.Text = "Change Icon";
            this.changeIconButton.Visible = false;
            this.changeIconButton.Click += new System.EventHandler(this.changeIconButton_Click);
            //
            // updateNuGetBtn
            //
            this.updateNuGetBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.updateNuGetBtn.Location = new System.Drawing.Point(160, 51);
            this.updateNuGetBtn.Name = "updateNuGetBtn";
            this.updateNuGetBtn.Size = new System.Drawing.Size(93, 24);
            this.updateNuGetBtn.TabIndex = 5;
            this.updateNuGetBtn.Text = "Update Nuget";
            this.toolTip1.SetToolTip(this.updateNuGetBtn, "Update nugetexe local copy.\r\n-------------------------------------------\r\nCheck N" +
        "uGet.olg for the latest nuget.exe version.\r\nDownload and replace the local copy " +
        "if the update is found.");
            this.updateNuGetBtn.Click += new System.EventHandler(this.updateNuGetBtn_Click);
            //
            // repareBtn
            //
            this.repareBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.repareBtn.Location = new System.Drawing.Point(259, 51);
            this.repareBtn.Name = "repareBtn";
            this.repareBtn.Size = new System.Drawing.Size(72, 24);
            this.repareBtn.TabIndex = 5;
            this.repareBtn.Text = "Repair";
            this.toolTip1.SetToolTip(this.repareBtn, "Update all CS-Script settings with the current values");
            this.repareBtn.Click += new System.EventHandler(this.repareBtn_Click);
            //
            // deactivateBtn
            //
            this.deactivateBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.deactivateBtn.Location = new System.Drawing.Point(337, 51);
            this.deactivateBtn.Name = "deactivateBtn";
            this.deactivateBtn.Size = new System.Drawing.Size(72, 24);
            this.deactivateBtn.TabIndex = 5;
            this.deactivateBtn.Text = "Deactivate";
            this.toolTip1.SetToolTip(this.deactivateBtn, "Deactivate current CS-Script installation");
            this.deactivateBtn.Click += new System.EventHandler(this.deactivateBtn_Click);
            //
            // textBox1
            //
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(95, 18);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(314, 20);
            this.textBox1.TabIndex = 4;
            //
            // label2
            //
            this.label2.Location = new System.Drawing.Point(120, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Target CLR version:";
            this.label2.Visible = false;
            //
            // label1
            //
            this.label1.Location = new System.Drawing.Point(6, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Home directiory:";
            //
            // comboBox1
            //
            this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox1.Location = new System.Drawing.Point(276, 92);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(133, 21);
            this.comboBox1.TabIndex = 2;
            this.comboBox1.Text = "<not selected>";
            this.toolTip1.SetToolTip(this.comboBox1, "Select target CLR version");
            this.comboBox1.Visible = false;
            this.comboBox1.TextChanged += new System.EventHandler(this.comboBox1_TextChanged);
            //
            // label4
            //
            this.label4.Location = new System.Drawing.Point(10, 103);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(147, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Open (double-click action):";
            //
            // tabPage2
            //
            this.tabPage2.Controls.Add(this.manageSearchDirs);
            this.tabPage2.Controls.Add(this.propertyGrid1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(425, 217);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Runtime options";
            //
            // manageSearchDirs
            //
            this.manageSearchDirs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.manageSearchDirs.Location = new System.Drawing.Point(3, 189);
            this.manageSearchDirs.Name = "manageSearchDirs";
            this.manageSearchDirs.Size = new System.Drawing.Size(250, 23);
            this.manageSearchDirs.TabIndex = 15;
            this.manageSearchDirs.TabStop = true;
            this.manageSearchDirs.Text = "Manage SearchDirs (probing directories)";
            this.toolTip1.SetToolTip(this.manageSearchDirs, "Open Search Directories management console");
            this.manageSearchDirs.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.manageSearchDirs_LinkClicked);
            //
            // propertyGrid1
            //
            this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propertyGrid1.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(425, 177);
            this.propertyGrid1.TabIndex = 1;
            this.propertyGrid1.ToolbarVisible = false;
            this.propertyGrid1.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged);
            //
            // tabPage3
            //
            this.tabPage3.Controls.Add(this.pictureBox1);
            this.tabPage3.Controls.Add(this.donateBtn);
            this.tabPage3.Controls.Add(this.textBox4);
            this.tabPage3.Controls.Add(this.groupBox3);
            this.tabPage3.Controls.Add(this.button5);
            this.tabPage3.Controls.Add(this.textBox3);
            this.tabPage3.Controls.Add(this.label6);
            this.tabPage3.Controls.Add(this.linkLabel2);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(425, 217);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "About";
            //
            // pictureBox1
            //
            this.pictureBox1.Location = new System.Drawing.Point(336, 13);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(75, 75);
            this.pictureBox1.TabIndex = 10;
            this.pictureBox1.TabStop = false;
            //
            // donateBtn
            //
            this.donateBtn.Location = new System.Drawing.Point(336, 131);
            this.donateBtn.Name = "donateBtn";
            this.donateBtn.Size = new System.Drawing.Size(66, 64);
            this.donateBtn.TabIndex = 9;
            this.donateBtn.Text = "Donate";
            this.donateBtn.Click += new System.EventHandler(this.donateBtn_Click_1);
            //
            // textBox4
            //
            this.textBox4.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox4.Location = new System.Drawing.Point(8, 17);
            this.textBox4.Multiline = true;
            this.textBox4.Name = "textBox4";
            this.textBox4.ReadOnly = true;
            this.textBox4.Size = new System.Drawing.Size(321, 73);
            this.textBox4.TabIndex = 7;
            this.textBox4.Text = "CS-Script\r\n\r\nC# script execution engine. \r\n\r\nCopyright (C) 2004-2019 Oleg Shilo.";
            //
            // groupBox3
            //
            this.groupBox3.Controls.Add(this.linkLabel11);
            this.groupBox3.Controls.Add(this.linkLabel9);
            this.groupBox3.Controls.Add(this.linkLabel8);
            this.groupBox3.Controls.Add(this.linkLabel7);
            this.groupBox3.Controls.Add(this.linkLabel6);
            this.groupBox3.Location = new System.Drawing.Point(10, 134);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(280, 58);
            this.groupBox3.TabIndex = 6;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Resources";
            //
            // linkLabel11
            //
            this.linkLabel11.Location = new System.Drawing.Point(8, 26);
            this.linkLabel11.Name = "linkLabel11";
            this.linkLabel11.Size = new System.Drawing.Size(121, 20);
            this.linkLabel11.TabIndex = 1;
            this.linkLabel11.TabStop = true;
            this.linkLabel11.Tag = "https://github.com/oleg-shilo/cs-script/issues";
            this.linkLabel11.Text = "Support/Feedback";
            this.linkLabel11.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel_LinkClicked);
            //
            // linkLabel9
            //
            this.linkLabel9.Location = new System.Drawing.Point(160, 3);
            this.linkLabel9.Name = "linkLabel9";
            this.linkLabel9.Size = new System.Drawing.Size(187, 13);
            this.linkLabel9.TabIndex = 0;
            this.linkLabel9.TabStop = true;
            this.linkLabel9.Tag = "http://www.csscript.net/Samples.html";
            this.linkLabel9.Text = "Online Samples";
            this.linkLabel9.Visible = false;
            this.linkLabel9.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel_LinkClicked);
            //
            // linkLabel8
            //
            this.linkLabel8.Location = new System.Drawing.Point(135, 29);
            this.linkLabel8.Name = "linkLabel8";
            this.linkLabel8.Size = new System.Drawing.Size(116, 13);
            this.linkLabel8.TabIndex = 0;
            this.linkLabel8.TabStop = true;
            this.linkLabel8.Tag = "https://github.com/oleg-shilo/cs-script/wiki";
            this.linkLabel8.Text = "Online Documentation";
            this.linkLabel8.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel_LinkClicked);
            //
            // linkLabel7
            //
            this.linkLabel7.Location = new System.Drawing.Point(202, 16);
            this.linkLabel7.Name = "linkLabel7";
            this.linkLabel7.Size = new System.Drawing.Size(113, 13);
            this.linkLabel7.TabIndex = 0;
            this.linkLabel7.TabStop = true;
            this.linkLabel7.Tag = "mailto:csscript.support@gmail.com?subject=Feedback";
            this.linkLabel7.Text = "CSScriptLibrary Help";
            this.linkLabel7.Visible = false;
            this.linkLabel7.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel6_LinkClicked);
            //
            // linkLabel6
            //
            this.linkLabel6.Location = new System.Drawing.Point(87, 3);
            this.linkLabel6.Name = "linkLabel6";
            this.linkLabel6.Size = new System.Drawing.Size(187, 13);
            this.linkLabel6.TabIndex = 0;
            this.linkLabel6.TabStop = true;
            this.linkLabel6.Tag = "mailto:csscript.support@gmail.com?subject=Feedback";
            this.linkLabel6.Text = "CS-Script Help";
            this.linkLabel6.Visible = false;
            this.linkLabel6.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel7_LinkClicked);
            //
            // button5
            //
            this.button5.Location = new System.Drawing.Point(215, 93);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(75, 23);
            this.button5.TabIndex = 5;
            this.button5.Tag = "";
            this.button5.Text = "Update";
            this.toolTip1.SetToolTip(this.button5, "Check for update");
            this.button5.Click += new System.EventHandler(this.button5_Click);
            //
            // textBox3
            //
            this.textBox3.Location = new System.Drawing.Point(107, 96);
            this.textBox3.Name = "textBox3";
            this.textBox3.ReadOnly = true;
            this.textBox3.Size = new System.Drawing.Size(98, 20);
            this.textBox3.TabIndex = 4;
            //
            // label6
            //
            this.label6.Location = new System.Drawing.Point(7, 98);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(94, 18);
            this.label6.TabIndex = 3;
            this.label6.Text = "Current version:";
            //
            // linkLabel2
            //
            this.linkLabel2.Location = new System.Drawing.Point(328, 93);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(94, 18);
            this.linkLabel2.TabIndex = 0;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Tag = "http://www.cs-script.net";
            this.linkLabel2.Text = "www.cs-script.net";
            this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel_LinkClicked);
            //
            // closeBtn
            //
            this.closeBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.closeBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeBtn.Location = new System.Drawing.Point(178, 261);
            this.closeBtn.Name = "closeBtn";
            this.closeBtn.Size = new System.Drawing.Size(75, 23);
            this.closeBtn.TabIndex = 2;
            this.closeBtn.Text = "&Close";
            this.closeBtn.Click += new System.EventHandler(this.button1_Click);
            //
            // button2
            //
            this.button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.button2.Enabled = false;
            this.button2.Location = new System.Drawing.Point(97, 261);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "&Ok";
            this.button2.Click += new System.EventHandler(this.OkButton_Click);
            //
            // button3
            //
            this.button3.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.button3.Enabled = false;
            this.button3.Location = new System.Drawing.Point(259, 261);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 2;
            this.button3.Text = "&Apply";
            this.button3.Click += new System.EventHandler(this.ApplyButton_Click);
            //
            // ConfigForm
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.closeBtn;
            this.ClientSize = new System.Drawing.Size(433, 298);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.closeBtn);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(2000, 1000);
            this.MinimumSize = new System.Drawing.Size(416, 300);
            this.Name = "ConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CS-Script Configuration";
            this.Load += new System.EventHandler(this.ConfigForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.tabControl2.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.iconPictureBox)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        static void InitEnvVars()
        {
#if DEBUG
            if (Environment.GetEnvironmentVariable("CSScriptDevPC") == "TRUE" && Environment.GetEnvironmentVariable("CSScriptRuntime") == null) //we are under visual studio
            {
                Environment.SetEnvironmentVariable("CSScriptRuntime", "2.8.0.0");
                Environment.SetEnvironmentVariable("CSScriptRuntimeLocation", @"E:\cs-script\cscs.exe");
            }

#endif
            if (Environment.GetEnvironmentVariable("CSScriptRuntime") == null)
            {
                string version = Assembly.LoadFrom(GetCSSLibPath()).GetName().Version.ToString();
                Environment.SetEnvironmentVariable("CSScriptRuntime", version);
            }
        }

        static Assembly ResolveAsm(object sender, ResolveEventArgs args)
        {
            Assembly retval = null;

            if (args.Name.StartsWith("CSScriptLibrary"))
            {
                string cssLib = GetCSSLibPath();

                try
                {
                    if (cssLib != null)
                        retval = Assembly.LoadFrom(cssLib);
                }
                catch { }
            }

            return retval;
        }

        static string GetCSSLibPath()
        {
            string cssRootDir = CSScriptInstaller.GetEnvironmentVariable("CSSCRIPT_DIR");

            if (cssRootDir == null)
            {
                string twoDirsUp = Path.GetDirectoryName(
                                       Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));

                if (File.Exists(Path.Combine(twoDirsUp, "css_config.exe")))
                    cssRootDir = twoDirsUp;
            }

            string cssLib = Path.Combine(cssRootDir, @"Lib\CSScriptLibrary.dll");

            try
            {
                return cssLib;
            }
            catch { }
            return null;
        }

        // static internal bool IsAdmin()
        // {
        //     WindowsIdentity id = WindowsIdentity.GetCurrent();
        //     WindowsPrincipal p = new WindowsPrincipal(id);
        //     return p.IsInRole(WindowsBuiltInRole.Administrator);
        // }

        [STAThread]
        static public void Main(string[] args)
        {
            // Debug.Assert(false);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveAsm);
            main(args);
        }

        static public void main(string[] args)
        {
            // if (!IsAdmin())
            // {
            //     MessageBox.Show("You must have administrative privileges to run this application.", "CS-Script");
            //     return;
            // }

            if (Environment.Version.Major >= 2)
                typeof(Application).GetMethod("EnableVisualStyles").Invoke(null, new object[0]);

            InitEnvVars();

            //System.Diagnostics.Debug.Assert(false);

            engineAsmName = Environment.GetEnvironmentVariable("CSScriptRuntimeLocation"); //name should be obtained here (it warranties that the calling assembly is the script engine but not the config.csc)

            if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
            {
                Console.WriteLine("Usage: cscscript config [<options>]...\nThis script displays configures CS-Script or displays the configuration console.\n" +
                                  "Options:\n" +
                                  " /quiet | /q - Quiet mode, no user interaction\n" +
                                  " /nogui | /ng - No GUI mode" +
                                  " /new - do not reuse the settings of the CS-Script if already installed\n");
            }
            else
            {
                try
                {
                    bool noGUI = false;
                    bool quiet = false;
                    bool update = true;

                    foreach (string arg in args)
                        if (arg.ToLower() == "/nogui" || arg.ToLower() == "/ng")
                            noGUI = true;
                        else if (arg.ToLower() == "/quiet" || arg.ToLower() == "/q")
                            quiet = true;
                        else if (arg.ToLower() == "/new")
                            update = false;

                    if (noGUI)
                    {
                        bool alreadyIstalled = CSScriptInstaller.IsInstalled();
                        new CSScriptInstaller(quiet, update);
                        if (!alreadyIstalled)
                        {
                            if (noGUI)
                                Console.WriteLine("CS-Scrip has been installed");
                            else
                                MessageBox.Show("CS-Scrip has been installed");
                        }
                    }
                    else
                        Application.Run(new ConfigForm(quiet, update));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "CS-Script Configuration");
                }
            }
        }

        static string engineAsmName = "";

        private void OkButton_Click(object sender, EventArgs e)
        {
            Save();
            Close();
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void Save()
        {
            button2.Enabled = false;
            button3.Enabled = false;

            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                installer.ContextMenus[i].Enabled = (checkedListBox1.GetItemCheckState(i) == CheckState.Checked);
            }

            installer.doubleClickAction = doubleClickAction.Text;
            installer.targetCLRVersion.Version = comboBox1.Text;
            installer.Update();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ConfigValueChanged()
        {
            button2.Enabled = true;
            button3.Enabled = true;
        }

        private void checkedListBox1_Click(object sender, EventArgs e)
        {
            ConfigValueChanged();
        }

        private void checkedListBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 32) //space
                ConfigValueChanged();
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            ConfigValueChanged();
        }

        private void deactivateBtn_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == MessageBox.Show("You are about to delete all information about current CS-script installation from the system.\n" +
                "(If you need to activate the particular CS-Script installation in the future just run corresponding config.bat.)\n\n" +
                "Please press Ok if you want to deactivate the current installation and close configuration console.",
                "CS-Script Configuration", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2))
            {
                Cursor = Cursors.WaitCursor;

                //Visible = false;
                this.Enabled = false;
                Application.DoEvents();//to force repainting
                installer.UnInstall();
                ignoreDirtyOnClose = true;
                this.Close();
                Cursor = Cursors.Default;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            ConfigValueChanged();
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            ConfigValueChanged();
        }

        private void linkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            TryToStartProcess((sender as LinkLabel).Tag.ToString());
        }

        private void button5_Click(object sender, EventArgs e)
        {
            MessageBox.Show("In the elevated command prompt execute:\n\n" +
                "   'choco upgrade cs-script'", "CS-Script");
        }

        private void TryToStartProcess(string file)
        {
            try
            {
                Process.Start(file);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void linkLabel7_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string file = Path.Combine(CSScriptInstaller.GetEnvironmentVariable("CSSCRIPT_DIR"), @"Docs\Help\CSScript.chm");

            if (File.Exists(file))
                TryToStartProcess(file);
            else
                MessageBox.Show("Requested help file cannot be found.\nPlease make sure you have installed CS-Script documentation pack,\n" +
                    "which is expected to be in the " + Path.Combine(CSScriptInstaller.GetEnvironmentVariable("CSSCRIPT_DIR"), @"Docs" + " folder"),
                    "CS-Script Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void linkLabel6_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string file = Path.Combine(CSScriptInstaller.GetEnvironmentVariable("CSSCRIPT_DIR"), @"Docs\Help\CSScriptLibrary.chm");

            if (File.Exists(file))
                TryToStartProcess(file);
            else
                MessageBox.Show("Requested help file cannot be found.\nPlease make sure you have installed CS-Script documentation pack,\n" +
                    "which is expected to be in the " + Path.Combine(CSScriptInstaller.GetEnvironmentVariable("CSSCRIPT_DIR"), @"Docs" + " folder"),
                    "CS-Script Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        //cannot use FormClosing as it will not run on .NET2.0 where Form does not have FormClosing enevt but rather Closing event with different signature
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0010)
            {
                if (button3.Enabled && !ignoreDirtyOnClose) //there are some unsaved changes
                {
                    DialogResult response = MessageBox.Show("The configuration has changed.\nDo you want to save the changes?", "CS-Script Configuration", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                    if (response == DialogResult.Yes)
                        ApplyButton_Click(null, null);

                    if (response == DialogResult.Cancel)
                        return;
                }
            }

            base.WndProc(ref m);
        }

        bool initialised = false;

        void BringOnTop()
        {
            //this is the only way to bring the form on top after regsvr32.exe execution
            this.TopMost = true;
            Application.DoEvents();
            this.TopMost = false;
        }

        [DllImport("shell32.dll", EntryPoint = "ExtractIconA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr ExtractIcon(int hInst, string lpszExeFileName, int nIconIndex);

        void RerfreshFileIcon()
        {
            try
            {
                string iconSpec = CSScriptInstaller.GetScriptIcon();

                string[] parts = iconSpec.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                string file = parts[0];

                int index = 0;

                if (parts.Length > 1)
                    int.TryParse(parts[1], out index);

                IntPtr lIcon = ExtractIcon(0, file, index);
                iconPictureBox.Image = Icon.FromHandle(lIcon).ToBitmap();
            }
            catch { }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            SetForegroundWindow(this.Handle);
            SetActiveWindow(this.Handle);
            Environment.SetEnvironmentVariable("ConfigConsoleLoaded", "true"); //loosely coupled notification as the progress SplashScreen can be in another assembly

            //SplashScreen.HideSplash();

            if (CSScriptInstaller.IsLightVersion)
                tabControl2.TabPages.RemoveAt(0);

            initialised = true;

            RerfreshFileIcon();

            //repareBtn.Visible = !CSScriptInstaller.IsFileAssosiationOk();
            try
            {
                var resources = new ResourceManager("images", Assembly.GetExecutingAssembly());

                string imgFile = Path.Combine(CSScriptInstaller.GetEnvironmentVariable("CSSCRIPT_DIR"), @"Lib\Donate.png");

                if (File.Exists(imgFile))
                    donateBtn.BackgroundImage = Bitmap.FromFile(imgFile);
                else
                    donateBtn.BackgroundImage = (Bitmap)resources.GetObject("donate.png");

                //donateBtn.BackgroundImageLayout = ImageLayout.Center; //will fail under 1.1
                donateBtn.Width = donateBtn.BackgroundImage.Width + 5;
                donateBtn.Height = donateBtn.BackgroundImage.Height + 5;
                donateBtn.Text = "";

                imgFile = Path.Combine(CSScriptInstaller.GetEnvironmentVariable("CSSCRIPT_DIR"), @"Lib\css_logo_256x256.png");
                if (File.Exists(imgFile))
                    pictureBox1.Image = Image.FromFile(imgFile);
                else
                    pictureBox1.Image = (Bitmap)resources.GetObject("css_logo_256x256.png");
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            catch { }

            doubleClickAction.Items.Add(doubleClickRunAction);
            doubleClickAction.Items.Add(doubleClickOpenWithAction);
            doubleClickAction.Items.Add(doubleClickSysDefaultAction);

            string nppPath = CSScriptInstaller.GetNotepadPP();

            if (nppPath != null)
            {
                doubleClickAction.Items.Insert(0, "\"" + nppPath + "\" \"%1\"");
            }
        }

        static string doubleClickSysDefaultAction = "<System Default>";
        static string doubleClickRunAction = "<Run>";
        static string doubleClickOpenWithAction = "<Open with...>";

        private void integrateVS_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://marketplace.visualstudio.com/items?itemName=OlegShilo.CS-ScriptToolsVS2017");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void configureShellExt_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Shell-X");

            var cs_configDir = Path.Combine(configDir, "cs");
            var anyFile_configDir = Path.Combine(configDir, "[any]");

            bool isInstalled = Directory.Exists(anyFile_configDir);

            if (isInstalled)
            {
                try
                {
                    // Process.Start("shell-x", "-open -nogui"); // good but will trigger UAC prompt
                    Process.Start("explorer", "\"" + cs_configDir + "\"");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
            else
            {
                MessageBox.Show("It seems Shell Extension manager 'Shell-X' is not installed.\n" +
                    "If it is the case install it from Chocolatey");
            }
        }

        private void manageSearchDirs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string homeDir = CSScriptInstaller.GetEnvironmentVariable("CSSCRIPT_DIR");

            if (homeDir != null)
            {
                try
                {
                    using (CSSScript.SearchDirs f = new CSSScript.SearchDirs())
                    {
                        if (DialogResult.OK == f.ShowDialog())
                        {
                            //update from the file
                            installer.settings.SearchDirs = Settings.Load(installer.ConfigFile).SearchDirs;
                            this.propertyGrid1.SelectedObject = installer.settings;
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }

            BringOnTop();
        }

        private void repareBtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            Save();

            //repareBtn.Visible = !CSScriptInstaller.IsFileAssosiationOk();
            CSScriptInstaller.SetKeyValue("CsScript", "CheckShellExtensionsCompatibility", "true");
            this.Cursor = Cursors.Default;

            installer.Install();
            CSScriptInstaller.ResetCsDefaultProgram();
        }

        private void donateBtn_Click_1(object sender, EventArgs e)
        {
            TryToStartProcess("http://www.csscript.net/Donation.html");
        }

        private void doubleClickAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (doubleClickAction.SelectedItem != null)
            {
                string action = null;

                if (doubleClickAction.SelectedItem.ToString() == doubleClickRunAction)
                {
                    string homeDir = CSScriptInstaller.GetEnvironmentVariable("CSSCRIPT_DIR");
                    action = "\"" + homeDir + "\\cscs.exe\" \"%1\" %*";
                }
                else if (doubleClickAction.SelectedItem.ToString() == doubleClickSysDefaultAction)
                {
                    string originalType = CSScriptInstaller.GetKeyValue(".cs", "pre_css_default") as string;

                    if (originalType != null && originalType != "")
                    {
                        try
                        {
                            string originalCommand = CSScriptInstaller.GetKeyValue(originalType + @"\shell\open\command", "") as string;

                            if (originalCommand.Contains("%"))
                                action = originalCommand;
                            else
                                action = originalCommand + " \"%1\"";
                        }
                        catch { }
                    }
                    else
                        action = "";
                }
                else if (doubleClickAction.SelectedItem.ToString() == doubleClickOpenWithAction)
                {
                    using (OpenFileDialog dlg = new OpenFileDialog())
                    {
                        dlg.Title = "Choose the program you want to use to open scripts on double -clicking";
                        dlg.RestoreDirectory = true;
                        dlg.CheckFileExists = true;
                        dlg.Filter = "Applications|*.exe";

                        if (dlg.ShowDialog() == DialogResult.OK)
                            action = "\"" + dlg.FileName + "\" \"%1\" %*";
                        else
                            doubleClickAction.SelectedItem = null;
                    }
                }

                if (action != null)
                {
                    doubleClickAction.SelectedItem = null;

                    new Thread(delegate () //without this trick combobox does not update its text
                    {
                        Invoke((MethodInvoker)delegate ()
                        {
                            doubleClickAction.Text = action;
                        });
                    }).Start();
                }
            }
        }

        private void doubleClickAction_Click(object sender, EventArgs e)
        {
            //doubleClickAction.Text = "cscs.exe \"%1\"";
        }

        private void RestartExplorer_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CSScriptInstaller.RestartExplorer();
        }

        private void changeIconButton_Click(object sender, EventArgs e)
        {
            using (Form dialog = new SelectIconForm())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    RerfreshFileIcon();
            }
        }

        private void updateNuGetBtn_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("cmd.exe", "/K \"%CSSCRIPT_DIR%\\lib\\nuget.exe\" Update -self");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void integrateNpp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/oleg-shilo/cs-script.npp/");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void integrateST3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/oleg-shilo/cs-script-sublime");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }

    class CSScriptInstaller
    {
        public static string GetScriptIcon()
        {
            var icon = GetKeyValue(@"CsScript\DefaultIcon", "");//, GetEnvironmentVariable("CSSCRIPT_DIR") + "\\cscs.exe,0");
            return icon as string;
        }

        public static void RestartExplorer()
        {
            foreach (Process p in Process.GetProcessesByName("explorer"))
                p.Kill();

            Thread.Sleep(2000);

            if (Process.GetProcessesByName("explorer").Length == 0)
                Process.Start("explorer.exe");
        }

        public static bool IsFileAssosiationOk()
        {
            using (RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(".cs"))
            {
                if (regKey != null)
                {
                    object val = regKey.GetValue("");

                    if (val is string)
                        return val.ToString() == "CsScript";
                }
            }

            return false;
        }

        #region Advanced COM Shell Extensions

        static string ChooseDefaultProgramApp
        {
            get
            {
                if (GetEnvironmentVariable("CSSCRIPT_DIR") != null)
                    return Path.Combine(GetEnvironmentVariable("CSSCRIPT_DIR"), @"Lib\ShellExtensions\ChooseDefaultProgram.exe");
                else
                    return "";
            }
        }

        internal static void EnsureRoslynShadowCopy()
        {
            if (!File.Exists(RoslynShadowDir))
            {
                Directory.CreateDirectory(RoslynShadowDir); //Directory.GetDirectories needs it to exist
                foreach (Process p in Process.GetProcessesByName("VBCSCompiler"))
                    try
                    {
                        p.Kill(); //unlock potentially locked folder
                    }
                    catch { }

                foreach (string roslyn_version_dir in Directory.GetDirectories(Path.GetDirectoryName(RoslynShadowDir)))
                    try
                    {
                        Directory.Delete(roslyn_version_dir); //try to delete old versions
                    }
                    catch { }

                CopyAllFiles(RoslynDistroDir, RoslynShadowDir);
            }
        }

        static void CopyAllFiles(string srcRootDir, string destRootDir)
        {
            foreach (string srcFile in Directory.GetFiles(srcRootDir, "*", SearchOption.AllDirectories))
            {
                string rlativePath = srcFile.Substring(srcRootDir.Length + 1); //+1 to cut off dir delimiter
                string destFile = Path.Combine(destRootDir, rlativePath);
                string destDir = Path.GetDirectoryName(destFile);

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                if (File.Exists(destFile))
                {
                    try
                    {
                        File.Copy(srcFile, destFile, true);
                    }
                    catch { } //shell extension file can be locked
                }
                else
                    File.Copy(srcFile, destFile);
            }
        }

        static string RoslynDistroDir
        {
            get
            {
                if (GetEnvironmentVariable("CSSCRIPT_DIR") != null)
                    return Path.Combine(GetEnvironmentVariable("CSSCRIPT_DIR"), @"Lib\Bin\Roslyn");
                else
                    return "";
            }
        }

        static string RoslynShadowDir
        {
            get
            {
                var commonAppDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                string retval = Path.Combine(commonAppDir, "CS-Script\\Roslyn\\" + Environment.GetEnvironmentVariable("CSScriptRuntime"));

                return retval;
            }
        }

        public static bool IsWin8OrHigher
        {
            get
            {
                return Environment.OSVersion.Version >= Version.Parse("6.2");
            }
        }

        public static void ResetCsDefaultProgram()
        {
            RunAppSafe(ChooseDefaultProgramApp, "-e:.cs -prog:CsScript -hash:pA2ZT35FDyE=");
        }

        #endregion Advanced COM Shell Extensions

        public static string GetExecutingEngineDir()
        {
            var retval = Path.GetDirectoryName(Environment.GetEnvironmentVariable("CSScriptRuntimeLocation"));

            if (retval == null)
            {
                //Then it is a standalone configuration console and the CS-S Runtime location is the
                //the first parent directory with the cscs.exe/csws.exe files

                string parent = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                while (parent != null && Directory.Exists(parent))
                {
                    if (Directory.GetFiles(parent, "cscs.exe").Length != 0)
                    {
                        retval = parent;
                        break;
                    }

                    parent = Path.GetDirectoryName(parent);
                }
            }

            return retval;
        }

        static public bool IsLightVersion
        {
            get
            {
                string vsIntegrationScript = Path.Combine(GetEnvironmentVariable("CSSCRIPT_DIR"), @"Lib\VSIntegration.cs");
                return !File.Exists(vsIntegrationScript);
            }
        }

        public CSScriptInstaller(bool quiet, bool update)
        {
            //System.Diagnostics.Debug.Assert(false);
            this.quiet = quiet;

            //analyse previous installations
            bool oldVersionInstalled = CSScriptInstaller.IsInstalled() && update;

            ArrayList oldMenus = new ArrayList();
            string oldDblClickAction = "";
            string oldClrVersion = "";
            string oldHomeDir = "";
            string oldConfigFile = "";

            bool runningAsScript = Environment.GetEnvironmentVariable("CSScriptRuntime") != null &&
                                   Environment.GetEnvironmentVariable("CSScriptDebugging") == null;

            bool preventMigration = false;
#if CSS_PROJECT
            preventMigration = true; //we are running the script under VS
#endif
            if (oldVersionInstalled)
            {
                oldHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");

                string executingEngineDir = CSScriptInstaller.GetExecutingEngineDir();

                if (executingEngineDir == null || Path.GetFullPath(oldHomeDir) == Path.GetFullPath(executingEngineDir))
                {
                    oldVersionInstalled = false;
                }
                else
                {
                    GetCurrentConfig(oldMenus, ref oldDblClickAction);

                    //oldClrVersion = GetTargetCLRVersion(Path.Combine(oldHomeDir, "cscs.exe.config")); //CLR versions are not supported any more
                }
            }

            try
            {
                using (RegistryKey envVars = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", true)) { }

                using (RegistryKey csReadOnly = Registry.ClassesRoot.OpenSubKey(".cs", false))
                    if (csReadOnly != null)
                        using (RegistryKey csWritable = Registry.ClassesRoot.OpenSubKey(".cs", true)) { }
            }
            catch
            {
                restrictedMode = true;
            }

            string scHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");

            if (scHomeDir == null)
            {
                if (restrictedMode)
                    throw new Exception("CS-Script cannot be installed on this PC.\nYour current login does not allow you to change the system configuration.\n" +
                                        "Please login as a different user (with higher security level) and start the configuraion console again.");

                Install();

                string editor = GetNotepadPP();

                if (editor != null)
                    doubleClickAction = "\"" + editor + "\" \"%1\""; //default value
                else
                    doubleClickAction = ConfigForm.DoubleClickNotepadAction;

                scHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");
                MergeLocalIncluds(scHomeDir);
            }
            else if (string.Compare(GetExecutingEngineDir(), GetEnvironmentVariable("CSSCRIPT_DIR"), true) != 0 && !preventMigration)
            {
                bool deadInstallationDetected = false;

                try
                {
                    if (!Directory.Exists(GetEnvironmentVariable("CSSCRIPT_DIR")))
                        deadInstallationDetected = true;
                }
                catch { }

                string msg = "This configuration console corresponds to the CS-Script engine located in '" + GetExecutingEngineDir() + "'.\n" +
                    "However another copy of the CS-Script engine is currently installed on this computer ('" + GetEnvironmentVariable("CSSCRIPT_DIR") + "').\n\n";

                if (restrictedMode)
                    throw new Exception(msg + "Please run the configuration console from correct location.");

                if (quiet || deadInstallationDetected ||
                    DialogResult.OK == ShowMessageBox(msg +
                    "Please, press Ok if you want to activate the script engine from '" + GetExecutingEngineDir() + "' instead of the currently installed one.",
                    "CS-Script Configuration", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2))
                {
                    if (!quiet) SplashScreen.ShowSplash("CS-Script", "Please wait while CS-Script is being configured...");

                    oldConfigFile = ConfigFile;

                    UnInstall();
                    Install();

                    scHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");
                    MergeLocalIncluds(scHomeDir);
                }
                else
                    throw new Exception("Operation canceled by user.");
            }

            if (File.Exists(ConfigFile)) //installed
            {
                if (oldConfigFile != "" && File.Exists(oldConfigFile)) //previous installation config is available
                    settings = Settings.Load(oldConfigFile);
                else
                    settings = Settings.Load(ConfigFile);

                if (update)
                {
                    if (settings.DefaultArguments.IndexOf("/sconfig") == -1)
                    {
                        settings.DefaultArguments += " /sconfig"; //do it manualy for now but in future insure all new default DefaultArgs are added
                        settings.Save(ConfigFile);
                    }
                }
            }
            else
            {
                settings = new Settings();
                settings.Save(ConfigFile);

                if (File.Exists(Path.Combine(scHomeDir, @"Lib\clearTemp.cs")))
                    settings.CleanupShellCommand = "csws.exe clearTemp.cs";

                if (IsLightVersion)
                {
                    settings.UseAlternativeCompiler = "";
                }
                else
                {
                    if (File.Exists(Path.Combine(scHomeDir, @"Lib\CSSRoslynProvider.dll")))
                        settings.UseAlternativeCompiler = @"%CSSCRIPT_DIR%\Lib\CSSRoslynProvider.dll";

                    try
                    {
                        Version availableVer = GetHighestNetVersion();

                        if (File.Exists(Path.Combine(scHomeDir, @"Lib\CSSRoslynProvider.dll"))
                            && availableVer.Major >= 3
                            && availableVer.Minor >= 5)
                        {
                            settings.UseAlternativeCompiler = @"%CSSCRIPT_DIR%\Lib\CSSRoslynProvider.dll";
                        }
                    }
                    catch { }
                }
            }

            //CLR versions are not supported any more
            //if (!File.Exists(Path.Combine(HomeDir, "cscs.exe.config")))
            //{
            //CreateDefaultConfig(Path.Combine(HomeDir, "cscs.exe.config"));
            //CreateDefaultConfig(Path.Combine(HomeDir, "csws.exe.config"));
            //}
            GetCurrentConfig(this.contextMenus, ref doubleClickAction);
            targetCLRVersion = new CLRVersion(GetTargetCLRVersion(null));

            if (oldVersionInstalled)
            {
                for (int i = 0; i < oldMenus.Count; i++)
                    ((ContextMenuInfo)this.contextMenus[i]).Enabled = ((ContextMenuInfo)oldMenus[i]).Enabled;

                this.doubleClickAction = oldDblClickAction;
                this.targetCLRVersion.Version = oldClrVersion;

                UpdateFromInstallation(settings, oldHomeDir);

                Update();
                CSScriptInstaller.justInstalled = false;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindow(string ClassName, string WindowText);

        static DialogResult ShowMessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
        {
            try
            {
                //it is very easy to get an invalid handle
                IntPtr topMost = FindWindow(null, "CS-Script Config - Splash Screen");
                return MessageBox.Show(new WindowWrapper(topMost), text, caption, buttons, icon, defaultButton);
            }
            catch
            {
                return MessageBox.Show(text, caption, buttons, icon, defaultButton);
            }
        }

        public class WindowWrapper : System.Windows.Forms.IWin32Window
        {
            public WindowWrapper(IntPtr handle)
            {
                _hwnd = handle;
            }

            public IntPtr Handle
            {
                get { return _hwnd; }
            }

            private IntPtr _hwnd;
        }

        static string localIncludesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CS-Script\\inc");

        static void MergeLocalIncluds(string scHomeDir)
        {
            string cssIncludesDir = Path.Combine(scHomeDir, "inc");

            if (Directory.Exists(cssIncludesDir))
            {
                if (!Directory.Exists(localIncludesDir))
                    Directory.CreateDirectory(localIncludesDir);

                foreach (string srcFile in Directory.GetFiles(cssIncludesDir))
                {
                    string destFile = Path.Combine(localIncludesDir, Path.GetFileName(srcFile));

                    if (!File.Exists(destFile))
                        File.Copy(srcFile, destFile);
                }
            }
        }

        public static string GetNotepadPP()
        {
            string nppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Notepad++\notepad++.exe");

            if (File.Exists(nppPath))
            {
                return nppPath;
            }
            else
            {
                nppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Notepad++\notepad++.exe");
                if (File.Exists(nppPath))
                    return nppPath;
            }

            return null;
        }

        static public Version GetHighestNetVersion()
        {
            string netDir = Path.GetDirectoryName(Path.GetDirectoryName("".GetType().Assembly.Location));
            Version retval = Environment.Version;

            foreach (string name in Directory.GetDirectories(netDir, "v*.*"))
            {
                try
                {
                    Version ver = new Version(new DirectoryInfo(name).Name.Replace("v", ""));

                    if (ver > retval)
                        retval = ver;
                }
                catch { }
            }

            return retval;
        }

        private void GetCurrentConfig(ArrayList menus, ref string doubleClickAction)
        {
            string editor = GetNotepadPP();

            if (editor != null)
                doubleClickAction = "\"" + editor + "\" \"%1\"";
            else

                //doubleClickAction = "notepad.exe \"%1\"";
                doubleClickAction = ConfigForm.DoubleClickNotepadAction;

            string scHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");

            //collect IDE info
            ArrayList availableIDE = new ArrayList();

            // availableIDE.AddRange(VS90.Script.VS90IDE.GetAvailableIDE()); //Visual Studio 2008
            // availableIDE.AddRange(VS80.Script.VS80IDE.GetAvailableIDE()); //Visual Studio 2005

            // if ((ideInfo = SD.Script.SharpDevelopIDE.GetAvailableIDE()) != null) //"SharpDevelop"
            // availableIDE.Add(ideInfo);
            // if ((ideInfo = CLRDebugger.Script.CLRDE.GetAvailableIDE()) != null) //Visual Studio 2003
            // availableIDE.Add(ideInfo);

            //populate contextMenu list
            ContextMenuInfo newDocument;
            RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(@".cs\ShellNew");

            if (regKey != null && regKey.GetValue("FileName") != null)
                newDocument = new ContextMenuInfo("New", "\t\t\t- Creates new C# script file", "", true, true);
            else
                newDocument = new ContextMenuInfo("New", "\t\t\t- Creates new C# script file", "", false, true);

            menus.Add(newDocument);
            menus.Add(new ContextMenuInfo("Run", "\t\t\t- Runs .cs file as a script.", "\"" + scHomeDir + "\\cscs.exe\" \"%1\" %*", false));
            menus.Add(new ContextMenuInfo("Verify script", "\t\t- Check C# syntax", "\"" + scHomeDir + "\\cscs.exe\" /c \"" + scHomeDir + "\\Lib\\verify.cs\" \"%1\"", false));
            menus.Add(new ContextMenuInfo("Debug script", "\t\t- Run script under the system debugger", "\"" + scHomeDir + "\\cscs.exe\" \"%1\" %* //x", false));
            menus.Add(new ContextMenuInfo("CF Build   ", "\t\t- Builds executable for PocketPC", "\"" + scHomeDir + "\\cscs.exe\" /c \"" + scHomeDir + "\\Lib\\cfbuild.cs\" \"%1\"", false));

            foreach (string[] info in availableIDE)
            {
                menus.Add(new ContextMenuInfo(info[0], info[1], info[2], false));
            }

            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"CsScript\shell\Open\command"))
            {
                string valueName = "";

                if (IsWin8OrHigher)
                    valueName = "App";

                if (key != null && key.GetValue(valueName) != null)
                {
                    doubleClickAction = key.GetValue(valueName).ToString();
                }
            }
        }

        public static bool justInstalled = false;

        public class ContextMenuInfo
        {
            public ContextMenuInfo(string name, string hint, string command, bool enabledDefault)
            {
                this.name = name;
                this.hint = hint;
                this.command = command;
                this.enabled = IsShellCmdEnabled(name, command);
                this.enabledDefault = enabledDefault;
            }

            public ContextMenuInfo(string name, string hint, string command, bool enabled, bool enabledDefault)
            {
                this.name = name;
                this.hint = hint;
                this.command = command;
                this.enabled = enabled;
                this.enabledDefault = enabledDefault;
            }

            public string name;
            public string hint;
            public string command;

            public bool Enabled
            {
                get { return enabled; }
                set { if (enabled != value) dirty = true; enabled = value; }
            }

            private bool enabled;
            public bool dirty = false;
            public bool enabledDefault;

            static bool IsShellCmdEnabled(string name, string command)
            {
                RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(@"CsScript\shell\" + name + @"\command");

                if (regKey != null && regKey.GetValue("") != null && regKey.GetValue("").ToString() == command)
                    return true;
                else
                    return false;
            }
        }

        public class CLRVersion
        {
            public CLRVersion(string version)
            {
                this.version = version;
            }

            public string Version
            {
                get { return version; }
                set { if (version != value) dirty = true; version = value; }
            }

            private string version;
            public bool dirty = false;
        }

        public ContextMenuInfo[] ContextMenus
        {
            get { return (ContextMenuInfo[])contextMenus.ToArray(typeof(ContextMenuInfo)); }
        }

        public ArrayList contextMenus = new ArrayList();
        public Settings settings;
        public string doubleClickAction = "";
        public bool restrictedMode = false;
        private bool quiet = false;

        static public bool IsInstalled()
        {
            return GetEnvironmentVariable("CSSCRIPT_DIR") != null;
        }

        internal static string UninstallShelExtensionsIfPresent()
        {
            //C:\ProgramData\CS-Script\ShellExtension\3.28.4.0\CS-Script
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"CS-Script\ShellExtension");
            var match = Directory.GetDirectories(root)
                .Select(x => new
                {
                    Version = new Version(Path.GetFileName(x)),
                    Path = x
                })
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            var deploymentDir = match != null ? match.Path : null;

            if (deploymentDir != null)
            {
                var x32Dll = Path.Combine(deploymentDir, @"CS-Script\ShellExt.cs.{25D84CB0-7345-11D3-A4A1-0080C8ECFED4}.dll");
                var x64Dll = Path.Combine(deploymentDir, @"CS-Script\ShellExt64.cs.{25D84CB0-7345-11D3-A4A1-0080C8ECFED4}.dll");

                var is64OS = Directory.Exists(Environment.ExpandEnvironmentVariables(@"%windir%\SysWOW64"));

                if (is64OS)
                {
                    RunAppSafe(Environment.ExpandEnvironmentVariables(@"%windir%\System32\regsvr32"), "/u /s \"" + x64Dll + "\"");
                    RunAppSafe(Environment.ExpandEnvironmentVariables(@"%windir%\SysWOW64\regsvr32"), "/u /s \"" + x32Dll + "\"");
                }
                else
                {
                    RunAppSafe(Environment.ExpandEnvironmentVariables(@"%windir%\System32\regsvr32"), "/u /s \"" + x32Dll + "\"");
                }
                return Path.Combine(deploymentDir, "CS-Script");
            }
            return null;
        }

        static void CopyToShellX(string sourceDir, string fileExtension)
        {
            var destDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                       @"shell-x\" + fileExtension);
            Directory.CreateDirectory(destDir);

            File.WriteAllText(Path.Combine(destDir, "01.separator"), "");
            File.WriteAllText(Path.Combine(destDir, "02.separator"), "");

            try
            {
                var resources = new ResourceManager("images", Assembly.GetExecutingAssembly());
                var logo = (Icon)resources.GetObject("css_logo.ico");

                using (var fs = new FileStream(Path.Combine(destDir, "01.CS-Script.ico"), FileMode.Create))
                    logo.Save(fs);
            }
            catch { }

            destDir = Path.Combine(destDir, "01.CS-Script");

            Directory.CreateDirectory(destDir);

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                // ignore com servers and install.bat and uninstall.bat
                if (!newPath.EndsWith("A4A1-0080C8ECFED4}.dll") && !newPath.EndsWith(".bat"))
                {
                    var destPath = newPath.Replace(sourceDir, destDir);
                    var newFilehLogicalName = Path.GetFileName(destPath).Split(".".ToCharArray(), 2).Last(); //00.Open with VS2017.cmd

                    if (Directory.GetFiles(Path.GetDirectoryName(destPath), "*" + newFilehLogicalName).Any())
                        continue; // do not override the existing file with the same logical name

                    File.Copy(newPath, destPath);
                }
            }
        }

        static public void ConfigureShellExtensions()
        {
            var localDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var oldConfigDir = UninstallShelExtensionsIfPresent();
            var newConfigDir = Path.Combine(localDir, @"..\ShellExtensions\Template\CS-Script");
            var fileExtension = "cs";

            if (oldConfigDir != null)
                CopyToShellX(oldConfigDir, fileExtension);
            else
                CopyToShellX(newConfigDir, fileExtension);
        }

        public void Install()
        {
            // System.Diagnostics.Debug.Assert(false);

            string path = "";

            using (RegistryKey envVars = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", true))
            {
                envVars.SetValue("CSSCRIPT_DIR", GetExecutingEngineDir());

                string nuGetCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CS-Script", "nuget");
                string includes = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CS-Script", "inc");
                envVars.SetValue("css_nuget", nuGetCache);
                envVars.SetValue("CSSCRIPT_INC", includes);
                envVars.SetValue("CSSCRIPT_ROSLYN", RoslynShadowDir);

                Environment.SetEnvironmentVariable("CSSCRIPT_DIR", GetExecutingEngineDir()); //for current process too
                path = RegGetValueExp(HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "Path");
            }

            path = AddToPath(@"%CSSCRIPT_DIR%", path);
            path = AddToPath(@"%CSSCRIPT_DIR%\lib", path);
            RegSetStrValue(HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "Path", path);

            int dwResult = 0;
            bool bResult = SendMessageTimeout((System.IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, 0, "Environment", SMTO_ABORTIFHUNG, 5000, dwResult);
            justInstalled = true;

            ConfigureShellExtensions();

            EnsureRoslynShadowCopy();
        }

        public void UnInstall()
        {
            string oldHomeDir = GetEnvironmentVariable("CSSCRIPT_DIR");

            string path = "";

            using (RegistryKey envVars = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", true))
            {
                path = RegGetValueExp(HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "Path");
                envVars.DeleteValue("CSSCRIPT_INC", false);
                envVars.DeleteValue("CSSCRIPT_ROSLYN", false);
                envVars.DeleteValue("CSSCRIPT_DIR", false);
                envVars.DeleteValue("CSSCRIPT_SHELLEX_DIR", false);
                envVars.DeleteValue("css_nuget", false);
                Environment.SetEnvironmentVariable("CSSCRIPT_SHELLEX_DIR", null);
            }

            path = RemoveFromPath(@"%CSSCRIPT_DIR%", path);
            path = RemoveFromPath(@"%CSSCRIPT_DIR%\lib", path);
            RegSetStrValue(HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "Path", path);

            DeleteKey(@".cs\ShellNew");
            DeleteKey(@".ccs");
            DeleteKey("CsExecutableScript");
            DeleteKey(".csx");

            //restore the original file type
            CopyKeyValue(".cs", "pre_css_default", "");
            CopyKeyValue(".cs", "pre_css_contenttype", "Content Type");

            DeleteKeyValue(".cs", "pre_css_default");
            DeleteKeyValue(".cs", "pre_css_contenttype");

            using (RegistryKey csKey = GetKey(".cs", true))
            {
                if (csKey.GetValue("OldDefault") != null)
                {
                    csKey.SetValue("", csKey.GetValue("OldDefault").ToString());
                }
            }

            int dwResult = 0;
            bool bResult = SendMessageTimeout((System.IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, 0, "Environment", SMTO_ABORTIFHUNG, 5000, dwResult);

            try
            {
                if (File.Exists(Path.Combine(GetExecutingEngineDir(), "css_config.xml")))
                    File.Delete(Path.Combine(GetExecutingEngineDir(), "css_config.xml"));

                //if (File.Exists(Path.Combine(GetExecutingEngineDir(), "cscs.exe.config")))
                //    File.Delete(Path.Combine(GetExecutingEngineDir(), "cscs.exe.config"));
                //if (File.Exists(Path.Combine(GetExecutingEngineDir(), "csws.exe.config")))
                //    File.Delete(Path.Combine(GetExecutingEngineDir(), "csws.exe.config"));
            }
            catch { }
        }

        static public string RunApp(string app, string args)
        {
            Trace.WriteLine(app + " " + args);

            Process myProcess = new Process();
            myProcess.StartInfo.FileName = app;
            myProcess.StartInfo.Arguments = args;
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.Start();

            StringBuilder sb = new StringBuilder();
            string line = null;

            while (null != (line = myProcess.StandardOutput.ReadLine()))
            {
                sb.Append(line + "\n");
                Console.WriteLine(line);
                Trace.WriteLine(line);
            }

            myProcess.WaitForExit();
            return sb.ToString();
        }

        static public string RunAppSafe(string app, string args)
        {
            try
            {
                return RunApp(app, args);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }

            return "";
        }

        public void Update()
        {
            settings.Save(ConfigFile);

            if (restrictedMode)
                return;

            //.cs

            if (GetKeyValue(".cs", "pre_css_default") == null)
            {
                //preserve the original file type
                CopyKeyValue(".cs", "", "pre_css_default");
                CopyKeyValue(".cs", "Content Type", "pre_css_contenttype");
            }

            SetKeyValue(".cs", "", "CsScript");
            SetKeyValue(".ccs", "", "CsScript");
            SetKeyValue(".ccs", "Content Type", "text/csscript");
            SetKeyValue(".ccs", "PerceivedType", "text");

            // ShellNew (this is a special type of ShellExtensions and it is always the first itm in the ContextMenus collection)
            if (ContextMenus[0].dirty)
            {
                if (ContextMenus[0].Enabled)
                {
                    SetKeyValue(@".cs\ShellNew", "FileName", GetEnvironmentVariable("CSSCRIPT_DIR") + @"\Lib\new_script.template");
                    SetKeyValue(".cs", "Content Type", "text/csscript");
                }
                else
                {
                    DeleteKey(@".cs\ShellNew");
                    SetKeyValue(".cs", "Content Type", "test/plain");
                }
            }

            //CsScript
            SetKeyValue("CsScript", "", "C# Script");

            if (GetKeyValue(@"CsScript\DefaultIcon", "") == null)
                SetKeyValue(@"CsScript\DefaultIcon", "", GetEnvironmentVariable("CSSCRIPT_DIR") + "\\cscs.exe,0");

            for (int i = 1; i < ContextMenus.Length; i++)
            {
                ContextMenuInfo info = ContextMenus[i];

                if (info.dirty)
                {
                    if (info.Enabled)
                        SetKeyValue(@"CsScript\Shell\" + info.name + @"\command", "", info.command);
                    else
                        DeleteKey(@"CsScript\Shell\" + info.name);

                    ContextMenus[i].dirty = false;
                }
            }

            if (doubleClickAction == "")
            {
                DeleteKey(@"CsScript\Shell\Open\command");
            }
            else
            {
                if (IsWin8OrHigher)
                {
                    SetKeyValue(@"CsScript\Shell\Open\command", "", "\"" + GetEnvironmentVariable("CSSCRIPT_DIR") + "\\Lib\\ShellExtensions\\CS-Script.exe\" \"%1\" %*");
                    SetKeyValue(@"CsScript\Shell\Open\command", "App", doubleClickAction);
                }
                else
                {
                    SetKeyValue(@"CsScript\Shell\Open\command", "", doubleClickAction);
                }
            }

            SetKeyValue(".csx", "", "CsExecutableScript");
            SetKeyValue("CsExecutableScript", "", "C# Executable Script");
            SetKeyValue(@"CsExecutableScript\DefaultIcon", "", GetEnvironmentVariable("CSSCRIPT_DIR") + "\\cscs.exe,0");
            SetKeyValue(@"CsExecutableScript\Shell\Open\command", "", "\"" + GetEnvironmentVariable("CSSCRIPT_DIR") + "\\cscs.exe\" \"%1\" %*");

            //CLR versions are not supported any more
            //update .configs with the version
            //if (this.targetCLRVersion.dirty)
            //{
            //SetTargetCLRVersion(this.targetCLRVersion.Version, "cscs.exe.config");
            //SetTargetCLRVersion(this.targetCLRVersion.Version, "csws.exe.config");
            //}
        }

        private RegistryKey GetKey(string name, bool writable)
        {
            RegistryKey key = Registry.ClassesRoot.OpenSubKey(name, writable);

            if (key == null)
            {
                Registry.ClassesRoot.CreateSubKey(name);
                key = Registry.ClassesRoot.OpenSubKey(name, writable);
            }

            return key;
        }

        public static object GetKeyValue(string name, string valueName)
        {
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(name, false))
            {
                if (key == null)
                    return null;
                else
                    return key.GetValue(valueName, null);
            }
        }

        private void DeleteKeyValue(string name, string valueName)
        {
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(name, true))
            {
                if (key != null)
                    key.DeleteValue(valueName, false);
            }
        }

        private void CopyKeyValue(string keyName, string srcValueName, string destValueName)
        {
            object value = GetKeyValue(keyName, srcValueName);

            if (value != null)
                SetKeyValue(keyName, destValueName, value);
        }

        static public void SetKeyValue(string keyName, string name, object value)
        {
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    Registry.ClassesRoot.CreateSubKey(keyName);
                    using (RegistryKey newKey = Registry.ClassesRoot.OpenSubKey(keyName, true))
                    {
                        newKey.SetValue(name, value);
                    }
                }
                else
                    key.SetValue(name, value);
            }
        }

        static bool KeyExists(string name)
        {
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(name, false))
            {
                return (key != null);
            }
        }

        static void DeleteKey(string name)
        {
            RegistryKey key = Registry.ClassesRoot.OpenSubKey(name);

            if (key != null)
            {
                key.Close();
                Registry.ClassesRoot.DeleteSubKeyTree(name);
            }
        }

        private void CreateDefaultConfig(string filename)
        {
            string configText =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<configuration>\r\n" +
                    "\t<startup>\r\n" +
                        "\t\t<supportedRuntime version=\"v2.0.50727\" />\r\n" +
                        "\t\t<supportedRuntime version=\"v1.1.4322\" />\r\n" +
                        "\t\t<supportedRuntime version=\"v2.0.50215\" />\r\n" +
                    "\t</startup>\r\n" +
                "</configuration>";

            using (StreamWriter sw = new StreamWriter(filename))
                sw.WriteLine(configText);
        }

        private void SetTargetCLRVersion(string version, string filename)
        {
            //Example:
            //<?xml version="1.0" encoding="utf-8"?>
            //<configuration>
            //  <startup>
            //  <supportedRuntime version="v2.0.50727" css_target="true" />
            //  <supportedRuntime version="v2.0.50727" />
            //  <supportedRuntime version="v1.1.4322" />
            //  <supportedRuntime version="v2.0.50215" />
            //  </startup>
            //</configuration>

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(HomeDir, filename));
            foreach (XmlElement elem in doc.GetElementsByTagName("startup"))
            {
                if (elem.HasChildNodes)
                {
                    if (((XmlElement)elem.FirstChild).GetAttribute("css_target") != "") //cs-script entry
                        elem.RemoveChild(elem.FirstChild);

                    if (version != "<default>")
                    {
                        XmlElement newElem = doc.CreateElement("supportedRuntime");
                        XmlAttribute newAttr;

                        newAttr = doc.CreateAttribute("version");
                        newAttr.Value = version;
                        newElem.Attributes.Append(newAttr);
                        newAttr = doc.CreateAttribute("css_target");
                        newAttr.Value = "true";
                        newElem.Attributes.Append(newAttr);

                        elem.InsertBefore(newElem, elem.FirstChild);
                    }
                }

                break;
            }

            doc.Save(Path.Combine(HomeDir, filename));
        }

        private string GetTargetCLRVersion(string appConfigFile)
        {
            string retval = "<default>";

            //XmlDocument doc = new XmlDocument();
            //try
            //{
            //    doc.Load(appConfigFile != null ? appConfigFile : Path.Combine(GetEnvironmentVariable("CSSCRIPT_DIR"), "cscs.exe.config"));
            //    foreach (XmlElement elem in doc.GetElementsByTagName("supportedRuntime"))
            //    {
            //        if (elem.GetAttribute("css_target") != "")
            //            retval = elem.GetAttribute("version");
            //        break;
            //    }
            //}
            //catch { }
            return retval;
        }

        public string ConfigFile
        {
            get
            {
                string homeDir = GetEnvironmentVariable("CSSCRIPT_DIR");
                return homeDir == null ? null : Path.Combine(homeDir, "css_config.xml");
            }
        }

        public string HomeDir
        {
            get
            {
                return GetEnvironmentVariable("CSSCRIPT_DIR");
            }
        }

        public CLRVersion targetCLRVersion = new CLRVersion("<default>");

        public string[] AvailableCLRVersions
        {
            get
            {
                ArrayList retval = new ArrayList();
                retval.Add("<default>");
                foreach (string dir in Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework")))
                {
                    if (File.Exists(Path.Combine(dir, "system.dll")))
                        retval.Add(dir.Substring(Path.GetDirectoryName(dir).Length).Replace("\\", ""));
                }

                return (string[])retval.ToArray(typeof(string));
            }
        }

        public static string GetEnvironmentVariable(string variable)
        {
            using (RegistryKey envVars = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", false))
            {
                object value = envVars.GetValue(variable);
                return (value == null) ? null : value.ToString();
            }
        }

        public static void SetEnvironmentVariable(string variable, string value)
        {
            using (RegistryKey envVars = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", true))
            {
                envVars.SetValue(variable, value);
            }
        }

        static private string AddToPath(string dir, string path)
        {
            string[] pathDirs = path.Split(';');

            if (!new ArrayList(pathDirs).Contains(dir))
                return dir + ";" + path;
            else
                return path;
        }

        static private string RemoveFromPath(string dir, string path)
        {
            string[] pathDirs = path.Split(';');

            if (new ArrayList(pathDirs).Contains(dir))
            {
                string pathVal = "";

                foreach (string pathDir in pathDirs)
                {
                    if (pathDir != "" && pathDir.ToUpper() != dir.ToUpper())
                    {
                        pathVal += pathDir + ";";
                    }
                }

                return pathVal;
            }
            else
            {
                return path;
            }
        }

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("Advapi32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern int RegOpenKeyEx(uint hKey, string lpSubKey, uint ulOptions, int samDesired, out int phkResult);

        [DllImport("advapi32.dll", EntryPoint = "RegQueryValueEx")]
        public static extern int RegQueryValueEx(int hKey, string lpValueName, int lpReserved, out uint lpType, StringBuilder lpData, ref int lpcbData);

        [DllImport("advapi32.dll", EntryPoint = "RegSetValueExA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int RegSetValueEx(int hKey, string lpValueName, int Reserved, int dwType, [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpData, int cbData);

        [DllImport("Advapi32.dll")]
        static extern uint RegCloseKey(int hKey);

        const uint HKEY_LOCAL_MACHINE = 0x80000002;

        static string RegGetValueExp(uint key, string subKey, string valName)
        {
            //this method is required in order to rtreive REG_EXPAND_SZ registry value
            const int KEY_READ = 0x00000001;
            int hkey = 0;

            try
            {
                if (0 == RegOpenKeyEx(key, subKey, 0, KEY_READ, out hkey))
                {
                    StringBuilder sb = new StringBuilder(1024 * 10);
                    int lpcbData = sb.Capacity;
                    uint lpType;

                    if (0 == RegQueryValueEx(hkey, valName, 0, out lpType, sb, ref lpcbData))
                        return sb.ToString();
                }
            }
            finally
            {
                if (0 != hkey)
                    RegCloseKey(hkey);
            }

            return null;
        }

        static int RegSetStrValue(uint key, string subKey, string valName, string val)
        {
            //this method is required in order to set REG_EXPAND_SZ registry value
            const int KEY_WRITE = 0x00020006;
            const int KEY_READ = 0x00000001;
            int lResult = 0;
            int hkey = 0;

            try
            {
                lResult = RegOpenKeyEx(key, subKey, 0, KEY_WRITE | KEY_READ,
                    out hkey);
                if (lResult == 0)
                {
                    lResult = RegSetValueEx(hkey, valName, 0, 2, ref val, val.Length);
                }
            }
            finally
            {
                if (0 != hkey)
                    RegCloseKey(hkey);
            }

            return lResult;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SendMessageTimeout(IntPtr hWnd, int Msg, int wParam, string lParam, int fuFlags, int uTimeout, int lpdwResult);

        public const int HWND_BROADCAST = 0xffff;
        public const int WM_SETTINGCHANGE = 0x001A;
        public const int SMTO_ABORTIFHUNG = 0x0002;

        static public void UpdateFromInstallation(csscript.Settings destSettings, string installDir)
        {
            //update Settings object with the data from the installDir config file
            //use reflection in order to overcome any incompatibility between different versions of the csscript.Settings
            string srcCSSAssembly = Path.Combine(installDir, @"Lib\CSScriptLibrary.dll");
            string srcCondigDat = Path.Combine(installDir, "css_config.xml");

            if (!File.Exists(srcCondigDat))
                Path.ChangeExtension(srcCondigDat, ".dat"); //older (non-xml based) version of the CS-Script Config file
            try
            {
                MethodInfo Load = GetMethod(Assembly.LoadFrom(srcCSSAssembly), "csscript.Settings.Load", new Type[] { typeof(string) }, BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod);

                if (Load != null)
                {
                    object srcSettings = Load.Invoke(null, new object[] { srcCondigDat });
                    MethodInfo srcGet = null;

                    foreach (MemberInfo srcPropSet in destSettings.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty))
                    {
                        //property name: get_<propName> / set_<propName>
                        if (srcPropSet.Name.StartsWith("set_")
                            && srcPropSet.Name != "set_UseAlternativeCompiler" && srcPropSet.Name != "set_CleanupShellCommand") //ignore alt compilers and cleanup command as they can point to obsolete files
                            if (null != (srcGet = srcSettings.GetType().GetMethod("get_" + srcPropSet.Name.Substring(4))))
                            {
                                object srcValue = srcGet.Invoke(srcSettings, null);
                                destSettings.GetType().GetMethod(srcPropSet.Name).Invoke(destSettings, new object[] { srcValue });
                            }
                    }
                }
            }
            catch { }
        }

        static object GetPropertyValue(object o, string name)
        {
            //browse the type properties first in oprder to avoid throwing exception (which is time consuming) if the property not found.
            foreach (MemberInfo mi in o.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
                if (mi.Name == ("get_" + name))
                    return o.GetType().GetMethod("get_" + name).Invoke(o, null);
            return null;
        }

        static MethodInfo GetMethod(Assembly assembly, string name, Type[] types, BindingFlags bf)
        {
            foreach (Module m in assembly.GetModules())
                foreach (Type t in m.GetTypes())
                    foreach (MemberInfo mi in t.GetMembers(bf))
                        if ((t.FullName + "." + mi.Name) == name)
                            return t.GetMethod(mi.Name, types);
            return null;
        }
    }

    public partial class SelectIconForm : Form
    {
        public SelectIconForm()
        {
            InitializeComponent();

            string iconSpec = CSScriptInstaller.GetScriptIcon();

            string[] parts = iconSpec.Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            textBox1.Text = parts[0];

            int index = 0;

            if (parts.Length > 1)
            {
                int.TryParse(parts[1], out index);
                textBox2.Text = index.ToString();
            }
        }

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
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.browseButton = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.linkLabel4 = new System.Windows.Forms.LinkLabel();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();

            //
            // okButton
            //
            this.okButton.Location = new System.Drawing.Point(142, 194);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "&OK";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);

            //
            // cancelButton
            //
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(241, 194);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "&Cancel";

            //
            // browseButton
            //
            this.browseButton.Location = new System.Drawing.Point(417, 44);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(24, 23);
            this.browseButton.TabIndex = 2;
            this.browseButton.Text = "...";
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);

            //
            // textBox1
            //
            this.textBox1.Location = new System.Drawing.Point(12, 46);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(403, 20);
            this.textBox1.TabIndex = 3;

            //
            // label1
            //
            this.label1.Location = new System.Drawing.Point(12, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "File:";

            //
            // textBox2
            //
            this.textBox2.Location = new System.Drawing.Point(12, 85);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(58, 20);
            this.textBox2.TabIndex = 3;

            //
            // label2
            //
            this.label2.Location = new System.Drawing.Point(12, 69);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(36, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Index:";

            //
            // linkLabel1
            //
            this.linkLabel1.Location = new System.Drawing.Point(9, 143);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(175, 13);
            this.linkLabel1.TabIndex = 6;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Set Default Visual Studio 2010 Icon";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);

            //
            // linkLabel2
            //
            this.linkLabel2.Location = new System.Drawing.Point(9, 120);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(131, 13);
            this.linkLabel2.TabIndex = 6;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Text = "Set Default CS-Script Icon";
            this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);

            //
            // label3
            //
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.MediumBlue;
            this.label3.Location = new System.Drawing.Point(8, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(359, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "You may need to restart Windows Explorer before the change takes affect.";

            //
            // linkLabel4
            //
            this.linkLabel4.Location = new System.Drawing.Point(9, 163);
            this.linkLabel4.Name = "linkLabel1";
            this.linkLabel4.Size = new System.Drawing.Size(175, 13);
            this.linkLabel4.TabIndex = 6;
            this.linkLabel4.TabStop = true;
            this.linkLabel4.Text = "Set Default Visual Studio 2012 Icon";
            this.linkLabel4.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel4_LinkClicked);

            //
            // SelectIconForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(453, 230);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.linkLabel2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.linkLabel4);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SelectIconForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Script file icon";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion Windows Form Designer generated code

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.LinkLabel linkLabel4;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.Label label3;

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            textBox2.Text = "0";
            textBox1.Text = Path.Combine(CSScriptInstaller.GetEnvironmentVariable("CSSCRIPT_DIR"), "cscs.exe");
            textBox1.SelectionStart = textBox1.Text.Length - 1;
        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            textBox2.Text = "1";
            textBox1.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft Visual Studio 11.0\VC#\VCSPackages\csproj.dll");
            textBox1.SelectionStart = textBox1.Text.Length - 1;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            textBox2.Text = "1";
            textBox1.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft Visual Studio 10.0\VC#\VCSPackages\csproj.dll");
            textBox1.SelectionStart = textBox1.Text.Length - 1;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (!File.Exists(textBox1.Text))
            {
                MessageBox.Show("The specified file does not exist");
                return;
            }

            int index;

            if (!int.TryParse(textBox2.Text, out index))
            {
                MessageBox.Show("The specified index value is invalid does not exist");
                return;
            }

            CSScriptInstaller.SetKeyValue(@"CsScript\DefaultIcon", "", textBox1.Text + "," + textBox2.Text);

            Close();

            this.DialogResult = DialogResult.OK;
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Choose the file containing the icon image.";
                dlg.RestoreDirectory = true;
                dlg.CheckFileExists = true;
                dlg.Multiselect = false;
                dlg.Filter = "Applications|*.exe";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    textBox1.Text = dlg.FileName;
                    textBox1.SelectionStart = textBox1.Text.Length - 1;
                }
            }
        }
    }
}