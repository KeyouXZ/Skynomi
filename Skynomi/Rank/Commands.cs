using Microsoft.Xna.Framework;
using Skynomi.Modules;
using TShockAPI;

namespace Skynomi.Rank;

public abstract class Commands
{
    public static void Initialize()
    {
        // Init Commands
        var cmds = new[]
        {
            new
            {
                Perm = Permissions.RankUp,
                Handler = (CommandDelegate)RankUp,
                AllowServer = false,
                Help =
                    "Rank up to the next level",
                Names = new[] { "rankup", "levelup" }
            },
            new
            {
                Perm = Permissions.RankDown,
                Handler = (CommandDelegate)RankDown,
                AllowServer = false,
                Help =
                    "Rank down to the previous level",
                Names = new[] { "rankdown", "leveldown" }
            },
            new
            {
                Perm = Permissions.RankInfo,
                Handler = (CommandDelegate)RankInfo,
                AllowServer = true,
                Help =
                    "Get information about a rank",
                Names = new[] { "rankinfo" }
            },
            new
            {
                Perm = Permissions.RankList,
                Handler = (CommandDelegate)RankList,
                AllowServer = true,
                Help =
                    "List all available ranks\"",
                Names = new[] { "ranklist" }
            },
            new
            {
                Perm = Permissions.RankReset,
                Handler = (CommandDelegate)RankReset,
                AllowServer = true,
                Help = "Reset the rank of all players",
                Names = new[] { "rankreset" }
            },
            new
            {
                Perm = Permissions.HighestRankReset,
                Handler = (CommandDelegate)HighestRankReset,
                AllowServer = true,
                Help = "Reset the highest rank of all players",
                Names = new[] { "highestrankreset" }
            }
        };

        foreach (var c in cmds)
            TShockAPI.Commands.ChatCommands.Add(
                new Command(c.Perm, c.Handler, c.Names)
                {
                    AllowServer = c.AllowServer,
                    HelpText = c.Help
                });
    }

    private static void RankUp(CommandArgs args)
    {
        var rankModule = ModuleManager.Get<Rank>();
        var economyModule = ModuleManager.Get<Economy.EconomyModule>();
        var utilsModule = ModuleManager.Get<Skynomi.Utils.UtilsModule>();

        var player = args.Player;
        var regex = Rank.RankRegex();
        var match = regex.Match(player.Account.Group);

        // not in rank check
        if (!player.Account.Group.Equals(TShock.Config.Settings.DefaultRegistrationGroupName) || !match.Success)
        {
            args.Player.SendErrorMessage("You are not in a rank group!");
            return;
        }

        var rankData = rankModule.Db.GetRankData(player.Account.Name);
        if (rankData is null)
        {
            args.Player.SendErrorMessage("You do not have rank data!");
            return;
        }

        var rank = match.Success ? int.Parse(match.Groups[1].Value) : 0; // from 1

        if (rank >= rankModule.RankConfig.Ranks.Count)
        {
            args.Player.SendErrorMessage("You are already at the highest rank.");
            return;
        }

        string nextRank = Utils.GetRankByIndex(rank)!;
        var balance = economyModule.Db.GetWalletBalance(player.Account.Name);

        if (balance is null)
        {
            args.Player.SendErrorMessage("You do not have balance!");
            return;
        }

        var rankCost = rankModule.RankConfig.Ranks[nextRank].Cost;

        if (balance.Value < rankCost)
        {
            args.Player.SendErrorMessage(
                $"Your balance is not enough to level up. ({utilsModule.CurrencyFormat((int)(rankCost - balance.Value))} more)");
            return;
        }

        var highestRank = rankData.HighestRank;

        if ((rank + 1) > highestRank)
        {
            foreach (var item in rankModule.RankConfig.Ranks[nextRank].Rewards)
            {
                args.Player.GiveItem(Convert.ToInt32(item.Key), item.Value);
            }
        }

        rankModule.Db.UpdateRank(args.Player.Account.Name, e =>
        {
            e.Rank = rank + 1;
            e.HighestRank = (rank + 1) > highestRank ? rank + 1 : highestRank;
        });

        economyModule.Db.UpdateWalletBalance(args.Player.Account.Name, x => x.Balance -= rankCost);
        TShock.UserAccounts.SetUserGroup(TShock.UserAccounts.GetUserAccountByName(args.Player.Account.Name),
            "rank_" + (rank + 1));

        if (rankModule.RankConfig.AnnounceRankUp)
        {
            TSPlayer.All.SendInfoMessage($"{args.Player.Name} has ranked up to {nextRank}!");
        }
        else
        {
            args.Player.SendInfoMessage($"Your rank has been upgraded to {nextRank}.");
        }
    }

