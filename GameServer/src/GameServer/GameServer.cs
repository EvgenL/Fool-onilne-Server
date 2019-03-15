//TODO refractor this: add regions

using System;
using System.Collections.Generic;
using System.Text;
using FoolOnlineServer.GameServer.Packets;
using Logginf;
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
        /// Call it first. Starts a server instance.
        /// </summary>
        public static void ServerStart(int port)
        {
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
        /// Server's web socket callback on when somebody succesfully connected
        /// Checks his auth token and proceed
        /// </summary>
        private void OnNewSessionConnected(WebSocketSession session)
        {
            ClientManager.CreateNewConnection(session);
            }

        /// <summary>
        /// Callback on client sends data to server
        /// </summary>
        private void OnNewDataReceived(WebSocketSession session, byte[] data)
        {
            Client client = ClientManager.GetConnectedClient(session);
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

        /// <summary>
        /// Callback on client connection lost
        /// Removes client from ClientManager's list
        /// </summary>
        private void OnSessionClosed(WebSocketSession session, CloseReason value)
        {
            Client client = ClientManager.GetConnectedClient(session);
            client.Disconnect();
        }

        /// <summary>
        /// Shuts server down
        /// </summary>
        public static void StopServer()
        {
            _instance.webSocketServer.Stop();
            _instance.webSocketServer = null;
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
