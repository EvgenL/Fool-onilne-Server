using System;
using System.Threading;
using FoolOnlineServer.GameServer;
using FoolOnlineServer.TimeServer.Listeners;

namespace FoolOnlineServer.TimeServer {
	/// <summary>
	/// Содержит событие, которое вызывается %CheckTimesPerSecond% раз в секунду
	/// </summary>
	public class TimeServer {
		public delegate void CheckDel();
		public event CheckDel Check;

		private const  int        CheckTimesPerSecond = 10;
		private static TimeServer instance;
		private static Thread     timeThread;


		public static void Init() {
			if (instance == null) {
				instance   = new TimeServer();
				timeThread = new Thread(instance.Start);
				timeThread.Start();
			}
		}


		private void Start() {
			InitListeners();

			while (true) {
				Thread.Sleep(1000 / CheckTimesPerSecond);
				Check?.Invoke();
			}
		}


		private void InitListeners() {
			Check += Payment.Check;
		}
	}
}