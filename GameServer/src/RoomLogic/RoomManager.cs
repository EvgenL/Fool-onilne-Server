using System;
using System.Collections.Generic;
using GameServer.Packets;

namespace GameServer.RoomLogic
{
    /// <summary>
    /// Manages room instances.
    /// Only call this class methods when deling with client-room logic.
    /// </summary>
    public static class RoomManager
    {
        public const int RANDOM_ROOM_PLAYER_COUNT = 2;

        /// <summary>
        /// Active rooms. Both playing and waiting.
        /// Contains pair <RoomId-RoomInstance>
        /// </summary>
        public static Dictionary<long, RoomInstance> ActiveRooms = new Dictionary<long, RoomInstance>();

        /// <summary>
        /// Joins player to totally random room
        /// </summary>
        /// <param name="connectionId">Player's who wanna join connection id</param>
        public static void JoinRandom(long connectionId)
        {
            Log.WriteLine("[" + Server.GetClient(connectionId) + "] wants to join random room.", typeof(RoomManager));

            if (Server.GetClient(connectionId).IsInRoom)
            {
                Log.WriteLine("[" + Server.GetClient(connectionId) + "] is already in room. Abort.", typeof(RoomManager));
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
            foreach (var roomOb in ActiveRooms.Values)
            {
                RoomInstance roomInstance = (RoomInstance) roomOb;

                if (roomInstance.State == RoomInstance.RoomState.WaitingForPlayersToConnect && roomInstance.HasFreeSlots)
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
            ActiveRooms.Add(id, room);
            Log.WriteLine("Created a new room. Id: " + id, typeof(RoomManager));
            return room;
        }

        /// <summary>
        /// Get id for room which is not used
        /// </summary>
        /// <returns>Not used id. Returns 0 if every of [-long..+long] values are used.</returns>
        private static long GetFreeRoomId()
        {
            for (long id = long.MinValue; id < long.MaxValue; id++)
            {
                //If not contains certain id
                RoomInstance room;
                if (!ActiveRooms.TryGetValue(id, out room))
                {
                    return id;
                }
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
            room.GiveUp(connectionId);

        }

        public static void GetReady(long connectionId)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            room.GetReady(connectionId);
        }

        public static void GetNotReady(long connectionId)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            room.GetNotReady(connectionId);
        }

        public static void DropCardOnTable(long connectionId, string cardCode)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            room.DropCardOnTable(connectionId, cardCode);
        }

        public static void Pass(long connectionId)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            room.Pass(connectionId);
        }

        public static void PickUpCards(long connectionId)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
            room.PickUpCards(connectionId);
        }

        public static void CoverCardOnTable(long connectionId, string cardCodeOnTable, string cardCodeDropped)
        {
            RoomInstance room = GetRoomForPlayer(connectionId);
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

            long roomId = client.RoomId;
            //if theres room with certain player
            RoomInstance room = null;
            if (ActiveRooms.TryGetValue(roomId, out room))
            {
                room = (RoomInstance)ActiveRooms[roomId];
            }

            return room;
        }


        public static void DeleteRoom(RoomInstance room)
        {
            ActiveRooms.Remove(room.RoomId);
            room.Dispose();
        }
    }
}
