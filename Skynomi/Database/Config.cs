using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi.Database
{
    public class Config
    {
        [JsonProperty("Database Type")]
        public string databaseType { get; set; } = "sqlite";

        [JsonProperty("Auto Save Interval (Seconds)")]
        public int autoSaveInterval { get; set; } = 600;

        [JsonProperty("SQLite Database Path")]
        public string databasePath { get; set; } = "Skynomi.sqlite3";

        [JsonProperty("MySqlHost")]
        public string MySqlHost { get; set; } = "localhost:3306";
        
        [JsonProperty("MySqlDbName")]
        public string MySqlDbName { get; set; } = "";

        [JsonProperty("MySqlUsername")]
        public string MySqlUsername { get; set; } = "";

        [JsonProperty("MySqlPassword")]
        public string MySqlPassword { get; set; } = "";

        public static Config Read()
        {
            string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
            string configPath = Path.Combine(directoryPath, "Database.json");
            Directory.CreateDirectory(directoryPath);

            try
            {
                Config config = new Config();

                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
                }
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? config;

                return config;
            }

            catch (Exception ex)
            {
                Skynomi.Utils.Log.Error(ex.ToString());
                return new Config();
            }
        }
    }
}