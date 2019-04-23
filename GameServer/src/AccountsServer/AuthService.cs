using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoolOnlineServer.GameServer;
using FoolOnlineServer.GameServer.Packets;

namespace FoolOnlineServer.src.AccountsServer
{
    /// <summary>
    /// manages client authorization between accounts server and game server
    /// </summary>
    static class AuthService
    {

        /// <summary>
        /// Client sends auth token after succesful connection to game server
        /// This method checks if token is correct
        /// and marks user as authorized.
        /// Also sends Send_ErrorBadAuthToken and Send_AuthorizedOk 
        /// </summary>
        /// <param name="connectionId">User who sent</param>
        /// <param name="tokenString">user's token string</param>
        /// <returns>true on succesful connect, false on fail</returns>
        public static bool AuthorizeClientOnGameServer(long connectionId, int tokenHash)
        {
            Client client = ClientManager.GetConnectedClient(connectionId);

            // If client was already authorized then ignore.
            // It will be true if client did suddenly lost connection
            // and then reconnected in a short period of time
            // he will use the same tocken in this case
            if (client.Authorized)
            {
                // Send OK message to client
                ServerSendPackets.Send_AuthorizedOk(connectionId);
                ServerSendPackets.Send_UpdateUserData(connectionId);
                return true;
            }

            // get token from manager if exists
            Token token = TokenManager.UseToken(tokenHash);

            // if token doesn't exist then send error
            if (token == null)
            {
                ServerSendPackets.Send_ErrorBadAuthToken(connectionId);
                return false;
            }


            // Authorized OK
            client.Authorize(token);
            // Send OK message to client
            ServerSendPackets.Send_AuthorizedOk(connectionId);
            ServerSendPackets.Send_UpdateUserData(connectionId);

            return true;
        }
    }
}
