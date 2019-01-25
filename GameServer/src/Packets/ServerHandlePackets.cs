﻿using System.Collections.Generic;
using Evgen.Byffer;
using GameServer.RoomLogic;

namespace GameServer.Packets
{
    /// <summary>
    /// Class for handling packets sent by clients.
    /// The packets are defined in ClientSendPackets class of client's side.
    /// </summary>
    public static class ServerHandlePackets
    {
        /// <summary>
        /// Packet id's. Gets converted to long and send at beginning of each packet
        /// Ctrl+C, Ctrl+V between ServerHandlePackets on server and ClientSendPackets on client
        /// </summary>
        private enum ClientPacketId
        {
            NewAccount = 1,
            Login,
            ThankYou,

            //ROOMS
            JoinRandom,
            GiveUp,
            GetReady,
            GetNotReady,

            //GAMEPLAY
            DropCardOnTable,
            Pass,
            CoverCardOnTable,
        }

        private delegate void Packet(long connectionId, byte[] data);

        private static Dictionary<long, Packet> packets;

        private static void InitPackets()
        {
            packets = new Dictionary<long, Packet>();

            //ROOMS
            packets.Add((long)ClientPacketId.NewAccount, Packet_NewAccount);
            packets.Add((long)ClientPacketId.ThankYou, Packet_ThankYou);
            packets.Add((long)ClientPacketId.JoinRandom, Packet_JoinRandom);
            packets.Add((long)ClientPacketId.GiveUp, Packet_GiveUp);
            packets.Add((long)ClientPacketId.GetReady, Packet_GetReady);
            packets.Add((long)ClientPacketId.GetNotReady, Packet_GetNotReady);

            //GAMEPLAY
            packets.Add((long)ClientPacketId.DropCardOnTable, Packet_DropCardOnTable);
            packets.Add((long)ClientPacketId.Pass, Packet_Pass);
            packets.Add((long)ClientPacketId.CoverCardOnTable, Packet_CoverCardOnTable);
        }

        /// <summary>
        /// Called by Client.OnRecieveDataCallback
        /// Handles data sent by client represented by an array of bytes.
        /// </summary>
        /// <param name="connectionId">ConnectionId of client who sent data</param>
        /// <param name="data">Data represented by an array of bytes</param>
        public static void HandleData(long connectionId, byte[] data)
        {
            //init messages if first call
            if (packets == null)
            {
                InitPackets();
            }

            byte[] buffer = (byte[]) data.Clone();

            //Get client who sent data
            Client client = Server.GetClient(connectionId);

            //Clean it's buffer
            client.CleanBuffer();
            ByteBuffer clientBuffer = client.GetReadBuffer();
            clientBuffer.WriteBytes(data);

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

            while (packetLength > 0 && packetLength <= clientBuffer.Length() - 8)
            {
                if (packetLength <= clientBuffer.Length() - 8)
                {
                    clientBuffer.ReadLong(); //read packet length
                    data = clientBuffer.ReadBytes((int)packetLength);
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
        private static void HandleDataPackets(long connectionId, byte[] data)
        {
            //Add our data to buffer
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);
            //Read out a packet id
            long packetId = buffer.ReadLong();
            //Delete buffer from memory
            buffer.Dispose();
            buffer = null;

            //Try find function tied to this packet id
            if (packets.TryGetValue(packetId, out Packet packet))
            {
                //Call method tied to a Packet by InitPackets() method
                packet.Invoke(connectionId, data);
            }
            else
            {
                Log.WriteLine("Wrong packet: " + packetId, typeof(ServerHandlePackets));
            }
        }


        ////////////////////////////DATA PACKETS////////////////////////////
        ////////////////////////////DATA PACKETS////////////////////////////
        ////////////////////////////DATA PACKETS////////////////////////////


        private static void Packet_NewAccount(long connectionId, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);//TODO different server for registration
        }

        private static void Packet_ThankYou(long connectionId, byte[] data)
        {
            //Add our data to buffer
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);

            //skip packet id
            buffer.ReadLong();
            string message = buffer.ReadString();
            Log.WriteLine(Server.GetClient(connectionId) + " says: " + message, typeof(ServerHandlePackets));
        }

        private static void Packet_JoinRandom(long connectionId, byte[] data)
        {
            RoomManager.JoinRandom(connectionId);
        }

        private static void Packet_GiveUp(long connectionId, byte[] data)
        {
            RoomManager.GiveUp(connectionId);
        }
        /*Packet_GetReady);
        y, Packet_GetNotReady);*/

        private static void Packet_GetReady(long connectionId, byte[] data)
        {
            RoomManager.GetReady(connectionId);
        }

        private static void Packet_GetNotReady(long connectionId, byte[] data)
        {
            RoomManager.GetNotReady(connectionId);
        }

        private static void Packet_DropCardOnTable(long connectionId, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);

            //Skip packet id
            buffer.ReadLong();

            //Read card code
            string cardCode = buffer.ReadString();

            RoomManager.DropCardOnTable(connectionId, cardCode);
        }

        private static void Packet_Pass(long connectionId, byte[] data)
        {
            RoomManager.Pass(connectionId);
        }

        private static void Packet_CoverCardOnTable(long connectionId, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);

            //Skip packet id
            buffer.ReadLong();

            //Read card on table code
            string cardOnTableCode = buffer.ReadString();
            //Read card dropped code
            string cardDroppedCode = buffer.ReadString();

            RoomManager.CoverCardOnTable(connectionId, cardOnTableCode, cardDroppedCode);
        }
    }
}
