using System;
using System.Windows.Forms;
using System.Web;
using System.IO;
using System.Web.Mail;

namespace CSScript
{
	class Script
	{
		const string usage = "Usage: cscscript smtpMailTo smtpServer to from subject body [file0] [fileN] ...\nSends e-mail to the specified address (use \"\" for local smtp server)\n"+
		  					 "Example: cscscript smtpMailTo \"\" user_to@domain user_from@domain \"Test\" \"This mail was sent by CS-Script.\" \"c:\\boot.ini\" \n";

		[STAThread]						
		static public void Main(string[] args)
		{
			if (args.Length < 5 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
			{
				Console.WriteLine(usage);
			}
			else
			{
				try
				{
					string server = args[0];
					string to = args[1];
					string from = args[2];
					string subject = args[3];
					string body = args[4];
					string[] attachments = null;
					if (args.Length > 5)
					{
						attachments = new string[args.Length - 5];
						for (int i = 0; i < attachments.Length; i++)
						{
							attachments[i] = args[5 + i];
						}
					}
	
					MailMessage myMail = new MailMessage();
					myMail.To = to;
					myMail.From = from;
					myMail.Subject = subject;
					myMail.Body = body;
					if (attachments != null)
					{
						
						foreach (string file in attachments)
						{
							if (File.Exists(file))
							{
								string filePath = Path.GetFullPath(file);
								myMail.Attachments.Add(new MailAttachment(filePath, MailEncoding.Base64));
							}
							else
								throw new Exception("File "+file+" cannot be attached");
						}
					}
	
					SmtpMail.SmtpServer = server;
					SmtpMail.Send(myMail);
				}
				catch(Exception e)
				{
					Console.WriteLine(e.Message);
					return;
				}

				Console.WriteLine("Message has been sent");
			}
		}
	}

}