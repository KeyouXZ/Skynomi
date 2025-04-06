using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi {
    public class Config
    {
        public string Currency { get; set; } = "Skyorb";
        [JsonProperty("Currency Format")]
        public string CurrencyFormat { get; set; } = "{currency} {amount}";
        [JsonProperty("Abbreviasi Numerik")]
        public bool AbbsreviasiNumeric { get; set; }
        [JsonProperty("Reward Chance")]
        public int RewardChance { get; set; } = 100;
        [JsonProperty("Boss Reward")]
        public string BossReward { get; set; } = "{hp}/4*0.5";
        [JsonProperty("NPC Reward")]
        public string NpcReward { get; set; } = "{hp}/4*1.2";
        [JsonProperty("Drop on Death")]
        public decimal DropOnDeath { get; set; } = 0.5m;
        [JsonProperty("Reward From Statue")]
        public bool RewardFromStatue { get; set; }
        [JsonProperty("Reward From Friendly NPC")]
        public bool RewardFromFriendlyNpc { get; set; }
        [JsonProperty("Blacklist NPC")]
        public List<int> BlacklistNpc { get; set; } = new List<int>();
        [JsonProperty("Log Path")]
        public string LogPath { get; set; } = "tshock/Skynomi/logs";

        public static Config Read() {
            string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
            string configPath = Path.Combine(directoryPath, "Skynomi.json");
            Directory.CreateDirectory(directoryPath);

            try {
				Config config = new Config();

				if (!File.Exists(configPath)) {
					File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
				}
				config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? new Config();

				return config;
			}
			
			catch (Exception ex) {
				Utils.Log.Error(ex.ToString());
				return new Config();
			}
        }
    }
}