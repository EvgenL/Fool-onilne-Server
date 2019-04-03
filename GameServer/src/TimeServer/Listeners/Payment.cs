using System;
using FoolOnlineServer.Db;
using FoolOnlineServer.HTTPServer;
using FoolOnlineServer.Utils;
using Logginf;
using MySql.Data.MySqlClient;

namespace FoolOnlineServer.TimeServer.Listeners {
	public static class Payment {
		private const  DayOfWeek PayDayOfWeek = DayOfWeek.Wednesday;
		private const  int       PayHour      = 9;
		private static DateTime  nextPay      = DateTime.Today;


		public static void Check() {
			// Получаем текущее время
			var timeNow   = DateTime.UtcNow;
			var timeToday = DateTime.Today;

			// Загружаем дату следующей оплаты из базы
			string s_payDayTicks = ServerSettings.Get("PayDay");
			long   payDayTicks   = 0;

			if (!string.IsNullOrEmpty(s_payDayTicks)) payDayTicks = Convert.ToInt64(s_payDayTicks);
			// Если в базе нет записи о следующей оплате
			if (payDayTicks == 0) {
				SendPayment();
				return;
			}

			// Загружаем в DateTime дату оплаты из базы
			var payDay = new DateTime(payDayTicks);

			// Если сегодня день оплаты или он уже прошел (допустим сервер был выключен)
			if ((timeToday == payDay && timeNow.Hour >= PayHour) || timeToday > payDay) {
				SendPayment();
			}
		}


		public static void SendPayment() {
			Log.WriteLine("Start sending payments", typeof(Payments));
			var today = DateTime.Today;

			var receiver = ServerSettings.Get("payment_receiver_email");

			if (string.IsNullOrEmpty(receiver)) {
				Log.WriteLine("Receiver email not set", typeof(Payment));
				return;
			}

			var message = GetPaymentsMessage();

			// Отправка сообщения
			if (!Email.SendEmail("Вывод средств", message, receiver, "Сервер игры")) {
				// Если сообщение отправлено не было, то следующая попытка через день.
				var payDay = today.AddDays(1);
				ServerSettings.Set("PayDay", payDay.Ticks.ToString());
				Console.WriteLine("Failed to send payments");
				return;
			}

			// Получаем количество дней до следующей оплаты
			int daysToAdd = (((int) PayDayOfWeek + 1) - (int) today.DayOfWeek + 7) % 7;
			// День следующей оплаты
			var payDay2 = today.AddDays(daysToAdd);
			// Записываем день в БД
			ServerSettings.Set("PayDay", payDay2.Ticks.ToString());

			// Помечаем записи в базе как отправленные
			MessageSent();
			Log.WriteLine("Payments sent!", typeof(Payments));
		}


		private static string GetPaymentsMessage() {
			// create new command
			MySqlCommand command = new MySqlCommand {
				CommandText = "SELECT a.UserId, p.sum, p.created, a.Nickname, a.Email "  +
							"FROM payment p INNER JOIN accounts a on a.UserId=user_id  " +
							"WHERE type='1' AND status='0';"
			};

			command.Parameters.AddWithValue("@type",   ((int) Payments.Type.Withdrawal).ToString());
			command.Parameters.AddWithValue("@status", ((int) Payments.Status.NotPayed).ToString());

			var reader = DatabaseConnection.ExecuteReader(command);

			if (!reader.HasRows) {
				reader.Close();
				DatabaseConnection.CloseReader();
				return "Заявок на вывод нет";
			}

			string message = "";
			while (reader.Read()) {
				var userId   = reader.GetInt64 ("UserId");
				var sum      = reader.GetDouble("sum");
				var created  = reader.GetInt64 ("created");
				var nickname = reader.GetString("Nickname");
				var email    = reader.GetString("Email");

				var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
				dateTime = dateTime.AddSeconds(created).ToLocalTime();

				message += $"({dateTime}) {userId}:{nickname} запросил вывод в размере {sum} у.е. Email: {email}" + Environment.NewLine;
			}

			reader.Close();
			DatabaseConnection.CloseReader();
			return message;
		}


		private static void MessageSent() {
			var command = new MySqlCommand {
				CommandText = "UPDATE `payment` SET `status`=@status WHERE `status`=@old_status AND `type`=@old_type;"
			};

			command.Parameters.AddWithValue("@status", ((int) Payments.Status.Payed).ToString());
			command.Parameters.AddWithValue("@old_status", ((int) Payments.Status.NotPayed).ToString());
			command.Parameters.AddWithValue("@old_type", ((int) Payments.Type.Withdrawal).ToString());

			DatabaseConnection.ExecuteNonQuery(command);
		}
	}
}