using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Skynomi.Rank;

public partial class Rank : Modules.IModule, Modules.IReloadable, Modules.IDisposable, Modules.IDependent
{
    public string Name => "Rank";
    public string Description => "Rank System module";
    public Version Version => new(0, 1, 0);
    public string Author => "Keyou";

    public IReadOnlyList<Type> RequiredModules =>
    [
        typeof(Skynomi.Utils.UtilsModule), typeof(Skynomi.Database.DatabaseModule), typeof(Economy.EconomyModule)
    ];

    public Config RankConfig = null!;
    public Database Db = new();

    public void Initialize()
    {
        RankConfig = Config.Read();
        CreateGroup();

        ServerApi.Hooks.NetGreetPlayer.Register(SkynomiPlugin.Instance, GreetPlayer);
        ServerApi.Hooks.GameUpdate.Register(SkynomiPlugin.Instance, OnGameUpdate);
        GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
        
        Commands.Initialize();
    }

    public void Reload(ReloadEventArgs args)
    {
        var tempConf = RankConfig.UseParent;
        RankConfig = Config.Read();

        if (tempConf == RankConfig.UseParent) return;

        Log.Warn(Messages.ParentSettingChanged);
        RankConfig.UseParent = tempConf;

        CreateGroup(true);
    }

    public void Dispose()
    {
        ServerApi.Hooks.NetGreetPlayer.Deregister(SkynomiPlugin.Instance, GreetPlayer);
        ServerApi.Hooks.GameUpdate.Deregister(SkynomiPlugin.Instance, OnGameUpdate);
        GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
    }

    private void GreetPlayer(GreetPlayerEventArgs e)
    {
        // Only affect logged in player
        var player = TShock.Players[e.Who];
        if (!player.IsLoggedIn) return;

        // Create if not found
        if (Db.GetRankData(player.Account.Name) is null)
        {
            Db.CreateRankData(player.Account.Name);
        }

        var playerRank = Db.GetRankData(player.Account.Name);
        if (playerRank is null) return;

        var ranks = RankConfig.Ranks;

        var playerGroup = player.Group;
        var regex = RankRegex();
        var match = regex.Match(playerGroup.Name);

        if (!match.Success) return;
        if (playerRank.Rank >= ranks.Count)
        {
            Db.UpdateRank(player.Account.Name, x => { x.Rank = ranks.Count; });

            TShock.UserAccounts.SetUserGroup(player.Account, "rank_" + ranks.Count);

            var targetRank = Utils.GetRankByIndex(ranks.Count - 1);
            player.SendMessage($"Your rank has been set to {targetRank}!",
                Color.Orange);

            Log.Debug($"Set {player.Account.Name} to {targetRank}!");
            return;
        }

        if (!int.TryParse(match.Groups[1].Value, out var currentRank)) return;
        {
            if (playerRank.Rank == currentRank) return;
            string corrected;
            string message;

            if (playerRank.Rank == 0)
            {
                corrected = TShock.Config.Settings.DefaultRegistrationGroupName;
                var match2 = regex.Match(corrected);
                if (match2.Success && int.TryParse(match2.Groups[1].Value, out var defaultRank))
                {
                    Db.UpdateRank(player.Account.Name, x => { x.Rank = defaultRank; });

                    message = $"Your rank has been corrected to {Utils.GetRankByIndex(playerRank.Rank - 1)}.";
                }
                else
                {
                    message = "Your rank has been corrected to the default registration rank.";
                }
            }
            else
            {
                corrected = "rank_" + (playerRank.Rank - 1);
                message = $"Your rank has been corrected to {Utils.GetRankByIndex(playerRank.Rank - 1)}.";
            }

            TShock.UserAccounts.SetUserGroup(player.Account, corrected);
            player.SendMessage(message, Color.Orange);
            Log.Debug($"Corrected {player.Account.Name} rank!");
        }
    }

    [GeneratedRegex(@"rank_(\d+)")]
    internal static partial Regex RankRegex();

