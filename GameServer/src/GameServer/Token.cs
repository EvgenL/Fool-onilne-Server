using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoolOnlineServer.src.GameServer
{
    public sealed class Token
    {
        public Token(string userId, string nickname, string passwordHash)
        {
            UserId = userId;
            Nickname = nickname;
            PasswordHash = passwordHash;

            StringBuilder sb = new StringBuilder();
            sb.Append(userId);
            sb.Append(nickname);
            sb.Append(passwordHash);
            sb.Append((DateTime.Now - DateTime.MinValue).TotalSeconds);
            TokenHash = sb.ToString().GetHashCode();
        }

        public Token CreateAnonymous(string nickname)
        {
            return new Token(nickname.GetHashCode().ToString(), nickname, nickname);
        }

        public string UserId;
        public string Nickname;
        public string PasswordHash;

        public bool Used;

        public int TokenHash;

    }
}
