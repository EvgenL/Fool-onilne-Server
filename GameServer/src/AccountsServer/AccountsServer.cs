﻿using System;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoolOnlineServer.src.GameServer;
using Logging;
using Newtonsoft.Json.Linq;
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
            gameServerIp = (string)configReader.GetValue("gameServerIp-local", typeof(string)); //TODO there we return server ip to where client connects next
            gameServerPort = (int)configReader.GetValue("gameServerPort", typeof(int));

            //Create listener
            listener = new HttpListener();
            listener.Prefixes.Add($"http://+:{port}/");
            listener.Start();
            listener.BeginGetContext(OnGetContext, null);
            Log.WriteLine("Server started on port " + port, Instance);
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

            var headers = request.Headers.AllKeys;
            if (request.Headers["Content-Type"].StartsWith("text/json"))
            {
                //Read body
                byte[] buffer = new byte[4096];
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
                    if (CheckVersion(context))
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
                response.Close();
            }
            else
            {
                response.StatusCode = (int) HttpStatusCode.BadRequest;
                response.Close();
            }
            

        }

        /// <summary>
        /// Checks if client's version actual to servers version
        /// Sends following headers to client:
        /// Version-check
        /// Info
        /// Auth-token
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
                else //else if version is outdated
                {
                    response.AddHeader("Version-check", "Error");
                    response.Headers.Add("Info", "Wrong version.");
                    response.StatusCode = (int) HttpStatusCode.Forbidden;
                    return false;
                }
            }
            else //else if version not sent
            {
                response.AddHeader("Version-check", "Error");
                response.Headers.Add("Info", "Did not found 'Client-version' header.");
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                return false;
            }
        }


        private static bool Login(HttpListenerContext context, JObject jsonBody)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            

            string loginMethod = jsonBody["LoginMethod"].ToString();

            switch (loginMethod)
            {
                case "anonymous":
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
