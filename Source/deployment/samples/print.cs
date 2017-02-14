using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Printing;

class Script
{
	static string usage = "Usage: cscscript print ...\nThis script contains genaral purpose printing routines and can be imported to assist with implementation of any printing tasks\n";

	static public void Main(string[] args)
	{
		if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
		{
			Console.WriteLine(usage);
		}
		else
		{
			//printing sample
			SimplePrinting printer = new SimplePrinting();
			
			//print file with preview
			printer.PrintFile(@"C:\BOOT.ini", true); 

			//print string with preview
			printer.Print(@"qqqqqqqqq 3333333333 4444444444444444 5555555555555 66666666666666 7777777777777 888888888888", true);
		}
	}

	public class SimplePrinting 
	{
		public void PrintFile (string filePath, bool preview)
		{
			PrintOut(filePath, true, preview);
		}
		public void Print (string text, bool preview)
		{
			PrintOut(text, false, preview);
		}

		private Font printFont;
		private TextReader streamToPrint;

		// The PrintPage event is raised for each page to be printed.
		private void PrintPageRoutine(object sender, PrintPageEventArgs ev) 
		{
			float linesPerPage = 0;
			float yPos =  0;
			int count = 0;
			float leftMargin = ev.MarginBounds.Left;
			float topMargin = ev.MarginBounds.Top;
			String line=null;
			
			// Calculate the number of lines per page.
			linesPerPage = ev.MarginBounds.Height  / printFont.GetHeight(ev.Graphics) ;

			// Iterate over the file, printing each line.
			while (count < linesPerPage && ((line=streamToPrint.ReadLine()) != null)) 
			{
				yPos = topMargin + (count * printFont.GetHeight(ev.Graphics));
				ev.Graphics.DrawString (line, printFont, Brushes.Black, leftMargin, yPos, new StringFormat());
				count++;
			}

			// If more lines exist, print another page.
			if (line != null) 
				ev.HasMorePages = true;
			else 
				ev.HasMorePages = false;
		}
		private void PrintOut(string data, bool file, bool prview)
		{
			try 
			{
				using (streamToPrint = file ? (TextReader)new StreamReader (data) : (TextReader)new StringReader(data))
				{
					printFont = new Font("Arial", 10);
					PrintDocument pd = new PrintDocument(); 
					pd.PrintPage += new PrintPageEventHandler(PrintPageRoutine);
					if (prview)
					{
						PrintPreviewDialog dlg = new PrintPreviewDialog();
						dlg.Document = pd;
						dlg.ShowDialog();
					}
					else
						pd.Print();
				} 
			} 
			catch(Exception ex) 
			{ 
				MessageBox.Show(ex.Message);
			}
		}
	}
}

