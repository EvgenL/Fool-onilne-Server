using System;
using System.Collections.Generic;
using System.Linq;
using GameServer.Packets;

namespace GameServer.RoomLogic
{
    /// <summary>
    /// Manages room instances.
    /// Only call this class methods when deling with client-room logic.
    /// </summary>
    public static class RoomManager
    {
        public const int RANDOM_ROOM_PLAYER_COUNT = 4;//2;

        /// <summary>
        /// Active rooms. Both playing and waiting.
        /// Contains pair <RoomId-RoomInstance>
        /// </summary>
        public static HashSet<RoomInstance> ActiveRooms = new HashSet<RoomInstance>();

        /// <summary>
        /// Joins player to totally random room
        /// </summary>
        /// <param name="connectionId">Player's who wanna join connection id</param>
        public static void JoinRandom(long connectionId)
        {
            /*Log.WriteLine("[" + Server.GetClient(connectionId) + "] wants to join random room.", typeof(RoomManager));

            if (Server.GetClient(connectionId).IsInRoom)
            {
                Log.WriteLine("[" + Server.GetClient(connectionId) + "] is already in room. Abort.", typeof(RoomManager));
                return;
            }*/

            //Getting not-full rooms
            List<RoomInstance> availableRooms = GetAvailableRooms();

            RoomInstance randomRoom;

            //if no rooms
            if (availableRooms.Count == 0)
            {
                Log.WriteLine("No available rooms.", typeof(RoomManager));

                //Creting a new room
                randomRoom = CreateRandomRoom();
            }
            else //if somebody's playing
            {
                //Selecting a random room
                int randomNum = new Random().Next(0, availableRooms.Count);
                randomRoom = availableRooms[randomNum];
            }

            //Client joins random room
            if (randomRoom.JoinRoom(connectionId))
            {
                //Send 'OK' if room has free slots
                ServerSendPackets.Send_JoinRoomOk(connectionId, randomRoom.RoomId);
            }
            else //join fails
            {
                //TODO continue finding another room
            }
        }

        /// <summary>
        /// Selecting rooms which are not playing and have a free slot
        /// </summary>
        /// <returns>List of available rooms</returns>
        public static List<RoomInstance> GetAvailableRooms()
        {
            List<RoomInstance> AvailableRooms =new List<RoomInstance>();

            //Selecting rooms which are not playing and have a free slot
            foreach (var roomInstance in ActiveRooms)
            {
                bool haveFreeSlots = roomInstance.ConnectedPlayersN < roomInstance.MaxPlayers;

                if (roomInstance.State == RoomInstance.RoomState.WaitingForPlayersToConnect && haveFreeSlots)
                {
                    AvailableRooms.Add(roomInstance);
                }
            }

            return AvailableRooms;
        }

        private static RoomInstance CreateRandomRoom()
        {
            long id = GetFreeRoomId();
            RoomInstance room = new RoomInstance(id, RANDOM_ROOM_PLAYER_COUNT); //TODO set not random amount of players
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
            Client client = Server.GetClient(connectionId);

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
