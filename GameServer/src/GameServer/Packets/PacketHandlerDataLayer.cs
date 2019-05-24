﻿using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Evgen.Byffer;
using FoolOnlineServer.AccountsServer;
using FoolOnlineServer.GameServer.RoomLogic;
using FoolOnlineServer.src.AccountsServer;
using FoolOnlineServer.src.GameServer.Clients;

namespace FoolOnlineServer.GameServer.Packets
{
    /// <summary>
    /// Contains methods called by HandlePacketsTransportLayer 
    /// to proceed certain packet
    /// </summary>
    abstract class PacketHandlerDataLayer
    {
        protected void Packet_Authorize(long connectionId, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);

            //skip packet id
            buffer.ReadLong();

            string token = buffer.ReadString();

            if (int.TryParse(token, out int tokenHash))
            {
                AuthService.AuthorizeClientOnGameServer(connectionId, tokenHash);
            }
            else
            {
                ServerSendPackets.Send_ErrorBadAuthToken(connectionId);
            }
        }

        protected  void Packet_CreateRoom(long connectionId, byte[] data)
        {
            //Add our data to buffer
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);

            //skip packet id
            buffer.ReadLong();

            //Read max players
            int maxPlayers = buffer.ReadInteger();
            //Read deckSize players
            int deckSize = buffer.ReadInteger();

            RoomManager.CreateRoom(connectionId, maxPlayers, deckSize);
        }

        protected  void Packet_RefreshRoomList(long connectionId, byte[] data)
        {
            RoomManager.RefreshRoomList(connectionId);
        }
        protected  void Packet_JoinRoom(long connectionId, byte[] data)
        {
            //Add our data to buffer
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);

            //skip packet id
            buffer.ReadLong();

            //Read room id
            long roomId = buffer.ReadLong();

            RoomManager.JoinRoom(connectionId, roomId);
        }

        protected  void Packet_JoinRandom(long connectionId, byte[] data)
        {
            RoomManager.JoinRandom(connectionId);
        }

        protected  void Packet_GiveUp(long connectionId, byte[] data)
        {
            RoomManager.GiveUp(connectionId);
        }
        protected  void Packet_LeaveRoom(long connectionId, byte[] data)
        {
            RoomManager.LeaveRoom(connectionId);
        }

        protected  void Packet_GetReady(long connectionId, byte[] data)
        {
            RoomManager.GetReady(connectionId);
        }

        protected  void Packet_GetNotReady(long connectionId, byte[] data)
        {
            RoomManager.GetNotReady(connectionId);
        }

        protected  void Packet_DropCardOnTable(long connectionId, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);

            //Skip packet id
            buffer.ReadLong();

            //Read card code
            string cardCode = buffer.ReadString();

            RoomManager.DropCardOnTable(connectionId, cardCode);
        }

        protected  void Packet_Pass(long connectionId, byte[] data)
        {
            RoomManager.Pass(connectionId);
        }

        protected  void Packet_CoverCardOnTable(long connectionId, byte[] data)
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

        protected void Packet_WithdrawFunds(long connectionId, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);

            //Skip packet id
            buffer.ReadLong();

            //Read withdraw sum
            float sum = buffer.ReadFloat();
            string requisites = buffer.ReadString();

            AccountManager.WithdrawFunds(connectionId, sum, requisites);
        }

        protected void Packet_UloadAvatar(long connectionId, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteBytes(data);

            //Skip packet id
            buffer.ReadLong();

            //Read image bytes
            byte[] imageBytes = buffer.ReadBytes(buffer.Length());


            // save file
            string avatarsFolderName = "avatars"; // todo load from app.config

            // create directory if not exists
            Directory.CreateDirectory(avatarsFolderName);


            // TODO read path from DB and delete old avatar file

            // find out the format of image
            string format = AvatarsManager.ByteArrayFormat(imageBytes);

            // write to file
            string filePath = avatarsFolderName + "/" + imageBytes.GetHashCode() + format;

            var stream = File.Create(filePath);
            stream.Write(imageBytes, 0, imageBytes.Length);
            stream.Close();

            // todo if... send avatar error

            // TODO update avatar in db

            // test: send avatar back
            string urlPath = "http://" +
                             AccountsServer.AccountsServer.GameServerIp + ":" + HTTPServer.HTTPServer.Port +
                             "/" + filePath;            
            ServerSendPackets.Send_UpdateUserAvatar(connectionId, connectionId, urlPath);
        }


    }
}
