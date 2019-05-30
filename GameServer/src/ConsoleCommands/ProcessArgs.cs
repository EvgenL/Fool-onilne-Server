using System;
using System.Configuration;
using Logginf;

namespace FoolOnlineServer.ConsoleCommands
{
    /// <summary>
    /// console arguments prosessor
    /// </summary>
    static class ProcessArgs
    {

        /// <summary>
        /// todo make it better and support more than one arg
        /// </summary>
        public static void Process(string[] args)
        {
            //foreach (var arg in args)
            var arg = args.Length == 0 ? "" : args[0];
            if (arg.StartsWith("connectionString="))
            {
                string connectionString = arg.Substring(arg.IndexOf("=", StringComparison.Ordinal) + 1);
                Log.WriteLine("Using connection string argument: " + connectionString, typeof(ProcessArgs));

                // write to config
                Configuration config = ConfigurationManager
                    .OpenExeConfiguration(System.Reflection.Assembly.GetEntryAssembly().Location);
                config.AppSettings.Settings.Remove("connectionString");
                config.AppSettings.Settings.Add("connectionString", connectionString);
                config.Save(ConfigurationSaveMode.Minimal);
            }
        }
    }
}
