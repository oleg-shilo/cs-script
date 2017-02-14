using System;
using System.Net;
using System.IO;
using System.Text;
using System.Globalization;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Threading;

class Script 
{
	const string usage = "Usage: cscscript synTime ...\nGets time from http://tycho.usno.navy.mil/cgi-bin/timer.pl and synchronises PC systemtime.\n";

	static public void Main(string[] args)
	{
		if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
		{
			Console.WriteLine(usage);
		}
		else
		{		
			string htmlTimeStr = GetHTML("http://tycho.usno.navy.mil/cgi-bin/timer.pl");
			/*string htmlTimeStr =	"US Naval Observatory Master Clock Time\n" + 
									"<BR>Oct. 08, 06:28:59 UTC\n" + 
									"Oct. 08, 02:28:59 AM EDT\n" + 
									"Oct. 08, 01:28:59 AM CDT\n" + 
									"Oct. 08, 12:28:59 AM MDT\n" + 
									"Oct. 07, 11:28:59 PM PDT\n" + 
									"Oct. 07, 10:28:59 PM YDT\n" + 
									"Oct. 07, 08:28:59 PM AST\n" +
									"Time Service Department, US Naval Observatory";
									*/
			string strMyDateTime = null;
	
			StringReader strReader = new StringReader(htmlTimeStr);
			string line;
	
			while ((line = strReader.ReadLine()) != null) 
			{
				int pos = line.LastIndexOf("UTC");
				if (pos != -1)
				{
					strMyDateTime = line.Substring(4, pos - 4); //start from 4 because <BR> tag
					break;
				}
			}
			ProcessWEBTime(strMyDateTime);
		}
	}

	static string GetHTML(string url)
	{
		StringBuilder sb  = new StringBuilder();
		byte[] buf = new byte[8192];

		HttpWebRequest  request  = (HttpWebRequest)WebRequest.Create(url);
		HttpWebResponse response = (HttpWebResponse)request.GetResponse();

		Stream resStream = response.GetResponseStream();

		string tempString = null;
		int count = 0;

		while (0 < (count = resStream.Read(buf, 0, buf.Length)))
		{
			tempString = Encoding.ASCII.GetString(buf, 0, count);
			sb.Append(tempString);
		}
		return sb.ToString();
	}

	static void ProcessWEBTime(string strMyDateTime)
	{
		strMyDateTime =	strMyDateTime.Insert(strMyDateTime.IndexOf(",") + 1, GetCurrentYear() + ",");
	 
		CultureInfo en = new CultureInfo("en-US");
		DateTime myDateTime = DateTime.Parse(strMyDateTime, en);
			
		SetNewSysTime(myDateTime);
	}

	static string GetCurrentYear()
	{
		SYSTEMTIME st = new SYSTEMTIME();
		GetSystemTime(ref st);
		return st.wYear.ToString(); 
	}
	
	static public void SetNewSysTime(DateTime dateTime)
	{
		SYSTEMTIME st = new SYSTEMTIME(dateTime);
		Console.WriteLine("Time has been set: " + dateTime.ToLocalTime());
		SetSystemTime(ref st);
	}

	public struct SYSTEMTIME
	{
		public short wYear;
		public short wMonth;
		public short wDayOfWeek;
		public short wDay;
		public short wHour;
		public short wMinute;
		public short wSecond;
		public short wMilliseconds;
		public SYSTEMTIME(DateTime dateTime)
		{
			wYear = (short)dateTime.Year; 
			wMonth = (short)dateTime.Month;
			wDayOfWeek = (short) dateTime.DayOfWeek;
			wDay = (short)dateTime.Day;
			wHour = (short)dateTime.Hour;
			wMinute = (short)dateTime.Minute;
			wSecond= (short)dateTime.Second;
			wMilliseconds =(short) dateTime.Millisecond;
		}
	}
	[DllImport("kernel32.dll", SetLastError=true)] 
	static extern void GetSystemTime (ref SYSTEMTIME lpSystemTime); 
	[DllImport("kernel32.dll", SetLastError=true)] 
	static extern int SetSystemTime(ref SYSTEMTIME lpSystemTime); 
}