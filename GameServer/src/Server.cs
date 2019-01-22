//TODO refractor this: add regions

using System;
using System.Net;
using System.Net.Sockets;
using GameServer.Packets;


namespace GameServer
{
    /// <summary>
    /// Calss that handles network connection between clients and server
    /// </summary>
    class Server : IDisposable //IDisposable needed for closing listener on server stop
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
        private TcpListener serverSocket;

        /// <summary>
        /// Slots for clients.
        /// Stores both busy and empty slots.
        /// </summary>
        private static Client[] Clients; 

        /// <summary>
        /// Call it first. Starts a server instance.
        /// </summary>
        public void ServerStart()
        {
            Clients = new Client[StaticParameters.MaxClients];
            for (int i = 0; i < StaticParameters.MaxClients; i++)
            {
                Clients[i] = new Client();
            }

            //Creating a new listener. IPAddress.Any means that client with any ipp can connect to us
            serverSocket = new TcpListener(IPAddress.Any, 5055);
            serverSocket.Start();

            //Beginning accepting sockets asynchronously
            serverSocket.BeginAcceptSocket(OnClientConnectCallback, null);
            Log.WriteLine("Server started", this);

            //DatabaseOperations.AddAccount("New u\";ser", "123456", "test@example.com");
        }

        /// <summary>
        /// Called on client connected
        /// </summary>
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

            Log.WriteLine("Connection rejected from " + client.Client.RemoteEndPoint /*that's an ip*/ + ". Server is full.", this);
            client.Close();
            client = null;

            //client.GetStream().Wr;
        }

        /// <summary>
        /// Shuts server down
        /// </summary>
        public static void StopServer()
        {
            _instance.serverSocket.Stop();
            _instance.serverSocket = null;
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
