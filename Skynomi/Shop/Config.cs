namespace Skynomi.Shop;

using Newtonsoft.Json;
using TShockAPI;

public class Config
{
    [JsonProperty("Auto Broadcast Shop")] public bool AutoBroadcastShop { get; set; }

    [JsonProperty("Broadcast Interval in Seconds")]
    public int BroadcastIntervalInSeconds { get; set; } = 60;

    [JsonProperty("Shop Region")] public string ShopRegion { get; set; } = "ShopRegion";

    [JsonProperty("List Length")] public int ListLength { get; set; } = 5;

    [JsonProperty("Shop Items")]
    public Dictionary<string, ShopItem> ShopItems { get; set; } = new Dictionary<string, ShopItem>();

    public static Config Read()
    {
        string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
        string configPath = Path.Combine(directoryPath, "Shop.json");
        Directory.CreateDirectory(directoryPath);

        try
        {
            Config config = DefaultConfig();

            if (!File.Exists(configPath))
            {
                File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
            }

            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? config;

            return config;
        }

        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            return new Config();
        }
    }

    private static Config DefaultConfig()
    {
        var defaultConfig = new Config
        {
            ShopItems =
            {
                ["4444"] = new ShopItem
                {
                    prefix = 0,
                    buyPrice = 1000,
                    sellPrice = 900,
                },
                ["1"] = new ShopItem
                {
                    prefix = 15,
                    buyPrice = 2,
                    sellPrice = 1,
                }
            }
        };

        return defaultConfig;
    }

    public class ShopItem
    {
        [JsonProperty("Prefix")] public int prefix { get; set; }
        [JsonProperty("Buy Price")] public int buyPrice { get; set; }

        [JsonProperty("Sell Price")] public int sellPrice { get; set; }
    }
}