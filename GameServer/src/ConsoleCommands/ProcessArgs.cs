using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoolOnlineServer.Db;

namespace FoolOnlineServer.src.ConsoleCommands
{
    /// <summary>
    /// console arguments prosessor
    /// </summary>
    static class ProcessArgs
    {

        /// <summary>
        /// Non-void rerurn value maybe?
        /// </summary>
        public static void Process(string[] args)
        {
            //args = new string[]{ "connectionString=server=localhost;uid=root;pwd=" };
            // todo make it better

            foreach (var arg in args)
            {
                if (arg.StartsWith("connectionString="))
                {
                    DatabaseConnection.ConnectionString = arg.Substring(arg.IndexOf("=")+1);
                }
            }
        }
    }
}
