using TShockAPI;

namespace Skynomi.RankSystem
{
    public class Ranks
    {
        private static Skynomi.RankSystem.Config rankConfig;

        public static void Initialize()
        {
            //LoadCommandSpecifiers();
            rankConfig = Skynomi.RankSystem.Config.Read();
            Skynomi.RankSystem.Commands.Initialize();
            CreateGroup(1);
        }

        public static void Reload()
        {
            //LoadCommandSpecifiers();
            rankConfig = Skynomi.RankSystem.Config.Read();
            Skynomi.RankSystem.Commands.Reload();
            CreateGroup(2);
        }

        public static void CreateGroup(int status)
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

                // Create the group
                if (status == 1)
                {
                    if (rankConfig.recreateInStart)
                    {
                        if (TShock.Groups.GroupExists(name))
                        {
                            TShock.Groups.DeleteGroup(name);
                        }
                        TShock.Groups.AddGroup(name, parent, TShock.Groups.GetGroupByName(parent).Permissions, "255,255,255");
                    }
                    else if (!TShock.Groups.GroupExists(name))
                    {
                        TShock.Groups.AddGroup(name, parent, TShock.Groups.GetGroupByName(parent).Permissions, "255,255,255");
                    }
                }
                
                // Assign prefix and suffix
                var group = TShock.Groups.GetGroupByName(name);
                if (group != null)
                {
                    group.Prefix = prefix;
                    group.Suffix = suffix;
                    group.ChatColor = chatColor;
                }
            }
        }
    }
}
