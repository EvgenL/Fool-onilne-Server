using FoolOnlineServer.Db;
using FoolOnlineServer.GameServer;
using FoolOnlineServer.HTTPServer;
using MySql.Data.MySqlClient;

namespace FoolOnlineServer.AccountsServer {
	public static class AccountManager {
		public static void WithdrawFunds(long connectionId, float sum) {
			Client client = ClientManager.GetConnectedClient(connectionId);

			// TODO: Нужно какое то уведомление если недостаточно средств
			if (client.UserData.Money < sum) return;

			var money = client.UserData.Money - sum;

			// Создание записи об оплате в БД
			Payments.CreatePayment(client.UserData.UserId, sum, Payments.Type.Withdrawal);

			// Обновление баланса игрока в БД
			MySqlCommand command = new MySqlCommand {
				CommandText = "UPDATE `accounts` SET `money`=@money WHERE UserId=@UserId;"
			};
			command.Parameters.AddWithValue("@money",  money);
			command.Parameters.AddWithValue("@UserId", client.UserData.UserId);
			DatabaseConnection.ExecuteNonQuery(command);

			client.UserData.Money = money;
		}
	}
}