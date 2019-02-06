using Logging;

namespace FoolOnlineServer.AccountsServer
{
    internal class AccountsServer
    {
        public static void ServerStart(int port)
        {
            Log.WriteLine("Accounts server started on port " + port, typeof(AccountsServer));
        }
    }
}
