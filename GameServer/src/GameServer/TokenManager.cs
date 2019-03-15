using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoolOnlineServer.src.AccountsServer;

namespace FoolOnlineServer.GameServer
{
    public static class TokenManager
    {
        private const int UNUSED_TOKEN_EXPIRE_DELAY = 60000;

        private static HashSet<Token> notUsedTokens = new HashSet<Token>();
        private static HashSet<Token> usedTokens = new HashSet<Token>();

        /// <summary>
        /// Set new token as unused 
        /// and expire in after UNUSED_TOKEN_EXPIRE_DELAY
        /// </summary>
        /// <param name="tokenString"></param>
        private static void AcceptToken(Token token)
        {
            notUsedTokens.Add(token);
            Task.Delay(UNUSED_TOKEN_EXPIRE_DELAY).ContinueWith(t => Expire(token));
        }

        /// <summary>
        /// Creates login token from only username for anonymous login
        /// </summary>
        public static Token CreateAnonymousToken(string nickname)
        {
            Token token = new Token(nickname);
            AcceptToken(token);
            return token;
        }

        /// <summary>
        /// Creates login token from registred user's data
        /// </summary>
        public static Token CreateTokenRegistredAccount(FoolUser user)
        {
            Token token = new Token(user);
            AcceptToken(token);
            return token;
        }

        public static Token UseToken(int tokenHash)
        {
            Token token = notUsedTokens.First(t => t.TokenHash == tokenHash);
            if (token != null)
            {
                notUsedTokens.Remove(token);
                usedTokens.Add(token);

                token.Used = true;

                return token;
            }
            else
            {
                return null;
            }
        }

        public static void DeleteToken(Token token)
        {
            if (notUsedTokens.Contains(token))
            {
                notUsedTokens.Remove(token);
            }
            if (usedTokens.Contains(token))
            {
                usedTokens.Remove(token);
            }
        }

        private static void Expire(Token token)
        {
            if (notUsedTokens.Contains(token))
            {
                notUsedTokens.Remove(token);
            }
        }
    }
}
