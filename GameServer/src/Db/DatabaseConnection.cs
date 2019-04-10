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

        public const string ConnectionString = "server=localhost;uid=root;pwd=";//"server=localhost;uid=root;pwd=";

        private static MySqlConnection presistentConnection;
        private static MySqlDataReader presistentReader;

        /// <summary>
        /// Set true if some method reads out from reader
        /// </summary>
        private static bool ReaderIsBusy = false;
        
        /// <summary>
        /// Opens new connection between server and db.
        /// If connection was open - does nothing
        /// </summary>
        /// <returns>Opened conneсtion. Null if inaccessible.</returns>
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
                    Log.WriteLine("Can't open connection. " + e, typeof(DatabaseConnection));
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
            command.CommandText =
                "CREATE TABLE IF NOT EXISTS `accounts` (\r   `UserId` bigint(20) unsigned NOT NULL AUTO_INCREMENT,\r   `Nickname` varchar(20) NOT NULL,\r   `Password` varchar(40) NOT NULL,\r   `Email` varchar(50) NOT NULL,\r   `Money` double unsigned DEFAULT \'0\',\r   `MoneyFrozen` double unsigned DEFAULT \'0\',\r   PRIMARY KEY (`UserId`,`Email`,`Nickname`),\r   UNIQUE KEY `ID_UNIQUE` (`UserId`),\r   UNIQUE KEY `Email_UNIQUE` (`Email`)\r ) ENGINE=InnoDB AUTO_INCREMENT=232 DEFAULT CHARSET=utf8mb4 COMMENT=\'Table for storing user account information\'";
            ExecuteNonQuery(command);



            command.CommandText = "CREATE TABLE IF NOT EXISTS `payment` ("                                           +
                                  "`payment_id` BIGINT(20) UNSIGNED NOT NULL AUTO_INCREMENT,"                        +
                                  "`user_id` BIGINT(20) NOT NULL,`sum` FLOAT NULL DEFAULT 0,"                        +
                                  "`created` BIGINT(20) NULL,"                                                       +
                                  "`status` ENUM('0', '1', '2') NULL DEFAULT '0',"                                   +
                                  "`external_id` BIGINT(20) NULL COMMENT 'External payment id from payment system'," +
                                  "`requisites` VARCHAR(255) NULL COMMENT 'Requisites for withdraw money',"          +
                                  "`type` ENUM('0', '1') NOT NULL DEFAULT '0' COMMENT '0 - income, 1 - withdrawal'," +
                                  "PRIMARY KEY (`payment_id`),UNIQUE INDEX `payment_id_UNIQUE` (`payment_id` ASC) ) COMMENT = 'Payment requests';";

            ExecuteNonQuery(command);



            command.CommandText = "CREATE TABLE IF NOT EXISTS `server_settings` (" +
                                  "`name` CHAR(128) NOT NULL,"                     +
                                  "`value` VARCHAR(255) NULL,"                     +
                                  "PRIMARY KEY (`name`),"                          +
                                  "UNIQUE INDEX `name_UNIQUE` (`name` ASC) );";
            ExecuteNonQuery(command);
        }
    }
}
