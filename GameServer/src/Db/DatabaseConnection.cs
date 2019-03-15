using System;
using System.ComponentModel.Design;
using System.Data;
using FoolOnlineServer.GameServer;
using Logginf;
using MySql.Data.MySqlClient;

namespace FoolOnlineServer.Db
{
    /// <summary>
    /// Class working with connection to MySQL database
    /// </summary>
    class DatabaseConnection //TODO add db
    {
        private enum SqlCommandType
        {
            ExecuteReader,
            ExecuteNonQuery,
            ExecuteScalar
        }

        #region Open/close connection

        public const string ConnectionString = "server=localhost;uid=root;pwd=";

        private static MySqlConnection presistentConnection;
        private static MySqlDataReader presistentReader;

        /// <summary>
        /// Set true if some method reads out from reader
        /// </summary>
        private static bool ReaderIsBusy = false;
        
        /// <summary>
        /// Opens new connection between server and db.
        /// If connection was open - does nuthing
        /// </summary>
        /// <returns>Opened connetion. Null if inacessable.</returns>
        private static bool ConnectionOpen()
        {
            if (presistentConnection == null
                || presistentConnection.State == ConnectionState.Closed
                || presistentConnection.State == ConnectionState.Broken)
            {
                presistentConnection = new MySqlConnection();
                presistentConnection.ConnectionString = ConnectionString;

                try
                {
                    presistentConnection.Open();
                    return true;
                }
                catch (Exception e)
                {
                    Log.WriteLine("Can't open connection.", typeof(DatabaseConnection));
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        #endregion

        /// <summary>
        /// Executes command with return of data reader
        /// </summary>
        /// <param name="command">command to execute</param>
        /// <returns>data reader</returns>
        public static MySqlDataReader ExecuteReader(MySqlCommand command)
        {
            return (MySqlDataReader)ExecuteCommand(command, SqlCommandType.ExecuteReader);
        }
        /// <summary>
        /// Executes command with no return
        /// </summary>
        /// <param name="command">command to execute</param>
        public static void ExecuteNonQuery(MySqlCommand command)
        {
            ExecuteCommand(command, SqlCommandType.ExecuteNonQuery);
        }
        /// <summary>
        /// Executes command with return of scalar object
        /// </summary>
        /// <param name="command">command to execute</param>
        /// <returns>scalar</returns>
        public static object ExecuteScalar(MySqlCommand command)
        {
            return ExecuteCommand(command, SqlCommandType.ExecuteScalar);
        }

        /// <summary>
        /// Executes command 
        /// </summary>
        private static object ExecuteCommand(MySqlCommand command, SqlCommandType commandType)
        {
            // Connect to mysql if wasn't
            ConnectionOpen();

            // tie command to connetion
            command.Connection = presistentConnection;

            // try execute command
            try
            {
                switch (commandType)
                {
                    case SqlCommandType.ExecuteReader:
                        presistentReader = command.ExecuteReader();
                        ReaderIsBusy = true;
                        return presistentReader;
                        break;
                    case SqlCommandType.ExecuteNonQuery:
                        command.ExecuteNonQuery();
                        break;
                    case SqlCommandType.ExecuteScalar:
                        return command.ExecuteScalar();
                        break;
                }
            }
            catch (Exception e)
            {
                // print if error occured 
                Log.WriteLine(e.Message, typeof(DatabaseConnection));
            }

            return null;
        }

        /// <summary>
        /// Closes reader and sets readerBusy flag to false
        /// SHOULD be called by method who got reader and
        /// finished reading.
        /// Reader.Close() should not be called by method who got reader and
        /// </summary>
        public static void CloseReader()
        {
            ReaderIsBusy = false;
            presistentReader.Close();
        }

        /// <summary>
        /// Tests if connection ok
        /// </summary>
        public static bool TestConnection()
        {
            // Connect to mysql if wasn't
            if (!ConnectionOpen())
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        /// <summary>
        /// If database doesn't exist 
        /// then tries to create it
        /// </summary>
        public static void TestIfDatabaseExists()
        {
            MySqlCommand command = new MySqlCommand();

            // create db
            command.CommandText = "CREATE DATABASE IF NOT EXISTS foolonline;";
            ExecuteNonQuery(command);

            // select db
            command.CommandText = "USE foolonline;";
            ExecuteNonQuery(command);

            // create table
            command.CommandText = "CREATE TABLE IF NOT EXISTS accounts(" +
                                  "" +
                                  "" +
                                  "" +
                                  "" +
                                  ");";
            ExecuteNonQuery(command);


        }
    }
}
