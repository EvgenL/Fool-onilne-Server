using System;
using System.Collections.Generic;
using System.Linq;
using FoolOnlineServer.GameServer.Packets;
using FoolOnlineServer.src.GameServer;
using Logging;

namespace FoolOnlineServer.GameServer.RoomLogic
{
    /// <summary>
    /// Manages room instances.
    /// Only call this class methods when deling with client-room logic.
    /// </summary>
    public static class RoomManager
    {
        public const int MAX_ONE_PAKCKET_ROOM_LIST_SIZE = 50;//2;

        /// <summary>
        /// Active rooms. Both playing and waiting.
        /// Contains pair <RoomId-RoomInstance>
        /// </summary>
        public static HashSet<RoomInstance> ActiveRooms = new HashSet<RoomInstance>();

        /// <summary>
        /// Sent by player how wants to create a new room
        /// </summary>
        /// <param name="connectionId">Room creator connection id</param>
        /// <param name="maxPlayers">Max players in room</param>
        /// <param name="deckSize">Deck size in room</param>
        public static void CreateRoom(long connectionId, int maxPlayers, int deckSize)
        {
            var client = ClientManager.GetConnectedClient(connectionId);
            Log.WriteLine("[" + client + "] wants to create room.", typeof(RoomManager));

            if (client.IsInRoom)
            {
                Log.WriteLine("[" + client + "] is already in room. Abort.", typeof(RoomManager));
                return;
            }

            //Validate
            if (maxPlayers < 2 || maxPlayers > 6 ||
                !(deckSize == 24 || deckSize == 36 || deckSize == 52))
            {
                //Send incorrect room
                return;
            }

            RoomInstance room = CreateNewRoomInstance();
            room.MaxPlayers = maxPlayers;
            room.DeckSize = deckSize;
            room.HostId = connectionId;

            //Client joins random room
            if (room.JoinRoom(connectionId))
            {
                //Send 'OK' if room has free slots
                ServerSendPackets.Send_JoinRoomOk(connectionId, room.RoomId);
            }
        }

        /// <summary>
        /// Sent by player who looks at open rooms list.
        /// Sending him open rooms list
        /// </summary>
        public static void RefreshRoomList(long connectionId)
        {
            //Get rooms which are not full and not playing
            List<RoomInstance> openRooms = GetAvailableRooms();
            if (openRooms.Count > MAX_ONE_PAKCKET_ROOM_LIST_SIZE)
            {
                //filter first MAX_ONE_PAKCKET_ROOM_LIST_SIZE (=50) rooms
                int i = 0;
                openRooms = openRooms.TakeWhile(_ => i < MAX_ONE_PAKCKET_ROOM_LIST_SIZE).ToList();
            }

            ServerSendPackets.Send_RoomList(connectionId, openRooms.ToArray());
        }

