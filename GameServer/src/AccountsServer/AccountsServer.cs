using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using FoolOnlineServer.Db;
using FoolOnlineServer.Extensions;
using Logginf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.SocketBase;
using SuperWebSocket;





namespace FoolOnlineServer.AccountsServer
{
    /// <summary>
    /// Server which manages validation of client accounts
    /// and sends them to gameserver
    /// </summary>
    internal class AccountsServer
    {

        #region Singleton

        private static AccountsServer _instance;
        public static AccountsServer Instance => _instance ?? (_instance = new AccountsServer());

        #endregion

        public static string ServerVersion;
        public static bool AnonymousAllowed;
        public static string GameServerIp;
        public static int GameServerPort;

        /// <summary>
        /// Object that listenes to connections to my server
        /// </summary>
        private WebSocketServer webSocketServer;
        private static HttpListener listener;

        public static async void ServerStart(int port)
        {
            // check if database exists 
            DatabaseConnection.TestIfDatabaseExists();

            //Read and buffer config
            var configReader = new AppSettingsReader();
            ServerVersion = (string)configReader.GetValue("serverVersion", typeof(string));
            AnonymousAllowed = (bool)configReader.GetValue("anonymousAllowed", typeof(bool));

            if ((bool) configReader.GetValue("testModeUseLocalhost", typeof(bool)))
            {
                GameServerIp = (string)configReader.GetValue("gameServerIp_local", typeof(string));
            }
            else
            {
                //GameServerIp = (string)configReader.GetValue("gameServerIp", typeof(string));

                // get my ip 
                var httpClient = new HttpClient();
                GameServerIp = await httpClient.GetStringAsync("https://api.ipify.org");
                // and send to player
                Log.WriteLine($"My public IP address is: {GameServerIp}", typeof(AccountsServer));
            }
            GameServerPort = (int)configReader.GetValue("gameServerPort", typeof(int));



            //Creating a new server instance
            Instance.webSocketServer = new WebSocketServer();

            //trying start up on port
            var ws = Instance.webSocketServer;
            if (!ws.Setup(port))
            {
                throw new Exception("Error starting on port");
            }

            //Init callbacks
            ws.NewSessionConnected += OnNewSessionConnected;
            ws.NewDataReceived += OnNewDataReceived;
            ws.NewMessageReceived += OnNewMessageReceived;
            ws.SessionClosed += OnSessionClosed;

            //Start server and begin accepting sockets
            ws.Start();
            Log.WriteLine("Server started on port " + port, Instance);
        }

        private static void OnNewSessionConnected(WebSocketSession session)
        {
            Log.WriteLine("OnNewSessionConnected", Instance);
        }

        /// <summary>
        /// Callback on client sends data to server
        /// </summary>
        private static void OnNewDataReceived(WebSocketSession session, byte[] data)
        {
            // read bytes as xml
            XElement body = XElement.Parse(Encoding.Unicode.GetString(data));


            Log.WriteLine("OnNewDataReceived " + body, typeof(AccountsServer));

            //todo Check version SendErrorAndClose(session, "Wrong version. Server version is " + serverVersion);
            //todo add node VersionCheck

            // read connection data
            XElement connectionXml = body.GetChildElement("Connection");
            if (!AccountsService.ValidateConnectionXml(session, connectionXml)) return;

            // read login method
            string loginMethod = connectionXml.GetChildElement("LoginMethod")?.Value;
            switch (loginMethod)
            {
                case "Anonymous":
                    AccountsService.AnonLogin(session, connectionXml);
                    break;

                case "EmailLogin":
                    AccountsService.EmailLogin(session, connectionXml);
                    break;

                case "EmailRegistration":
                    AccountsService.EmailRegistration(session, connectionXml);
                    break;


                //todo Oauth, Existing-account

                default: return;
            }

        }

        /// <summary>
        /// Callback on client sends string data to server
        /// </summary>
        private static void OnNewMessageReceived(WebSocketSession session, string value)
        {
            //get message encoding
            var encoding = session.Charset;
            //pass to data processing method
            OnNewDataReceived(session, encoding.GetBytes(value));
        }

        private static void OnSessionClosed(WebSocketSession session, CloseReason value)
        {
            Log.WriteLine("OnSessionClosed " + session + ". Reason: " + value, typeof(AccountsServer));
        }


        /// <summary>
        /// Shuts server down
        /// </summary>
        public static void StopServer()
        {
            _instance.webSocketServer.Stop();
            _instance.webSocketServer = null;
        }
        
    }
}
