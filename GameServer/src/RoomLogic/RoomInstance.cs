using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GameServer.Packets;

namespace GameServer.RoomLogic
{
    public class RoomInstance : IDisposable
    {
        public enum RoomState
        {
            WaitingForPlayersToConnect,
            PlayersGettingReady,
            Playing
        }

        public RoomState State { get; private set; }


        /// <summary>
        /// pause before sending NextTurn 
        /// Needed to animations on client side end
        /// </summary>
        private const int SLEEP_BEFORE_NEXT_TURN = 1000;


        /// <summary>
        /// Unique room number
        /// </summary>
        private long RoomId;

        /// <summary>
        /// Players in room
        /// </summary>
        private List<long> PlayerIds;

        /// <summary>
        /// if room was used more than once
        /// </summary>
        private int roundsPlayerInThisRoom;

        /// <summary>
        /// who was fool in last game if was
        /// </summary>
        private long lastLoserPlayerId = -1;

        /// <summary>
        /// MaxPlayers allowed to join
        /// </summary>
        public int MaxPlayers { private set; get; }

        /// <summary>
        /// Returns a number of players currently in room
        /// </summary>
        public int ConnectedPlayersN => PlayerIds.Count;


        public bool HasFreeSlots => ConnectedPlayersN < MaxPlayers;


        private Client[] clientsInRoom;

        /// <summary>
        /// This is numbers of slots to player id dictionary
        /// In fool game, players pass cards to each other counter-clockwise so the position on table matters.
        /// </summary>
        public Dictionary<int, long> Slots;

        #region Game rules fields

        /// <summary>
        /// Initial number of cards in talon
        /// </summary>
        private int talonCardsNumber = 32;

        /// <summary>
        /// in fool game usually ace can not be a trump card
        /// </summary>
        private bool aceCanBeTrump = false;

        /// <summary>
        /// In case i will want to change it
        /// </summary>
        private int maxCardsOnTable = 6;

        /// <summary>
        /// In case i will want to change it
        /// </summary>
        private int maxCardsOnTableFirstTurn = 5;

        /// <summary>
        /// Money bet in this room
        /// </summary>
        private int bet;


        #endregion

        #region Gameplay fields

        /// <summary>
        /// (Прикуп)
        /// Every card looks like this: 0.14 = ace of spades
        /// </summary>
        private Stack<string> talon;

        private string trumpCard;

        /// <summary>
        /// Player id : player cards in hand
        /// </summary>
        private List<List<string>> playerHands;

        /// <summary>
        /// cardsOnTable[i][0] - card on table
        /// cardsOnTable[i][1] - card covering this card
        /// </summary>
        private List<string[]> cardsOnTable; 

        /// <summary>
        /// Max amount of cards which player can draw from talon
        /// If player has more cards than this number, he wont take any
        /// </summary>
        private const int MAX_DRAW_CARDS = 6;

        /// <summary>
        /// Turn number
        /// </summary>
        private int turnN = 0;

        /// <summary>
        /// Did defender fassed in this game
        /// </summary>
        private bool defenderGaveUpDefence;

        /// <summary>
        /// Did attacker passed in this game
        /// </summary>
        private bool attackerPassedPriority;

        private Client attacker;
        private Client defender;

        private bool[] playersWon;

        #endregion

        public RoomInstance(long roomId, int maxPlayers)
        {
            this.RoomId = roomId;
            this.MaxPlayers = maxPlayers;

            //Create player connectionId list with capacity of MaxPlayers
            PlayerIds = new List<long>(maxPlayers);
            Slots = new Dictionary<int, long>(maxPlayers);
            clientsInRoom = new Client[maxPlayers];
        }

        #region Connection


