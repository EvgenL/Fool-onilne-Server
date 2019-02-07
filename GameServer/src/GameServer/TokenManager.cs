using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoolOnlineServer.src.GameServer
{
    public static class TokenManager
    {
        private const int UNUSED_TOKEN_EXPIRE_DELAY = 60000;

        private static HashSet<Token> notUsedTokens = new HashSet<Token>();

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
        /// Creates login token from only username
        /// </summary>
        public static Token CreateAnonymousToken(string username)
        {
            Token token = new Token(String.Empty, username, String.Empty);
            AcceptToken(token);
            return token;
        }

        public static Token UseToken(int tokenHash)
        {
            Token token = notUsedTokens.First(t => t.TokenHash == tokenHash);
            if (token != null)
            {
                notUsedTokens.Remove(token);
                return token;
            }
            else
            {
                return null;
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
