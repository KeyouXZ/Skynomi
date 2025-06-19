using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Skynomi.Database;
using TShockAPI;

namespace Skynomi.RankSystem
{
    public abstract class Commands
    {
        private static Config rankConfig;
        private static readonly Skynomi.Database.Database database = new();
        private static readonly Database rankDatabase = new();
        public static void Initialize()
        {
            rankConfig = Config.Read();
            Skynomi.Config.Read();

            // Init Commands
            TShockAPI.Commands.ChatCommands.Add(new Command(Permissions.Rank, Rank, "rank", "level")
            {
                AllowServer = true,
                HelpText = "Rank commands:\nup - Rank up to the next level\ndown - Rank down to the previous level\ninfo <rank name> - Get information about a rank\nlist - List all available ranks"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command(Permissions.ResetRank, ResetRank, "resetrank")
            {
                AllowServer = true,
                HelpText = "Reset the rank of all players"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command(Permissions.ResetRank, ResetHighestRank, "resethighestrank")
            {
                AllowServer = true,
                HelpText = "Reset the highest rank of all players"
            });
        }

        public static void Reload()
        {
            rankConfig = Config.Read();
            Skynomi.Config.Read();
        }

        private static void Rank(CommandArgs args)
        {
            try
            {
                string usage = "Usage: /rank <up/down/info/list>";

                if (args.Parameters.Count == 0)
                {
                    args.Player.SendErrorMessage(usage);
                    return;
                }

                // Check if the player is from rank group
                if ((args.Parameters[0] == "up" || args.Parameters[0] == "down") && !args.Player.Group.Name.StartsWith("rank_") && args.Player.Group.Name != TShock.Config.Settings.DefaultRegistrationGroupName)
                {
                    args.Player.SendErrorMessage("You are not in a rank group.");
                    return;
                }

                if (args.Parameters[0] == "up")
                {
                    if (!Utils.Util.CheckPermission(Permissions.RankUp, args)) return;

                    // rank index start at 1
                    var player = args.Player;
                    var regex = new Regex(@"rank_(\d+)");
                    var match = regex.Match(player!.Group.Name);

                    int rank = match.Success ? int.Parse(match.Groups[1].Value) : 0;

                    Skynomi.Database.CacheManager.CacheEntry<Database.TRank> rankCache = Skynomi.Database.CacheManager.Cache.GetCache<Database.TRank>("Ranks");

                    // nextindex start at 0
                    int nextIndex = rank;
                    if (nextIndex < rankConfig.Ranks.Count)
                    {
                        string nextRank = GetRankByIndex(nextIndex);

                        // Check user balance
                        long balance = database.GetBalance(args.Player.Name);
                        long rankCost = rankConfig.Ranks[nextRank].Cost;

                        if (balance < rankCost)
                        {
                            args.Player.SendErrorMessage($"Your balance is not enough to level up. ({Utils.Util.CurrencyFormat((int)(rankCost - balance))} more)");
                            return;
                        }

                        // Give Player Rewards
                        int highestRank = rankCache.GetValue(args.Player.Name).HighestRank;

                        if ((rank + 1) > highestRank)
                        {
                            foreach (var item in rankConfig.Ranks[nextRank].Rewards)
                            {
                                args.Player.GiveItem(Convert.ToInt32(item.Key), item.Value);
                            }
                        }

                        // Set the Highest Level
                        rankCache.Modify(args.Player.Name, e =>
                        {
                            e.Rank = rank + 1;
                            e.HighestRank = (rank + 1) > highestRank ? rank + 1 : highestRank;
                            return e;
                        });

                        database.RemoveBalance(args.Player.Name, rankCost);
                        TShock.UserAccounts.SetUserGroup(TShock.UserAccounts.GetUserAccountByName(args.Player.Name), "rank_" + (rank + 1));

                        if (rankConfig.announceRankUp)
                        {
                            TSPlayer.All.SendInfoMessage($"{args.Player.Name} has ranked up to {nextRank}!");
                        }
                        else
                        {
                            args.Player.SendInfoMessage($"Your rank has been upgraded to {nextRank}.");
                        }
                    }
                    else
                    {
                        args.Player.SendErrorMessage("You are already at the highest rank.");
                    }
                }
                else if (args.Parameters[0] == "down")
                {
                    if (!Utils.Util.CheckPermission(Permissions.RankDown, args)) return;

                    if (rankConfig.enableRankDown == false)
                    {
                        args.Player.SendErrorMessage("Rank down is disabled.");
                        return;
                    }

                    // start at 1
                    var player = args.Player;
                    var regex = new Regex(@"rank_(\d+)");
                    var match = regex.Match(player!.Group.Name);

                    int rank = match.Success ? int.Parse(match.Groups[1].Value) : 0;

                    // start at 0
                    int nextIndex = (rank - 2);
                    if (nextIndex >= 0)
                    {
                        string nextRank = GetRankByIndex(nextIndex);

                        long rankCost = rankConfig.Ranks[GetRankByIndex(nextIndex)].Cost;

                        database.AddBalance(args.Player.Name, rankCost);
                        Skynomi.Database.CacheManager.Cache.GetCache<Database.TRank>("Ranks").Modify(args.Player.Name, e =>
                        {
                            e.Rank = rank - 1;
                            return e;
                        });
                        TShock.UserAccounts.SetUserGroup(TShock.UserAccounts.GetUserAccountByName(args.Player.Name), "rank_" + (rank - 1));
                        args.Player.SendInfoMessage($"Your rank has been downgraded to {nextRank} and get {Utils.Util.CurrencyFormat(rankCost)}.");
                    }
                    else
                    {
                        args.Player.SendErrorMessage("You are already at the lowest rank.");
                    }
                }
                else if (args.Parameters[0] == "info")
                {
                    if (!Utils.Util.CheckPermission(Permissions.RankInfo, args)) return;
                    string infoUsage = "Usage: /rank info <rank name>";

                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendErrorMessage(infoUsage);
                        return;
                    }

                    string rank = args.Parameters[1];
                    if (rankConfig.Ranks.ContainsKey(rank))
                    {
                        string rankPrefix = rankConfig.Ranks[rank].Prefix;
                        if (string.IsNullOrEmpty(rankPrefix))
                        {
                            rankPrefix = "-";
                        }

                        string rankSuffix = rankConfig.Ranks[rank].Suffix;
                        if (string.IsNullOrEmpty(rankSuffix))
                        {
                            rankSuffix = "-";
                        }

                        int[] ChatColor = rankConfig.Ranks[rank].ChatColor;

                        string hex = $"{ChatColor[0]:X2}{ChatColor[1]:X2}{ChatColor[2]:X2}";

                        string formattedColor = $"[c/{hex}:Hello]";

                        long rankCost = rankConfig.Ranks[rank].Cost;

                        string rankPermission = rankConfig.Ranks[rank].Permission;
                        if (string.IsNullOrEmpty(rankPermission))
                        {
                            rankPermission = "-";
                        }

                        string rankReward = "";
                        if (rankConfig.Ranks[rank].Rewards.Count == 0)
                        {
                            rankReward = "-";
                        }
                        else
                        {
                            foreach (var item in rankConfig.Ranks[rank].Rewards)
                            {
                                rankReward += $"[i/s{item.Value}:{item.Key}] ";
                            }
                        }

                        string detail = $"[c/00FF00:=== Rank Details ===]\n" +
                            $"[c/0000FF:Name:] {rank}\n" +
                            $"[c/0000FF:Prefix:] {rankPrefix}\n" +
                            $"[c/0000FF:Suffix:] {rankSuffix}\n" +
                            $"[c/0000FF:Chat Color:] {formattedColor} ([c/{hex}:#{hex}])\n" +
                            $"[c/0000FF:Cost:] {Utils.Util.CurrencyFormat(rankCost)}\n" +
                            $"[c/0000FF:Permission:] {rankPermission}\n" +
                            $"[c/0000FF:Reward:] {rankReward}";

                        args.Player.SendMessage(detail, Color.White);
                    }
                    else
                    {
                        args.Player.SendErrorMessage($"Rank '{rank}' not found.");
                    }
                }
                else if (args.Parameters[0] == "list")
                {
                    if (!Utils.Util.CheckPermission(Permissions.RankList, args)) return;
                    if (rankConfig.Ranks.Count == 0)
                    {
                        args.Player.SendErrorMessage("No ranks available.");
                        return;
                    }

                    string text = "[c/00FF00:=== Rank List ===]";
                    int counter = 1;
                    foreach (var rank in rankConfig.Ranks)
                    {
                        int[] ChatColor = rank.Value.ChatColor;
                        string hex = $"{ChatColor[0]:X2}{ChatColor[1]:X2}{ChatColor[2]:X2}";

                        text += $"\n{counter}. [c/{hex}:{rank.Key}]";
                        counter++;
                    }
                    args.Player.SendMessage(text, Color.White);
                }
                else
                {
                    args.Player.SendErrorMessage(usage);
                }
            }
            catch (Exception ex)
            {
                Utils.Log.Error(ex.ToString());
            }

        }

        private static void ResetRank(CommandArgs args)
        {
            try
            {
                string usage = "Usage: /resetrank <player/all>";

                if (args.Parameters.Count < 1)
                {
                    args.Player.SendErrorMessage(usage);
                    return;
                }

                string who = args.Parameters[0];

                var cache = CacheManager.Cache.GetCache<Database.TRank>("Ranks");
                if (who == "all")
                {
                    foreach (var rank in cache.GetAllKeys())
                    {
                        cache.Modify(rank, e =>
                        {
                            if (e.Rank > 1)
                                e.Rank = 1;
                            return e;
                        });

                        var plr = TSPlayer.FindByNameOrID(rank).FirstOrDefault(x => x != null && x.Name.Equals(rank, StringComparison.OrdinalIgnoreCase));
                        if (plr != null && cache.GetValue(rank).Rank > 1)
                        {
                            TShock.UserAccounts.SetUserGroup(plr.Account, TShock.Config.Settings.DefaultRegistrationGroupName);
                        }
                    }
                    args.Player.SendSuccessMessage("All players rank has been reset.");
                    return;
                }

                var player = TSPlayer.FindByNameOrID(who).FirstOrDefault(x => x != null && x.Name.Equals(who, StringComparison.OrdinalIgnoreCase));
                if (player != null)
                {
                    if (cache.GetValue(player.Name).Rank <= 1)
                    {
                        args.Player.SendErrorMessage($"{who} is already at the lowest rank.");
                        return;
                    }

                    cache.Modify(who, e =>
                    {
                        if (e.Rank > 1)
                            e.Rank = 1;
                        return e;
                    });

                    if (player!.Account != null)
                        TShock.UserAccounts.SetUserGroup(player.Account, TShock.Config.Settings.DefaultRegistrationGroupName);
                    args.Player.SendSuccessMessage($"Rank of {who} has been reset.");
                }
                else
                {
                    args.Player.SendErrorMessage($"{who} not found.");

                }
            }
            catch (Exception ex)
            {
                Utils.Log.Error(ex.ToString());
            }
        }

        private static void ResetHighestRank(CommandArgs args)
        {
            try
            {
                string usage = "Usage: /resethighestrank <player/all>";

                if (args.Parameters.Count < 1)
                {
                    args.Player.SendErrorMessage(usage);
                    return;
                }

                string who = args.Parameters[0];

                var cache = CacheManager.Cache.GetCache<Database.TRank>("Ranks");
                if (who == "all")
                {
                    foreach (var rank in cache.GetAllKeys())
                    {
                        cache.Modify(rank, e =>
                        {
                            e.HighestRank = 0;
                            return e;
                        });
                    }
                    args.Player.SendSuccessMessage("Highest rank has been reset.");
                }
                else
                {
                    bool userExists = TSPlayer.FindByNameOrID(who)
                        .Any(x => x != null &&
                            (x.Name.Equals(who, StringComparison.OrdinalIgnoreCase) ||
                            (int.TryParse(who, out int id) && x.Index == id)));

                    if (userExists)
                    {
                        cache.Modify(who, e =>
                        {
                            e.HighestRank = 0;
                            return e;
                        });
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
                Utils.Log.Error(ex.ToString());
            }
        }

        public static string GetRankByIndex(int index)
        {
            var rankKeys = new List<string>(rankConfig.Ranks.Keys);

            if (index >= 0 && index < rankKeys.Count)
            {
                return rankKeys[index];
            }

            return null;
        }

        public static int GetRankIndex(string rankName)
        {
            var rankKeys = new List<string>(rankConfig.Ranks.Keys);
            return rankKeys.IndexOf(rankName);
        }
    }
}