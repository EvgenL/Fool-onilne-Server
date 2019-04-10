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
				Console.WriteLine($"Insert{name}:{value}");
			}

			command.Parameters.AddWithValue("@name",  name);
			command.Parameters.AddWithValue("@value", value);

			DatabaseConnection.ExecuteNonQuery(command);
		}


		private static void LoadCache() {

		    settings = new Dictionary<string, string>();

            var command = new MySqlCommand {
				CommandText = "SELECT * FROM `server_settings`;"
			};

			var reader = DatabaseConnection.ExecuteReader(command);

			if (!reader.HasRows) {
				DatabaseConnection.CloseReader();
				return;
			}

			while (reader.Read()) {
				var name  = reader.GetString("name");
			    //var value = reader.GetString("value");
			    var value = reader.GetValue(1)?.ToString();

                settings.Add(name, value);
			}

			DatabaseConnection.CloseReader();
		}


		private static string GetFromCache(string name) {

            // if record exists
		    if (settings.ContainsKey(name))
		    {
		        return settings[name];
		    }
		    // else if not exists
		    else
		    {
                // create new record with empty value
                var command = new MySqlCommand
                {
                    CommandText = "INSERT INTO `server_settings` (name)" +
                                  "VALUES (@name);",
                };
                command.Parameters.AddWithValue("@name", name);

                // add to db
                DatabaseConnection.ExecuteNonQuery(command);
                


                // add to chached settings
                settings.Add(name, "");

                // return empty string
		        return "";
            }


		}
	}
}