    private static void RankDown(CommandArgs args)
    {
        var rankModule = ModuleManager.Get<Rank>();
        var economyModule = ModuleManager.Get<Economy.EconomyModule>();
        var utilsModule = ModuleManager.Get<Skynomi.Utils.UtilsModule>();

        if (!rankModule.RankConfig.EnableRankDown)
        {
            args.Player.SendErrorMessage("Rank down is disabled.");
            return;
        }

        var player = args.Player;
        var regex = Rank.RankRegex();
        var match = regex.Match(player.Account.Group);

        // not in rank check
        if (!player.Account.Group.Equals(TShock.Config.Settings.DefaultRegistrationGroupName) || !match.Success)
        {
            args.Player.SendErrorMessage("You are not in a rank group!");
            return;
        }

        var rank = match.Success ? int.Parse(match.Groups[1].Value) : 0;

        int prevIndex = (rank - 2); // rank is +1 ahead of rankIndex, so we need to sub by 2 to get prev rankIndex
        if (prevIndex >= 0)
        {
            string nextRank = Utils.GetRankByIndex(prevIndex)!;

            long rankCost = rankModule.RankConfig.Ranks[Utils.GetRankByIndex(prevIndex)!].Cost;

            rankModule.Db.UpdateRank(player.Account.Name, x => x.Rank--);
            economyModule.Db.UpdateWalletBalance(player.Account.Name, x => x.Balance += rankCost);

            TShock.UserAccounts.SetUserGroup(TShock.UserAccounts.GetUserAccountByName(args.Player.Name),
                "rank_" + (rank - 1));
            args.Player.SendInfoMessage(
                $"Your rank has been downgraded to {nextRank} and get {utilsModule.CurrencyFormat(rankCost)}.");
        }
        else
        {
            args.Player.SendErrorMessage("You are already at the lowest rank.");
        }
    }

    private static void RankInfo(CommandArgs args)
    {
        var rankModule = ModuleManager.Get<Rank>();
        var utilsModule = ModuleManager.Get<Skynomi.Utils.UtilsModule>();

        const string infoUsage = "Usage: /rankinfo <rankname>";

        if (args.Parameters.Count < 1)
        {
            args.Player.SendErrorMessage(infoUsage);
            return;
        }

        var rank = args.Parameters[0];
        if (rankModule.RankConfig.Ranks.TryGetValue(rank, out var configRank))
        {
            var rankPrefix = configRank.Prefix;
            if (string.IsNullOrEmpty(rankPrefix))
            {
                rankPrefix = "-";
            }

            var rankSuffix = configRank.Suffix;
            if (string.IsNullOrEmpty(rankSuffix))
            {
                rankSuffix = "-";
            }

            var chatColor = configRank.ChatColor;
            var hex = $"{chatColor[0]:X2}{chatColor[1]:X2}{chatColor[2]:X2}";
            var formattedColor = $"[c/{hex}:Hello]";

            var rankCost = configRank.Cost;

            var rankPermission = configRank.Permission;
            if (string.IsNullOrEmpty(rankPermission))
            {
                rankPermission = "-";
            }

            var rankReward = "";
            if (configRank.Rewards.Count == 0)
            {
                rankReward = "-";
            }
            else
            {
                rankReward = configRank.Rewards.Aggregate(rankReward,
                    (current, item) => current + $"[i/s{item.Value}:{item.Key}] ");
            }

            var detail = $"[c/00FF00:=== Rank Details ===]\n" +
                         $"[c/0000FF:Name:] {rank}\n" +
                         $"[c/0000FF:Prefix:] {rankPrefix}\n" +
                         $"[c/0000FF:Suffix:] {rankSuffix}\n" +
                         $"[c/0000FF:Chat Color:] {formattedColor} ([c/{hex}:#{hex}])\n" +
                         $"[c/0000FF:Cost:] {utilsModule.CurrencyFormat(rankCost)}\n" +
                         $"[c/0000FF:Permission:] {rankPermission}\n" +
                         $"[c/0000FF:Reward:] {rankReward}";

            args.Player.SendMessage(detail, Color.White);
        }
        else
        {
            args.Player.SendErrorMessage($"Rank '{rank}' not found.");
        }
    }

