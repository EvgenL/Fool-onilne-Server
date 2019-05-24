using System.Collections.Generic;
using Evgen.Byffer;
using FoolOnlineServer.GameServer.RoomLogic;
using Logginf;

namespace FoolOnlineServer.GameServer.Packets
{
    /// <summary>
    /// Class for handling packets sent by clients.
    /// The packets are defined in ClientSendPackets class of client's side.
    /// This exact file is responsive for transpot-layer of packet handler
    /// </summary>
    class PacketHandlerTransportLayer : PacketHandlerDataLayer
    {

        /// <summary>
        /// Packet id's. Gets converted to long and send at beginning of each packet
        /// Ctrl+C, Ctrl+V between ServerHandlePackets on server and ClientSendPackets on client
        /// </summary>
        private enum ClientPacketId
        {
            // LOGIN
            Authorize = 1,

            // ROOMS
            CreateRoom,
            RefreshRoomList,
            JoinRoom,
            JoinRandom,
            GiveUp, // todo not used
            LeaveRoom,
            GetReady,
            GetNotReady,

            // GAMEPLAY
            DropCardOnTable,
            Pass,
            CoverCardOnTable,

            // ACCOUNT
            WithdrawFunds,
            UpdateAvatar
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public PacketHandlerTransportLayer()
        {
            // init methods on start
            InitPackets();
        }

        private delegate void Packet(long connectionId, byte[] data);

        private static Dictionary<long, Packet> packets;

        /// <summary>
        /// ties methods to an enum ClientPacketId
        /// </summary>
        private void InitPackets()
        {
            packets = new Dictionary<long, Packet>();

            packets.Add((long)ClientPacketId.Authorize, Packet_Authorize);

            // ROOMS
            packets.Add((long)ClientPacketId.CreateRoom, Packet_CreateRoom);
            packets.Add((long)ClientPacketId.RefreshRoomList, Packet_RefreshRoomList);
            packets.Add((long)ClientPacketId.JoinRoom, Packet_JoinRoom);
            packets.Add((long)ClientPacketId.JoinRandom, Packet_JoinRandom);
            packets.Add((long)ClientPacketId.LeaveRoom, Packet_LeaveRoom);
            packets.Add((long)ClientPacketId.GetReady, Packet_GetReady);
            packets.Add((long)ClientPacketId.GetNotReady, Packet_GetNotReady);

            // GAMEPLAY
            packets.Add((long)ClientPacketId.DropCardOnTable, Packet_DropCardOnTable);
            packets.Add((long)ClientPacketId.Pass, Packet_Pass);
            packets.Add((long)ClientPacketId.CoverCardOnTable, Packet_CoverCardOnTable);

            // ACCOUNT
            packets.Add((long)ClientPacketId.WithdrawFunds, Packet_WithdrawFunds);
            packets.Add((long)ClientPacketId.UpdateAvatar, Packet_UpdateAvatar);
        }

        /// <summary>
        /// Called by Client.OnRecieveDataCallback
        /// Handles data sent by client represented by an array of bytes.
        /// </summary>
        /// <param name="connectionId">ConnectionId of client who sent data</param>
        /// <param name="data">Data represented by an array of bytes</param>
        public void HandleData(long connectionId, byte[] data)
        {
            //Get client who sent data
            Client client = ClientManager.GetConnectedClient(connectionId);

            //Clean it's buffer
            client.CleanBuffer();
            ByteBuffer clientBuffer = client.GetReadBuffer();
            clientBuffer.WriteBytes(data);

            //packet is empty
            if (clientBuffer.Length() == 0)
            {
                clientBuffer.Clear();
                return;
            }

            //Read packet length
            long packetLength = 0;

            if (clientBuffer.Length() >= 8) //long = 8 bytes
            {
                packetLength = clientBuffer.ReadLong(false);

                //if packet is incomplete
                if (packetLength <= 0)
                {
                    clientBuffer.Clear();
                    return;
                }
            }

            //while has plackets
            while (packetLength > 0 && packetLength <= clientBuffer.Length() - 8)
            {
                if (packetLength <= clientBuffer.Length() - 8)
                {
                    clientBuffer.ReadLong(); //read packet length
                    data = clientBuffer.ReadBytes((int) packetLength);

                    //process packet
                    HandleDataPackets(connectionId, data);
                }

                packetLength = 0;

                if (clientBuffer.Length() >= 8)
                {
                    packetLength = clientBuffer.ReadLong(false);

                    //if packet is incomplete
                    if (packetLength <= 0)
                    {
                        clientBuffer.Clear();
                        return;
                    }
                }

                if (packetLength <= 1)
                {
                    clientBuffer.Clear();
                }
            }
        }

        /// <summary>
        /// Called by HandleData
        /// Proceeds data by calling Packet_MethodName methods
        /// </summary>
        private void HandleDataPackets(long connectionId, byte[] data)
        {
            //Add our data to buffer
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);
            //Read out a packet id
            long packetId = buffer.ReadLong();
            //Delete buffer from memory
            buffer.Dispose();

            //Try find function tied to this packet id
            if (packets.TryGetValue(packetId, out Packet packet))
            {
                //Log packet id
                Client client = ClientManager.GetConnectedClient(connectionId);
                Log.WriteLine($"{client} sent {(ClientPacketId) data[0]}", typeof(PacketHandlerTransportLayer));

                //check if client is authorized
                if (!client.Authorized)
                {
                    // if he doenst sent authorize this time
                    if ((ClientPacketId)data[0] != ClientPacketId.Authorize)
                    {
                        ServerSendPackets.Send_ErrorBadAuthToken(connectionId);
                    }
                }
                // else if was authorized ok
                {
                    //Call method tied to a Packet by InitPackets() method
                    packet.Invoke(connectionId, data);
                }

            }
            else
            {
                Log.WriteLine("Wrong packet: " + packetId, typeof(PacketHandlerTransportLayer));
            }
        }


    }
}
