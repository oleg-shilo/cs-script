using System;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

//Based on code of  Agha Ali Raza (http://www.csharphelp.com/archives2/archive393.html)

namespace Script
{
	public class CaptureScreen
	{
		const string usage = "Usage: cscscript printScreen [filename]\n"+
							 "Captures screen image and saves it to a file (default file: screen.gif).\n";
			
		static public void Main(string[] args)
		{
			if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
			{
				Console.WriteLine(usage);
			}
			else
			{
				try
				{
					Bitmap capture = CaptureScreen.GetDesktopImage();
					string file = Path.Combine(Environment.CurrentDirectory, "screen.gif");
					ImageFormat format = ImageFormat.Gif;
			
					if (args.Length == 1)
					{
						file = args[0];
						if (args[0].ToUpper().EndsWith(".GIF")) 
							format = ImageFormat.Gif;
						else if (args[0].ToUpper().EndsWith(".BMP")) 
							format = ImageFormat.Bmp;
						else if (args[0].ToUpper().EndsWith(".JPEG")) 
							format = ImageFormat.Jpeg;
						else if (args[0].ToUpper().EndsWith(".PNG")) 
							format = ImageFormat.Png;

					}
					capture.Save(file, format);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
		}

		public static Bitmap GetDesktopImage()
		{
			WIN32_API.SIZE size;
			
			IntPtr 	hDC = WIN32_API.GetDC(WIN32_API.GetDesktopWindow()); 
			IntPtr hMemDC = WIN32_API.CreateCompatibleDC(hDC);
			
			size.cx = WIN32_API.GetSystemMetrics(WIN32_API.SM_CXSCREEN);
			size.cy = WIN32_API.GetSystemMetrics(WIN32_API.SM_CYSCREEN);
			
			m_HBitmap = WIN32_API.CreateCompatibleBitmap(hDC, size.cx, size.cy);

			if (m_HBitmap!=IntPtr.Zero)
			{
				IntPtr hOld = (IntPtr) WIN32_API.SelectObject(hMemDC, m_HBitmap);
				WIN32_API.BitBlt(hMemDC, 0, 0,size.cx,size.cy, hDC, 0, 0, WIN32_API.SRCCOPY);
				WIN32_API.SelectObject(hMemDC, hOld);
				WIN32_API.DeleteDC(hMemDC);
				WIN32_API.ReleaseDC(WIN32_API.GetDesktopWindow(), hDC);
				return System.Drawing.Image.FromHbitmap(m_HBitmap); 
			}
			return null;
		}
		
		protected static IntPtr m_HBitmap;
	}

	public class WIN32_API
	{
		public struct SIZE
		{
			public int cx;
			public int cy;
		}
		public  const int SRCCOPY = 13369376;
		public  const int SM_CXSCREEN=0;
		public  const int SM_CYSCREEN=1;

		[DllImport("gdi32.dll",EntryPoint="DeleteDC")]
		public static extern IntPtr DeleteDC(IntPtr hDc);

		[DllImport("gdi32.dll",EntryPoint="DeleteObject")]
		public static extern IntPtr DeleteObject(IntPtr hDc);

		[DllImport("gdi32.dll",EntryPoint="BitBlt")]
		public static extern bool BitBlt(IntPtr hdcDest,int xDest,int yDest,int wDest,int hDest,IntPtr hdcSource,int xSrc,int ySrc,int RasterOp);

		[DllImport ("gdi32.dll",EntryPoint="CreateCompatibleBitmap")]
		public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc,	int nWidth, int nHeight);

		[DllImport ("gdi32.dll",EntryPoint="CreateCompatibleDC")]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport ("gdi32.dll",EntryPoint="SelectObject")]
		public static extern IntPtr SelectObject(IntPtr hdc,IntPtr bmp);

		[DllImport("user32.dll", EntryPoint="GetDesktopWindow")]
		public static extern IntPtr GetDesktopWindow();

		[DllImport("user32.dll",EntryPoint="GetDC")]
		public static extern IntPtr GetDC(IntPtr ptr);

		[DllImport("user32.dll",EntryPoint="GetSystemMetrics")]
		public static extern int GetSystemMetrics(int abc);

		[DllImport("user32.dll",EntryPoint="GetWindowDC")]
		public static extern IntPtr GetWindowDC(Int32 ptr);

		[DllImport("user32.dll",EntryPoint="ReleaseDC")]
		public static extern IntPtr ReleaseDC(IntPtr hWnd,IntPtr hDc);
	}
	
}
