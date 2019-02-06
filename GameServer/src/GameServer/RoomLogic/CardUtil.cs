namespace FoolOnlineServer.GameServer.RoomLogic
{
    /// <summary>
    /// Static class for helping convert card codes like 0.14 into readable values
    /// </summary>
    public static class CardUtil
    {
        /*
            J = 11, //Валет
            Q = 12, //Королева
            K = 13, //Король
            A = 14, //Туз
        */

        public static int Value(string cardName)
        {
            return int.Parse(cardName.Split('.')[1]);
        }
        public static int Suit(string cardName)
        {
            return int.Parse(cardName.Split('.')[0]);
        }

        public static bool IsAce(string cardName)
        {
            return Value(cardName) == 14; //14 is ace value
        }
    }
}
