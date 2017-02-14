using System;
using System.Windows.Forms;
using System.Web;
using System.IO;
using System.Web.Mail;

namespace CSScript
{
	class Script
	{
		const string usage = "Usage: cscscript smtpMailTo smtpServer to subject body [file0] [fileN] ...\nSends e-mail to the specified address (use \"\" for local smtp server)\n"+
		  					 "Example: cscscript \"\" user@domain \"Test\" \"This mail was sent by CS-Scrypt.\" \"c:\\boot.ini\" \n";
		[STAThread]
		static public void Main(string[] args)
		{
			if (args.Length < 4 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
			{
				Console.WriteLine(usage);
			}
			else
			{
				try
				{
					string server = args[0];
					string to = args[1];
					string subject = args[2];
					string body = args[3];
					string[] attachments = null;
					if (args.Length > 4)
					{
						attachments = new string[args.Length - 4];
						for (int i = 0; i < attachments.Length; i++)
						{
							attachments[i] = args[4 + i];
						}
					}
	
					MailMessage myMail = new MailMessage();
					myMail.To = to;
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