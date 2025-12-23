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

    public static Config Read()
    {
        string directoryPath = Path.Combine(TShock.SavePath, "Skynomi");
        string configPath = Path.Combine(directoryPath, "Skynomi.json");
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