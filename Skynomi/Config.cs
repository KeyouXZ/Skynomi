using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi {
    public class Config
    {
        [JsonProperty("Currency")]
        public string Currency { get; set; } = "Skyorb";
        [JsonProperty("Currency Format")]
        public string CurrencyFormat { get; set; } = "{currency} {amount}";
        [JsonProperty("Reward Chance")]
        public int RewardChance { get; set; } = 100;
        [JsonProperty("Theme")]
        public string Theme { get; set; } = "Simple";
        [JsonProperty("Theme List")]
        public string _ThemeList { get; set; } = "Simple & Detailed";
        [JsonProperty("Boss Reward")]
        public string BossReward { get; set; } = "{hp}/4*0.5";
        [JsonProperty("NPC Reward")]
        public string NpcReward { get; set; } = "{hp}/4*1.2";
        [JsonProperty("Drop on Death")]
        public decimal DropOnDeath { get; set; } = 0.5m;
        [JsonProperty("Reward From Statue")]
        public bool RewardFromStatue { get; set; } = false;
        [JsonProperty("Reward From Friendly NPC")]
        public bool RewardFromFriendlyNPC { get; set; } = false;

        public static Config Read() {
            string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
            string configPath = Path.Combine(directoryPath, "Skynomi.json");
            Directory.CreateDirectory(directoryPath);

            try {
				Config config = new Config();

				if (!File.Exists(configPath)) {
					File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
				}
				config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));

				return config;
			}
			
			catch (Exception ex) {
				TShock.Log.ConsoleError(ex.ToString());
				return new Config();
			}
        }
    }
}