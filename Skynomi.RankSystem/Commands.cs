using Microsoft.Xna.Framework;
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
                AllowServer = false,
                HelpText = "Rank commands:\nup - Rank up to the next level\ndown - Rank down to the previous level"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command(Permissions.RankList, RankUtils, "rankutils")
            {
                AllowServer = true,
                HelpText = "Rank Utils commands:\ninfo <rank> - Get information about a rank\nlist - List all available ranks"
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
                string usage = "Usage: /rank <up/down>";

                if (args.Parameters.Count == 0)
                {
                    args.Player.SendErrorMessage(usage);
                    return;
                }

                if (args.Parameters[0] == "up")
                {
                    if (!Utils.Util.CheckPermission(Permissions.RankUp, args)) return;

                    // rank index start at 1
                    int rank = rankDatabase.GetRank(args.Player.Name);
                    
                    Skynomi.Database.CacheManager.CacheEntry<Database.TRank> rankCache = Skynomi.Database.CacheManager.Cache.GetCache<Database.TRank>("Ranks");

                    // nextindex start at 0
                    int nextIndex = rank;
                    if (nextIndex < rankConfig.Ranks.Count)
                    {
                        string nextRank = GetRankByIndex(nextIndex);

                        // Check user balance
                        long balance = database.GetBalance(args.Player.Name);
                        int rankCost = rankConfig.Ranks[nextRank].Cost;

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
                        rankCache.Modify(args.Player.Name, e => {
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
                    int rank = rankDatabase.GetRank(args.Player.Name);

                    // start at 0
                    int nextIndex = (rank - 2);
                    if (nextIndex >= 0)
                    {
                        string nextRank = GetRankByIndex(nextIndex);

                        int rankCost = rankConfig.Ranks[GetRankByIndex(nextIndex)].Cost;

                        database.AddBalance(args.Player.Name, rankCost);
                        Skynomi.Database.CacheManager.Cache.GetCache<Database.TRank>("Ranks").Modify(args.Player.Name, e => {
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

        private static void RankUtils(CommandArgs args)
        {
            try
            {
                string usage = "Usage: /rankutils <info/list>";
                if (args.Parameters.Count == 0)
                {
                    args.Player.SendErrorMessage(usage);
                    return;
                }

                if (args.Parameters[0] == "info")
                {
                    if (!Utils.Util.CheckPermission(Permissions.RankInfo, args)) return;
                    string infoUsage = "Usage: /rankutils info <rank>";

                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendErrorMessage(infoUsage);
                        return;
                    }

                    string rank = args.Parameters[1];
                    if (rankConfig.Ranks.ContainsKey(rank))
                    {
                        string rankPrefix = rankConfig.Ranks[rank].Prefix;
                        string rankSuffix = rankConfig.Ranks[rank].Suffix;
                        int[] ChatColor = rankConfig.Ranks[rank].ChatColor;
                        string hex = $"{ChatColor[0]:X2}{ChatColor[1]:X2}{ChatColor[2]:X2}";
                        string formattedColor = $"[c/{hex}:Hello]";
                        int rankCost = rankConfig.Ranks[rank].Cost;
                        string rankPermission = rankConfig.Ranks[rank].Permission;
                        string rankReward = "";
                        foreach (var item in rankConfig.Ranks[rank].Rewards)
                        {
                            rankReward += $"[i/s{item.Value}:{item.Key}] ";
                        }

                        string detail = $"[c/00FF00:=== Rank Details ===]\n" +
                            $"[c/0000FF:Name:] {rank}\n" +
                            $"[c/0000FF:Prefix:] {rankPrefix}\n" +
                            $"[c/0000FF:Suffix:] {rankSuffix}\n" +
                            $"[c/0000FF:Chat Color:] {formattedColor} ([c/{hex}:#{hex}])\n" +
                            $"[c/0000FF:Cost:] {Utils.Util.CurrencyFormat(rankCost)}\n" +
                            $"[c/0000FF:Permissioon:] {rankPermission}\n" +
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

                    string text = "Rank List:";
                    args.Player.SendSuccessMessage(text);
                    text = "";
                    foreach (var rank in rankConfig.Ranks)
                    {
                        text += $"\"{rank.Key}\" ";
                    }
                    args.Player.SendInfoMessage(text);
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