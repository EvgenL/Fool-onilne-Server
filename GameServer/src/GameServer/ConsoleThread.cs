using System;
using System.Threading;
using FoolOnlineServer.GameServer.RoomLogic;
using Logginf;

namespace FoolOnlineServer.GameServer
{

    /// <summary>
    /// Creates console thread, processes commands.
    /// </summary>
    public class ConsoleThread
    {
        private static ConsoleThread instance;

        private static Thread consoleThread;
        private static bool consoleIsRunning = false;

        private DateTime startTime = DateTime.Now;

        /// <summary>
        /// Starts to read commands
        /// </summary>
        public static void Start()
        {
            if (instance == null)
            {
                instance = new ConsoleThread();
                consoleThread = new Thread(instance.ProcessCommands);
                consoleThread.Start();
            }
        }

        private void ProcessCommands()
        {
            string line;
            consoleIsRunning = true;
            instance = this;

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
                else if (line == "stats" || line == "stat")
                {
                    string text = "Players on server: " + ClientManager.GetOnlineClientsCount() + ".\n"
                        + "Active rooms: " + RoomManager.ActiveRooms.Count + "\n"
                        + "Uptime: " + (DateTime.Now - startTime);

                    Log.WriteLine(text, typeof(GameServer));
                }
            }
        }
    }
}
