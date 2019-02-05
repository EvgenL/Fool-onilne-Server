﻿//TODO refractor this: add regions

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GameServer.Packets;
using SuperSocket.SocketBase;
using SuperWebSocket;

namespace GameServer
{
    /// <summary>
    /// Calss that handles network connection between clients and server
    /// </summary>
    public class Server : IDisposable //IDisposable needed for closing listener on server stop
    {

        #region Singleton

        private static Server _instance;
        public static Server Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Server();
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

        private static Dictionary<WebSocketSession, Client> sessionClientPairs =
            new Dictionary<WebSocketSession, Client>();

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

            //Creating a new server (also implict creation of new instance)
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
            
            //Try assign socket to newly connected client if server's not full
            for (int i = 0; i < Clients.Length; i++)
            {
                //Finding first free socket
                if (!Clients[i].Online())
                {
                    sessionClientPairs.Add(session, Clients[i]);
                    //Clients[i].Socket = client; //Assign newly created socket to this client
                    Clients[i].Session = session;
                    Clients[i].ConnectionId = i; //Set connection index
                    Clients[i].IP = session.RemoteEndPoint; //Client's ip
                    //todo Clients[i].IP = session.
                    Log.WriteLine($"Client connected. Index: {i} IP: {session.RemoteEndPoint}", this); //TODO normal say function
                    Clients[i].Start(); //Start recieving data from client

                    //send welcome message when client is connected
                    ServerSendPackets.Send_Information(i);

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
            OnNewDataReceived(session, Encoding.ASCII.GetBytes(value));
        }

        private void OnSessionClosed(WebSocketSession session, CloseReason value)
        {
            if (sessionClientPairs.ContainsKey(session))
            {
                Client client = sessionClientPairs[session];
                client.CloseConnection();
                sessionClientPairs.Remove(session);
            }
        }

        /// <summary>
        /// Called on client connected
        /// </summary>
       /*
        private void OnClientConnectCallback(IAsyncResult result)
        {
            TcpClient client = serverSocket.EndAcceptTcpClient(result);
            client.NoDelay = false;
            
            //Call this recursivelly while server is up
            serverSocket.BeginAcceptTcpClient(OnClientConnectCallback, null);

            //////////////////CONNECTING A NEW CLIENT//////////////////
            
            //Try assign socket to newly connected client if server's not full
            for (int i = 0; i < Clients.Length; i++)
            {
                //Finding first free socket
                if (!Clients[i].Online())
                {
                    Clients[i].Socket = client; //Assign newly created socket to this client
                    Clients[i].ConnectionId = i; //Set connection index
                    Clients[i].IP = client.Client.RemoteEndPoint.ToString(); //Client's ip
                    Log.WriteLine($"Client connected. Index: {i} IP: {client.Client.RemoteEndPoint}", this); //TODO normal say function
                    Clients[i].Start(); //Start recieving data from client

                    //send welcome message when client is connected
                    ServerSendPackets.Send_Information(i);

                    return; //Exit after succesfully assigned id to client
                }
            }

            Log.WriteLine("Connection rejected from " + client.Client.RemoteEndPoint + ". Server is full.", this);
            client.Close();
            client = null;

            //client.GetStream().Wr;
        }
        */

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
                Client client = Server.GetClient(i);

                if (!client.Online())
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
