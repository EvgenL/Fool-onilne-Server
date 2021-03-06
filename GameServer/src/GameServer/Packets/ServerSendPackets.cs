﻿using System.Collections.Generic;
using System.Linq;
using Evgen.Byffer;
using FoolOnlineServer.GameServer.Clients;
using FoolOnlineServer.GameServer.RoomLogic;
using Logginf;

namespace FoolOnlineServer.GameServer.Packets
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
        public enum ServerPacketId
        {
            // Connection
            AuthorizedOk = 1,
            ErrorBadAuthToken,
            UpdateUserData,

            // ROOMS
            RoomList,
            JoinRoomOk,
            FaliToJoinFullRoom,
            YouAreAlreadyInRoom,
            RoomData,
            OtherPlayerJoinedRoom,
            OtherPlayerLeftRoom,
            OtherPlayerGotReady,
            OtherPlayerGotNotReady,

            // GAMEPLAY
            StartGame,
            NextTurn,
            YouGotCardsFromTalon,
            EnemyGotCardsFromTalon,
            TalonData,
            DropCardOnTableOk,
            DropCardOnTableErrorNotYourTurn,
            DropCardOnTableErrorTableIsFull,
            DropCardOnTableErrorCantDropThisCard,
            OtherPlayerDropsCardOnTable,
            EndGame,
            EndGameGiveUp,
            OtherPlayerPassed,
            OtherPlayerCoversCard,
            Beaten,
            DefenderPicksCards,
            EndGameFool,
            PlayerWon,
            Message,
            Toast,

            // ACCOUNT
            UpdateUserAvatar
        }
       


        /// <summary>
        /// Sends data to connected client
        /// </summary>
        /// <param name="connectionId">Id of connected client</param>
        /// <param name="data">byte array data</param>
        public static void SendDataTo(long connectionId, byte[] data)
        {
            //check if client's online
            Client client = ClientManager.GetConnectedClient(connectionId);
            if (client == null || !client.Online())
            {
                Log.WriteLine($"ERROR: Tried to send data to client {client} who isn't online. ", typeof(ServerSendPackets));
                return;
            }

            ByteBuffer buffer = new ByteBuffer();

            //Writing packet length as first value
            buffer.WriteLong(data.Length);
            //writing the data
            buffer.WriteBytes(data);

            Log.WriteLine($"Sent: {(ServerPacketId)data[0]} to client " + client, typeof(ServerSendPackets));


            client.Session.Send(buffer.ToArray(), 0, buffer.Length());
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
                Client client = ClientManager.GetConnectedClient(i);

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
        /// Sends hello message to client when accepted authorizetion
        /// </summary>
        public static void Send_AuthorizedOk(long connectionId)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.AuthorizedOk);

            //Add client's connection id
            buffer.WriteLong(connectionId);

            //Add data to packet
            buffer.WriteString("Welcome to server!");

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());

        }

        /// <summary>
        /// Sends hello message to client when accepted authorizetion
        /// </summary>
        public static void Send_ErrorBadAuthToken(long connectionId)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.ErrorBadAuthToken);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());

            //Disconnect client from server
            ClientManager.GetConnectedClient(connectionId).Disconnect("Bad auth token");

        }

        public static void Send_UpdateUserData(long connectionId)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.UpdateUserData);

            Client client = ClientManager.GetConnectedClient(connectionId);

            //Add current connection id
            buffer.WriteLong(connectionId);

            //Add userId
            buffer.WriteLong(client.UserData.UserId);

            //Add client's display name
            buffer.WriteStringUnicode(client.UserData.Nickname);

            //Add client's money
            buffer.WriteDouble(client.UserData.Money);

            //Add client's avatar file URL
            string avatarUrl = client.UserData.AvatarFileUrl;
            buffer.WriteString(avatarUrl);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());

        }

        /// <summary>
        /// Sends only packetId 
        /// </summary>
        private static void SendOnlyPacketId(long connectionId, ServerPacketId packetId)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)packetId);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Send when player has succesfully connected to room
        /// </summary>
        public static void Send_RoomList(long connectionId, RoomInstance[] rooms)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.RoomList);
            //Add room list length
            buffer.WriteInteger(rooms.Length);

            //add rooms
            foreach (var room in rooms)
            {
                //add room id
                buffer.WriteLong(room.RoomId);
                //add max players in room
                buffer.WriteInteger(room.MaxPlayers);
                //add deck size
                buffer.WriteInteger(room.DeckSize);
                //add players count
                buffer.WriteInteger(room.ConnectedPlayersN);
                //add player names and avatars
                foreach (var name in room.GetPlayerNicknames())
                {
                    buffer.WriteStringUnicode(name); //using unicode there because player names support all languages
                }
            }

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Send when player has succesfully connected to room
        /// </summary>
        public static void Send_JoinRoomOk(long connectionId, long roomId)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.JoinRoomOk);
            //Add roomId
            buffer.WriteLong(roomId);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Send error when player tries join full room
        /// </summary>
        public static void Send_FaliToJoinFullRoom(long connectionId)
        {
            SendOnlyPacketId(connectionId, ServerPacketId.FaliToJoinFullRoom);
        }

        /// <summary>
        /// Send error when player tries join room but he is in room already
        /// </summary>
        public static void Send_YouAreAlreadyInRoom(long connectionId)
        {
            SendOnlyPacketId(connectionId, ServerPacketId.YouAreAlreadyInRoom);
        }

        /// <summary>
        /// Sends data about the room
        /// </summary>
        /// <param name="connectionId">Player who gets message if not sending to everybody</param>
        /// <param name="playerIdsInRoom">Ordered player list in room</param>
        /// <param name="maxPlayers">Max connected players</param>
        /// <param name="sendDataToAll">Send to evetybody</param>
        public static void Send_RoomData(long connectionId, RoomInstance room)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.RoomData);

            var clients = room.clientsInRoom;

            //Add maxPlayers
            buffer.WriteInteger(room.MaxPlayers);

            //Add players count
            buffer.WriteInteger(room.ConnectedPlayersN);
            //Add players
            foreach (var client in clients)
            {
                if (client == null) continue;
                 
                //Write player's id
                buffer.WriteLong(client.ConnectionId);
                //Write player's slot number
                buffer.WriteInteger(client.SlotInRoom);
                //Write player's nickname
                buffer.WriteStringUnicode(client.UserData.Nickname);
                //Write player's avatar
                buffer.WriteString(client.UserData.AvatarFileUrl);
            }

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Sends this when somebody joins room
        /// </summary>
        public static void Send_OtherPlayerJoinedRoom(long connectionId, Client joinedClient)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.OtherPlayerJoinedRoom);

            //Add player id
            buffer.WriteLong(joinedClient.ConnectionId);

            //Add player's slot number
            buffer.WriteInteger(joinedClient.SlotInRoom);

            //Add nickname
            buffer.WriteStringUnicode(joinedClient.UserData.Nickname);

            //Add avatar
            buffer.WriteString(joinedClient.UserData.AvatarFileUrl);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Sends this when somebody lefts room
        /// </summary>
        public static void Send_OtherPlayerLeftRoom(long connectionId, long playerIdWhoLeft, int slotN)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.OtherPlayerLeftRoom);

            //Add player id
            buffer.WriteLong(playerIdWhoLeft);

            //Add player's slot number
            buffer.WriteInteger(slotN);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Sends this when somebody lefts room
        /// </summary>
        public static void Send_OtherPlayerGotReady(long connectionId, long otherPlayerId, int slotN)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.OtherPlayerGotReady);

            //Add player id
            buffer.WriteLong(otherPlayerId);
            //Add player slot
            buffer.WriteInteger(slotN);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Sends this when somebody lefts room
        /// </summary>
        public static void Send_OtherPlayerGotNotReady(long connectionId, long otherPlayerId, int slotN)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.OtherPlayerGotNotReady);

            //Add player id
            buffer.WriteLong(otherPlayerId);
            //Add player slot
            buffer.WriteInteger(slotN);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Send this when game in room started
        /// </summary>
        public static void Send_StartGame(long connectionId)
        {
            SendOnlyPacketId(connectionId, ServerPacketId.StartGame);
        }

        /// <summary>
        /// Send this when game in room started
        /// and on every turn so on
        /// </summary>
        public static void Send_NextTurn(long connectionId, long firstPlayerId, int firstPlayerSlotN, long defendingPlayerId, int defSlotN, int turnN)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.NextTurn);

            //Add first player id
            buffer.WriteLong(firstPlayerId);
            //Add first player slot
            buffer.WriteInteger(firstPlayerSlotN);
            //Add def player id
            buffer.WriteLong(defendingPlayerId);
            //Add def player slot
            buffer.WriteInteger(defSlotN);
            //Add turn number
            buffer.WriteInteger(turnN);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Sends this when you get cards from talon
        /// </summary>
        public static void Send_YouGotCardsFromTalon(long connectionId, string[] cards)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.YouGotCardsFromTalon);

            //Add cards number
            buffer.WriteInteger(cards.Length);

            //Add cards
            for (int i = 0; i < cards.Length; i++)
            {
                buffer.WriteString(cards[i]);
            }

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Sends this when other player gets cards from talon
        /// </summary>
        public static void Send_EnemyGotCardsFromTalon(long connectionId, long otherPlayerId, int cardsN, int slotN)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.EnemyGotCardsFromTalon);

            //Add player id
            buffer.WriteLong(otherPlayerId);
            //Add player cardsN
            buffer.WriteInteger(cardsN);
            //Add plyer slot
            buffer.WriteInteger(slotN);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }


        /// <summary>
        /// Sends this when other player gets cards from talon
        /// </summary>
        public static void Send_TalonData(long connectionId, int talonSize, string trumpCardCode)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.TalonData);

            //Add talon length
            buffer.WriteInteger(talonSize);

            //Add trump card
            buffer.WriteString(trumpCardCode);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_DropCardOnTableOk(long connectionId, string cardCode)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.DropCardOnTableOk);

            //Add card
            buffer.WriteString(cardCode);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }
        public static void Send_DropCardOnTableErrorNotYourTurn(long connectionId, string cardCode)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.DropCardOnTableErrorNotYourTurn);

            //Add card
            buffer.WriteString(cardCode);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }
        public static void Send_DropCardOnTableErrorTableIsFull(long connectionId, string cardCode)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.DropCardOnTableErrorTableIsFull);

            //Add card
            buffer.WriteString(cardCode);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }
        public static void Send_DropCardOnTableErrorCantDropThisCard(long connectionId, string cardCode)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.DropCardOnTableErrorCantDropThisCard);

            //Add card
            buffer.WriteString(cardCode);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_OtherPlayerDropsCardOnTable(long connectionId, long playerWhoDrop, int slotN, string cardCode)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.OtherPlayerDropsCardOnTable);

            //Add playerWhoDrop
            buffer.WriteLong(playerWhoDrop);
            //Add playerWhoDrop slotN
            buffer.WriteInteger(slotN);
            //Add card
            buffer.WriteString(cardCode);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_EndGame(long connectionId, long foolConnectionId, Dictionary<long, int> rewards)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.EndGame);

            //Add fool
            buffer.WriteLong(foolConnectionId);

            //Add rewards count
            buffer.WriteInteger(rewards.Count);
            //Add rewards
            foreach (var reward in rewards)
            {
                //Add player id
                buffer.WriteLong(reward.Key);
                //Add player reward
                buffer.WriteInteger(reward.Value);
            }

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }
        
        public static void Send_EndGameGiveUp(long connectionId, long foolConnectionId, Dictionary<long, double> rewards)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.EndGameGiveUp);

            //Add fool
            buffer.WriteLong(foolConnectionId);

            //Add rewards count
            buffer.WriteInteger(rewards.Count);
            //Add rewards
            foreach (var reward in rewards)
            {
                //Add player id
                buffer.WriteLong(reward.Key);
                //Add player reward
                buffer.WriteDouble(reward.Value);
            }

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_EndGameFool(long connectionId, long foolPlayerId, Dictionary<long, double> rewards)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.EndGameFool);

            //Add foolPlayerId
            buffer.WriteLong(foolPlayerId);

            //Add rewards count
            buffer.WriteInteger(rewards.Count);
            //Add rewards
            foreach (var reward in rewards)
            {
                //Add player id
                buffer.WriteLong(reward.Key);
                //Add player reward
                buffer.WriteDouble(reward.Value);
            }

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_OtherPlayerPassed(long connectionId, long passedPlayerId, int slotN)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.OtherPlayerPassed);

            //Add passedPlayerId
            buffer.WriteLong(passedPlayerId);

            //Add passed palyer slotN
            buffer.WriteInteger(slotN);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_OtherPlayerCoversCard(long connectionId, long coveredPlayerId, int slotN,
            string cardOnTableCode, string cardDroppedCode)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.OtherPlayerCoversCard);

            //Add coveredPlayerId
            buffer.WriteLong(coveredPlayerId);
            //Add covered palyer slotN
            buffer.WriteInteger(slotN);

            //Add card on table
            buffer.WriteString(cardOnTableCode);
            //Add card dropped
            buffer.WriteString(cardDroppedCode);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_Beaten(long connectionId)
        {
            SendOnlyPacketId(connectionId, ServerPacketId.Beaten);
        }

        public static void Send_DefenderPicksCards(long connectionId, long defenderPlayerId, int defSlotN, int cardsOnTableN)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.DefenderPicksCards);

            //Add defenderPlayerId
            buffer.WriteLong(defenderPlayerId);
            //Add defender palyer slotN
            buffer.WriteInteger(defSlotN);
            //Add number of cards on table
            buffer.WriteInteger(cardsOnTableN);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_PlayerWon(long connectionId, long winnerId, double winnerReward)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.PlayerWon);

            //Add winnerId
            buffer.WriteLong(winnerId);

            //Add winner's reward
            buffer.WriteDouble(winnerReward);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_Message(long connectionId, string message)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.Message);

            //Add message
            buffer.WriteStringUnicode(message);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_Toast(long connectionId, string message)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.Toast);

            //Add message
            buffer.WriteStringUnicode(message);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_UpdateUserAvatar(long connectionIdReciever, long connectionIdAvatarHolder, string path)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)ServerPacketId.UpdateUserAvatar);

            // Add Id of Avatar Holder
            buffer.WriteLong(connectionIdAvatarHolder);

            // Add avatar
            buffer.WriteString(path);

            //Send packet
            SendDataTo(connectionIdReciever, buffer.ToArray());
        }



    }
}
