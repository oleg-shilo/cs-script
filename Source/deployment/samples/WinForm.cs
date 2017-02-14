using System;
using System.Drawing;
using System.Windows.Forms;

namespace Scripting
{
	public class Form1 : System.Windows.Forms.Form
	{
		private System.ComponentModel.Container components = null;

		public Form1()
		{
			InitializeComponent();
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		#region Windows Form Designer generated code
		private void InitializeComponent()
		{
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(435, 264);
            this.Name = "Form1";
            this.ResumeLayout(false);

		}
		#endregion
	}
	
	class Script
	{
		const string usage = "Usage: cscscript WinForm ...\nThe primitive example that demonstrates how to create WinForm application.\n";
        [STAThread]
		static public void Main(string[] args)
		{
			if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
			{
				Console.WriteLine(usage);
			}
			else
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new Form1());
			}
		}
	}
}