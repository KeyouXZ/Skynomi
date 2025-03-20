using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi.ShopSystem
{
    public class Config
    {
        [JsonProperty("Auto Broadcast Shop")]
        public bool AutoBroadcastShop { get; set; }

        [JsonProperty("Broadcast Interval in Seconds")]
        public int BroadcastIntervalInSeconds { get; set; } = 60;

        [JsonProperty("Protected by Region")]
        public bool ProtectedByRegion { get; set; }

        [JsonProperty("Shop Region")]
        public string ShopRegion { get; set; } = "ShopRegion";

        [JsonProperty("Shop Items")]
        public Dictionary<string, ShopItem> ShopItems { get; set; } = new Dictionary<string, ShopItem>();

        public static Config Read()
        {
            string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
            string configPath = Path.Combine(directoryPath, "Shop.json");
            Directory.CreateDirectory(directoryPath);

            try
            {
                Config config = defaultConfig();

                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
                }
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? config;

                return config;
            }

            catch (Exception ex)
            {
                Utils.Log.Error(ex.ToString());
                return new();
            }
        }

        private static Config defaultConfig()
        {
            var defaultConfig = new Config
            {
                ShopItems =
                {
                    ["4444"] = new ShopItem
                    {
                        buyPrice = 1000,
                        sellPrice = 900,
                    },
                    ["1"] = new ShopItem
                    {
                        buyPrice = 2,
                        sellPrice = 1,
                    }
                }
            };

            return defaultConfig;
        }

        public class ShopItem {
            [JsonProperty("Buy Price")]
            public int buyPrice { get; set; }

            [JsonProperty("Sell Price")]
            public int sellPrice { get; set; }
        }
    }
}