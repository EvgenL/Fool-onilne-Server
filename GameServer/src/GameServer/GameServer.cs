﻿//TODO refractor this: add regions

using System;
using System.Collections.Generic;
using System.Text;
using FoolOnlineServer.GameServer.Packets;
using FoolOnlineServer.src.GameServer;
using Logging;
using SuperSocket.SocketBase;
using SuperWebSocket;

namespace FoolOnlineServer.GameServer
{
    /// <summary>
    /// Calss that handles network connection between clients and server
    /// </summary>
    public class GameServer : IDisposable //IDisposable needed for closing listener on server stop
    {

        #region Singleton

        private static GameServer _instance;
        public static GameServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameServer();
                }

                return _instance;
            }
            private set { }
        } //This would be null until first use of 'var something = Network.Instance' or calling a function from this instance

        #endregion

        /// <summary>
        /// Object that listenes to connections to my server
        /// </summary>
        private WebSocketServer webSocketServer;

        /// <summary>
        /// Slots for clients.
        /// Stores both busy and empty slots.
        /// </summary>
        private static Client[] Clients;

        private static readonly Dictionary<WebSocketSession, Client> sessionClientPairs =
            new Dictionary<WebSocketSession, Client>();

        private static Dictionary<string, Client> tokenClientPairs 
            = new Dictionary<string, Client>();

        //todo private static HashSet<Client> Clients;

        /// <summary>
        /// Call it first. Starts a server instance.
        /// </summary>
        public static void ServerStart(int port)
        {
            //Init client list
            Clients = new Client[StaticParameters.MaxClients];
            for (int i = 0; i < StaticParameters.MaxClients; i++)
            {
                Clients[i] = new Client();
            }

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

        /// <summary>
        /// Client sends auth token
        /// This method checks if token is correct
        /// and marks user as authorized.
        /// Also sends Send_ErrorBadAuthToken and Send_AuthorizedOk 
        /// </summary>
        /// <param name="connectionId">User who sent</param>
        /// <param name="tokenString">user's token string</param>
        /// <returns>true on succesful connect, false on fail</returns>
        public static bool AuthorizeClient(long connectionId, int tokenHash)
        {
            Client client = GetClient(connectionId);

            //If client was already authorized then ignore
            if (client.Authorized)
            {
                return true;
            }

            //get token from manager if exists
            Token token = TokenManager.UseToken(tokenHash);

            //if token doesn't exist then send error
            if (token == null)
            {
                ServerSendPackets.Send_ErrorBadAuthToken(connectionId);
                return false;
            }

            //Authorized OK
            client.Authorized = true;
            client.AuthToken = token;
            ServerSendPackets.Send_AuthorizedOk(connectionId);
            client.UserId = token.UserId;
            client.Nickname = token.Nickname;
            ServerSendPackets.Send_UpdateUserData(connectionId);
            return true;
        }


        private void OnNewSessionConnected(WebSocketSession session)
        {

            //Try assign socket to newly connected client if server's not full
            for (int i = 0; i < Clients.Length; i++)
            {
                //Finding first free socket
                if (!Clients[i].Online())
                {
                    sessionClientPairs.Add(session, Clients[i]);
                    Clients[i].Session = session;
                    Clients[i].ConnectionId = i; //Set connection index
                    Clients[i].IP = session.RemoteEndPoint; //Client's ip
                    Clients[i].Authorized = false;
                    //todo Clients[i].IP = session.
                    Log.WriteLine($"Client connected. Index: {i} IP: {session.RemoteEndPoint}", this); //TODO normal say function

                    return; //Exit after succesfully assigned id to client
                }
            }

            Log.WriteLine("Connection rejected from " + session.RemoteEndPoint + ". Server is full.", this);
        }

        /// <summary>
        /// Callback on client sends data to server
        /// </summary>
        private void OnNewDataReceived(WebSocketSession session, byte[] data)
        {
            Client client = sessionClientPairs[session];
            client.OnNewDataReceived(data);
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
            if (sessionClientPairs.ContainsKey(session))
            {
                Client client = sessionClientPairs[session];
                client.Disconnect();
                sessionClientPairs.Remove(session);
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

        /// <summary>
        /// Gets a client object with a specified index
        /// </summary>
        /// <param name="i">ConnctionID of the client</param>
        /// <returns>Client</returns>
        public static Client GetClient(long i)
        {
            return Clients[i];
        }

        /// <summary>
        /// Returns a number of connections
        /// </summary>
        public static long GetOnlineClientsCount()
        {
            long result = 0;

            //Loop through all the clients
            for (int i = 0; i < StaticParameters.MaxClients; i++)
            {
                Client client = GameServer.GetClient(i);

                if (client.Online())
                {
                    result++;
                }
            }

            return result;
        }


        #region IDisposable

        // Track whether Dispose has been called.
        private bool disposed = false;

        /// <summary>
        /// Called by IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called by IDisposable
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    StopServer();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                //CloseHandle(handle);
                //handle = IntPtr.Zero;

                // Note disposing has been done.
                disposed = true;
            }
        }


        #endregion

    }
}
