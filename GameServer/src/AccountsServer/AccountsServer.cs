


/// Server which manages validation of client accounts
/// and sends them to gameserver


// DEFINES
#define TEST_MODE_LOCALHOST // if defined, will route client to localhost game server



using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using FoolOnlineServer.src.GameServer;
using Logging;
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

#if TEST_MODE_LOCALHOST
                gameServerIp = (string)configReader.GetValue("gameServerIp-local", typeof(string)); 
#else
            gameServerIp = (string)configReader.GetValue("gameServerIp", typeof(string));
#endif
            gameServerPort = (int)configReader.GetValue("gameServerPort", typeof(int));

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
            Log.WriteLine("Server started on port " + port, Instance);
        }

        private void OnNewSessionConnected(WebSocketSession session)
        {
            Log.WriteLine("OnNewSessionConnected", Instance);
        }

        /// <summary>
        /// Callback on client sends data to server
        /// </summary>
        private void OnNewDataReceived(WebSocketSession session, byte[] data)
        {
            XElement body = XElement.Parse(Encoding.Unicode.GetString(data));

            //Check version
            XElement versionCheck = GetChildElement(body, "VersionCheck");
            if (versionCheck != null)
            {
                //check client's version
                string clientVersion = versionCheck.Value;
                if (CheckVersion(clientVersion))
                {
                    //Connect player
                    XElement connection = GetChildElement(body, "Connection");
                    if (connection != null)
                    {
                        Login(session, data);
                    }
                }
                else //if version is not ok
                {
                    //send error
                    SendErrorAndClose(session, "Wrong version. Server version is " + serverVersion);
                }

            }

            
        }

        /// <summary>
        /// Finds element nested in XElement by local name
        /// </summary>
        /// <param name="body">XElement which to look</param>
        /// <param name="elementLocalName">Target name</param>
        /// <returns>Found xelement. Null if none</returns>
        private static XElement GetChildElement(XElement body, string elementLocalName)
        {
            foreach (var element in body.Elements())
            {
                if (element.Name.LocalName == elementLocalName)
                {
                    return element;
                }
            }

            return null;
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


            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "OPTIONS, POST, GET");
            response.AddHeader("Access-Control-Max-Age", "86400");

            string log = "";
            log += ("method: " + request.HttpMethod + "\n");
            foreach (var key in request.Headers.AllKeys)
            {
                log += key + ": " + request.Headers[key] + "\n";
            }
            Log.WriteLine("GOT REQUEST: \n" + log, typeof(AccountsServer));


            // CORS preflight request sent by browser
            if (request.HttpMethod == "OPTIONS" && request.Headers.AllKeys.Contains("Origin"))
            {
                ProcessPreflightRequest(context);
                return;
            }

            var headers = request.Headers.AllKeys;
            string contentType = request.Headers["Content-Type"];
            if (contentType != null && contentType.StartsWith("text/json"))
            {
                //Read body
                byte[] buffer = new byte[2048];
                int len = request.InputStream.Read(buffer, 0, buffer.Length);
                byte[] readBytes = new byte[len];
                Buffer.BlockCopy(buffer, 0, readBytes, 0, len);
                buffer = null;
                var encoding = request.ContentEncoding;
                string readString = encoding.GetString(readBytes);

                //parse as json
                var jsonBody = JObject.Parse(readString);


                if (request.HttpMethod == "POST")
                {

                    //version check
                    if (/*CheckVersion(context, jsonBody)*/true)
                    {
                        //Login
                        if (jsonBody["LoginMethod"].ToString() == "anonymous")
                        {
                            Login(context, jsonBody);

                        }
                        //register
                        else if (headers.Contains("Register"))
                        {

                        }
                    }

                }

                /*response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "OPTIONS, POST, GET");
                response.AddHeader("Access-Control-Max-Age", "86400");*/
                response.Close();
            }
            else
            {
                response.StatusCode = (int) HttpStatusCode.BadRequest;
                response.Close();
            }
            

        }


        /// <summary>
        /// Answers to browser preflight requset that we are OK with cors
        /// </summary>
        private static void ProcessPreflightRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string origin = request.Headers["Origin"];

            Log.WriteLine("Got CORS request with origin: " + origin, typeof(AccountsServer));

            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "OPTIONS, POST, GET");
            response.AddHeader("Access-Control-Max-Age", "86400");
            response.AddHeader("Access-Control-Allow-Headers", "content-type");
            //Access-Control-Allow-Headers

            response.Close();
        }

        /// <summary>
        /// Checks if client's version actual to servers version
        /// </summary>
        private static bool CheckVersion(string clientVersion)
        {
            Log.WriteLine("Dummy version check: ok", Instance);

            //if client is 1.3.1.2 and server is 1.3 then allow
            bool ok = clientVersion.StartsWith(serverVersion);

            return ok;
        }

        /// <summary>
        /// Validates player login data and closes sonnection
        /// </summary>
        private static bool Login(WebSocketSession session, byte[] data)
        {
            //Parse data as xml
            XElement body = XElement.Parse(Encoding.Unicode.GetString(data));

            XElement connection = GetChildElement(body, "Connection");
            if (connection == null)
            {
                //Send error info to player
                session.CloseWithHandshake((int)HttpStatusCode.Forbidden, "Login data not found in request");
                return false;
            }

            //read login method
            string loginMethod = GetChildElement(connection, "LoginMethod")?.Value;
            switch (loginMethod)
            {
                case "Anonymous":
                    //disconnect player if we dont allow anonymous
                    if ( !anonymousAllowed)
                    {
                        //Send error info to player
                        SendErrorAndClose(session, "Anonymous login is not allowed");
                        
                        return false;
                    }

                    //read user Nickname
                    string Nickname = GetChildElement(connection, "Nickname")?.Value;
                    if (Nickname == "")
                    {
                        //Send error info to player
                        session.CloseWithHandshake((int) HttpStatusCode.Forbidden, "Nickname is requered");
                        return false;
                    }

                    //create auth token
                    Token token = TokenManager.CreateAnonymousToken(Nickname);

                    //init response
                    //send game server endpoint
                    XElement response = new XElement("Response",
                        new XElement("LoginData",
                            new XElement("GameServerIp", gameServerIp),
                            new XElement("GameServerPort", gameServerPort),
                            new XElement("Token", token.TokenHash)
                            )
                        );


                    //Send 
                    SendXmlAndClose(session, response);

                    return true;


                //todo Oauth, Existing-account

                default: return false;
            }
        }

        /// <summary>
        /// Create response body wtih error description and cloese session
        /// </summary>
        private static void SendErrorAndClose(WebSocketSession session, string info)
        {
            //init response
            XElement response = new XElement("Response");
            response.Add("Error",
                new XElement("Info", info
                )
            );
            //Send 
            SendXmlAndClose(session, response);
        }

        /// <summary>
        /// Sends xml data coded in Unicode to session 
        /// and closes it
        /// </summary>
        private static void SendXmlAndClose(WebSocketSession session, XElement body)
        {
            var bytes = Encoding.Unicode.GetBytes(body.ToString());
            session.Send(bytes, 0, bytes.Length);
            session.Close();
        }

        private static bool Login(HttpListenerContext context, JObject jsonBody)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            

            string loginMethod = jsonBody["LoginMethod"].ToString();

            switch (loginMethod)
            {
                case "Anonymous":
                    if (anonymousAllowed)
                    {
                        if (jsonBody["UserId"].ToString() != "")
                        {
                            //validate nickname
                            string nickname = jsonBody["UserId"].ToString();

                            //validation ok
                            response.StatusCode = (int)HttpStatusCode.OK;
                            //send game server endpoint

                            response.Headers.Add("Game-server-ip", gameServerIp);
                            response.Headers.Add("Game-server-port", gameServerPort.ToString());


                            //send auth token
                            Token token = TokenManager.CreateAnonymousToken(nickname);
                            
                            response.AddHeader("Auth-token", token.TokenHash.ToString());

                            /*JObject jsonResponse = new JObject();
                            jsonResponse.Add("Game-server-ip", JToken.FromObject(gameServerIp));
                            jsonResponse.Add("Auth-token", token.TokenHash.ToString());
                            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonResponse.ToString());
                            response.OutputStream.Write(jsonBytes, 0, jsonBytes.Length);*/

                            byte[] bodyBytes = Encoding.UTF8.GetBytes(token.TokenHash.ToString());
                            response.OutputStream.Write(bodyBytes, 0, bodyBytes.Length);

                            return true;
                        }

                        response.StatusCode = (int)HttpStatusCode.Forbidden;
                        response.Headers.Add("Info", "Nickname required");
                        return false;
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.Forbidden;
                        response.Headers.Add("Info", "Anonymous login is not allowed");
                        return false;
                    }
                    break;

                //todo Oauth, Existing-account

                default: return false;
            }
            


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