    private static void CreateGroup(bool inReload = false)
    {
        var rankModule = Modules.ModuleManager.Get<Rank>();
        int counter = 1;

        foreach (var key in rankModule.RankConfig.Ranks)
        {
            string name = "rank_" + counter;
            string prefix = key.Value.Prefix;
            string suffix = key.Value.Suffix;
            string parent = (counter == 1)
                ? TShock.Config.Settings.DefaultRegistrationGroupName
                : "rank_" + (counter - 1);

            if (!rankModule.RankConfig.UseParent) parent = "";

            var color = key.Value.ChatColor;
            if (color.Length != 3)
            {
                Log.Warn($"Rank '{name}' ({key.Key}) has invalid chat color. Defaulting to white.");
                color = [255, 255, 255];
            }

            string chatColor = $"{color[0]},{color[1]},{color[2]}";
            var parentGroup = TShock.Groups.GetGroupByName(parent);
            string parentPermissions = parentGroup != null ? parentGroup.Permissions : "";
            string permission = parent == ""
                ? key.Value.Permission.Replace(" ", ",")
                : parentPermissions + "," + key.Value.Permission.Replace(" ", ",");

            // Create the group
            if (inReload)
            {
                if (!TShock.Groups.GroupExists(name))
                {
                    TShock.Groups.AddGroup(name, parent, permission, "255,255,255");
                    Log.Info($"Group {name} does not exist, created.");
                }
            }
            else
            {
                if (TShock.Groups.GroupExists(name))
                {
                    TShock.Groups.DeleteGroup(name);
                }

                TShock.Groups.AddGroup(name, parent, permission, "255,255,255");
            }

            TShock.Groups.UpdateGroup(name, parent, permission, chatColor, suffix, prefix);

            counter++;
            
            Log.Debug($"New group: name={name} parent={parent}");
        }
    }

    private DateTime _lastTimelyRun = DateTime.UtcNow;

    private void OnSecondlyUpdate(EventArgs _)
    {
        foreach (var player in TShock.Players)
        {
            if (player is not { Active: true })
            {
                continue;
            }

            #region Restricted Items

            player.IsDisabledForBannedWearable = false;

            var rankData = Db.GetRankData(player.Account.Name);
            if (rankData is null) return;
            if (rankData.Rank == 0 || rankData.Rank > RankConfig.Ranks.Count) return;

            string rankName = Utils.GetRankByIndex(rankData.Rank - 1)!;

            if (!RankConfig.Ranks.TryGetValue(rankName, out var rank)) continue;

            if (rank.RestrictedItems.Contains(player.TPlayer.inventory[player.TPlayer.selectedItem].type))
            {
                var itemName = player.TPlayer.inventory[player.TPlayer.selectedItem].Name;
                player.Disable($"holding restricted item: {itemName}");
                player.SendErrorMessage($"You can't use {itemName} at your rank ({rankName})!");
            }

            if (Main.ServerSideCharacter && (!Main.ServerSideCharacter || !player.IsLoggedIn)) continue;

            foreach (var item in player.TPlayer.armor)
            {
                if (!rank.RestrictedItems.Contains(item.type)) continue;

                player.SetBuff(BuffID.Frozen, 330, true);
                player.SetBuff(BuffID.Stoned, 330, true);
                player.SetBuff(BuffID.Webbed, 330, true);
                player.IsDisabledForBannedWearable = true;

                player.SendErrorMessage($"You can't use {item.Name} at your rank ({rankName})!");
            }

            // Dye ban checks
            foreach (var item in player.TPlayer.dye)
            {
                if (!rank.RestrictedItems.Contains(item.type)) continue;

                player.SetBuff(BuffID.Frozen, 330, true);
                player.SetBuff(BuffID.Stoned, 330, true);
                player.SetBuff(BuffID.Webbed, 330, true);
                player.IsDisabledForBannedWearable = true;

                player.SendErrorMessage($"You can't use {item.Name} at your rank ({rankName})!");
            }

            // Misc equip ban checks
            foreach (var item in player.TPlayer.miscEquips)
            {
                if (!rank.RestrictedItems.Contains(item.type)) continue;

                player.SetBuff(BuffID.Frozen, 330, true);
                player.SetBuff(BuffID.Stoned, 330, true);
                player.SetBuff(BuffID.Webbed, 330, true);
                player.IsDisabledForBannedWearable = true;
                player.SendErrorMessage($"You can't use {item.Name} at your rank ({rankName})!");
            }

            // Misc dye ban checks
            foreach (var item in player.TPlayer.miscDyes)
            {
                if (!rank.RestrictedItems.Contains(item.type)) continue;

                player.SetBuff(BuffID.Frozen, 330, true);
                player.SetBuff(BuffID.Stoned, 330, true);
                player.SetBuff(BuffID.Webbed, 330, true);
                player.IsDisabledForBannedWearable = true;

                player.SendErrorMessage($"You can't use {item.Name} at your rank ({rankName})!");
            }

            #endregion
        }

        _lastTimelyRun = DateTime.UtcNow;
    }

    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs args)
    {
        var player = args.Player;
        var itemName = player.TPlayer.inventory[args.SelectedItem].Name;

        var rankData = Db.GetRankData(player.Account.Name);

        if (rankData is null) return;
        if (rankData.Rank == 0 || rankData.Rank > RankConfig.Ranks.Count) return;

        var rankName = Utils.GetRankByIndex(rankData.Rank - 1)!;

        if (RankConfig.Ranks.TryGetValue(rankName, out var rank))
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
    }

    private void OnGameUpdate(EventArgs args)
    {
        if ((DateTime.UtcNow - _lastTimelyRun).TotalSeconds >= 1)
        {
            OnSecondlyUpdate(args);
        }
    }
}