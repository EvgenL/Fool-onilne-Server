using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoolOnlineServer.src.AccountsServer
{
    /// <summary>
    /// Object returned by database
    /// </summary>
    public class FoolUser
    {
        public long UserId;
        public string Nickname;
        public string Password;
        public string Email;
        public double Money;
    }
}
