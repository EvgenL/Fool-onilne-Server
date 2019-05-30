using System.Net;
using Evgen.Byffer;
using FoolOnlineServer.Db;
using FoolOnlineServer.GameServer.Packets;
using FoolOnlineServer.GameServer.RoomLogic;
using Logginf;
using SuperWebSocket;

namespace FoolOnlineServer.GameServer.Clients
{

    /// <summary>
    /// Object represinting connected client
    /// Wraps WebSocketSession object and prowides methods
    /// to managing it
    /// </summary>
    public class Client
    {
        /// <summary>
        /// Constructor 
        /// </summary>
        public Client(WebSocketSession session)
        {
            this.Session = session;
            this.IP = session.RemoteEndPoint;
            this.ConnectionId = IP.Port;
            this.Authorized = false;
        }

        /// <summary>
        /// Class that handles byteArrays of data
        /// </summary>
        private static PacketHandlerTransportLayer packetHandler = new PacketHandlerTransportLayer();

        /// <summary>
        /// Clien's account data container
        /// </summary>
        public FoolUser UserData;

        /// <summary>
        /// Client's unique number
        /// </summary>
        public int ConnectionId;

        /// <summary>
        /// Client device's IP adress
        /// </summary>
        public IPEndPoint IP { get; private set; }

        /// <summary>
        /// Session to where this client should write data
        /// </summary>
        public WebSocketSession Session;
        
        /// <summary>
        /// Buffer for reading data
        /// </summary>
        private byte[] readBuffer;

        private ByteBuffer buffer;

        /// <summary>
        /// Is player in room
        /// </summary>
        public bool IsInRoom = false;

        /// <summary>
        /// If player's IsInRoom ten this will be room's id
        /// </summary>
        public long RoomId;

        /// <summary>
        /// On what place (chair) player is sitting in room
        /// </summary>
        public int SlotInRoom;

        /// <summary>
        /// Did player clicked 'ready' button in room before game start
        /// </summary>
        public bool IsReady = false;

        /// <summary>
        /// Client's token
        /// </summary>
        private Token AuthToken;

        /// <summary>
        /// Set to true if client did send token
        /// </summary>
        public bool Authorized;

        /// <summary>
        /// Amount of money on this account
        /// </summary>
        public double Money;

        /// <summary>
        /// Authorize client by token and set Authorized flag to true
        /// </summary>
        /// <param name="token"></param>
        public void Authorize(Token token)
        {
            this.UserData = token.OwnerUser;

            // set authorized status
            this.Authorized = true;
            this.AuthToken = token;
        }

        /// <summary>
        /// Clean buffer. Create new if null.
        /// </summary>
        public void CleanBuffer()
        {
            if (buffer == null)
            {
                buffer = new ByteBuffer();
            }
            buffer.Clear();
        }

        /// <summary>
        /// Returns buffer for reading data
        /// </summary>
        public ByteBuffer GetReadBuffer()
        {
            return buffer;
        }

        /// <summary>
        /// Tells if this user was connected or no.
        /// </summary>
        public bool Online()
        {
            return Session != null && Session.Connected;
        }

        public void OnNewDataReceived(byte[] data)
        {
            packetHandler.HandleData(ConnectionId, data);
        }

        /// <summary>
        /// Destroys connection between this client and server and marks this socket as free (null)
        /// </summary>
        public void Disconnect(string disconnectReason = null) //todo send disconnect reason (todo by enum)
        {
            Log.WriteLine("Disconnected", this);

            //If i was in room i disconnect. //TODO wait for reconnect if it was not intentional
            if (this.IsInRoom)
            {
                //if client was in room then wait for him to return
                RoomManager.OnClientDisconnectedSuddenly(ConnectionId);
            }

            Session = null;

            ClientManager.Disconnect(ConnectionId);
            TokenManager.DeleteToken(AuthToken);
        }

        public override string ToString()
        {
            return $"User {ConnectionId} {IP}";
        }
    }
}
