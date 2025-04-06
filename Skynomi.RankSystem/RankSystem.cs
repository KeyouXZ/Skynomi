using Skynomi.Database;
using Skynomi.Utils;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace Skynomi.RankSystem
{
    public class Ranks : Loader.ISkynomiExtension, Loader.ISkynomiExtensionReloadable
    {
        public string Name => "Rank System";
        public string Description => "Rank system extension for Skynomi";
        public Version Version => new(1, 2, 0);
        public string Author => "Keyou";

        private static Config rankConfig;
        public void Initialize()
        {
            rankConfig = Config.Read();
            Database.Initialize();
            ServerApi.Hooks.NetGreetPlayer.Register(Loader.GetPlugin(), OnPlayerJoin);
            ServerApi.Hooks.GameUpdate.Register(Loader.GetPlugin(), OnGameUpdate);
            GetDataHandlers.PlayerUpdate += OnPlayerUpdate;

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

        private DateTime LastTimelyRun = DateTime.UtcNow;
        private void OnSecondlyUpdate(EventArgs args)
        {
            var cache = CacheManager.Cache.GetCache<Database.TRank>("Ranks");

            foreach (TSPlayer player in TShock.Players)
            {
                if (player == null || !player.Active)
                {
                    continue;
                }

                player.IsDisabledForBannedWearable = false;

                if (!cache.TryGetValue(player.Name, out var rankIndex)) return;
                if (rankIndex.Rank == 0 ||rankIndex.Rank > cache.GetAllKeys().Length) return;
                string rankName = Commands.GetRankByIndex(rankIndex.Rank - 1);

                if (rankConfig.Ranks.TryGetValue(rankName, out var rank))
                {
                    if (rank.RestrictedItems.Contains(player.TPlayer.inventory[player.TPlayer.selectedItem].type))
                    {
                        string itemName = player.TPlayer.inventory[player.TPlayer.selectedItem].Name;
                        player.Disable($"holding restricted item: {itemName}");
                        player.SendErrorMessage($"You can't use {itemName} at your rank ({rankName})!");
                    }

                    if (!Main.ServerSideCharacter || (Main.ServerSideCharacter && player.IsLoggedIn))
                    {
                        foreach (Item item in player.TPlayer.armor)
                        {
                            if (rank.RestrictedItems.Contains(item.type))
                            {
                                player.SetBuff(BuffID.Frozen, 330, true);
                                player.SetBuff(BuffID.Stoned, 330, true);
                                player.SetBuff(BuffID.Webbed, 330, true);
                                player.IsDisabledForBannedWearable = true;

                                player.SendErrorMessage($"You can't use {item.Name} at your rank ({rankName})!");
                            }
                        }

                        // Dye ban checks
                        foreach (Item item in player.TPlayer.dye)
                        {
                            if (rank.RestrictedItems.Contains(item.type))
                            {
                                player.SetBuff(BuffID.Frozen, 330, true);
                                player.SetBuff(BuffID.Stoned, 330, true);
                                player.SetBuff(BuffID.Webbed, 330, true);
                                player.IsDisabledForBannedWearable = true;

                                player.SendErrorMessage($"You can't use {item.Name} at your rank ({rankName})!");
                            }
                        }

                        // Misc equip ban checks
                        foreach (Item item in player.TPlayer.miscEquips)
                        {
                            if (rank.RestrictedItems.Contains(item.type))
                            {
                                player.SetBuff(BuffID.Frozen, 330, true);
                                player.SetBuff(BuffID.Stoned, 330, true);
                                player.SetBuff(BuffID.Webbed, 330, true);
                                player.IsDisabledForBannedWearable = true;
                                player.SendErrorMessage($"You can't use {item.Name} at your rank ({rankName})!");
                            }
                        }

                        // Misc dye ban checks
                        foreach (Item item in player.TPlayer.miscDyes)
                        {
                            if (rank.RestrictedItems.Contains(item.type))
                            {
                                player.SetBuff(BuffID.Frozen, 330, true);
                                player.SetBuff(BuffID.Stoned, 330, true);
                                player.SetBuff(BuffID.Webbed, 330, true);
                                player.IsDisabledForBannedWearable = true;

                                player.SendErrorMessage($"You can't use {item.Name} at your rank ({rankName})!");
                            }
                        }
                    }
                }
            }

            LastTimelyRun = DateTime.UtcNow;
        }

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
        {
            TSPlayer player = args.Player;
            string itemName = player.TPlayer.inventory[args.SelectedItem].Name;

            var cache = CacheManager.Cache.GetCache<Database.TRank>("Ranks");

            if (!cache.TryGetValue(player.Name, out var rankIndex)) return;
            if (rankIndex.Rank == 0 || rankIndex.Rank > cache.GetAllKeys().Length) return;
            string rankName = Commands.GetRankByIndex(rankIndex.Rank - 1);

            if (rankConfig.Ranks.TryGetValue(rankName, out var rank))
            {
                if (rank.RestrictedItems.Contains(player.TPlayer.inventory[args.SelectedItem].type))
                {
                    player.TPlayer.controlUseItem = false;
                    player.Disable($"holding restricted item: {itemName}");

                    player.SendErrorMessage($"You can't use {itemName} at your rank ({rankName})!");

                    player.TPlayer.Update(player.TPlayer.whoAmI);
                    NetMessage.SendData((int)PacketTypes.PlayerUpdate, -1, player.Index, NetworkText.Empty, player.Index);

                    args.Handled = true;
                    return;
                }
            }

            args.Handled = false;
            return;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - LastTimelyRun).TotalSeconds >= 1)
            {
                OnSecondlyUpdate(args);
            }
        }
    }
}
