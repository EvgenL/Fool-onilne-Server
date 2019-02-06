using System;
using FoolOnlineServer.GameServer;
using Logging;
using MySql.Data.MySqlClient;

namespace FoolOnlineServer.Db
{
    /// <summary>
    /// Class working with connection to MySQL database
    /// </summary>
    class DatabaseConnection //TODO add db
    {
        #region Singleton Instance

        private static readonly object padlock = new object();

        private static DatabaseConnection _instance;

        /// <summary>
        /// Thread-safe singleton instance. Created on first use.
        /// </summary>
        public static DatabaseConnection Instance
        {
            get
            {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new DatabaseConnection();
                    }
                    return _instance;
                }
            }
            private set { }
        }


        #endregion

        /// <summary>
        /// Connecton to mysql db server
        /// </summary>
        private MySqlConnection connection;

        /// <summary>
        /// Called first. Connects to DB
        /// </summary>
        public void MySQLInit()
        {
            connection = new MySqlConnection();
            connection.ConnectionString = StaticParameters.ConnetionString;

            //SayReadAllAccounts();

            //MySqlCommand command = new MySqlCommand("SELECT * FROM foolonline.accounts;", connection);
        }

        /// <summary>
        /// Opens the connection between server and db.
        /// </summary>
        /// <returns>Opened connetion. Null if inacessable.</returns>
        public static MySqlConnection ConnectionOpen()
        {
            Log.WriteLine("Connecting...", typeof(DatabaseConnection));
            try
            {
                Instance.connection.Open();
            }
            catch (Exception e)
            {
                Log.WriteLine("Database server is unacessable.", typeof(DatabaseConnection));
                return null;
            }
            return Instance.connection;
        }

        public static void ConnectionClose()
        {
            Instance.connection.Close();
        }
    }
}
