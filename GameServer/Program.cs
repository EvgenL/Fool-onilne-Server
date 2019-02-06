namespace FoolOnlineServer
{
    /// <summary>
    /// Program entry point
    /// </summary>
    public class Program
    {
        private static void Main(string[] args)
        {
            AccountsServer.AccountsServer.ServerStart(5056);

            GameServer.GameServer.ServerStart(5055);
        }
    }
}
