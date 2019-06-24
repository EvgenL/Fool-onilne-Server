using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FoolOnlineServer.AuthServer;
using FoolOnlineServer.AuthServer.Packets;
using FoolOnlineServer.GameServer.Clients;
using FoolOnlineServer.GameServer.Packets;
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
                Money = reader.GetDouble("Money"),
                //AvatarFile = reader.GetString("AvatarFile")

            };

            // if avatar is set
            if (!reader.IsDBNull(6))
            {
                user.AvatarFile = reader.GetString("AvatarFile");
            }

            DatabaseConnection.CloseReader();

            return user;
        }

        /// <summary>
        /// Returns FoolUser object from database
        /// returns null if not registred
        /// </summary>
        /// <param name="userId">user's userId</param>
        public static FoolUser GetUserById(long userId)
        {
            // create new command
            MySqlCommand command = new MySqlCommand();
            command.CommandText = "SELECT * " +
                                  "FROM foolonline.accounts " +
                                  "WHERE UserId=@userId;";

            command.Parameters.AddWithValue("@userId", userId);

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
                Money = reader.GetDouble("Money"),
                //AvatarFile = reader.GetString("AvatarFile")
            };

            // if avatar is set
            if (!reader.IsDBNull(6))
            {
                user.AvatarFile = reader.GetString("AvatarFile");
            }

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


            // to1do временно даём каждому только зарегестрированому 100р (пока что)
            //command = new MySqlCommand();
            //command.CommandText = "UPDATE `foolonline`.`accounts` " +
            //                     "SET Money=100 WHERE `Nickname`=@nickname";
            //command.Parameters.AddWithValue("@nickname", nickname);
            // execute
            //DatabaseConnection.ExecuteNonQuery(command);

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
        

        public static void UpdateAvatar(long userId, string path)
        {
            // create new command
            var command = new MySqlCommand();
            command.CommandText = "UPDATE foolonline.accounts SET `AvatarFile`= @path WHERE UserId=@userId";

            command.Parameters.AddWithValue("@path", path);
            command.Parameters.AddWithValue("@userId", userId);

            // execute
            DatabaseConnection.ExecuteNonQuery(command);
        }

        public static void AddMoney(long userId, double sum)
        {
            var user = DatabaseOperations.GetUserById(userId);

            // create new command
            var command = new MySqlCommand();
            command.CommandText = "UPDATE foolonline.accounts SET `Money`= `Money` + @reward WHERE UserId=@userId";
            command.Parameters.AddWithValue("@reward", sum);
            command.Parameters.AddWithValue("@userId", userId);

            // execute
            DatabaseConnection.ExecuteNonQuery(command);

            var client = ClientManager.GetConnectedClientByUserId(userId);
            if (client != null)
            {
                client.UserData.Money += sum;
                ServerSendPackets.Send_UpdateUserData(client.ConnectionId);
            }
        }
    }
}