        /// <summary>
        /// Called on player tries join room
        /// </summary>
        public bool JoinRoom(long connectionId)
        {
            //Do we have space for player?
            if (PlayerIds.Count > MaxPlayers)
            {
                //Send 'Fali To Join Full Room' if it somehow not empty
                ServerSendPackets.Send_FaliToJoinFullRoom(connectionId); 
                return false;
            }

            //Isnt this player aldeady in this room
            if (ContainsPlayer(connectionId))
            {
                //Send 'You Are Already In Room' if somehow player already joined this room
                ServerSendPackets.Send_YouAreAlreadyInRoom(connectionId);
                return false;
            }

            //Set player's slot
            int slotN = OccupyFreeSlot(connectionId);

            //if no free slots then return
            if (slotN == -1)
            {
                //Send 'Fali To Join Full Room' if it somehow not empty
                ServerSendPackets.Send_FaliToJoinFullRoom(connectionId); 
                return false;
            }

            //Add to player list
            Slots.Add(slotN, connectionId);
            PlayerIds.Add(connectionId);
            clientsInRoom[slotN] = Server.GetClient(connectionId);

            Log.WriteLine($"{GetClient(connectionId)} joined room.", this);

            //Set client's status to InRoom
            Client client = GetClient(connectionId);
            client.IsInRoom = true;
            client.RoomId = RoomId;
            client.SlotInRoom = slotN;

            //Send room data to newly connected client
            ServerSendPackets.Send_RoomData(connectionId, this);


            //Send message about this player joined to everybody expect this player
            for (int i = 0; i < PlayerIds.Count; i++)
            {
                if (PlayerIds[i] != connectionId)
                {
                    ServerSendPackets.Send_OtherPlayerJoinedRoom(PlayerIds[i], connectionId, slotN);
                }
            }

            //Check if all players joined
            CheckEverybobyJoined();

            return true;
        }

        /// <summary>
        /// Called by player who wants to leave room
        /// </summary>
        /// <param name="leftPlayerId">Player's connection id</param>
        public void LeaveRoom(long leftPlayerId)
        {
            //SEND WIN MESSAGE
            //if was playing: end game
            if (State == RoomState.Playing)
            {
                Dictionary<long, int> rewards = new Dictionary<long, int>();

                for (int i = 0; i < PlayerIds.Count; i++)
                {
                    rewards.Add(PlayerIds[i], 1000); //TODO calculate rewards
                }
                rewards[leftPlayerId] = -1000;

                //Send everybody endgame message
                foreach (var playerId in PlayerIds)
                {
                    ServerSendPackets.Send_EndGameGiveUp(playerId, leftPlayerId, rewards);
                }
                ClearLists();
            }

            var client = RemoveClientFromRoom(leftPlayerId);

            //send to everybody 'on player leave'
            foreach (var playerId in PlayerIds)
            {
                ServerSendPackets.Send_OtherPlayerLeftRoom(playerId, playerId, client.SlotInRoom);
            }

            Log.WriteLine("Player left room " + client, this);

            CheckEverybobyJoined();

            if (ConnectedPlayersN == 0)
            {
                RoomManager.DeleteRoom(this);
            }
        }

        private void ClearLists()
        {
            foreach (var hand in playerHands)
            {
                hand.Clear();
            }
            playerHands.Clear();
            cardsOnTable.Clear();

            talon.Clear();
            playersWon = null;
        }

        private Client RemoveClientFromRoom(long playerId)
        {
            //REMOVE PLAYER FROM GAME
            Client client = GetClient(playerId);
            client.IsInRoom = false;

            PlayerIds.Remove(playerId);
            Slots.Remove(client.SlotInRoom);
            clientsInRoom[client.SlotInRoom] = null;
            return client;
        }

        /// <summary>
        /// Is this player already joined this room
        /// </summary>
        /// <param name="connectionId">Player's connection id</param>
        public bool ContainsPlayer(long connectionId)
        {
            return PlayerIds.Contains(connectionId);
        }

