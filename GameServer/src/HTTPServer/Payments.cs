using System;
using FoolOnlineServer.Db;
using FoolOnlineServer.GameServer;
using MySql.Data.MySqlClient;

namespace FoolOnlineServer.HTTPServer {
	public static class Payments {
		public enum Status {
			NotPayed  = 0,
			Cancelled = 1,
			Payed     = 2,
		}

		public enum Type {
			Income     = 0,
			Withdrawal = 1,
		}


		/// <summary>
		/// Создание записи об оплате в БД
		/// </summary>
		public static Payment CreatePayment(long userId, float sum, Type type, string requisites = "") {
			MySqlCommand command = new MySqlCommand {
				CommandText = "INSERT INTO `payment` "                        +
							"(`user_id`, `sum`, `created`, `status`, `requisites`) VALUES " +
							"(@user_id, @sum, @time, @status, @requisites);"
			};

			command.Parameters.AddWithValue("@user_id",    userId);
			command.Parameters.AddWithValue("@sum",        sum);
			command.Parameters.AddWithValue("@time",       DateTimeOffset.UtcNow.ToUnixTimeSeconds());
			command.Parameters.AddWithValue("@status",     ((int) Status.NotPayed).ToString());
			command.Parameters.AddWithValue("@type",       ((int) type).ToString());
			command.Parameters.AddWithValue("@requisites", requisites);

			DatabaseConnection.ExecuteNonQuery(command);

			// Получение созданной строки
			command = new MySqlCommand {
				CommandText = "SELECT * FROM `payment` WHERE user_id = @user_id ORDER BY payment_id DESC;"
			};

			command.Parameters.AddWithValue("@user_id", userId);
			var reader = DatabaseConnection.ExecuteReader(command);

			Payment payment = null;

			// Получение объекта
			if (reader.HasRows) {
				payment = new Payment(reader); // Создаем объект из результатов
			}

			reader.Close();
			DatabaseConnection.CloseReader();
			return payment;
		}


		public static void UpdateStatus(long paymentId, Status status) {
			MySqlCommand command = new MySqlCommand {
				CommandText = "UPDATE `payment` SET `status`=@status WHERE payment_id=@payment_id;"
			};

			command.Parameters.AddWithValue("@status",     ((int) status).ToString());
			command.Parameters.AddWithValue("@payment_id", paymentId);

			DatabaseConnection.ExecuteNonQuery(command);
		}


		public static void SetExternalPaymentId(long paymentId, long extPaymentId) {
			MySqlCommand command = new MySqlCommand {
				CommandText = "UPDATE `payment` SET `external_id`=@ext_id WHERE payment_id=@payment_id;"
			};

			command.Parameters.AddWithValue("@payment_id", paymentId);
			command.Parameters.AddWithValue("@ext_id",     extPaymentId);

			DatabaseConnection.ExecuteNonQuery(command);
		}


		/// <summary>
		/// Returns Payment object from database
		/// </summary>
		public static Payment GetPaymentById (long paymentId, Type type) {
			// create new command
			MySqlCommand command = new MySqlCommand {
				CommandText = "SELECT * "              +
							"FROM foolonline.payment " +
							"WHERE payment_id=@payment_id AND type=@type;"
			};

			command.Parameters.AddWithValue("@payment_id", paymentId);
			command.Parameters.AddWithValue("@type", ((int) type).ToString());


			// execute
			var reader = DatabaseConnection.ExecuteReader(command);
			return new Payment(reader);
		}


		public static void PayUserMoney(long userId, double sum) {
			Console.WriteLine("user_id: " + userId);
			MySqlCommand command = new MySqlCommand {
				CommandText = "SELECT * "               +
							"FROM foolonline.accounts " +
							"WHERE UserId=@UserId;"
			};

			command.Parameters.AddWithValue("@UserId", userId);
			var reader = DatabaseConnection.ExecuteReader(command);

			if (!reader.HasRows) {
				reader.Close();
				DatabaseConnection.CloseReader();
				Console.WriteLine("Has no rows");
				return;
			}

			reader.Read();

			if (reader.IsDBNull(reader.GetOrdinal("UserId"))) {Console.WriteLine("Has no user_id");return;}

			double money = 0;
			if (!reader.IsDBNull(reader.GetOrdinal("Money"))) {
				money = reader.GetDouble("Money");
			}
			money += sum;

			reader.Close();
			DatabaseConnection.CloseReader();

			command = new MySqlCommand {
				CommandText = "UPDATE `accounts` SET `Money`=@money WHERE UserId=@UserId;"
			};

			command.Parameters.AddWithValue("@UserId", userId);
			command.Parameters.AddWithValue("@money",  money);

			DatabaseConnection.ExecuteNonQuery(command);
		}


		public class Payment {
			public Payment(MySqlDataReader reader) {
				if (!reader.HasRows) {
					reader.Close();
					DatabaseConnection.CloseReader();
					return;
				}

				reader.Read();

				if (!reader.IsDBNull(reader.GetOrdinal("payment_id"))) {
					PaymentId = reader.GetInt64("payment_id");
				}
				if (!reader.IsDBNull(reader.GetOrdinal("user_id"))) {
					UserId = reader.GetInt64("user_id");
				}
				if (!reader.IsDBNull(reader.GetOrdinal("sum"))) {
					Sum = reader.GetDouble("sum");
				}
				if (!reader.IsDBNull(reader.GetOrdinal("created"))) {
					Created = reader.GetInt32("created");
				}
				if (!reader.IsDBNull(reader.GetOrdinal("status"))) {
					Status = (Status) (int.Parse(reader.GetString("status")));
				}
				if (!reader.IsDBNull(reader.GetOrdinal("external_id"))) {
					ExternalPaymentId = reader.GetInt64("external_id");
				}
				if (!reader.IsDBNull(reader.GetOrdinal("type"))) {
					Type = (Type) (int.Parse(reader.GetString("type")));
				}
				if (!reader.IsDBNull(reader.GetOrdinal("requisites"))) {
					Requisites = reader.GetString("requisites");
				}

				reader.Close();
			}


			public long   PaymentId;
			public long   UserId;
			public Double Sum;
			public int    Created;
			public Status Status;
			public long   ExternalPaymentId;
			public Type   Type;
			public string Requisites;
		}
	}
}