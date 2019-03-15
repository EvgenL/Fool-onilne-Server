using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using FoolOnlineServer.Db;
using FoolOnlineServer.Extensions;
using FoolOnlineServer.GameServer;
using FoolOnlineServer.src.AccountsServer.Packets;
using Logginf;
using SuperWebSocket;

namespace FoolOnlineServer.AccountsServer
{
    /// <summary>
    /// methods managing clients registration and auth on server
    /// This methods called straight after accounts server recieves data
    /// </summary>
    static class AccountsService
    {
        /// <summary>
        /// Validates player login data and closes sonnection
        /// </summary>
        public static bool AnonLogin(WebSocketSession session, XElement bodyXml)
        {
            //disconnect player if we dont allow anonymous
            if (!AccountsServer.AnonymousAllowed)
            {
                //Send error info to player
                AccountsServerSend.Send_Error(session, AccountReturnCodes.AnonymousLoginIsNotAllowed, "Anonymous login is not allowed");
                return false;
            }

            // read user Nickname
            string nickname = bodyXml.GetChildElement("Nickname")?.Value;
            if (!AccountsUtil.NicknameValid(nickname))
            {
                // Send error info to player
                AccountsServerSend.Send_Error(session, AccountReturnCodes.NicknameIsInvalid, "Nickname is invalid");
                return false;
            }

            // create auth token
            Token token = TokenManager.CreateAnonymousToken(nickname);

            AccountsServerSend.Send_LoginOk(session, token);

            return true;
        }

        /// <summary>
        /// Validates player login data and closes sonnection
        /// </summary>
        public static bool EmailLogin(WebSocketSession session, XElement bodyXml)
        {
            Log.WriteLine("Email login from " + session.RemoteEndPoint, typeof(AccountsService));

            // read user email
            string email = bodyXml.GetChildElement("Email")?.Value;

            // validate email from xml
            if (!AccountsUtil.EmailIsValid(email))
            {
                // Send error info to player
                AccountsServerSend.Send_Error(session, AccountReturnCodes.NicknameIsInvalid, "Email is invalid");
                return false;
            }



            // check if account exist in database
            var user = DatabaseOperations.GetUserByEmail(email);
            if (user == null)
            {
                AccountsServerSend.Send_Error(session, AccountReturnCodes.NoSuchAccount);
                return false;
            }


            // read password from xml
            string password = bodyXml.GetChildElement("Password")?.Value;
            // check password
            if (user.Password != password)
            {
                AccountsServerSend.Send_Error(session, AccountReturnCodes.WrongPassword);
                return false;
            }


            // create auth token
            Token token = TokenManager.CreateTokenRegistredAccount(user);

            AccountsServerSend.Send_LoginOk(session, token);

            return true;
        }

        
        /// <summary>
        /// Validate player login data and close sonnection
        /// Register new account if data is correct
        /// </summary>
        public static bool EmailRegistration(WebSocketSession session, XElement bodyXml)
        {
            // read user Nickname
            string nickname = bodyXml.GetChildElement("Nickname")?.Value;

            // read user email
            string email = bodyXml.GetChildElement("Email")?.Value;

            // read user pass
            string password = bodyXml.GetChildElement("Password")?.Value;
            
            // try register new acc
            var returnCode = DatabaseOperations.AddNewAccount(nickname, email, password);
            if (returnCode != AccountReturnCodes.Ok)
            {
                // Send error info to player
                AccountsServerSend.Send_Error(session, returnCode);
            }

            // auth user

            EmailLogin(session, bodyXml);

            return true;
        }

        /// <summary>
        /// validates xml body with 'Connection' child element
        /// </summary>
        public static bool ValidateConnectionXml(WebSocketSession session, XElement connectionXml)
        {
            if (connectionXml == null)
            {
                AccountsServerSend.Send_Error(session, AccountReturnCodes.EmptyRequest);
                return false;
            }

            // read game version
            string gameVersion = connectionXml.GetChildElement("ClientVersion")?.Value;
            if (!AccountsUtil.CheckVersion(gameVersion))
            {
                AccountsServerSend.Send_Error(session, AccountReturnCodes.BadVersion);
                return false;
            }

            return true;
        }



    }
}