        /// <summary>
        /// Set player's chair when he enters room
        /// </summary>
        /// <param name="connctionId">Player's connection id</param>
        private int OccupyFreeSlot(long connctionId)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (!Slots.ContainsKey(i))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets player's slot number
        /// </summary>
        /// <param name="connctionId">Player's connection id</param>
        public int GetPlayerSlotNumber(long connctionId)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (Slots.ContainsKey(i))
                {
                    if (Slots[i] == connctionId)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public long[] GetPlayerIds()
        {
            return PlayerIds.ToArray();
        }



        #endregion

        //Handle what clients do in room
        #region Client's messages

        /// <summary>
        /// If client's connection was destroyed we need make other players to wait him.
        /// This is different situation to when client itself wants to quit or give up.
        /// </summary>
        public void OnClientDisconnectedSuddenly(long connectionId)
        {
            //TODO wait for client reconnect and differ OnClientDisconnectedSuddenly and GiveUp
            LeaveRoom(connectionId);
            Log.WriteLine($"{GetClient(connectionId)} Suddenly disconnected from their room. Removing from room.", this);
        }

        public void GiveUp(long connectionId)
        {
            LeaveRoom(connectionId);

            
        }

        /// <summary>
        /// Recieved on someboby clicked ready
        /// </summary>
        public void GetReady(long connectionId)
        {
            //If somehow it was sent not during GetReady state then ignore
            if (State != RoomState.PlayersGettingReady) return;

            GetClient(connectionId).IsReady = true;

            //Send this to everybody in room
            foreach (var otherPlayerId in PlayerIds)
            {
                if (otherPlayerId == connectionId) continue;
                int slotN = GetSlotN(connectionId);
                ServerSendPackets.Send_OtherPlayerGotReady(otherPlayerId, connectionId, slotN);
            }

            //Check if everybody got ready
            foreach (var playerId in PlayerIds)
            {
                //if at least single client in room is not ready then do nothing
                if (!GetClient(playerId).IsReady) return;
            }

            //if everybody is ready
            StartGame();
        }

        /// <summary>
        /// Recieved on someboby clicked not ready
        /// </summary>
        public void GetNotReady(long connectionId)
        {
            //If somehow it was sent not during GetReady state then ignore
            if (State != RoomState.PlayersGettingReady) return;

            //Send this to everybody in room
            foreach (var playerId in PlayerIds)
            {
                if (playerId == connectionId) continue;
                int slotN = GetSlotN(connectionId);
                ServerSendPackets.Send_OtherPlayerGotNotReady(playerId, connectionId, slotN);
            }

            GetClient(connectionId).IsReady = false;
        }

        /// <summary>
        /// Recieved on somebody drops card on table
        /// </summary>
        public void DropCardOnTable(long connectionId, string cardCode)
        {
            //VALIDATION

            if (State != RoomState.Playing)
            {
                return;
            }

            //if somehow player sent this not during his turn //TODO or can add cards
            if (attacker.ConnectionId != connectionId)
            {
                ServerSendPackets.Send_DropCardOnTableErrorNotYourTurn(connectionId, cardCode);
                return;
            }

            //if table is empty: OK
            if (cardsOnTable.Count == 0)
            {
                PlayerDropsCardOnTable(connectionId, cardCode);
                return;
            }
            //else if table is not empty

            //if table is full
            if (cardsOnTable.Count >= maxCardsOnTable)
            {
                ServerSendPackets.Send_DropCardOnTableErrorTableIsFull(connectionId, cardCode);
                return;
            }

            //if table contains same value card
            //(get card values which are present on table)
            SortedSet<int> cardValuesOnTable = new SortedSet<int>();
            foreach (var cardPair in cardsOnTable)
            {
                if (cardPair.Length > 0)
                {
                    cardValuesOnTable.Add(CardUtil.Value(cardPair[0]));
                }
                if (cardPair[1] != null)
                {
                    cardValuesOnTable.Add(CardUtil.Value(cardPair[1]));
                }
            }

            //if table contains same value card: ///////////////////OK
            int droppedCardValue = CardUtil.Value(cardCode);

            if (cardValuesOnTable.Contains(droppedCardValue))
            {
                //Add to our table

                PlayerDropsCardOnTable(connectionId, cardCode);
                return;
            }
            //else if card with same value not present
            ServerSendPackets.Send_DropCardOnTableErrorCantDropThisCard(connectionId, cardCode);


        }

        /// <summary>
        /// Send OK to player who dropped card
        /// Send info about this card to every other player
        /// </summary>
        private void PlayerDropsCardOnTable(long playerWhoDrop, string cardCode)
        {
            //send OK to this player
            ServerSendPackets.Send_DropCardOnTableOk(playerWhoDrop, cardCode);

            //send card to everybody else
            foreach (var recieverPlayerId in PlayerIds)
            {
                if (recieverPlayerId == playerWhoDrop) continue;

                ServerSendPackets.Send_OtherPlayerDropsCardOnTable(recieverPlayerId, playerWhoDrop, GetSlotN(playerWhoDrop), cardCode);
            }

            //add to list
            cardsOnTable.Add(new string[] { cardCode, null });
            playerHands[GetSlotN(playerWhoDrop)].Remove(cardCode);

            TableUpdated(playerWhoDrop);
        }

        /// <summary>
        /// Called everytime somebody drops a card on table
        /// </summary>
        private void TableUpdated(long playerUpdatedTableId)
        {
            //If there is at least one card
            if (cardsOnTable.Count >= 1)
            {
                //if defender didn't gave up an attack
                if (!defenderGaveUpDefence)
                {
                    //set every player to no-pass
                    foreach (var client in clientsInRoom)
                    {
                        client.Pass = false;
                    }

                    if (AllCardsBeaten() && cardsOnTable.Count >= 6)
                    {
                        //send beaten
                        foreach (var playerId in PlayerIds)
                        {
                            ServerSendPackets.Send_Beaten(playerId);
                        }
                        NextTurnDelay();
                        
                    }
                }
                else //if defender DID gave up an attack
                {
                    if (EverybodyPassed() || cardsOnTable.Count >= 6) //todo check
                    {
                        //Send defender picks cards
                        SendDefenderPicksUp();

                        NextTurnDelay();
                    }
                }
            }

            //check player's win conditions
            int playerSlotN = GetSlotN(playerUpdatedTableId);
            if (playerHands[playerSlotN].Count == 0)
            {
                playersWon[playerSlotN] = true;
            }

            if (CheckGameEndConditions())
            {
                int foolSlotN = playersWon.ToList().IndexOf(false);
                EndGameFool(clientsInRoom[foolSlotN]);
            }
        }

        /// <summary>
        /// Ends game with somobody as fool (loser)
        /// </summary>
        private void EndGameFool(Client fool)
        {
            State = RoomState.PlayersGettingReady;

            CheckEverybobyJoined();

            foreach (var player in PlayerIds)
            {
                ServerSendPackets.Send_EndGameFool(player, fool.ConnectionId);
            }
        }

        /// <summary>
        /// This means that defender did gave up his defence
        /// </summary>
        private void SendDefenderPicksUp()
        {
            //Put cards from table to defender's hand
            List<string> defenderHand = playerHands[defender.SlotInRoom];
            foreach (var cardPair in cardsOnTable)
            {
                defenderHand.Add(cardPair[0]);
                if (cardPair.Length > 1 && cardPair[1] != null)
                {
                    defenderHand.Add(cardPair[1]);
                }
            }

            foreach (var playerId in PlayerIds)
            {
                ServerSendPackets.Send_DefenderPicksCards(playerId, defender.ConnectionId, defender.SlotInRoom);
            }
        }

        /// <summary>
        /// Recieved on somebody passes a turn
        /// </summary>
        public void Pass(long passedPlayerId)
        {
            if (State != RoomState.Playing)
            {
                return;
            }

            if (cardsOnTable.Count == 0) return;

            //Send to other players
            int slotN = GetSlotN(passedPlayerId);
            foreach (var otherPlayer in PlayerIds)
            {
                if (otherPlayer == passedPlayerId) continue;

                ServerSendPackets.Send_OtherPlayerPassed(otherPlayer, passedPlayerId, slotN);
            }

            GetClient(passedPlayerId).Pass = true;

            SomebodyPassed();
        }

        /// <summary>
        /// Recieved on defender wants to pick up cards from table
        /// </summary>
        public void PickUpCards(long pickedPlayerId)
        {
            if (State != RoomState.Playing)
            {
                return;
            }

            if (cardsOnTable.Count == 0) return;

            if (pickedPlayerId != defender.ConnectionId) return;

            //Set state variable
            defenderGaveUpDefence = true;

            //Send to other players
            int slotN = GetSlotN(pickedPlayerId);
            foreach (var otherPlayer in PlayerIds)
            {
                if (otherPlayer == pickedPlayerId) continue;

                ServerSendPackets.Send_OtherPlayerPickedUp(otherPlayer, pickedPlayerId, slotN);
            }

            GetClient(pickedPlayerId).Pass = true;

            SomebodyPassed();
        }

        /// <summary>
        /// Recieved on somebody covers card on table
        /// </summary>
        public void CoverCardOnTable(long coveredPlayerId, string cardCodeOnTable, string cardCodeDropped)
        {
            //if player doesnt have this card then ignore
            if (!playerHands[GetSlotN(coveredPlayerId)].Contains(cardCodeDropped))
            {
                return;
            }

            int cardOnTableIndex = FindNotCoveredCardOnTable(cardCodeOnTable);

            //if there is no this card
            if (cardOnTableIndex < 0)
            {
                //TODO send cant cover
                //ServerSendPackets.Send_CantCoverThisCard(connectionId, cardCodeOnTable, cardCodeDropped);
            }

            if (CanCoverThisCardWith(cardCodeOnTable, cardCodeDropped))
            {
                //Send to everybody in room
                int slotN = GetSlotN(coveredPlayerId);
                foreach (var recieverPlayerId in PlayerIds)
                {
                    if (recieverPlayerId == coveredPlayerId) continue;

                    ServerSendPackets.Send_OtherPlayerCoversCard(recieverPlayerId, coveredPlayerId, slotN,
                        cardCodeOnTable, cardCodeDropped);
                }

                //Set card [cardOnTableIndex] to covered
                cardsOnTable[cardOnTableIndex][1] = cardCodeDropped;
                playerHands[GetSlotN(coveredPlayerId)].Remove(cardCodeDropped);

                TableUpdated(coveredPlayerId);
            }
            else
            {
                //TODO send cant cover
                //ServerSendPackets.Send_CantCoverThisCard(connectionId, cardCodeOnTable, cardCodeDropped);
            }
        }

        #endregion

        /// <summary>
        /// Validation of can card on table be covered with come card
        /// </summary>
        private bool CanCoverThisCardWith(string cardCodeOnTable, string cardCodeDropped)
        {
            bool cardOnTableIsTrump = CardUtil.Suit(trumpCard) == CardUtil.Suit(cardCodeOnTable);
            bool droppedCardIsTrump = CardUtil.Suit(trumpCard) == CardUtil.Suit(cardCodeDropped);

            if (cardOnTableIsTrump)
            {
                if (droppedCardIsTrump)
                {
                    return CardUtil.Value(cardCodeOnTable) < CardUtil.Value(cardCodeDropped);
                }

                // else if dropped card is not trump
                return false;
            }

            //else if card on table is not trump

            if (droppedCardIsTrump)
            {
                return true;
            }

            // else if dropped card is not trump
            return CardUtil.Value(cardCodeOnTable) < CardUtil.Value(cardCodeDropped);
        }

        /// <summary>
        /// Returns index of card on table
        /// </summary>
        private int FindNotCoveredCardOnTable(string cardCodeOnTable)
        {
            for (int i = 0; i < cardsOnTable.Count; i++)
            {
                var cardPair = cardsOnTable[i];
                if (cardPair[0] == cardCodeOnTable)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// New turn can be started when:
        /// 1) everybody passes
        /// 2) time's up
        /// </summary>
        private void SomebodyPassed()
        {
            //if everybody are passed (also defender)
            if (EverybodyPassed())
            {
                //send defender takes cards
                SendDefenderPicksUp();

                NextTurnDelay();
            }
            else if (EverybodyButDefenderPassed())
            {
                if (AllCardsBeaten())
                {
                    // send beaten
                    foreach (var playerId in PlayerIds)
                    {
                        ServerSendPackets.Send_Beaten(playerId);
                    }
                    NextTurnDelay();
                }
            }
        }

        private bool EverybodyPassed()
        {
            //if everybody are passed
            return clientsInRoom.All(x => x.Pass);
        }

        private bool EverybodyButDefenderPassed()
        {
            List<Client> noDefender = new List<Client>();
            noDefender.AddRange(clientsInRoom.ToList());
            noDefender.Remove(defender);

            return noDefender.All(x => x.Pass);
        }

        private void NextTurnDelay()
        {
            //Wait a sec before next turn
            new Thread(() =>
            {
                //Wait a sec before next turn
                Thread.Sleep(SLEEP_BEFORE_NEXT_TURN);

                //Next turn

                if (CheckGameEndConditions())
                {
                    ClearLists();
                }
                else
                {
                    NextTurn();
                }

            }).Start();
        }

        private void NextTurn()
        {

            if (State != RoomState.Playing) return;

            Log.WriteLine("Next turn", this);

            turnN++;

            cardsOnTable.Clear();

            GiveCardsToPlayers();

            SetNextAttackerAndDefender();

            foreach (var client in clientsInRoom)
            {
                client.Pass = false;
            }

            foreach (var playerId in PlayerIds)
            {
                ServerSendPackets.Send_NextTurn(playerId, attacker.ConnectionId, attacker.SlotInRoom,
                    defender.ConnectionId, defender.SlotInRoom, turnN);
            }
            defenderGaveUpDefence = false;
            attackerPassedPriority = false;
        }

        /// <summary>
        /// Returns slot number for player
        /// </summary>
        private int GetSlotN(long connectionId)
        {
            return GetClient(connectionId).SlotInRoom;
        }

        /// <summary>
        /// Checks if everybody joined.
        /// If yes then tells player to click their 'ready' buttons
        /// </summary>
        private void CheckEverybobyJoined()
        {
            //if everybody is joined
            if (ConnectedPlayersN == MaxPlayers)
            {
                //Players get ready
                State = RoomState.PlayersGettingReady;

                foreach (var playerId in PlayerIds)
                {
                    GetClient(playerId).IsReady = false;
                }
            }
            else if (ConnectedPlayersN + 1 == MaxPlayers) //if not
            {
                //Players get unready
                State = RoomState.WaitingForPlayersToConnect;

                foreach (var playerId in PlayerIds)
                {
                    GetClient(playerId).IsReady = false;
                }
            }
        }

        private Client GetClient(long connectionId)
        {
            foreach (var client in clientsInRoom)
            {
                if (client != null && client.ConnectionId == connectionId)
                {
                    return client;
                }
                
            }
            return null;
        }

        #region Gameplay

        /// <summary>
        /// Inits game. Sets state to Playing.
        /// </summary>
        private void StartGame()
        {
            turnN = 0;

            Log.WriteLine("Everybody are ready. Start game.", this);

            cardsOnTable = new List<string[]>();
            playersWon = new bool[MaxPlayers];

            MixTalon();
            Log.WriteLine("Talon mixed", this);


            //tell to every player who does first turn
            foreach (var playerId in PlayerIds)
            {
                ServerSendPackets.Send_StartGame(playerId);

                //Send to every player data about: how much cards are in talon and what card is trump
                ServerSendPackets.Send_TalonData(playerId, talon.Count, trumpCard);
            }

            State = RoomState.Playing;

            NextTurn();


            //TODO start timer
        }

        /// <summary>
        /// who does first turn
        /// </summary>
        /// <returns>PlayerId</returns>
        private long SelectFirstAttacker()
        {
            //If there was round then first turn will be for player who sits before loser
            if (roundsPlayerInThisRoom > 0 && PlayerIds.Contains(lastLoserPlayerId))
            {
                int loserSlotN = GetSlotN(lastLoserPlayerId);
                int firstPlayerSlotN = loserSlotN--;
                if (firstPlayerSlotN < 0) firstPlayerSlotN = MaxPlayers - 1;
                return Slots[firstPlayerSlotN];
            }

            //else turn will be done by player who has LEAST value trump suit card
            long firstPlayer = GetPlayerWithLeastTrump();

            //if nobody has trumps then choose randomly
            if (firstPlayer < 0)
            {
                firstPlayer = new Random().Next(0, MaxPlayers);
            }

            return firstPlayer;
        }

        /// <summary>
        /// returns player who has LEAST value trump suit card
        /// </summary>
        /// <returns>-1 if noboby has trump cards</returns>
        private long GetPlayerWithLeastTrump()
        {
            string leastTrump = "";
            int leastValue = int.MaxValue;
            long firstPlayer = -1;

            foreach (var playerId in PlayerIds)
            {
                List<string> hand = playerHands[GetSlotN(playerId)];

                //Search for least trump
                foreach (var card in hand)
                {
                    if (CardUtil.Suit(card) == CardUtil.Suit(trumpCard)
                        && CardUtil.Value(card) < leastValue)
                    {
                        leastTrump = card;
                        leastValue = CardUtil.Value(card);
                        firstPlayer = playerId;
                    }
                }
            }

            return firstPlayer;
        }

        private void GiveCardsToPlayers()
        {
            //if talon empty
            if (talon.Count <= 0)
            {
                return;
            }

            //on first turn
            if (turnN == 1)
            {
                playerHands = new List<List<string>>();

                foreach (var playerId in PlayerIds)
                {
                    playerHands.Add(new List<string>());
                }
            }

            //give cards to players
            foreach (var recieverPlayer in PlayerIds)
            {
                if (playersWon[GetSlotN(recieverPlayer)]) continue;

                //if you aldeary have 6+ cards then you wot take any more on this turn
                int recieverSlotN = GetSlotN(recieverPlayer);
                int cardsToDraw = Math.Max(MAX_DRAW_CARDS - playerHands[recieverSlotN].Count, 0);
                cardsToDraw = Math.Min(cardsToDraw, talon.Count);

                //give up to 6 cards
                string[] cards = TakeCardsFromTaloon(cardsToDraw);

                    //add cards to hand
                    playerHands[recieverSlotN].AddRange(cards.ToList());

                    //Send cards to player
                    ServerSendPackets.Send_YouGotCardsFromTalon(recieverPlayer, cards);

                    //Everybody other got only information about amount of cards got by this player
                    foreach (var otherPlayer in PlayerIds)
                    {
                        if (otherPlayer == recieverPlayer) continue;
                        int slotN = GetSlotN(recieverPlayer);
                        ServerSendPackets.Send_EnemyGotCardsFromTalon(otherPlayer, recieverPlayer, cards.Length, slotN);
                    }
            }
        }

        private void MixTalon()
        {
            //Create sorted deck
            List<string> deck = new List<string>(talonCardsNumber);
            
            //Fill deck with cards
            for (byte i = 0; i < 4; i++) //Four suits (четыре масти)
            {
                for (byte j = 14; j > 14 - talonCardsNumber / 4; j--) // N/4 cards of each suit
                {
                    //Every card looks like this: 0.14 = ace of spades
                    deck.Add(i + "." + j);
                }
            }

            //Fill talon with randomly sorted cards
            talon = new Stack<string>(talonCardsNumber);
            Random random = new Random();
            for (int i = 0; i < talonCardsNumber; i++)
            {
                string randomCard = deck[random.Next(0, deck.Count)];
                talon.Push(randomCard);
                deck.Remove(randomCard);
            }


            //Set last card of talon as trump
            SelectTrumpCard();
        }

        private void SelectTrumpCard()
        {
            if (aceCanBeTrump)
            {
                trumpCard = talon.ElementAt(talon.Count - 1);
                return;
            }
            //else
            // In fool, ace usually can not be trump card so we search for first 
            // non-ace card and put it as very last card of talon

            var tempTalon = talon.ToList();

            foreach (var card in talon)
            {
                if (CardUtil.IsAce(card)) continue;
                // else
                trumpCard = card;
                tempTalon.Remove(card);
                break;
            }
            talon = new Stack<string>(talonCardsNumber);

            //put trump as very last card of talon
            talon.Push(trumpCard);

            foreach (var card in tempTalon)
            {
                talon.Push(card);
            }
        }

        private string[] TakeCardsFromTaloon(int amount)
        {
            string[] result = new string[amount];


            for (int i = 0; i < amount; i++)
            {

                if (talon.Count > 0)
                    result[i] = talon.Pop();
            }

            return result;
        }

        private bool AllCardsBeaten()
        {
            foreach (var cardPair in cardsOnTable)
            {
                if (cardPair.Length != 2)
                {
                    if (cardPair[0] == null || cardPair[0] == ""
                        || cardPair[1] == null || cardPair[1] == "")
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void SetNextAttackerAndDefender()
        {
            //Set them at first turn
            if (turnN == 1)
            {
                long attackerId = SelectFirstAttacker();
                attacker = GetClient(attackerId);

                int defenderSlotN = attacker.SlotInRoom + 1;
                if (defenderSlotN >= MaxPlayers) defenderSlotN = 0;
                defender = clientsInRoom[defenderSlotN];
            }
            else
            {
                int attackerSlotN = attacker.SlotInRoom;
                attackerSlotN++;
                if (attackerSlotN >= MaxPlayers) attackerSlotN = 0;
                if (defenderGaveUpDefence)
                {
                    attackerSlotN++;
                    if (attackerSlotN >= MaxPlayers) attackerSlotN = 0;
                }
                while (playersWon[attackerSlotN])
                {
                    attackerSlotN++;
                    if (attackerSlotN >= MaxPlayers) attackerSlotN = 0;
                }
                attacker = clientsInRoom[attackerSlotN];

                int defenderSlotN = attackerSlotN + 1;
                if (defenderSlotN >= MaxPlayers) defenderSlotN = 0;
                while (playersWon[attackerSlotN])
                {
                    defenderSlotN++;
                    if (defenderSlotN >= MaxPlayers) defenderSlotN = 0;
                }
                defender = clientsInRoom[defenderSlotN];
            }
        }

        /// <summary>
        /// Returns true all but one players won
        /// </summary>
        private bool CheckGameEndConditions()
        {
            if (talon.Count == 0)
            {
                //get numbers of players who not won
                int notWon = 0;
                for (int i = 0; i < MaxPlayers; i++)
                {
                    if (!playersWon[i])
                    {
                        notWon++;
                    }
                }

                if (notWon == 1)
                    return true;
            }

            return false;
        }

        #endregion


        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
            PlayerIds = null;
            Slots.Clear();
            Slots = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~RoomInstance()
        {
            ReleaseUnmanagedResources();
        }

        #endregion

        public override string ToString()
        {
            return $"Room {RoomId} players: {ConnectedPlayersN}/{MaxPlayers}";
        }
    }
}
