using IL.Terraria.GameContent.RGB;
using TShockAPI;

namespace Skynomi.RankSystem
{
    public class Ranks
    {
        private static Skynomi.RankSystem.Config rankConfig;

        public static void Initialize()
        {
            rankConfig = Skynomi.RankSystem.Config.Read();
            Skynomi.RankSystem.Commands.Initialize();
            CreateGroup(1);
        }

        public static void Reload()
        {
            bool tempConf = rankConfig.useParent;
            rankConfig = Skynomi.RankSystem.Config.Read();
            Skynomi.RankSystem.Commands.Reload();
            CreateGroup(2);

            if (tempConf != rankConfig.useParent)
            {
                TShock.Log.ConsoleWarn(Skynomi.Utils.Messages.ParentSettingChanged);
            }
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
                string parent = (counter == 1) ? "default" : "rank_" + (counter - 1).ToString();
                if (!rankConfig.useParent) parent = "";

                int[] color = key.Value.ChatColor;
                string chatColor = $"{color[0]},{color[1]},{color[2]}";
                string permission = key.Value.Permission.Replace(" ", ",") ?? "";

                // Create the group
                if (status == 1)
                {
                    if (TShock.Groups.GroupExists(name))
                    {
                        TShock.Groups.DeleteGroup(name);
                    }
                    TShock.Groups.AddGroup(name, parent, TShock.Groups.GetGroupByName(parent != "" ? parent : "default").Permissions, "255,255,255");
                }

                // Assign prefix and suffix
                var group = TShock.Groups.GetGroupByName(name);
                if (group != null)
                {
                    group.Prefix = prefix;
                    group.Suffix = suffix;
                    group.ChatColor = chatColor;

                    // Remove Permissions
                    if (parent == "")
                    {
                        var parentGroup = TShock.Groups.GetGroupByName("default");

                        string topermission = parentGroup.Permissions;
                        if (!string.IsNullOrEmpty(permission))
                        {
                            topermission += "," + permission;
                        }

                        group.Permissions = topermission;
                    }
                    else if (rankConfig.useParent)
                    {
                        if (!string.IsNullOrEmpty(permission))
                        {
                            group.Permissions = permission;
                        }
                    }
                }

                counter++;
            }
        }
    }
}
