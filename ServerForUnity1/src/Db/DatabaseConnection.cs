using System;
using MySql.Data.MySqlClient;

namespace ServerForUnity1.Db
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
            Say("Connecting...");
            try
            {
                Instance.connection.Open();
            }
            catch (Exception e)
            {
                Say("Database server is unacessable.");
                return null;
            }
            return Instance.connection;
        }

        public static void ConnectionClose()
        {
            Instance.connection.Close();
        }

        private static void Say(string message)
        {
            Console.WriteLine("[DbConnection]: " + message);
        }
    }
}
