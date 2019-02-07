using System;

namespace Logging
{
    /// <summary>
    /// Static class for logging to console or (todo) to file
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Log for non-static classes
        /// </summary>
        /// <param name="message">Text to log</param>
        /// <param name="sender">Sender object</param>
        public static void WriteLine(string message, object sender)
        {
            string[] senderNames = sender.ToString().Split('.');
            string senderName = senderNames[senderNames.Length-1];
            Console.WriteLine($"[{GetDateTime()}][{senderName}]: {message}");
        }

        /// <summary>
        /// Log for static classes
        /// </summary>
        /// <param name="message">Text to log</param>
        /// <param name="senderType">typeof(SenderClass)</param>
        public static void WriteLine(string message, Type senderType)
        {
            Console.WriteLine($"[{GetDateTime()}][{senderType.Name}]: {message}");
        }

        private static string GetTime()
        {
            return DateTime.Now.TimeOfDay.ToString("c").Split('.')[0];
        }

        private static string GetDateTime()
        {
            return DateTime.Now.ToString("g");
        }
    }
}