    private static void RankList(CommandArgs args)
    {
        var rankModule = ModuleManager.Get<Rank>();

        if (rankModule.RankConfig.Ranks.Count == 0)
        {
            args.Player.SendErrorMessage("No ranks available.");
            return;
        }

        var text = "[c/00FF00:=== Rank List ===]";
        var counter = 1;
        foreach (var rank in rankModule.RankConfig.Ranks)
        {
            var chatColor = rank.Value.ChatColor;
            var hex = $"{chatColor[0]:X2}{chatColor[1]:X2}{chatColor[2]:X2}";

            text += $"\n{counter}. [c/{hex}:{rank.Key}]";
            counter++;
        }

        args.Player.SendMessage(text, Color.White);
    }

    private static void RankReset(CommandArgs args)
    {
        try
        {
            var rankModule = ModuleManager.Get<Rank>();
            var dbModule = ModuleManager.Get<Skynomi.Database.DatabaseModule>();

            const string usage = "Usage: /rankreset <player/all>";

            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage(usage);
                return;
            }

            var who = args.Parameters[0];

            if (who == "all")
            {
                var allRank = dbModule.Db.GetCollection<Database.RankData>().FindAll();
                foreach (var rank in allRank)
                {
                    rankModule.Db.UpdateRank(rank.Name, x => x.Rank = 1);
                    var plr = TShock.UserAccounts.GetUserAccountByName(rank.Name);

                    if (plr is not null && rank.Rank > 1)
                    {
                        TShock.UserAccounts.SetUserGroup(plr,
                            TShock.Config.Settings.DefaultRegistrationGroupName);
                    }
                }

                args.Player.SendSuccessMessage("All players rank has been reset.");
                return;
            }

            var player = TShock.UserAccounts.GetUserAccountByName(who);
            if (player != null)
            {
                var rankData = rankModule.Db.GetRankData(player.Name);
                if (rankData is null)
                {
                    args.Player.SendErrorMessage($"{who} doesn't have rank data!");
                    return;
                }

                if (rankData.Rank <= 1)
                {
                    args.Player.SendErrorMessage($"{who} is already at the lowest rank.");
                    return;
                }

                rankModule.Db.UpdateRank(player.Name, x => x.Rank = 1);


                TShock.UserAccounts.SetUserGroup(player,
                    TShock.Config.Settings.DefaultRegistrationGroupName);
                args.Player.SendSuccessMessage($"Rank of {who} has been reset.");
            }
            else
            {
                args.Player.SendErrorMessage($"{who} not found.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }

    private static void HighestRankReset(CommandArgs args)
    {
        try
        {
            var rankModule = ModuleManager.Get<Rank>();
            var dbModule = ModuleManager.Get<Skynomi.Database.DatabaseModule>();

            const string usage = "Usage: /highestrankreset <player/all>";

            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage(usage);
                return;
            }

            var who = args.Parameters[0];

            if (who == "all")
            {
                var allRank = dbModule.Db.GetCollection<Database.RankData>().FindAll();
                foreach (var rank in allRank)
                {
                    rankModule.Db.UpdateRank(rank.Name, e => e.HighestRank = 1);
                }

                args.Player.SendSuccessMessage("Highest rank has been reset.");
            }
            else
            {
                var player = TShock.UserAccounts.GetUserAccountByName(who);

                if (player is not null)
                {
                    rankModule.Db.UpdateRank(player.Name, x => x.HighestRank = 1);
                    args.Player.SendSuccessMessage($"Highest rank of {who} has been reset.");
                }
                else
                {
                    args.Player.SendErrorMessage($"{who} not found.");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }
}