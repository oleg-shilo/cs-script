//css_reference csscriptlibrary.dll;
//css_reference system.data.dll;
//css_reference System.Windows.Forms.dll;
//css_reference System.Drawing.dll;
//css_include configFile.cs;
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Xml;
using System.Drawing;
using Microsoft.Win32;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using csscript;

namespace Config
{
	public class ConfigForm : Form
	{
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.MenuItem menuItem4;
		private System.Windows.Forms.MenuItem menuItem5;
		private System.Windows.Forms.MenuItem menuItem6;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.PropertyGrid propertyGrid1;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem7;
		private System.Windows.Forms.MenuItem menuItem8;
		private System.Windows.Forms.MenuItem menuItem9;
		private System.Windows.Forms.MenuItem menuItem10;
		private System.Windows.Forms.MenuItem menuItem11;
		private System.Windows.Forms.MenuItem menuItem12;
		private System.Windows.Forms.MenuItem menuItem13;
		private string file = "";
		private bool modified = false;
		
		public ConfigForm()
		{
			InitializeComponent();
			menuNew_Click(null, null);
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
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			this.menuItem4 = new System.Windows.Forms.MenuItem();
			this.menuItem5 = new System.Windows.Forms.MenuItem();
			this.menuItem6 = new System.Windows.Forms.MenuItem();
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.menuItem7 = new System.Windows.Forms.MenuItem();
			this.menuItem8 = new System.Windows.Forms.MenuItem();
			this.menuItem9 = new System.Windows.Forms.MenuItem();
			this.menuItem10 = new System.Windows.Forms.MenuItem();
			this.menuItem11 = new System.Windows.Forms.MenuItem();
			this.menuItem12 = new System.Windows.Forms.MenuItem();
			this.menuItem13 = new System.Windows.Forms.MenuItem();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(0, 0);
			this.button1.Name = "button1";
			this.button1.TabIndex = 0;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(0, 0);
			this.button2.Name = "button2";
			this.button2.TabIndex = 0;
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(0, 0);
			this.button3.Name = "button3";
			this.button3.TabIndex = 0;
			// 
			// menuItem1
			// 
			this.menuItem1.Index = -1;
			this.menuItem1.Text = "";
			// 
			// menuItem2
			// 
			this.menuItem2.Index = -1;
			this.menuItem2.Text = "";
			// 
			// menuItem3
			// 
			this.menuItem3.Index = -1;
			this.menuItem3.Text = "";
			// 
			// menuItem4
			// 
			this.menuItem4.Index = -1;
			this.menuItem4.Text = "";
			// 
			// menuItem5
			// 
			this.menuItem5.Index = -1;
			this.menuItem5.Text = "";
			// 
			// menuItem6
			// 
			this.menuItem6.Index = -1;
			this.menuItem6.Text = "";
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.CommandsVisibleIfAvailable = true;
			this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid1.HelpVisible = false;
			this.propertyGrid1.LargeButtons = false;
			this.propertyGrid1.LineColor = System.Drawing.SystemColors.ScrollBar;
			this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.Size = new System.Drawing.Size(448, 266);
			this.propertyGrid1.TabIndex = 2;
			this.propertyGrid1.Text = "PropertyGrid";
			this.propertyGrid1.ToolbarVisible = false;
			this.propertyGrid1.ViewBackColor = System.Drawing.SystemColors.Window;
			this.propertyGrid1.ViewForeColor = System.Drawing.SystemColors.WindowText;
			this.propertyGrid1.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged);
			// 
			// mainMenu1
			// 
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuItem7});
			// 
			// menuItem7
			// 
			this.menuItem7.Index = 0;
			this.menuItem7.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuItem8,
																					  this.menuItem9,
																					  this.menuItem10,
																					  this.menuItem11,
																					  this.menuItem12,
																					  this.menuItem13});
			this.menuItem7.Text = "&File";
			// 
			// menuItem8
			// 
			this.menuItem8.Index = 0;
			this.menuItem8.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
			this.menuItem8.Text = "&New";
			this.menuItem8.Click += new System.EventHandler(this.menuNew_Click);
			// 
			// menuItem9
			// 
			this.menuItem9.Index = 1;
			this.menuItem9.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
			this.menuItem9.Text = "&Open";
			this.menuItem9.Click += new System.EventHandler(this.menuOpen_Click);
			// 
			// menuItem10
			// 
			this.menuItem10.Index = 2;
			this.menuItem10.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
			this.menuItem10.Text = "&Save";
			this.menuItem10.Click += new System.EventHandler(this.menuSave_Click);
			// 
			// menuItem11
			// 
			this.menuItem11.Index = 3;
			this.menuItem11.Text = "Save&As";
			this.menuItem11.Click += new System.EventHandler(this.menuSaveAs_Click);
			// 
			// menuItem12
			// 
			this.menuItem12.Index = 4;
			this.menuItem12.Text = "-";
			// 
			// menuItem13
			// 
			this.menuItem13.Index = 5;
			this.menuItem13.Text = "E&xit";
			this.menuItem13.Click += new System.EventHandler(this.menuExit_Click);
			// 
			// ConfigForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(448, 266);
			this.Controls.Add(this.propertyGrid1);
			this.MaximizeBox = false;
			this.Menu = this.mainMenu1;
			this.Name = "ConfigForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Closing += new System.ComponentModel.CancelEventHandler(this.ConfigForm_Closing);
			this.ResumeLayout(false);

		}
	   

		[STAThread]
		static public void Main(string[] args)
		{
			if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
				Console.WriteLine("Usage: cscscript configFile...\nThis script displays CS-Script configuration file editor.\n");
			else
				try
				{
					Application.Run(new ConfigForm());
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "CS-Script");
				}
		}

		private void menuNew_Click(object sender, System.EventArgs e)
		{
			propertyGrid1.SelectedObject = new Settings();
			file = "Untitled";
			modified = false;
			UpdateTitle();
		}

		private void menuOpen_Click(object sender, System.EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog())
			{
				dlg.InitialDirectory = Environment.ExpandEnvironmentVariables("%CSSCRIPT_DIR%");
				if (dlg.InitialDirectory.IndexOf("%") != -1)
					dlg.InitialDirectory = "";
				dlg.Filter = "config files (*.xml)|*.xml|All files (*.*)|*.*" ;
				dlg.FilterIndex = 1 ;
				dlg.RestoreDirectory = true ;

				try
				{
					if(dlg.ShowDialog() == DialogResult.OK && File.Exists(dlg.FileName))
					{
						propertyGrid1.SelectedObject = Settings.Load(dlg.FileName);		
						file = dlg.FileName;
						UpdateTitle();
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
				}
			}
		}

		private void menuSave_Click(object sender, System.EventArgs e)
		{
			if (file.ToLower() == "untitled")
				menuSaveAs_Click(null, null);
			else
			{	
				((Settings)propertyGrid1.SelectedObject).Save(file);
				modified = false;
				UpdateTitle();
			}
		}

		private void menuSaveAs_Click(object sender, System.EventArgs e)
		{
			SaveFileDialog dlg = new SaveFileDialog();
 
			dlg.InitialDirectory = Environment.ExpandEnvironmentVariables("%CSSCRIPT_DIR%");
			if (dlg.InitialDirectory.IndexOf("%") != -1)
				dlg.InitialDirectory = "";
			dlg.Filter = "config files (*.xml)|*.xml|All files (*.*)|*.*" ;
			dlg.FilterIndex = 1;
			dlg.RestoreDirectory = true ;

			if(dlg.ShowDialog() == DialogResult.OK)
			{
				((Settings)propertyGrid1.SelectedObject).Save(dlg.FileName);
				file = dlg.FileName;
				UpdateTitle();
			}
		}

		private void menuExit_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void propertyGrid1_PropertyValueChanged(object s, System.Windows.Forms.PropertyValueChangedEventArgs e)
		{
			modified = true;
			UpdateTitle();
		}
		private void UpdateTitle()
		{
			this.Text = "CS-Script configuration - "+Path.GetFileName(file)+ (modified ? "*" : "");
		}

		private void ConfigForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (modified) 
			{
				DialogResult response = MessageBox.Show("The configuration file has been modified.\n Do you want to save it?", "CS-Script", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				if (response == DialogResult.Yes)
				{
					menuSave_Click(null, null);
					if (modified)
						e.Cancel = true;
				}
				else if (response == DialogResult.Cancel)
					e.Cancel = true;
			}
		}
	}
	//settings.Save(ConfigFile);
	//settings = File.Exists(ConfigFile) ? Settings.Load(ConfigFile) : new Settings();
}