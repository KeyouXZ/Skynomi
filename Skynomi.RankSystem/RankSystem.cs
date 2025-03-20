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

        private static Config rankConfig;
        public void Initialize()
        {
            rankConfig = Config.Read();
            Database.Initialize();
            ServerApi.Hooks.NetGreetPlayer.Register(Loader.GetPlugin(), OnPlayerJoin);
            
            Commands.Initialize();
            CreateGroup(1);
        }

        public void Reload(ReloadEventArgs args)
        {
            bool tempConf = rankConfig.useParent;
            rankConfig = Config.Read();
            Commands.Reload();
            CreateGroup(2);

            if (tempConf != rankConfig.useParent)
            {
                Log.Warn(Messages.ParentSettingChanged);
                rankConfig.useParent = tempConf;
            }
        }

        private void OnPlayerJoin(GreetPlayerEventArgs args)
        {
            Database.CreatePlayer(TShock.Players[args.Who].Name);
        }

        private static void CreateGroup(int status)
        {
            int counter = 1;
            if (rankConfig == null || rankConfig.Ranks == null)
            {
                Log.Error("Config for Ranks is null.");
                return;
            }

            foreach (var key in rankConfig.Ranks)
            {
                string name = "rank_" + counter;
                string prefix = key.Value.Prefix;
                string suffix = key.Value.Suffix;
                string parent = (counter == 1) ? "default" : "rank_" + (counter - 1);
                if (!rankConfig.useParent) parent = "";

                int[] color = key.Value.ChatColor;
                string chatColor = $"{color[0]},{color[1]},{color[2]}";
                string permission = parent == "" ? key.Value.Permission.Replace(" ", ",") : TShock.Groups.GetGroupByName(parent).Permissions + "," + key.Value.Permission.Replace(" ", ",");

                // Create the group
                if (status == 1)
                {
                    if (TShock.Groups.GroupExists(name))
                    {
                        TShock.Groups.DeleteGroup(name);
                    }
                    TShock.Groups.AddGroup(name, parent, permission, "255,255,255");
                }
                else
                {
                    if (!TShock.Groups.GroupExists(name))
                    {
                        TShock.Groups.AddGroup(name, parent, permission, "255,255,255");
                        Log.Info($"Group {name} does not exist, created.");
                    }
                }

                TShock.Groups.UpdateGroup(name, parent, permission, chatColor, suffix, prefix);

                counter++;
            }
        }
    }
}