        /// <summary>
        /// Adds player to room
        /// </summary>
        public static bool JoinRoom(long connectionId, long roomId)
        {
            //Getting not-full rooms
            List<RoomInstance> availableRooms = GetAvailableRooms();

            RoomInstance roomToJoin = availableRooms.Find(room => room.RoomId == roomId);

            //If this room is not present
            if (roomToJoin == null)
            {
                //todo send fail to join
                ServerSendPackets.Send_RoomList(connectionId, availableRooms.ToArray());
                return false;
            }

            if (roomToJoin.JoinRoom(connectionId))
            {
                //Send 'OK' if room has free slots
                ServerSendPackets.Send_JoinRoomOk(connectionId, roomToJoin.RoomId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Joins player to totally random room
        /// </summary>
        /// <param name="connectionId">Player's who wanna join connection id</param>
        public static void JoinRandom(long connectionId)
        {
            var client = ClientManager.GetConnectedClient(connectionId);
            Log.WriteLine("[" + client + "] wants to join random room.", typeof(RoomManager));

            if (client.IsInRoom)
            {
                Log.WriteLine("[" + client + "] is already in room. Abort.", typeof(RoomManager));
                return;
            }

            //Getting not-full rooms
            List<RoomInstance> availableRooms = GetAvailableRooms();

            RoomInstance randomRoom;

            //if no rooms
            if (availableRooms.Count == 0)
            {
                Log.WriteLine("No available rooms.", typeof(RoomManager));

                //Creting a new room
                randomRoom = CreateRandomRoom();
                randomRoom.HostId = connectionId;
            }
            else //if somebody's playing
            {
                //Selecting a random room
                //todo matchmaking
                int randomNum = new Random().Next(0, availableRooms.Count);
                randomRoom = availableRooms[randomNum];
            }

            //Client joins random room
            if (!JoinRoom(connectionId, randomRoom.RoomId))
            {
                //if join fails
                //TODO continue finding another room
            }
        }

        /// <summary>
        /// Selecting rooms which are not playing and have a free slot
        /// </summary>
        /// <returns>List of available rooms</returns>
        public static List<RoomInstance> GetAvailableRooms()
        {
            return ActiveRooms.Where(room => room.ConnectedPlayersN < room.MaxPlayers
                                                           && room.State == RoomInstance.RoomState
                                                               .WaitingForPlayersToConnect).ToList();
        }

        private static RoomInstance CreateRandomRoom()
        {
            long id = GetFreeRoomId();

            Random r = new Random();
            int randomPlayerCount = r.Next(2, 5); // 2 - 4 players in random room
            int randomDeckSize = 24 + 16 * r.Next(0, 3); //24-36-52 cards
            RoomInstance room = new RoomInstance(id, randomPlayerCount, randomDeckSize);
            ActiveRooms.Add(room);
            Log.WriteLine("Created a random room. Id: " + id, typeof(RoomManager));
            return room;
        }

        /// <summary>
        /// Creates room and adds to active rooms
        /// </summary>
        private static RoomInstance CreateNewRoomInstance()
        {
            long id = GetFreeRoomId();
            RoomInstance room = new RoomInstance(id); 
            ActiveRooms.Add(room);
            Log.WriteLine("Created a new room. Id: " + id, typeof(RoomManager));
            return room;
        }

        /// <summary>
        /// Get id for room which is not used
        /// </summary>
        /// <returns>Not used id. Returns 0 if every of [-long..+long] values are used.</returns>
        private static long GetFreeRoomId()
        {
            HashSet<long> usedIds = new HashSet<long>();

            foreach (var roomInstance in ActiveRooms)
            {
                usedIds.Add(roomInstance.RoomId);
            }

            for (long i = long.MinValue; i < long.MaxValue; i++)
            {
                if (!usedIds.Contains(i))
                    return i;
            }

            return 0;
        }


        /// <summary>
        /// If client's connection was destroyed we need make other players to wait him.
        /// This is different situation to when client itself wants to quit or give up.
        /// </summary>
        public static void OnClientDisconnectedSuddenly(long connectionId)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            if (room != null)
            {
                room.OnClientDisconnectedSuddenly(connectionId);
            }
        }

        //Things that client sends to server
        #region Client's events

        /// <summary>
        /// Called by client who gave up a game
        /// </summary>
        /// <param name="connectionId">Player's connection id</param>
        public static void GiveUp(long connectionId)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            if (room.ContainsPlayer(connectionId))
                room.GiveUp(connectionId);

        }

        /// <summary>
        /// Called by client who leaves a game
        /// </summary>
        /// <param name="connectionId">Player's connection id</param>
        public static void LeaveRoom(long connectionId)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            if (room == null) return;

            if (room.ContainsPlayer(connectionId))
                room.LeaveRoom(connectionId);

        }

        public static void GetReady(long connectionId)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            if (room.ContainsPlayer(connectionId))
                room.GetReady(connectionId);
        }

        public static void GetNotReady(long connectionId)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            if (room.ContainsPlayer(connectionId))
                room.GetNotReady(connectionId);
        }

        public static void DropCardOnTable(long connectionId, string cardCode)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            if (room.ContainsPlayer(connectionId))
                room.DropCardOnTable(connectionId, cardCode);
        }

        public static void Pass(long connectionId)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            if (room.ContainsPlayer(connectionId))
                room.Pass(connectionId);
        }

        public static void CoverCardOnTable(long connectionId, string cardCodeOnTable, string cardCodeDropped)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            if (room.ContainsPlayer(connectionId))
                room.CoverCardOnTable(connectionId, cardCodeOnTable, cardCodeDropped);
        }


        #endregion

        /// <summary>
        /// Gets room in which player is
        /// </summary>
        /// <param name="connectionId">Player connection Id</param>
        /// <returns>Room for this player. Null if none.</returns>
        public static RoomInstance GetRoomForPlayer(long connectionId)
        {
            Client client = ClientManager.GetConnectedClient(connectionId);

            if (!client.IsInRoom)
            {
                return null;
            }

            return ActiveRooms.First(room => room.ContainsPlayer(client.ConnectionId));
        }

        public static void DeleteRoom(RoomInstance room)
        {
            ActiveRooms.Remove(room);
            room.Dispose();
        }
    }
}
