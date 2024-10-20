using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi {
    public class Config
    {
        public string Currency { get; set; } = "Skyorb";
        public bool AutoBroadcastShop { get; set; } = false;
        public int BroadcastIntervalInSeconds { get; set; } = 60;
        public Dictionary<string, Int32> ShopItems { get; set; } = new Dictionary<string, Int32>();


        public void Write() {
            string configPath = Path.Combine(TShock.SavePath, "Skynomi.json");
            File.WriteAllText(configPath, JsonConvert.SerializeObject(this, Formatting.Indented));

        }

        public static Config Read() {
            string configPath = Path.Combine(TShock.SavePath, "Skynomi.json");

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