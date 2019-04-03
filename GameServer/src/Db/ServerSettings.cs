using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace FoolOnlineServer.Db {
	public static class ServerSettings {
		private static Dictionary<string, string> settings;


		public static string Get(string name) {
			if (settings == null) LoadCache();
			return GetFromCache(name);
		}


		public static void Set(string name, string value) {
			var command = new MySqlCommand();

			// Обновляем записи в базе и в кэше
			// Если запись уже существует
			if (settings != null && settings.ContainsKey(name)) {
				settings[name] = value;

				command.CommandText = "UPDATE `server_settings` SET `value`=@value WHERE name=@name;";
			}
			else {
				if (settings == null) settings = new Dictionary<string, string>();
				settings.Add(name, value);

				command.CommandText = "INSERT INTO `server_settings` (`name`, `value`) VALUES (@name, @value);";
			}

			command.Parameters.AddWithValue("@name",  name);
			command.Parameters.AddWithValue("@value", value);

			DatabaseConnection.ExecuteNonQuery(command);
		}


		private static void LoadCache() {
			var command = new MySqlCommand {
				CommandText = "SELECT * FROM `server_settings`;"
			};

			var reader = DatabaseConnection.ExecuteReader(command);

			if (!reader.HasRows) {
				reader.Close();
				DatabaseConnection.CloseReader();
				return;
			}

			settings = new Dictionary<string, string>();
			while (reader.Read()) {
				var name  = reader.GetString("name");
				var value = reader.GetString("value");

				settings.Add(name, value);
			}

			reader.Close();
			DatabaseConnection.CloseReader();
		}


		private static string GetFromCache(string name) {
			if (settings == null) return string.Empty;

			return settings.ContainsKey(name) ? settings[name] : "";
		}
	}
}