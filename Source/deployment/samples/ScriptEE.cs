//css_dbg /t:winexe;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using CSScriptLibrary;

namespace Scripting
{
	public class Form1 : System.Windows.Forms.Form
	{
		private TextBox textBox1;
		private MainMenu mainMenu1;
		private TextBox textBox2;
		private SplitContainer splitContainer1;
		private SplitContainer splitContainer2;
		private TextBox textBox3;
		private Label label1;
		private Label label2;
		private MenuStrip menuStrip1;
		private ToolStripMenuItem scriptToolStripMenuItem;
		private ToolStripMenuItem runToolStripMenuItem;
		private ToolStripMenuItem debugToolStripMenuItem;
		private ToolStripMenuItem previousToolStripMenuItem;
		private ToolStripMenuItem clearToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripMenuItem exitToolStripMenuItem;
		private System.ComponentModel.IContainer components;

		public Form1()
		{
			InitializeComponent();

			Console.SetOut(new ConsoleListener(this.textBox1));
		}
		#region Windows Form Designer generated code
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBox3 = new System.Windows.Forms.TextBox();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.scriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.runToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.previousToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// textBox1
			// 
			this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBox1.Location = new System.Drawing.Point(0, 0);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBox1.Size = new System.Drawing.Size(530, 256);
			this.textBox1.TabIndex = 0;
			this.textBox1.Text = "Console.WriteLine(\"HelloWorld\");\r\nMessageBox.Show(\"Hello World!\");\r\n";
			this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox1_KeyDown);
			// 
			// textBox2
			// 
			this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox2.Location = new System.Drawing.Point(0, 19);
			this.textBox2.Multiline = true;
			this.textBox2.Name = "textBox2";
			this.textBox2.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBox2.Size = new System.Drawing.Size(234, 50);
			this.textBox2.TabIndex = 0;
			this.textBox2.Text = "using System;\r\nusing System.Windows.Forms;";
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.label1);
			this.splitContainer1.Panel1.Controls.Add(this.textBox2);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.label2);
			this.splitContainer1.Panel2.Controls.Add(this.textBox3);
			this.splitContainer1.Size = new System.Drawing.Size(530, 69);
			this.splitContainer1.SplitterDistance = 234;
			this.splitContainer1.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 3);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(131, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Referenced Namespaces:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 3);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(121, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Referenced Assemblies:";
			// 
			// textBox3
			// 
			this.textBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox3.Location = new System.Drawing.Point(0, 19);
			this.textBox3.Multiline = true;
			this.textBox3.Name = "textBox3";
			this.textBox3.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBox3.Size = new System.Drawing.Size(292, 50);
			this.textBox3.TabIndex = 0;
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 24);
			this.splitContainer2.Name = "splitContainer2";
			this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.splitContainer1);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.textBox1);
			this.splitContainer2.Size = new System.Drawing.Size(530, 329);
			this.splitContainer2.SplitterDistance = 69;
			this.splitContainer2.TabIndex = 2;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.scriptToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(530, 24);
			this.menuStrip1.TabIndex = 3;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// scriptToolStripMenuItem
			// 
			this.scriptToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runToolStripMenuItem,
            this.debugToolStripMenuItem,
            this.previousToolStripMenuItem,
            this.clearToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
			this.scriptToolStripMenuItem.Name = "scriptToolStripMenuItem";
			this.scriptToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Up)));
			this.scriptToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
			this.scriptToolStripMenuItem.Text = "&Script";
			// 
			// runToolStripMenuItem
			// 
			this.runToolStripMenuItem.Name = "runToolStripMenuItem";
			this.runToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.runToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.runToolStripMenuItem.Text = "&Run";
			this.runToolStripMenuItem.Click += new System.EventHandler(this.runToolStripMenuItem_Click);
			// 
			// debugToolStripMenuItem
			// 
			this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
			this.debugToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.debugToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.debugToolStripMenuItem.Text = "&Debug";
			this.debugToolStripMenuItem.Click += new System.EventHandler(this.debugToolStripMenuItem_Click);
			// 
			// previousToolStripMenuItem
			// 
			this.previousToolStripMenuItem.Name = "previousToolStripMenuItem";
			this.previousToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Up)));
			this.previousToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.previousToolStripMenuItem.Text = "&Previous";
			this.previousToolStripMenuItem.Click += new System.EventHandler(this.previousToolStripMenuItem_Click);
			// 
			// clearToolStripMenuItem
			// 
			this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
			this.clearToolStripMenuItem.ShortcutKeyDisplayString = "";
			this.clearToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Space)));
			this.clearToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.clearToolStripMenuItem.Text = "&Clear";
			this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(157, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.exitToolStripMenuItem.Text = "&Exit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(530, 353);
			this.Controls.Add(this.splitContainer2);
			this.Controls.Add(this.menuStrip1);
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip1;
			this.Menu = this.mainMenu1;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Script Execution Environment";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			this.splitContainer2.Panel2.PerformLayout();
			this.splitContainer2.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		string lastScript = "";
		string GetLastScript()
		{
			string outStartToken = "\n>";
			string outEndToken = "\n";
			int lastOutStart = textBox1.Text.LastIndexOf(outStartToken);
			if (lastOutStart == -1)
				return lastScript = textBox1.Text; //the only text is C# code
			else
			{
				int lastOutEnd = textBox1.Text.IndexOf(outEndToken, lastOutStart + outStartToken.Length);
				if (lastOutEnd != -1 && lastOutEnd + outEndToken.Length < textBox1.Text.Length)
					return lastScript = textBox1.Text.Substring(lastOutEnd + 1); //the bottom text is C# code
				else
				{
					return lastScript;//the bottom text is a code execution output
				}
			}
		}

		bool IsScriptChanged()
		{
			if (scriptAsm == null || lastScript == null)
				return true;

			string outStartToken = "\n>";
			string outEndToken = "\n";
			int lastOutStart = textBox1.Text.LastIndexOf(outStartToken);
			if (lastOutStart == -1)
				return true; //the only text is C# code
			else
			{
				int lastOutEnd = textBox1.Text.IndexOf(outEndToken, lastOutStart + outStartToken.Length);
				if (lastOutEnd != -1 && lastOutEnd + outEndToken.Length < textBox1.Text.Length)
					return true; //the bottom text is C# code
				else
				{
					return false;//the bottom text is a code execution output
				}
			}
		}
		string ConvertToCode(string script, bool debug)
		{
			StringBuilder sb = new StringBuilder();

			foreach (string line in TextToLines(textBox3.Text)) // //css_ref statements
				sb.AppendLine("//css_ref " + line + ";");

			foreach (string line in TextToLines(textBox2.Text)) // using statements
				sb.AppendLine(line);

			sb.AppendLine(
				"class Script : MarshalByRefObject \r\n" +
				"{\r\n" +
				"static public void Main() " +
				"{\r\n" +
				(debug ? "System.Diagnostics.Debug.Assert(false);\r\n\r\n" : "") +
				script.Replace("\n", "\r\n") + "\r\n" +
				"}\r\n" +
				"}\r\n");
			return sb.ToString();
		}
		string[] TextToLines(string text)
		{
			List<string> retval = new List<string>();

			string line;
			using (StringReader sr = new StringReader(text))
				while ((line = sr.ReadLine()) != null)
					retval.Add(line);
			return retval.ToArray();
		}

		AsmHelper scriptAsm;

		string dbgSourceFileName = Path.GetTempFileName();

		private void Form1_Load(object sender, EventArgs e)
		{
			textBox1.Select();
			textBox1.SelectionLength = 0;
			textBox1.SelectionStart = textBox1.Text.Length;
		}

		private void runToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				if (IsScriptChanged())
				{
					string code = ConvertToCode(GetLastScript(), false);
					scriptAsm = new AsmHelper(CSScript.LoadCode(code, null, false));
				}
				scriptAsm.Invoke("Script.Main");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message.Trim());
			}
			Cursor.Current = Cursors.Default;
		}
		private void debugToolStripMenuItem_Click(object sender, EventArgs e)
		{
			textBox1.Text += Environment.NewLine;
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				if (IsScriptChanged())
				{
					using (StreamWriter sw = new StreamWriter(this.dbgSourceFileName))
						sw.Write(ConvertToCode(GetLastScript(), true));

					scriptAsm = new AsmHelper(CSScript.Load(this.dbgSourceFileName, null, true));
				}
				scriptAsm.Invoke("Script.Main");
				scriptAsm = null;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message.Trim());
			}
			Cursor.Current = Cursors.Default;
		}
		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
		private void previousToolStripMenuItem_Click(object sender, EventArgs e)
		{
			textBox1.Text += lastScript;
			ResetCaret();
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			textBox1.Text = "";
		}
		void ResetCaret()
		{
			textBox1.SelectionLength = 0;
			textBox1.SelectionStart = textBox1.Text.Length;
		}

		private void textBox1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
			{
				textBox1.SelectionStart = 0;
				textBox1.SelectionLength = textBox1.Text.Length;
			}
		}
	}

	class Script
	{
		[STAThread]
		static public void Main(string[] args)
		{
			if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
			{
				Console.WriteLine("This script is an example of the light C# script execution environment.\n");
				return;
			}
			
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}

	#region ConsoleListener
	public class ConsoleListener : TextWriter
	{
		private TextBox control;

		public ConsoleListener(TextBox control) { this.control = control; }

		public override void Write(string value) { control.Text = control.Text + value.Replace("\n", "\n>"); SetCaret(); }
		public override void WriteLine(string value) { control.Text += "\n>" + value.Replace("\n", "\n>") + Environment.NewLine; SetCaret(); }

		void SetCaret() { control.SelectionLength = 0; control.SelectionStart = control.Text.Length; }

		public override System.Text.Encoding Encoding { get { return System.Text.Encoding.Unicode; } }
		public override void Write(bool value) { this.Write(value.ToString()); }
		public override void Write(char value) { this.Write(value.ToString()); }
		public override void Write(char[] buffer) { this.Write(new string(buffer)); }
		public override void Write(char[] buffer, int index, int count) { this.Write(new string(buffer, index, count)); }
		public override void Write(decimal value) { this.Write(value.ToString()); }
		public override void Write(double value) { this.Write(value.ToString()); }
		public override void Write(float value) { this.Write(value.ToString()); }
		public override void Write(int value) { this.Write(value.ToString()); }
		public override void Write(long value) { this.Write(value.ToString()); }
		public override void Write(string format, object arg0) { this.WriteLine(string.Format(format, arg0)); }
		public override void Write(string format, object arg0, object arg1) { this.WriteLine(string.Format(format, arg0, arg1)); }
		public override void Write(string format, object arg0, object arg1, object arg2) { this.WriteLine(string.Format(format, arg0, arg1, arg2)); }
		public override void Write(string format, params object[] arg) { this.WriteLine(string.Format(format, arg)); }
		public override void Write(uint value) { this.WriteLine(value.ToString()); }
		public override void Write(ulong value) { this.WriteLine(value.ToString()); }
		public override void Write(object value) { this.WriteLine(value.ToString()); }
		public override void WriteLine() { this.WriteLine(Environment.NewLine); }
		public override void WriteLine(bool value) { this.WriteLine(value.ToString()); }
		public override void WriteLine(char value) { this.WriteLine(value.ToString()); }
		public override void WriteLine(char[] buffer) { this.WriteLine(new string(buffer)); }
		public override void WriteLine(char[] buffer, int index, int count) { this.WriteLine(new string(buffer, index, count)); }
		public override void WriteLine(decimal value) { this.WriteLine(value.ToString()); }
		public override void WriteLine(double value) { this.WriteLine(value.ToString()); }
		public override void WriteLine(float value) { this.WriteLine(value.ToString()); }
		public override void WriteLine(int value) { this.WriteLine(value.ToString()); }
		public override void WriteLine(long value) { this.WriteLine(value.ToString()); }
		public override void WriteLine(string format, object arg0) { this.WriteLine(string.Format(format, arg0)); }
		public override void WriteLine(string format, object arg0, object arg1) { this.WriteLine(string.Format(format, arg0, arg1)); }
		public override void WriteLine(string format, object arg0, object arg1, object arg2) { this.WriteLine(string.Format(format, arg0, arg1, arg2)); }
		public override void WriteLine(string format, params object[] arg) { this.WriteLine(string.Format(format, arg)); }
		public override void WriteLine(uint value) { this.WriteLine(value.ToString()); }
		public override void WriteLine(ulong value) { this.WriteLine(value.ToString()); }
		public override void WriteLine(object value) { this.WriteLine(value.ToString()); }
	}
	#endregion
}