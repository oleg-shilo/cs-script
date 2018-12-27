//css_pre com(/ax, MSCAL.Calendar, AxInterop.MSACAL.dll);
//css_pre res(dummy, Scripting.Form1.resources);
//css_ref AxInterop.MSACAL.dll;
//css_res Scripting.Form1.resources;
using System;
using System.Drawing;
using System.Windows.Forms;

/*
Note this sample can work only if MSOffice is installed
*/

namespace Scripting
{
	public class Form1 : System.Windows.Forms.Form
	{
		private AxMSACAL.AxCalendar axCalendar1;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.axCalendar1 = new AxMSACAL.AxCalendar();
			((System.ComponentModel.ISupportInitialize)(this.axCalendar1)).BeginInit();
			this.SuspendLayout();
			// 
			// axCalendar1
			// 
			this.axCalendar1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.axCalendar1.Enabled = true;
			this.axCalendar1.Location = new System.Drawing.Point(0, 0);
			this.axCalendar1.Name = "axCalendar1";
			this.axCalendar1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axCalendar1.OcxState")));
			this.axCalendar1.Size = new System.Drawing.Size(307, 223);
			this.axCalendar1.TabIndex = 2;
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(307, 223);
			this.Controls.Add(this.axCalendar1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "ActiveX control hosting sample";
			((System.ComponentModel.ISupportInitialize)(this.axCalendar1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		private void button1_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}
	}
	
	class Script
	{
		[STAThread]
		static public void Main(string[] args)
		{
			Application.Run(new Form1());
		}
	}
}