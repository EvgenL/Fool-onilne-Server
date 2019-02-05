using System;
using System.Text;
using System.Threading;
using Evgen.Byffer;
using GameServer.RoomLogic;

namespace GameServer
{
    /// <summary>
    /// Program entry point
    /// </summary>
    class Program
    {
        private static Thread consoleThread;
        private static bool consoleIsRunning = false;

        /// <summary>
        /// Program entry point
        /// </summary>
        private static void Main(string[] args)
        {
            consoleThread = new Thread(ConsoleThread);
            consoleThread.Start();

            //Connect to db
            //DatabaseConnection.Instance.MySQLInit();

            //Create a server instance and start it
            Server.ServerStart(5055);
        }

        /// <summary>
        /// Creates console thread, processes commands.
        /// </summary>
        private static void ConsoleThread()
        {
            string line;
            consoleIsRunning = true;

            //Command loop
            while (consoleIsRunning)
            {
                line = Console.ReadLine();

                if (line == "exit")
                {
                    consoleIsRunning = false;
                    return;
                }
                else if (line == "help")
                {
                    Console.WriteLine(Constants.HELP_COMMAND_STRING);
                }
                else if (line == "clear")
                {
                    Console.Clear();
                }
                else if (line == "stats")
                {
                    Console.WriteLine("Players on server: " 
                                      + Server.GetOnlineClientsCount() + ". Active rooms: " + RoomManager.ActiveRooms.Count);
                }
            }
        }
    }
}
