﻿using System;
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

    /// <summary>
    /// Object represinting connected client
    /// Wraps WebSocketSession object and prowides methods
    /// to managing it
    /// </summary>
    class Client
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
        /// Authorize client and set Authorized flag to true
        /// </summary>
        /// <param name="token"></param>
        public void Authorize(Token token)
        {
            this.Authorized = true;
            this.AuthToken = token;
            this.UserId = token.UserId;
            this.Nickname = token.Nickname;
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
                //if client was in room then wait for him to return
                RoomManager.OnClientDisconnectedSuddenly(ConnectionId);
            }

            Session = null;

            ClientManager.Disconnect(ConnectionId);
        }

        public override string ToString()
        {
            return $"Client {ConnectionId}: {IP}";
        }
    }
}
