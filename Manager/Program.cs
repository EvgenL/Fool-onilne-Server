using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manager
{
    /// <summary>
    /// Program entry point
    /// </summary>
    public class Program
    {
        private static void Main(string[] args)
        {
            
            //start game server
            GameServer.Server.ServerStart(5055);
            //start login server
            //AccountsServer.Server.ServerStart(5056);

        }
    }
}
