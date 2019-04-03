namespace FoolOnlineServer.GameServer
{
    public static class Constants
    {
        /// <summary>
        /// String printed out by a 'help' console command
        /// </summary>
        public const string HELP_COMMAND_STRING = "\nList of available commands:\n"
                                                  + "help - This page\n"
                                                  + "stats - Current server state\n"
                                                  + "exit - Shutdown the application\n"
                                                  + "setSenderEmail - Settings for EmailSender. You need to pass the parameters: \"email\", \"password\", \"smtp\" \"host\", port\n"
                                                  + "paymentReceiver - Email for withdrawal requests\n"
                                                  + "sendPayments - Sending orders for withdrawal in manual mode\n";
    }
}
