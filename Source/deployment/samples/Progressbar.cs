using System;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Scripting
{
	public class ProgressbarForm : System.Windows.Forms.Form
	{
		#region Public interface...
		public void Increment()
		{
			position++;
			if (position > maxPosition)
			{
				position = 0;
			}
			CalcUnits();
			
			using(Graphics g = CreateGraphics())
			{
				DrawProgressBar(g, true);
			}
		}

		public void UpdateProgress()
		{
			CalcUnits(); 
			using(Graphics g = CreateGraphics())
			{
				if (g != null)
				{
					DrawProgressBar(g, true);
				}
			}
		}
		public void Setup(int position, int maxPosition, int unitsInGrpup, int autoIncrementDelay) // -1 means no autoincrementing
		{
			this.position = position;	
			this.maxPosition = maxPosition;
			this.unitsInGrpup = unitsInGrpup;

			this.currCount = 0;
			this.extraPositions = 0;
			this.autoIncrementDelay = autoIncrementDelay;
			System.Diagnostics.Debug.Assert(maxPosition - unitsInGrpup >= unitsInGrpup);

			CalcUnits();
		}
		
		
		public int AutoIncrementDelay { set { autoIncrementDelay = value; UpdateProgress(); } get { return autoIncrementDelay; } }	
		public int Position { set { position = value; UpdateProgress(); } get { return position; } }	
		public int MaxPosition { set { maxPosition = value; UpdateProgress(); } get { return maxPosition; } }
		public int UnitsInGrpup { set { unitsInGrpup = value; UpdateProgress(); } get { return unitsInGrpup; }}
		#endregion

		//drawing and calculating rutines
		private void Init()
		{
			progressRectangle = ClientRectangle;
			progressRectangle.Inflate(-4, -4);
			unitsRectangle = progressRectangle;
			unitsRectangle.Inflate(-10, -2);
			
			RectangleF rcUnit0 = unitsRectangle;
			float unitWidth = (float)(unitsRectangle.Width / (maxPosition - unitsInGrpup + 1));
			rcUnit0.Width = unitWidth;
			
			float offset = ((float)(unitsRectangle.Width - ((maxPosition - unitsInGrpup + 1) * unitWidth))) / (float)2.0;
			rcUnit0.Offset(offset, 0);			//center units in clientRect
			rcUnit0.Inflate(-1, -4);			//shrink it a little
			
			unitRects.Clear();
	
			for (int i = 0; i < (maxPosition - unitsInGrpup + 1); i++)
			{	
				RectangleF rcUnit = rcUnit0;
				rcUnit.X += (float)i * (float)unitWidth;
				unitRects.Add(rcUnit);
			}

			if (autoIncrementDelay > 0)
			{
				timer1.Enabled = true;
				timer1.Interval = autoIncrementDelay;
			}
			else
			{
				timer1.Enabled = false;
			}
		}

		private void CalcUnits()
		{
			int maxUnits = maxPosition;
			if (position == 0)
			{
				currCount = -1;
				extraPositions = 0;
			}
			else
			{
				currCount = position - 1;
			
				if (currCount < unitsInGrpup)
				{
					extraPositions = currCount;
				}
				else
				{
					extraPositions = unitsInGrpup - 1;
					if (currCount > maxUnits - unitsInGrpup)
					{
						extraPositions -= unitsInGrpup - (maxUnits - currCount);
						currCount = maxUnits - unitsInGrpup;
					}
				}
			}
		}

		private void DrawProgressBar(Graphics g, bool unitsOnly)
		{
			Bitmap myBitmap = new Bitmap((int)ClientRectangle.Width, (int)ClientRectangle.Height, g);
			using(Graphics memSurface = Graphics.FromImage(myBitmap))
			{
				RectangleF rc = progressRectangle;
				if (!unitsOnly)
				{
					using (Brush blackBrush = new SolidBrush(Color.Black))
					{
						memSurface.FillRectangle(blackBrush, ClientRectangle);
					}
					using (Pen whitePan = new Pen(Color.White, 3))
					{
						using (GraphicsPath path = RoundRectangle(rc)) 
						{
							memSurface.DrawPath(whitePan, path);
						}					
					}

				}
				using (Brush darkBrush = new SolidBrush(Color.FromArgb(18, 1, 100)))
				{	
					rc.Inflate(-2, -2);
					using (GraphicsPath path = RoundRectangle(rc))
					{
						memSurface.FillPath(darkBrush, path);
					}
				}
				DrawUnits(memSurface);
			}
			g.DrawImage(myBitmap, 0, 0);
			myBitmap.Dispose();
		}

		private void DrawUnits(Graphics g)
		{
			int index = currCount;
			if (index != -1)
			{
				using (Brush unitBrush = CreateUnitBrash((RectangleF)unitRects[index]))
				{
					g.FillRectangle(unitBrush, (RectangleF)unitRects[index]);
					for (int i = extraPositions; i > 0; i--)
					{
						g.FillRectangle(unitBrush, (RectangleF)unitRects[index - i]);
					}
				}
			}
		}

		private Brush CreateUnitBrash(RectangleF rc)
		{
			bool useGradient = true; 
			if (useGradient)
			{
				using (GraphicsPath path = new GraphicsPath())
				{
					RectangleF rc1 = unitsRectangle;
					rc1.Offset(0, -10);	//shift "reflection spot" a bit up from the center
					rc1.Height += 10;
					path.AddRectangle(rc1);

					PathGradientBrush pthGrBrush = new PathGradientBrush(path);
					pthGrBrush.SurroundColors = new Color[]{Color.Green};
					pthGrBrush.CenterColor = Color.LawnGreen;
					pthGrBrush.FocusScales = new PointF(1.0f, 0.15f);
					return pthGrBrush;
				}

//				int unitVOffest = 2;
//				LinearGradientBrush linGrBrush = new LinearGradientBrush(
//					unitsRectangle,
//					Color.FromArgb(245, 255, 245),
//					Color.FromArgb(8,255,0), LinearGradientMode.Vertical);  
//				//linGrBrush.GammaCorrection = true;
//				return linGrBrush;
			}
			else
				return new SolidBrush(Color.FromArgb(8,255,0));	
		}

		private GraphicsPath RoundRectangle(RectangleF rc)
		{
			float radius = rc.Height / 2.0f;

			PointF point1 = new PointF(rc.X + radius, rc.Y);
			PointF point2 = new PointF(rc.X + rc.Width - radius, rc.Y);
			PointF point3 = new PointF(rc.X + rc.Width - radius, rc.Y + rc.Height);
			PointF point4 = new PointF(rc.X + radius, rc.Y + rc.Height);
						
			GraphicsPath myPath = new GraphicsPath();

			myPath.AddLine(point1, point2);
			myPath.AddArc(point2.X - radius, point2.Y, radius*2.0f, rc.Height, 270, 180);
			myPath.AddLine(point3, point4);
			myPath.AddArc(point4.X - radius, point1.Y, radius*2.0f, rc.Height, 90, 180);

			return myPath;
		}

		//internal progressbar data
		private int currCount;	
		private int extraPositions;	
		private RectangleF progressRectangle;
		private RectangleF unitsRectangle;

		private int position;	
		private int maxPosition;
		private int unitsInGrpup;
		private int autoIncrementDelay;
		private ArrayList unitRects;

		//form data and methods
		private System.Windows.Forms.Timer timer1;
		private System.ComponentModel.IContainer components;

		public ProgressbarForm(string title)
		{
			InitializeComponent();
			if (title != null)
			{
				this.Text = title;
			}
		}

		static public void ShowContinuous(string title)
		{
			using(ProgressbarForm dlg = new ProgressbarForm(title))
			{
				dlg.Setup(0, 20, 4, 70);
				dlg.ShowDialog();
			}
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
			this.components = new System.ComponentModel.Container();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			// 
			// timer1
			// 
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// ProgressbarForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(258, 32);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "ProgressbarForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Progressbar";
			this.Click += new System.EventHandler(this.ProgressbarForm_Click);
			this.Load += new System.EventHandler(this.ProgressbarForm_Load);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.ProgressbarForm_Paint);

		}
		#endregion
		
		private void ProgressbarForm_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			DrawProgressBar(e.Graphics, false);
		}

		private void ProgressbarForm_Load(object sender, System.EventArgs e)
		{
			unitRects = new ArrayList();
			Init();
		}

		private void timer1_Tick(object sender, System.EventArgs e)
		{
			Increment();
		}

		private void ProgressbarForm_Click(object sender, System.EventArgs e)
		{
			//timer1.Enabled = !timer1.Enabled; for testing only
		}
	}
	
	class Script
	{
		const string usage = "Usage: cscscript progressbar ...\nShows 'continuous' progressbar. This is an example how to draw in GDI+ without flickering.\n";

		static public void Main(string[] args)
		{
			if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
				Console.WriteLine(usage);
			else
				ProgressbarForm.ShowContinuous("Progress");
		}
	}
}