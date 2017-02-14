//css_ref App.exe;
using System;
using System.Threading;
using System.Windows.Forms;

class Script
{
    static public void Main()
    {
        IApplication app = new Form1();

        app.Start(); Thread.Sleep(1000);
		
		app.SetAge(17); Thread.Sleep(1000);
		
		app.SetEmail("john.smith@gmail.com"); Thread.Sleep(1000); 
		
		app.SetName("John Smith"); Thread.Sleep(1000);
		
		app.SetDateOfBirth(DateTime.Now.AddYears(-17)); Thread.Sleep(1000);
		
        app.Stop();
    }
}

