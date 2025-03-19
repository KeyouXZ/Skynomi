using Skynomi.Utils;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Skynomi.RankSystem
{
    public class Ranks : Loader.ISkynomiExtension, Loader.ISkynomiExtensionReloadable
    {
        public string Name => "Rank System";
        public string Description => "Rank system extension for Skynomi";
        public Version Version => new Version(1, 1, 3);
        public string Author => "Keyou";

        private static RankSystem.Config rankConfig;
        public void Initialize()
        {
            rankConfig = RankSystem.Config.Read();
            RankSystem.Database.Initialize();
            ServerApi.Hooks.NetGreetPlayer.Register(Loader.GetPlugin(), OnPlayerJoin);
            
            RankSystem.Commands.Initialize();
            CreateGroup(1);
        }

        public void Reload(ReloadEventArgs args)
        {
            bool tempConf = rankConfig.useParent;
            rankConfig = RankSystem.Config.Read();
            RankSystem.Commands.Reload();
            CreateGroup(2);

            if (tempConf != rankConfig.useParent)
            {
                Skynomi.Utils.Log.Warn(RankSystem.Messages.ParentSettingChanged);
                rankConfig.useParent = tempConf;
            }
        }

        private void OnPlayerJoin(GreetPlayerEventArgs args)
        {
            RankSystem.Database.CreatePlayer(TShock.Players[args.Who].Name);
        }

        public static void CreateGroup(int status)
        {
            int counter = 1;
            if (rankConfig == null || rankConfig.Ranks == null)
            {
                Skynomi.Utils.Log.Error("Config for Ranks is null.");
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

                    // Update Group
                    string newPermissions = "";
                    if (parent == "")
                    {
                        var parentGroup = TShock.Groups.GetGroupByName("default");

                        string topermission = parentGroup.Permissions;
                        if (!string.IsNullOrEmpty(permission))
                        {
                            topermission += "," + permission;
                        }

                        newPermissions = topermission;
                    }
                    else if (rankConfig.useParent)
                    {
                        if (!string.IsNullOrEmpty(permission))
                        {
                            newPermissions = permission;
                        }
                    }

                    TShock.Groups.UpdateGroup(name, parent, TShock.Groups.GetGroupByName(parent != "" ? parent : "default").Permissions, chatColor, suffix, prefix);
                }

                counter++;
            }
        }
    }
}
