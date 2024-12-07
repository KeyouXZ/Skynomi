using TerrariaApi.Server;
using TShockAPI;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Skynomi.RankSystem
{
    public class Ranks
    {
        private static Skynomi.RankSystem.Config config;
        private static string commandSpecifier = "/";
        private static string commandSilentSpecifier = ".";

        public static void Initialize()
        {
            LoadCommandSpecifiers();
            config = Skynomi.RankSystem.Config.Read();
            Skynomi.RankSystem.Commands.Initialize();
        }

        public static void Reload()
        {
            LoadCommandSpecifiers();
            config = Skynomi.RankSystem.Config.Read();
            Skynomi.RankSystem.Commands.Reload();
        }

        private static void LoadCommandSpecifiers()
        {
            string configPath = Path.Combine(TShock.SavePath, "config.json");

            if (File.Exists(configPath))
            {
                try
                {
                    string jsonText = File.ReadAllText(configPath);
                    JObject json = JObject.Parse(jsonText);

                    commandSpecifier = json["CommandSpecifier"]?.ToString() ?? "/";
                    commandSilentSpecifier = json["CommandSilentSpecifier"]?.ToString() ?? ".";
                }
                catch
                {
                    TShock.Log.ConsoleError("Failed to load CommandSpecifier from tshock/config.json. Using defaults.");
                }
            }
            else
            {
                TShock.Log.ConsoleError("tshock/config.json not found. Using default command specifiers.");
            }
        }

        public void OnChat(ServerChatEventArgs args)
        {
            if (args.Text.StartsWith(commandSpecifier)
                || args.Text.StartsWith(commandSilentSpecifier)
                || args.Text.StartsWith(commandSpecifier + "help")
                || args.Text.StartsWith(commandSilentSpecifier + "help")
                ) return;

            string playerName = TShock.Players[args.Who].Name;
            string prefix = Skynomi.Database.GetLevel(playerName);
            prefix = prefix == "default" ? "" : prefix + " ";

            string modifiedMessage = $"{prefix}{playerName}: {args.Text}";
            args.Handled = true;

            TShock.Utils.Broadcast(
                modifiedMessage,
                TShock.Players[args.Who].Group.R,
                TShock.Players[args.Who].Group.G,
                TShock.Players[args.Who].Group.B
            );
        }
    }
}
