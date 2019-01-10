using System;
using System.Net.Sockets;
using Evgen.Byffer;

namespace ServerForUnity1
{
    class Client
    {
        /// <summary>
        /// Client's unique number
        /// </summary>
        public int ConnectionIndex;

        /// <summary>
        /// Client device's IP adress
        /// </summary>
        public string IP;

        /// <summary>
        /// /Socket to where this client should write data
        /// </summary>
        public TcpClient Socket;

        /// <summary>
        /// Stream in which to write data to socket
        /// </summary>
        public NetworkStream MyStream;

        /// <summary>
        /// Buffer for reading data
        /// </summary>
        private byte[] readBuffer;


        private ByteBuffer buffer;

        public void CleanBuffer()
        {
            if (buffer == null)
            {
                buffer = new ByteBuffer();
            }
            buffer.Clear();

        }

        public ByteBuffer GetReadBuffer()
        {
            return buffer;
        }

        /// <summary>
        /// Tells if this user was connected or no.
        /// </summary>
        public bool Online()
        {
            return Socket != null;
        }

        /// <summary>
        /// Call on connection.
        /// </summary>
        public void Start()
        {
            Socket.SendBufferSize = 4096;
            Socket.ReceiveBufferSize = 4096;
            MyStream = Socket.GetStream();
            readBuffer = new byte[Socket.ReceiveBufferSize];
            MyStream.BeginRead(readBuffer, 0, Socket.ReceiveBufferSize, OnRecieveDataCallback, null);
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

                //TODO Handle data

                //if got disconnected
                if (Socket == null)
                {
                    return;
                }

                Say("Got " + readBytesSize + " bytes of data");

                ServerHandlePackets.
                //Read another pack of data
                MyStream.BeginRead(readBuffer, 0, Socket.ReceiveBufferSize, OnRecieveDataCallback, null);
            }
            catch (Exception e)
            {
                CloseConnection();
                Say(e.Message);
                return;
            }
        }

        //Destroys connection between this client and server and marks this socket as free (null)
        private void CloseConnection()
        {
            Say("Disconnected by client");
            Socket.Close();
            Socket = null;

        }

        private void Say(string message)
        {
            Console.WriteLine($"[Client {ConnectionIndex}: {IP}]: {message}");
        }
    }
}
