//css_pre soapsuds(http://localhost:8086//MyRemotingApp/CountryList?WSDL, CountryList, -new); 
//css_ref CountryList.dll;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;


namespace Scripting
{
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button button1;
		private ListBox listBox1;
		private TextBox textBox1;
		private Button button2;
		private System.ComponentModel.Container components = null;
		static string pcName = "localhost";

		public Form1()
		{
			InitializeComponent();
			//pcName = "somePC"; 
			HttpChannel c = new HttpChannel();
		    ChannelServices.RegisterChannel(c);
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
			this.button1 = new System.Windows.Forms.Button();
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.button2 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(90, 140);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 1;
			this.button1.Text = "Get data";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// listBox1
			// 
			this.listBox1.FormattingEnabled = true;
			this.listBox1.Location = new System.Drawing.Point(12, 39);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(226, 95);
			this.listBox1.TabIndex = 2;
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(12, 10);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(143, 20);
			this.textBox1.TabIndex = 3;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(163, 10);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 4;
			this.button2.Text = "Push";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(250, 175);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.listBox1);
			this.Controls.Add(this.button1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "WinForm Sample";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void button1_Click(object sender, System.EventArgs e)
		{
			CountryList cLst = (CountryList)Activator.GetObject(	typeof(CountryList),
																	"http://"+pcName+":8086/CountryList", 
																	WellKnownObjectMode.Singleton);
		    listBox1.DataSource = cLst.GetList();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (textBox1.Text == string.Empty)
				return;

			CountryList cLst = (CountryList)Activator.GetObject(typeof(CountryList),
																	"http://"+pcName+":8086/CountryList",
																	WellKnownObjectMode.Singleton);

			cLst.AddCountry(textBox1.Text);
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