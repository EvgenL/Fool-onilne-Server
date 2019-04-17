using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoolOnlineServer.Db;
using Logginf;
using System.Configuration;

namespace FoolOnlineServer.src.ConsoleCommands
{
    /// <summary>
    /// console arguments prosessor
    /// </summary>
    static class ProcessArgs
    {

        /// <summary>
        /// todo make it better and support more than one arg
        /// </summary>
        public static void Process(string[] args)
        {
            //foreach (var arg in args)
            var arg = args.Length == 0 ? "" : args[0];
            if (arg.StartsWith("connectionString="))
            {
                DatabaseConnection.ConnectionString = arg.Substring(arg.IndexOf("=", StringComparison.Ordinal) + 1);
                Log.WriteLine("Using connection string: " + DatabaseConnection.ConnectionString, typeof(ProcessArgs));
            }
            else
            {
                // read the default connection string
                var configReader = new AppSettingsReader();
                var connectionString = (string)configReader.GetValue("defaultConnectionString", typeof(string));
                DatabaseConnection.ConnectionString = connectionString;
            }
        }
    }
}
