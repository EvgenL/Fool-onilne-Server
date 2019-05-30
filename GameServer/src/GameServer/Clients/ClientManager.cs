using System;
using System.Collections.Generic;
using Logginf;
using SuperWebSocket;

namespace FoolOnlineServer.GameServer.Clients
{
    internal static class ClientManager
    {
        /// <summary>
        /// ConnectionId-Client pairs
        /// where ConnectionId is also a port of client's remote endpoint
        /// </summary>
        private static Dictionary<long, Client> clients = new Dictionary<long, Client>();

        /// <summary>
        /// Adds client session to client list and assigns a connection id to client
        /// </summary>
        public static void CreateNewConnection(WebSocketSession session)
        {
            lock (clients)
            {
                // instanciate new client object
                Client client = new Client(session);

                // add to clients list
                clients.Add(client.ConnectionId, client);

                Log.WriteLine($"Client connected. ConnectionId: {client.ConnectionId} IP: {session.RemoteEndPoint}",
                    typeof(ClientManager));
            }
        }

        /// <summary>
        /// Removes client session to client list
        /// </summary>
        public static void Disconnect(long connectionId)
        {
            // remove from clients list
            clients.Remove(connectionId);

        }

        /// <summary>
        /// Returns client object by connection id
        /// </summary>
        public static Client GetConnectedClient(long connectionId)
        {
            // sometimes this method is called earlier than Client object is created
            // todo use named semaphore
            lock (clients)
            {
                clients.TryGetValue(connectionId, out Client client);

                if (client == null)
                {
                    throw new Exception("Trying to get not existing client. ConnectionId: " + connectionId);
                }

                return client;
            }
        }

        /// <summary>
        /// Returns client object by session
        /// </summary>
        public static Client GetConnectedClient(WebSocketSession session)
        {
            return GetConnectedClient(session.RemoteEndPoint.Port);
        }

        /// <summary>
        /// Returns a number of connections
        /// </summary>
        public static long GetOnlineClientsCount()
        {
            return clients.Count;
        }

    }
}
