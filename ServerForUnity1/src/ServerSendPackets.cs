using Evgen.Byffer;

namespace ServerForUnity1
{
    /// <summary>
    /// Proceend sending data from server to client
    /// </summary>
    public static class ServerSendPackets
    {
        /// <summary>
        /// Pacet id's. Gets converted to long and send at beginning of each packet
        /// Ctrl+C, Ctrl+V between ServerSendPackets on server and ClientHandlePackets on client
        /// </summary>
        public enum SevrerPacketId
        {
            Information = 1,
            CallMethod
        }


        /// <summary>
        /// Sends data to connected client
        /// </summary>
        /// <param name="connectionId">Id of connected client</param>
        /// <param name="data">byte array data</param>
        public static void SendDataTo(int connectionId, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();

            //Writing packet length as first value
            buffer.WriteLong((data.GetUpperBound(0) - data.GetLowerBound(0)) + 1); //that's basically get length - 1

            Log.WriteLine($"Send: {data.Length} bytes of data.", typeof(ServerSendPackets));

            buffer.WriteBytes(data);
            Client client = Server.GetClient(connectionId);
            client.MyStream.BeginWrite(buffer.ToArray(), 0, buffer.Length(), null, null);
        }

        /// <summary>
        /// Sends data to EVERY connected client on game server
        /// </summary>
        /// <param name="data">byte array data</param>
        public static void SendDataToAll(byte[] data)
        {
            //Loop through all the clients
            for (int i = 0; i < StaticParameters.MaxClients; i++)
            {
                Client client = Server.GetClient(i);

                if (client.Online())
                {
                    SendDataTo(i, data);
                }
            }
        }

        ////////////////////////////DATA PACKETS////////////////////////////
        ////////////////////////////DATA PACKETS////////////////////////////
        ////////////////////////////DATA PACKETS////////////////////////////
        

        /// <summary>
        /// Sends method to client
        /// </summary>
        public static void SendInformation(int connectionId)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)SevrerPacketId.Information);

            //Add data to packet
            buffer.WriteString("Welcome to server!");

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());

        }

        /// <summary>
        /// Sends method to client
        /// </summary>
        public static void SendCallMethodOnClient(int connectionId, int methodId)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)SevrerPacketId.CallMethod);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }
    }
}
