using System;
using System.Net;
using System.Net.Sockets;
using Evgen.Byffer;
using GameServer.Packets;
using GameServer.RoomLogic;
using SuperWebSocket;

namespace GameServer
{
    public class Client
    {
        /// <summary>
        /// Clien's display name (not unique)
        /// //todo register, store nicknames
        /// </summary>
        public string Nickname => "Игрок " + ConnectionId;

        /// <summary>
        /// Clien's registration name (unique)
        /// //todo register, store idnames
        /// </summary>
        public string Id;

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
        /// Stream in which to write data to socket
        /// </summary>
        public NetworkStream MyStream;

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

        /// <summary>
        /// Call on connection.
        /// </summary>
        public void Start()
        {
            /*
            Socket.SendBufferSize = 4096;
            Socket.ReceiveBufferSize = 4096;
            MyStream = Socket.GetStream();
            readBuffer = new byte[Socket.ReceiveBufferSize];
            MyStream.BeginRead(readBuffer, 0, Socket.ReceiveBufferSize, OnRecieveDataCallback, null);*/
        }

        public void OnNewDataReceived(byte[] data)
        {
            ServerHandlePackets.HandleData(ConnectionId, data);
        }

        /// <summary>
        /// Callback for recieving data
        /// </summary>
        private void OnRecieveDataCallback(IAsyncResult result)
        {
            //Try get data
            try
            {
                //Get size of recieved data
                int readBytesSize = MyStream.EndRead(result);
                //if got disconnected
                if (Socket == null)
                {
                    return;
                }

                //This triggers on when client calls PlayerSocket.Close()
                if (readBytesSize <= 0)
                {
                    CloseConnection();
                    return;
                }

                //Copy data to buffer
                byte[] readBytes = new byte[readBytesSize];
                Buffer.BlockCopy(readBuffer, 0, readBytes, 0, readBytesSize);


                //Handle data

                //if got disconnected
                if (Socket == null)
                {
                    return;
                }

                //Handle readed data
                ServerHandlePackets.HandleData(ConnectionId, readBytes);

                //Read another pack of data
                MyStream.BeginRead(readBuffer, 0, Socket.ReceiveBufferSize, OnRecieveDataCallback, null);
            }
            catch (Exception e)
            {
                CloseConnection();
                Log.WriteLine(e.ToString(), this);
                return;
            }
        }

        /// <summary>
        /// Destroys connection between this client and server and marks this socket as free (null)
        /// </summary>
        public void CloseConnection()
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
            /*
            Socket.Close();
            Socket = null;
            */
        }

        public override string ToString()
        {
            return $"Client {ConnectionId}: {IP}";
        }
    }
}
