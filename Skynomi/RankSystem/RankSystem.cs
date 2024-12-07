using TerrariaApi.Server;
using TShockAPI;
using Newtonsoft.Json.Linq;

namespace Skynomi.RankSystem
{
    public class Ranks
    {
        private static Skynomi.RankSystem.Config rankConfig;
        private static string commandSpecifier = "/";
        private static string commandSilentSpecifier = ".";

        public static void Initialize()
        {
            //LoadCommandSpecifiers();
            rankConfig = Skynomi.RankSystem.Config.Read();
            Skynomi.RankSystem.Commands.Initialize();
            CreateGroup();
        }

        public static void Reload()
        {
            //LoadCommandSpecifiers();
            rankConfig = Skynomi.RankSystem.Config.Read();
            Skynomi.RankSystem.Commands.Reload();
        }

        public static void CreateGroup()
        {
            int counter = 1;
            if (rankConfig == null || rankConfig.Ranks == null)
            {
                TShock.Log.ConsoleError("Config for Ranks is null.");
                return;
            }

            foreach (var key in rankConfig.Ranks)
            {
                string name = "rank_" + counter;
                string prefix = key.Value.Prefix ?? "";
                string suffix = key.Value.Suffix ?? "";
                string parent = (counter == 1) ? "default" : "rank_" + (counter-1).ToString();
                int[] color = key.Value.ChatColor;
                string chatColor = $"{color[0]},{color[1]},{color[2]}";
               

                counter++;

                if (!TShock.Groups.GroupExists(name))
                {
                    // Create the group
                    TShock.Groups.AddGroup(name, parent, TShock.Groups.GetGroupByName(parent).Permissions, "255,255,255");
                }
                
                // Assign prefix and suffix
                var group = TShock.Groups.GetGroupByName(name);
                if (group != null)
                {
                    group.Prefix = prefix;
                    group.Suffix = suffix;
                    group.ChatColor = chatColor;
                }
                else
                {
                    TShock.Log.ConsoleError("Failed to find group " + name + " after creation. Prefix and suffix were not applied.");
                }
            }
        }
    }
}
