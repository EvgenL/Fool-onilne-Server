using FoolOnlineServer.GameServer;

namespace FoolOnlineServer
{
    /// <summary>
    /// Program entry point
    /// </summary>
    public class Program
    {

        private static void Main(string[] args)
        {
            //Start console thread for reading a commands
            ConsoleThread.Start();

            //Start login server
            AccountsServer.AccountsServer.ServerStart(5054);

            //Start game server
            GameServer.GameServer.ServerStart(5055);
        }
    }
}
