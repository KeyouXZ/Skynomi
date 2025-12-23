using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi.Rank
{
    public class Config
    {
        [JsonProperty("Use Parent for Rank")]
        public bool UseParent { get; set; } = true;
        [JsonProperty("Announce Rank Up")]
        public bool AnnounceRankUp { get; set; }
        [JsonProperty("Enable Rank Down")]
        public bool EnableRankDown { get; set; } = true;

        public Dictionary<string, Rank> Ranks { get; set; } = new();

        public static Config Read()
        {
            string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
            string configPath = Path.Combine(directoryPath, "Rank.json");
            Directory.CreateDirectory(directoryPath);

            try
            {
                var defaultConfig = new Config().DefaultConfig();
                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
                }
                var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? new Config();

                return config;
            }

            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new Config();
            }
        }

        private Config DefaultConfig()
        {
            var defaultConfig = new Config
            {
                Ranks =
                {
                    ["Rank1"] = new Rank
                    {
                        Prefix = "Level 1",
                        Suffix = "[i:4444]",
                        ChatColor = [255, 255, 255],
                        Cost = 100,
                        Permission = "",
                        Rewards = new Dictionary<string, int>
                        {
                            { "1", 1 },
                            { "2", 2 }
                        }
                    },
                    ["Rank2"] = new Rank
                    {
                        Prefix = "Level 2",
                        Suffix = "[i:4444]",
                        ChatColor = [255, 255, 255],
                        Cost = 200,
                        Permission = "",
                        Rewards = new Dictionary<string, int>
                        {
                            { "1", 1 },
                            { "2", 2 }
                        }
                    }
                }
            };

            return defaultConfig;
        }

        public class Rank
        {
            [JsonProperty("Prefix")]
            public string Prefix = string.Empty;

            [JsonProperty("Suffix")]
            public string Suffix = string.Empty;

            [JsonProperty("Chat Color")]
            public int[] ChatColor = [];

            [JsonProperty("Cost")]
            public long Cost;

            [JsonProperty("Permission")]
            public string Permission = string.Empty;

            [JsonProperty("Reward")]
            public Dictionary<string, int> Rewards = new();
            [JsonProperty("Restricted Items")]
            public List<int> RestrictedItems = [];
        }
    }
}