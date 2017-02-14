using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using Microsoft.Win32;

class Script
{
	const string usage = "Usage: cscscript sysFindConfig ...\nDisplays configuration console to adjust system 'FindInFile' options on WindowsXP. "+
						 "This is the work around for WindowsXP BUG described in MSDN articles Q309173 "+
						 "'Using the 'A Word or Phrase in the File' Search Criterion May Not Work'.\n";

	static public void Main(string[] args)
	{
		if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
		{
			Console.WriteLine(usage);
		}
		else
		{
			Application.Run(new ConfigForm());
		}
	}

	public class ConfigForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.TextBox textBox1;


		private System.ComponentModel.Container components = null;
		static string GUID = "{5e941d80-bf96-11cd-b579-08002b30bfeb}";
		public ConfigForm()
		{
			InitializeComponent();
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// listBox1
			// 
			this.listBox1.Location = new System.Drawing.Point(8, 40);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(128, 199);
			this.listBox1.Sorted = true;
			this.listBox1.TabIndex = 0;
			this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
			// 
			// button1
			// 
			this.button1.Enabled = false;
			this.button1.Location = new System.Drawing.Point(152, 8);
			this.button1.Name = "button1";
			this.button1.TabIndex = 1;
			this.button1.Text = "&Add";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// button2
			// 
			this.button2.Enabled = false;
			this.button2.Location = new System.Drawing.Point(152, 40);
			this.button2.Name = "button2";
			this.button2.TabIndex = 2;
			this.button2.Tag = "";
			this.button2.Text = "&Remove";
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(8, 8);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(128, 20);
			this.textBox1.TabIndex = 3;
			this.textBox1.Text = "";
			this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(242, 246);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.listBox1);
			this.Controls.Add(this.button2);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MinimizeBox = false;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "\"Find In File\" supported file extensions";
			this.TopMost = true;
			this.Load += new System.EventHandler(this.ConfigForm_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void ConfigForm_Load(object sender, System.EventArgs e)
		{
			string[] subKeysNames = Registry.ClassesRoot.GetSubKeyNames();
			foreach(string name in subKeysNames)
			{
				if (name.StartsWith("."))
				{
					RegistryKey subKeys = Registry.ClassesRoot.OpenSubKey(name+"\\PersistentHandler");
					if (subKeys != null)
					{
						object regValue = subKeys.GetValue("");
						if (regValue != null && regValue.ToString() == GUID)
						{
							listBox1.Items.Add(name);
						}
					}
				}
			}
		}

		private void textBox1_TextChanged(object sender, System.EventArgs e)
		{
			button1.Enabled = textBox1.Text.Length != 0;
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			try
			{
				if (textBox1.Text.Length != 0)
				{
					int index = -1;
					if (!textBox1.Text.StartsWith("."))
					{
						textBox1.Text = "." + textBox1.Text;
					}
					if (-1 != (index = listBox1.FindString(textBox1.Text)))
					{	
						MessageBox.Show(textBox1.Text+" is already supported extension.");
						listBox1.SelectedIndex = index;
					}
					else
					{
						RegistryKey subKeys = Registry.ClassesRoot.OpenSubKey(textBox1.Text+"\\PersistentHandler", true);
						if (subKeys == null)
						{
							subKeys = Registry.ClassesRoot.CreateSubKey(textBox1.Text+"\\PersistentHandler");
						}
						subKeys.SetValue("", GUID);
						index = listBox1.Items.Add(textBox1.Text);
						listBox1.SelectedIndex = index;
						textBox1.Text = "";
					}
				}
				else
				{
					MessageBox.Show(textBox1.Text+" is not a valid file extension.");
				}
			}
			catch (Exception exc)
			{
				MessageBox.Show("Cannot add to the extension to the registry./n"+exc);
			}
		}

		private void button2_Click(object sender, System.EventArgs e)
		{
			string name = "";
			try
			{
				if (listBox1.SelectedIndex != -1)
				{
					name = listBox1.Items[listBox1.SelectedIndex].ToString();
					RegistryKey subKeys = Registry.ClassesRoot.OpenSubKey(name+"\\PersistentHandler", true);
					if (subKeys != null)
					{
						subKeys.DeleteValue("");
						listBox1.Items.RemoveAt(listBox1.SelectedIndex);
					}
				}
				else
				{
					MessageBox.Show("Please select extension you want to remove.");
				}
			}
			catch (Exception exc)
			{
				MessageBox.Show("Cannot remove "+name+" extension from the registry./n"+exc);
			}
		}

		private void listBox1_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			button2.Enabled = (listBox1.SelectedIndex != -1);
		}
	}
}
