//////////////////////////////////////////////////////////////////////////
//SynTime.cs - script file from "Synchronising system clock from WEB source" tutorial

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
    const string usage = "Usage: csc synTime [user:password] ...\nGets time from http://tycho.usno.navy.mil/cgi-bin/timer.pl and synchronises PC systemtime.\n"+
                         "If internet acces requires proxy authentication optional user name and password can be used.";

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
                string proxyUser = null, proxyPw = null;
                if (args.Length == 1)
                {
                    string[] login = args[0].Split(":".ToCharArray());
                    proxyUser = login[0];
                    proxyPw = login[1];
                }
                string htmlTimeStr = GetHTML("http://tycho.usno.navy.mil/cgi-bin/timer.pl", proxyUser, proxyPw);
            
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    static string GetHTML(string url, string proxyUser, string proxyPw)
    {
        StringBuilder sb  = new StringBuilder();
        byte[] buf = new byte[8192];

        HttpWebRequest  request  = (HttpWebRequest)WebRequest.Create(url);
        if (proxyUser != null)
        {
            GlobalProxySelection.Select.Credentials = new NetworkCredential(proxyUser, proxyPw);
        }
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
        strMyDateTime =    strMyDateTime.Insert(strMyDateTime.IndexOf(",") + 1, GetCurrentYear() + ",");
     
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