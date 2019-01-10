using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace ServerForUnity1.Db
{
    public static class DatabaseOperations
    {
        public static void AddAccount(string username, string password, string email)
        {
            if (!TestEmail(email))
            {
                throw new Exception("Email contains wrong symbols");
            }

            //Connect to mysql and get connection object
            MySqlConnection connection = DatabaseConnection.ConnectionOpen();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO `foolonline`.`accounts` " +
                                  "(`Username`, `Password`, `Email`) " +
                                  "VALUES (@username, @password, @email);";

            string sha1pass = GetSha1(password);

            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", sha1pass);
            command.Parameters.AddWithValue("@email", email);

            try
            {
                int rowsAffected = command.ExecuteNonQuery();
                Say($"Added user {email}.");
            }
            catch (Exception e)
            {
                Say(e.Message);
            }
            finally
            {
                //disconnect from mysql
                DatabaseConnection.ConnectionClose();
            }
        }


        private static void Say(string message)
        {
            Console.WriteLine("[DatabaseOperations]: " + message);
        }

        #region Util methods to put in different calss TODO

        /// <summary>
        /// Creates sha1 from string
        /// </summary>
        /// <param name="text">input string</param>
        /// <returns>sha1</returns>
        private static string GetSha1(string text)
        {
            var sha1 = new SHA1Managed();
            var plaintextBytes = Encoding.UTF8.GetBytes(text);
            var hashBytes = sha1.ComputeHash(plaintextBytes);

            var sb = new StringBuilder();
            foreach (var hashByte in hashBytes)
            {
                sb.AppendFormat("{0:x2}", hashByte);
            }

            var hashString = sb.ToString();
            return hashString;
        }

        /// <summary>
        /// Test string by regexp for containing only letters and numbers
        /// </summary>
        private static bool TestSqlValue(string value)
        {
            //Begins with any letter or number, contains more than 3 and less than 32 symbols.
            var match = Regex.Match(value, @"^[A-z|0-9]{3,32}");
            return match.Success;
        }

        /// <summary>
        /// Test string for being an email
        /// </summary>
        private static bool TestEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
