using System;
using System.Net;
using System.Net.Sockets;
using Evgen.Byffer;
using FoolOnlineServer.GameServer.Packets;
using FoolOnlineServer.GameServer.RoomLogic;
using FoolOnlineServer.src.GameServer;
using Logging;
using SuperWebSocket;

namespace FoolOnlineServer.GameServer
{
    public class Client
    {
        /// <summary>
        /// Clien's display name (not unique)
        /// //todo register, store nicknames
        /// </summary>
        public string Nickname;

        /// <summary>
        /// Clien's registration name (unique)
        /// //todo register, store idnames
        /// </summary>
        public string UserId;

        /// <summary>
        /// Client's unique number
        /// </summary>
        public int ConnectionId;

        /// <summary>
        /// Client device's IP adress
        /// </summary>
        public IPEndPoint IP;

        /// <summary>
        /// Socket to where this client should write data
        /// </summary>
        public TcpClient Socket;

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
        public Token AuthToken;

        /// <summary>
        /// Set to true if client did send token
        /// </summary>
        public bool Authorized;

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
            ServerHandlePackets.HandleData(ConnectionId, data);
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
                RoomInstance room = RoomManager.GetRoomForPlayer(this.ConnectionId);
                room.LeaveRoom(this.ConnectionId);
            }

            Session = null;

            //if client was in room then wait for him to return
            RoomManager.OnClientDisconnectedSuddenly(ConnectionId);
        }

        public override string ToString()
        {
            return $"Client {ConnectionId}: {IP}";
        }
    }
}
