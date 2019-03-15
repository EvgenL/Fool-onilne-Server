using System;
using System.Text;
using FoolOnlineServer.src.AccountsServer;

namespace FoolOnlineServer.GameServer
{
    /// <summary>
    /// Auth token
    /// 1) Created by AccountsServer
    /// 2) Saved in TokenManager as not used
    /// 3) Sent to player
    /// 4) Player uses it to enter GameServer
    /// 5) GameServer asks TokenManager for validation
    /// </summary>
    public class Token
    {
        public enum AuthorizationMethod
        {
            Email,
            Anonymous,
            OAuth
        }

        public FoolUser OwnerUser;

        public bool Used;

        public int TokenHash => this.GetHashCode();

        public AuthorizationMethod authMethod;

        /// <summary>
        /// Anonymous token constructor
        /// </summary>
        public Token(string nickname)
        {
            // create new user object with specified nickname and random other values
            this.OwnerUser = new FoolUser
            {
                Nickname = nickname,

                UserId = DateTime.Now.Ticks,
                Email = "",
                Password = "",
                Money = 0d
        
            };
            this.authMethod = AuthorizationMethod.Anonymous;
        }

        /// <summary>
        /// Registred user token constructor
        /// </summary>
        public Token(FoolUser user)
        {
            this.OwnerUser = user;
            this.authMethod = AuthorizationMethod.Email;
        }

    }
}
