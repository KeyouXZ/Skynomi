using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi.RankSystem
{
    public class Config
    {
        public Dictionary<string, Rank> Ranks { get; set; } = new Dictionary<string, Rank>();

        public static Config Read()
        {
            string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
            string configPath = Path.Combine(directoryPath, "Rank.json");
            Directory.CreateDirectory(directoryPath);

            try
            {
                Config config = new Config();

                var defaultConfig = new Config().defaultConfig();
                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
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

        private Config defaultConfig()
        {
            var defaultConfig = new Config();

            defaultConfig.Ranks["Rank1"] = new Rank
            {
                Prefix = "Level 1",
                Suffix = "[i:4444]",
                ChatColor = new int[] { 255, 255, 255 },
                Cost = 100,
                Rewards = new Dictionary<string, int>
                {
                    { "1", 1 },
                    { "2", 2 }
                }
            };

            defaultConfig.Ranks["Rank2"] = new Rank
            {
                Prefix = "Level 2",
                Suffix = "[i:4444]",
                ChatColor = new int[] { 255, 255, 255 },
                Cost = 200,
                Rewards = new Dictionary<string, int>
                    {
                        { "1", 1 },
                        { "2", 2 }
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
            public int[] ChatColor = new int[]{};

            [JsonProperty("Cost")]
            public int Cost = 0;

            [JsonProperty("Reward")]
            public Dictionary<string, int> Rewards = new Dictionary<string, int>();
        }
    }
}