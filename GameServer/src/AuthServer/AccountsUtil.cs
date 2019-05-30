using System.Text.RegularExpressions;
using FoolOnlineServer.Db;

namespace FoolOnlineServer.AuthServer
{
    /// <summary>
    /// Static util methods to help with account managenent
    /// </summary>
    public static class AccountsUtil
    {
        /// <summary>
        /// Checks email is correct
        /// </summary>
        public static bool EmailIsValid(string email)
        {
            // Email regexp that 99.9% works
            // emailregex.com
            return (Regex.IsMatch(email,
                    "(?:[a-z0-9!#$%&\'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&\'*+/=?^_`{|}~-]+)*|\"(?:" +
                    "[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b" +
                    "\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])" +
                    "?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|" +
                    "[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])")
                );
        }

        /// <summary>
        /// Checks if account with this email is registred
        /// </summary>
        public static bool EmailUsed(string email)
        {
            // try get user from database
            var user = DatabaseOperations.GetUserByEmail(email);

            // if user wasn't registred, we will get null object
            return user != null;
        }

        /// <summary>
        /// Validate password
        /// todo validate to be not weak
        /// </summary>
        public static bool PasswordValid(string password)
        {
            return password.Length >= 6 && password.Length <= 40;
        }

        /// <summary>
        /// Validate nickname to be not too short and not too long and also dont contian whitespace
        /// </summary>
        public static bool NicknameValid(string nickmame)
        {
            return nickmame.Length >= 3 && nickmame.Length <= 20 && !nickmame.Contains(" ");
        }


        /// <summary>
        /// Checks if client's version actual to servers version
        /// </summary>
        public static bool CheckVersion(string clientVersion)
        {
            //if client is for example 1.3.1.2 and server is 1.3 then allow.
            //if client is for example 1.4.2 and server is 1.3 then not allow.

            bool ok = clientVersion.StartsWith(AccountsServer.ServerVersion);

            return ok;
        }
    }
}
