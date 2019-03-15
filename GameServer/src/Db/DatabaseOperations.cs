using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FoolOnlineServer.AccountsServer;
using FoolOnlineServer.src.AccountsServer;
using FoolOnlineServer.src.AccountsServer.Packets;
using MySql.Data.MySqlClient;

namespace FoolOnlineServer.Db
{
    public static class DatabaseOperations
    {
        /// <summary>
        /// Returns FoolUser object from database
        /// returns null if not registred
        /// </summary>
        /// <param name="email">user's email</param>
        public static FoolUser GetUserByEmail(string email)
        {
            // create new command
            MySqlCommand command = new MySqlCommand();
            command.CommandText = "SELECT * " +
                                  "FROM foolonline.accounts " +
                                  "WHERE Email=@email;";

            command.Parameters.AddWithValue("@email", email);

            // execute
            var reader = DatabaseConnection.ExecuteReader(command);
            // if user doesn't exists
            if (!reader.HasRows)
            {
                DatabaseConnection.CloseReader();
                return null;
            }

            reader.Read();

            // read from reader
            FoolUser user = new FoolUser
            {
                UserId = reader.GetInt64("UserId"),
                Nickname = reader.GetString("Nickname"),
                Password = reader.GetString("Password"),
                Email = reader.GetString("Email"),
                Money = reader.GetDouble("Money")
            };

            DatabaseConnection.CloseReader();

            return user;
        }

        /// <summary>
        /// validates and adds new accunt to database.
        /// </summary>
        public static AccountReturnCodes AddNewAccount(string nickname, string email, string password)
        {
            // validate form
            var returnCode = ValidateNewAccount(nickname, email, password);
            if (returnCode != AccountReturnCodes.Ok)
            {
                return returnCode;
            }

            // create new command
            MySqlCommand command = new MySqlCommand();
            command.CommandText = "INSERT INTO `foolonline`.`accounts` " +
                                  "(`Nickname`, `Password`, `Email`) " +
                                  "VALUES (@nickname, @password, @email);";

            command.Parameters.AddWithValue("@nickname", nickname);
            command.Parameters.AddWithValue("@password", password);
            command.Parameters.AddWithValue("@email", email);


            // execute
            DatabaseConnection.ExecuteNonQuery(command);

            return AccountReturnCodes.Ok;
        }

        /// <summary>
        /// Validated new account data
        /// </summary>
        private static AccountReturnCodes ValidateNewAccount(string nickname, string email, string password)
        {
            // check nickname validity
            if (!AccountsUtil.NicknameValid(nickname))
                return AccountReturnCodes.NicknameIsInvalid;

            // check email validity
            if (!AccountsUtil.EmailIsValid(email))
                return AccountReturnCodes.EmailIsInvalid;

            // check email used
            if (AccountsUtil.EmailUsed(email))
                return AccountReturnCodes.EmailUsed;

            // check password validity
            if (!AccountsUtil.PasswordValid(password))
                return AccountReturnCodes.PasswordIsInvalid;

            return AccountReturnCodes.Ok;
        }

    }
}
