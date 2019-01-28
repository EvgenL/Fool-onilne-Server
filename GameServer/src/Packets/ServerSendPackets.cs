using System.Collections.Generic;
using Evgen.Byffer;
using GameServer.RoomLogic;

namespace GameServer.Packets
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
            //Connection
            Information = 1,

            //ROOMS
            JoinRoomOk,
            FaliToJoinFullRoom,
            YouAreAlreadyInRoom,
            RoomData,
            OtherPlayerJoinedRoom,
            OtherPlayerLeftRoom,
            OtherPlayerGotReady,
            OtherPlayerGotNotReady,

            //GAMEPLAY
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
            PlayerWon

        }
       


        /// <summary>
        /// Sends data to connected client
        /// </summary>
        /// <param name="connectionId">Id of connected client</param>
        /// <param name="data">byte array data</param>
        public static void SendDataTo(long connectionId, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();

            //Writing packet length as first value
            buffer.WriteLong((data.GetUpperBound(0) - data.GetLowerBound(0)) + 1); //that's basically get length - 1

            Log.WriteLine($"Sent: {data.Length} bytes of data to client " + Server.GetClient(connectionId), typeof(ServerSendPackets));

            buffer.WriteBytes(data);
            Client client = Server.GetClient(connectionId);
            if (client.Online())
            {
                client.MyStream.BeginWrite(buffer.ToArray(), 0, buffer.Length(), null, null);
            }
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
        /// Sends hello message to client 
        /// </summary>
        public static void Send_Information(long connectionId)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)SevrerPacketId.Information);

            //Add client connection id
            buffer.WriteLong(connectionId);

            //Add data to packet
            buffer.WriteString("Welcome to server!");

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());

        }

        /// <summary>
        /// Sends only packetId 
        /// </summary>
        private static void SendOnlyPacketId(long connectionId, SevrerPacketId packetId)
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
        public static void Send_JoinRoomOk(long connectionId, long roomId)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)SevrerPacketId.JoinRoomOk);
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
            SendOnlyPacketId(connectionId, SevrerPacketId.FaliToJoinFullRoom);
        }

        /// <summary>
        /// Send error when player tries join room but he is in room already
        /// </summary>
        public static void Send_YouAreAlreadyInRoom(long connectionId)
        {
            SendOnlyPacketId(connectionId, SevrerPacketId.YouAreAlreadyInRoom);
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
            buffer.WriteLong((long)SevrerPacketId.RoomData);

            //Add players count
            long[] playerIdsInRoom = room.GetPlayerIds();
            buffer.WriteInteger(playerIdsInRoom.Length);

            //Add players
            foreach (var playerId in playerIdsInRoom)
            {
                //Write player's id
                buffer.WriteLong(playerId);
                //Write player's slot number
                buffer.WriteInteger(room.GetSlotN(playerId));
            }

            //Add maxPlayers
            buffer.WriteInteger(room.MaxPlayers);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        /// <summary>
        /// Sends this when somebody joins room
        /// </summary>
        public static void Send_OtherPlayerJoinedRoom(long connectionId, long playerIdWhoJoined, int slotN)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)SevrerPacketId.OtherPlayerJoinedRoom);

            //Add player id
            buffer.WriteLong(playerIdWhoJoined);

            //Add player's slot number
            buffer.WriteInteger(slotN);

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
            buffer.WriteLong((long)SevrerPacketId.OtherPlayerLeftRoom);

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
            buffer.WriteLong((long)SevrerPacketId.OtherPlayerGotReady);

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
            buffer.WriteLong((long)SevrerPacketId.OtherPlayerGotNotReady);

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
            SendOnlyPacketId(connectionId, SevrerPacketId.StartGame);
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
            buffer.WriteLong((long)SevrerPacketId.NextTurn);

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
            buffer.WriteLong((long)SevrerPacketId.YouGotCardsFromTalon);

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
            buffer.WriteLong((long)SevrerPacketId.EnemyGotCardsFromTalon);

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
            buffer.WriteLong((long)SevrerPacketId.TalonData);

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
            buffer.WriteLong((long)SevrerPacketId.DropCardOnTableOk);

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
            buffer.WriteLong((long)SevrerPacketId.DropCardOnTableErrorNotYourTurn);

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
            buffer.WriteLong((long)SevrerPacketId.DropCardOnTableErrorTableIsFull);

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
            buffer.WriteLong((long)SevrerPacketId.DropCardOnTableErrorCantDropThisCard);

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
            buffer.WriteLong((long)SevrerPacketId.OtherPlayerDropsCardOnTable);

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
            buffer.WriteLong((long)SevrerPacketId.EndGame);

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
            buffer.WriteLong((long)SevrerPacketId.EndGameGiveUp);

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
            buffer.WriteLong((long)SevrerPacketId.EndGameFool);

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
            buffer.WriteLong((long)SevrerPacketId.OtherPlayerPassed);

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
            buffer.WriteLong((long)SevrerPacketId.OtherPlayerCoversCard);

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
            SendOnlyPacketId(connectionId, SevrerPacketId.Beaten);
        }

        public static void Send_DefenderPicksCards(long connectionId, long defenderPlayerId, int defSlotN)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)SevrerPacketId.DefenderPicksCards);

            //Add defenderPlayerId
            buffer.WriteLong(defenderPlayerId);
            //Add defender palyer slotN
            buffer.WriteInteger(defSlotN);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }

        public static void Send_PlayerWon(long connectionId, long winnerId, double winnerReward)
        {
            //New packet
            ByteBuffer buffer = new ByteBuffer();

            //Add packet id
            buffer.WriteLong((long)SevrerPacketId.PlayerWon);

            //Add winnerId
            buffer.WriteLong(winnerId);

            //Add winner's reward
            buffer.WriteDouble(winnerReward);

            //Send packet
            SendDataTo(connectionId, buffer.ToArray());
        }
    }
}
