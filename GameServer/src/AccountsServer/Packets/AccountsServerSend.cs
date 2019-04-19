using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FoolOnlineServer.AccountsServer;
using FoolOnlineServer.GameServer;
using Logginf;
using SuperWebSocket;

namespace FoolOnlineServer.src.AccountsServer.Packets
{
    public enum AccountReturnCodes
    {
        Ok,
        NicknameIsInvalid,
        AnonymousLoginIsNotAllowed,
        EmptyRequest,
        BadVersion,
        EmailIsInvalid,
        EmailUsed,
        PasswordIsInvalid,
        WrongPassword,
        NoSuchAccount
    }

    /// <summary>
    /// class responsive for sending data to clients by session
    /// </summary>
    static class AccountsServerSend
    {
        public static void Send_LoginOk(WebSocketSession session, Token token)
        {
            // init response
            var response = CreateLoginResponse(token);

            //Send 
            Send_Xml(session, response);
        }

        /// <summary>
        /// Create response body wtih error description and cloese session
        /// </summary>
        public static void Send_Error(WebSocketSession session, AccountReturnCodes code, string message = null)
        {
            //init response
            XElement response = new XElement("Response",
                new XElement("Result", "Error"),
                new XElement("ErrorInfo",
                    new XElement("Code", (int)code),
                    new XElement("CodeString", code.ToString()),
                    new XElement("Message", message)
                    )
            );
            //Send 
            Send_Xml(session, response);
        }

        /// <summary>
        /// Sends xml data coded in Unicode to session 
        /// and closes it
        /// </summary>
        private static void Send_Xml(WebSocketSession session, XElement body)
        {
            Log.WriteLine("Send_Xml " + session + ". body: " + body, typeof(AccountsServerSend));

            var bytes = Encoding.Unicode.GetBytes(body.ToString());
            session.Send(bytes, 0, bytes.Length);
            session.Close();
        }

        /// <summary>
        /// Creates response with game server endpoint and 
        /// auth toke specific for user
        /// </summary>
        private static XElement CreateLoginResponse(Token token)
        {
            XElement response = new XElement("Response",
                new XElement("Result", "Ok"),
                new XElement("LoginData",
                    new XElement("GameServerIp", FoolOnlineServer.AccountsServer.AccountsServer.GameServerIp), // add game server endpoint
                    new XElement("GameServerPort", FoolOnlineServer.AccountsServer.AccountsServer.GameServerPort),
                    new XElement("Token", token.TokenHash) // add auth token
                )
            );
            return response;
        }

    }
}
