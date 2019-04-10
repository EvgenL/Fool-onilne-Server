using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using FoolOnlineServer.GameServer;
using FoolOnlineServer.HTTPServer.Pages.Payment;
using FoolOnlineServer.TimeServer.Listeners;
using FoolOnlineServer.Utils;
using Logginf;
using MySql.Data.MySqlClient;

namespace FoolOnlineServer
{
    /// <summary>
    /// Program entry point
    /// </summary>
    public class Program
    {

        private static void Main(string[] args)
        {
            // Start console thread for reading a commands
            ConsoleThread.Start();

            // Start login server
            AccountsServer.AccountsServer.ServerStart(5054);

            // Start game server
            GameServer.GameServer.ServerStart(5055);
           
            // Starting HTTP server
            HTTPServer.HTTPServer.StartServer(5056);

            TimeServer.TimeServer.Init();
            Email.LoadSettings();

            //Payment.SendPayment();
            
            string SmtpHost = "smtp.yandex.ru";
            int SmtpPort = 25; // 25

            string Sender = "noreply-foolonline@yandex.ru";
            string SmtpPassword = "12345678Test";

            string senderName = "Хуй с горы";
            string subject = "Тема темскaя";
            string message = "Привет";
            string receiver = "FoolPayout@yandex.ru";
            bool EnableSsl = false;


            SmtpClient smtp = new SmtpClient(SmtpHost, SmtpPort)
            {
                Credentials = new NetworkCredential(Sender, SmtpPassword),
                EnableSsl = EnableSsl
            };

            try
            {
                MailMessage mail = new MailMessage
                {
                    From = new MailAddress(Sender, senderName),
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8,
                    Subject = subject,
                    Body = message,
                    To = { new MailAddress(receiver) }
                };

                smtp.Send(mail);
            }
            catch (SmtpException e)
            {
                Log.WriteLine("Error! Mail not sent! " + e.Message, typeof(Email));
                return;
            }
            catch (Exception e)
            {
                Log.WriteLine(e.Message, typeof(Email));
                return;
            }
            Log.WriteLine("OK", typeof(Email));
            
             
        }
    }
}
