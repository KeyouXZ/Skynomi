using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi;

public class Config
{
    public string Currency { get; set; } = "Skyorb";
    [JsonProperty("Currency Format")] public string CurrencyFormat { get; set; } = "{currency} {amount}";
    [JsonProperty("Numeric Abbreviation")] public bool NumericAbbreviation { get; set; }
    [JsonProperty("Log Path")] public string LogPath { get; set; } = "tshock/Skynomi/logs";
    [JsonProperty("Debug Mode")] public bool DebugMode { get; set; }
    public WebServer Web { get; set; } = new();

    public static Config Read()
    {
        string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
        string configPath = Path.Combine(directoryPath, "Skynomi.json");
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
            Web =
            {
                Secure = false,
                Address = "localhost",
                Port = 7978,
                Root = "tshock/Skynomi/logs"
            }
        };
        
        return defaultConfig;
    }

    public class WebServer
    {
        public bool Secure { get; set; }
        public string Address { get; set; } = "localhost";
        public int Port { get; set; } = 7879;
        public string Root { get; set; } = "tshock/Skynomi/web";
    }
}