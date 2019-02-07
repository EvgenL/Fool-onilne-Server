using System;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Logging;
using SuperSocket.SocketBase;
using SuperSocket.SocketEngine;
using SuperWebSocket;

namespace FoolOnlineServer.AccountsServer
{
    //todo add version check
    /// <summary>
    /// Server which manages validation of client accounts
    /// and sends them to gameserver
    /// </summary>
    internal class AccountsServer
    {

        #region Singleton

        private static AccountsServer _instance;
        public static AccountsServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AccountsServer();
                }

                return _instance;
            }
            private set { }
        } 

        #endregion

        /// <summary>
        /// version of server
        /// </summary>
        private static string serverVersion;
        private static bool anonymousAllowed;
        private static string gameServerIp;
        private static int gameServerPort;

        /// <summary>
        /// Object that listenes to connections to my server
        /// </summary>
        private WebSocketServer webSocketServer;
        private static HttpListener listener;

        public static void ServerStart(int port)
        {
            //Read and buffer config
            var configReader = new AppSettingsReader();
            serverVersion = (string)configReader.GetValue("serverVersion", typeof(string));
            anonymousAllowed = (bool)configReader.GetValue("anonymousAllowed", typeof(bool));
            gameServerIp = (string)configReader.GetValue("gameServerIp", typeof(string));
            gameServerPort = (int)configReader.GetValue("gameServerPort", typeof(int));

            //Create listener
            listener = new HttpListener();
            listener.Prefixes.Add($"http://+:{port}/");
            listener.Start();
            listener.BeginGetContext(OnGetContext, null);
            Log.WriteLine("Server started on port " + port, Instance);

            /*
            //Creating a new server instance
            Instance.webSocketServer = new WebSocketServer();

            //trying start up on port
            var ws = Instance.webSocketServer;
            if (!ws.Setup(port))
            {
                Log.WriteLine("Error starting on port " + port, Instance);
            }

            //Init callbacks
            ws.NewSessionConnected += _instance.OnNewSessionConnected;
            ws.NewDataReceived += _instance.OnNewDataReceived;
            ws.NewMessageReceived += _instance.OnNewMessageReceived;
            ws.SessionClosed += _instance.OnSessionClosed;

            //Start server and begin accepting sockets
            ws.Start();
            */

        }

        /// <summary>
        /// Callback on when client sent us data.
        /// Mananaged in new thread
        /// </summary>
        private static void OnGetContext(IAsyncResult ar)
        {
            try
            {
                HttpListenerContext context = listener.EndGetContext(ar);
                listener.BeginGetContext(OnGetContext, null);
                
                ProcessRequest(context);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.HttpMethod == "POST")
            {

                //version check
                if (CheckVersion(context))
                {
                    //Login
                    var headers = request.Headers.AllKeys;
                    if (headers.Contains("Login"))
                    {
                        Login(context);
                        
                    }
                    else if (headers.Contains("Register"))
                    {
                        
                    }
                }

            }

            response.Close();
        }

        /// <summary>
        /// Checks if client's version actual to servers version
        /// </summary>
        private static bool CheckVersion(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            var headers = request.Headers.AllKeys;
            if (headers.Contains("Client-version"))
            {
                string clientVersion = request.Headers["Client-version"];

                //if client is 1.3.1.2 and server is 1.3
                //then allow
                if (clientVersion.StartsWith(serverVersion))
                {
                    response.AddHeader("Version-check", "Ok");
                    return true;
                }
                else
                {
                    response.AddHeader("Version-check", "Error");
                    response.Headers.Add("Info", "Wrong version.");
                    response.StatusCode = (int) HttpStatusCode.Forbidden;
                    return false;
                }
            }
            else
            {
                response.AddHeader("Version-check", "Error");
                response.Headers.Add("Info", "Did not found 'Client-version' header.");
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                return false;
            }
        }

        private static bool Login(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            var headers = request.Headers.AllKeys;

            string loginMethod = request.Headers["Login"];

            switch (loginMethod)
            {
                case "anonymous":
                    if (anonymousAllowed)
                    {
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.Headers.Add("Game-server-ip", gameServerIp);
                        response.Headers.Add("Game-server-port", gameServerPort.ToString());
                        return true;
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.Forbidden;
                        response.Headers.Add("Info", "Anonymous login is not allowed.");
                        return false;
                    }
                    break;

                default: return false;
            }
            


        }

        private void OnNewSessionConnected(WebSocketSession session)
        {
            Log.WriteLine("Connected to accounts server: " + session.RemoteEndPoint, this);
        }

        /// <summary>
        /// Callback on client sends data to server
        /// </summary>
        private void OnNewDataReceived(WebSocketSession session, byte[] data)
        {
            //todo if allow anonymous

        }

        /// <summary>
        /// Callback on client sends string data to server
        /// </summary>
        private void OnNewMessageReceived(WebSocketSession session, string value)
        {
            //get message encoding
            var encoding = session.Charset;
            //pass to data processing method
            OnNewDataReceived(session, encoding.GetBytes(value));
        }

        private void OnSessionClosed(WebSocketSession session, CloseReason value)
        {
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
