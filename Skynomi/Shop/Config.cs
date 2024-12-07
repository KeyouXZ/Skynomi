using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi.ShopSystem
{
    public class Config
    {
        [JsonProperty("Auto Broadcast Shop")]
        public bool AutoBroadcastShop { get; set; } = false;
        [JsonProperty("Broadcast Interval in Seconds")]
        public int BroadcastIntervalInSeconds { get; set; } = 60;
        [JsonProperty("Shop Items")]
        public Dictionary<string, Int32> ShopItems { get; set; } = new Dictionary<string, Int32>();

        public static Config Read()
        {
            string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
            string configPath = Path.Combine(directoryPath, "Shop.json");
            Directory.CreateDirectory(directoryPath);

            try
            {
                Config config = new Config();

                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
                }
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));

                return config;
            }

            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
                return new Config();
            }
        }
    }
}