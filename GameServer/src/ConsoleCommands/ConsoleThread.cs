using System;
using System.Net.Mime;
using System.Threading;
using FoolOnlineServer.Db;
using FoolOnlineServer.GameServer.RoomLogic;
using FoolOnlineServer.TimeServer.Listeners;
using Logginf;

namespace FoolOnlineServer.GameServer {
	/// <summary>
	/// Creates console thread, processes commands.
	/// </summary>
	public class ConsoleThread {
		private static ConsoleThread instance;

		private static Thread consoleThread;
		private static bool   consoleIsRunning = false;

		private DateTime startTime = DateTime.Now;


		/// <summary>
		/// Starts to read commands
		/// </summary>
		public static void Start() {
			if (instance == null) {
				instance      = new ConsoleThread();
				consoleThread = new Thread(instance.ProcessCommands);
				consoleThread.Start();
			}
		}


		private void ProcessCommands() {
			consoleIsRunning = true;
			instance         = this;

			//Command loop
			while (consoleIsRunning) {
				var line    = Console.ReadLine()?.Split(' ');
				var command =  line?[0];

				switch (command) {
				    case "exit":
				    case "stop":
				    case "\\q":
                        consoleIsRunning = false;
                        // todo: close all servers
                        Environment.Exit(0);
						return;
					case "help":
						Console.WriteLine(Constants.HELP_COMMAND_STRING);
						break;
					case "clear":
						Console.Clear();
						break;
					case "stats":
					case "stat": {
						string text = "Players on server: " + ClientManager.GetOnlineClientsCount() + ".\n"
									+ "Active rooms: "      + RoomManager.ActiveRooms.Count         + "\n"
									+ "Uptime: "            + (DateTime.Now - startTime);

						Log.WriteLine(text, typeof(GameServer));
						break;
					}
					case "setSenderEmail": {
						if (line.Length < 5 || string.IsNullOrEmpty(line[1]) || string.IsNullOrEmpty(line[2]) || string.IsNullOrEmpty(line[3]) || string.IsNullOrEmpty(line[4])) {
							Console.WriteLine("Email sender, password or smtp host not set. Example: setSenderEmail test@test.test password smtp.test.com port");
							continue;
						}

						var email = line[1];
						var pwd   = line[2];
						var smtp  = line[3];
						var port  = line[4];

						ServerSettings.Set("email_sender",    email);
						ServerSettings.Set("email_pwd",       pwd);
						ServerSettings.Set("email_smtp_host", smtp);
						ServerSettings.Set("email_smtp_port", port);
						Console.WriteLine("Success!");
						break;
					}
					case "paymentReceiver": {
						if (line.Length < 2) {
							Console.WriteLine("Email not set Example: paymentReceiver receiver@test.test");
							continue;
						}

						var email = line[1];
						ServerSettings.Set("payment_receiver_email", email);
						Console.WriteLine("Success!");
						break;
					}
					case "sendPayments": {
						Payment.SendPayment();
						Console.WriteLine("Done");
						break;
					}
					default:
						Console.WriteLine("Invalid command. Use the help by entering the command: \"help\"");
						break;
				}
			}
		}
	}
}