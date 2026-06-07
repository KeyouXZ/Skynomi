using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi.Auction;

public class Config
{
    [JsonProperty("Broadcast Auction")]
    public bool BroadcastAuction { get; set; } = true;

    [JsonProperty("Auction Duration Seconds")]
    public int AuctionDurationSeconds { get; set; } = 20;

    [JsonProperty("Bid Extension Seconds")]
    public int BidExtensionSeconds { get; set; } = 2;

    [JsonProperty("Minimum Bid Increment")]
    public int MinBidIncrement { get; set; } = 1;

    public static Config Read()
    {
        string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
        string configPath = Path.Combine(directoryPath, "Auction.json");
        Directory.CreateDirectory(directoryPath);

        try
        {
            Config config = new();

            if (!File.Exists(configPath))
            {
                File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? new Config();

            return config;
        }
        catch (Exception ex)
        {
            Skynomi.Log.Error(ex.ToString());
            return new Config();
        }
    }
}
