using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using FoolOnlineServer.Db;
using FoolOnlineServer.GameServer;
using FoolOnlineServer.HTTPServer.Pages.Payment;
using FoolOnlineServer.src.ConsoleCommands;
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
            // if args are used
            ProcessArgs.Process(args);
            
            // Check MySql connection. If ok then open server
            DatabaseConnection.TestConnection(onConnected: OpenServer);
        }

        /// <summary>
        /// Opens server for connections if database is accesible
        /// </summary>
        private static void OpenServer()
        {
            // Start console thread for reading a commands
            ConsoleThread.Start();

            // Start login server
            AccountsServer.AccountsServer.ServerStart(5054);

            // Start game server
            GameServer.GameServer.ServerStart(5055);

            // Starting HTTP server
            //HTTPServer.HTTPServer.StartServer(5056);

            TimeServer.TimeServer.Init();
            //Email.LoadSettings();

            //Payment.SendPayment();
        }
    }
}
