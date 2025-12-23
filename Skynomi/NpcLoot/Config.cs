using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi.NpcLoot;

public class Config
{
    [JsonProperty("Reward Chance")] public int RewardChance { get; set; } = 100;
    [JsonProperty("Boss Reward")] public string BossReward { get; set; } = "{hp}/4*0.5";
    [JsonProperty("NPC Reward")] public string NpcReward { get; set; } = "{hp}/4*1.2";
    [JsonProperty("Drop on Death")] public decimal DropOnDeath { get; set; } = 0.5m;
    [JsonProperty("Reward From Statue")] public bool RewardFromStatue { get; set; }

    [JsonProperty("Reward From Friendly NPC")]
    public bool RewardFromFriendlyNpc { get; set; }

    [JsonProperty("Blacklist NPC")] public List<int> BlacklistNpc { get; set; } = [];

    public static Config Read()
    {
        string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
        string configPath = Path.Combine(directoryPath, "NpcLoot.json");
        Directory.CreateDirectory(directoryPath);

        try
        {
            Config config = new Config();

            if (!File.Exists(configPath))
            {
                File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
            }

            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? new Config();

            return config;
        }

        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            return new Config();
        }
    }
}