using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FoolOnlineServer.Db;
using Logginf;

namespace FoolOnlineServer.Utils {
	public static class Email {
		private static string Sender       = "";
		private static string SmtpPassword = "";
		private static string SmtpHost     = "";
		private static int    SmtpPort     = 465;


		public static void LoadSettings() {
			Sender       = ServerSettings.Get("email_sender");
			SmtpPassword = ServerSettings.Get("email_pwd");
			SmtpHost     = ServerSettings.Get("email_smtp_host");
			SmtpPort     = Convert.ToInt32(ServerSettings.Get("email_smtp_port"));
		}


		public static bool SendEmail(string subject, string message, string receiver, string senderName = "") {
			if (string.IsNullOrEmpty(Sender)) {
				Log.WriteLine("Email not set", typeof(Email));
				return false;
			}
			if (string.IsNullOrEmpty(SmtpPassword)) {
				Log.WriteLine("Email password not set", typeof(Email));
				return false;
			}
			if (string.IsNullOrEmpty(SmtpHost)) {
				Log.WriteLine("Smtp host not set", typeof(Email));
				return false;
			}
			if (string.IsNullOrEmpty(SmtpHost)) {
				Log.WriteLine("Smtp port not set", typeof(Email));
				return false;
			}

			SmtpClient smtp = new SmtpClient(SmtpHost, 25) {
				Credentials = new NetworkCredential(Sender, SmtpPassword), EnableSsl = true
			};
			MailMessage mail = new MailMessage {
				From            = new MailAddress(Sender, senderName),
				SubjectEncoding = Encoding.UTF8,
				BodyEncoding    = Encoding.UTF8,
				Subject         = subject,
				Body            = message,
				To              = { new MailAddress(receiver) }
			};

			try {
				smtp.Send(mail);
				Log.WriteLine("Message sent successfully!", typeof(Email));
			}
			catch (SmtpException e) {
				Log.WriteLine("Error!Message not sent! " + e.Message, typeof(Email));
				return false;
			}

			return true;
		}
	}
}