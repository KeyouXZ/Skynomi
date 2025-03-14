using Microsoft.Xna.Framework;
using TShockAPI;

namespace Skynomi.RankSystem
{
    public class Commands
    {
        private static RankSystem.Config rankConfig;
        private static Skynomi.Config config;
        private static Skynomi.Database.Database database = new();
        private static RankSystem.Database rankDatabase = new();
        public static void Initialize()
        {
            rankConfig = RankSystem.Config.Read();
            config = Skynomi.Config.Read();

            // Init Commands
            TShockAPI.Commands.ChatCommands.Add(new Command(RankSystem.Permissions.Rank, Rank, "rank", "level")
            {
                AllowServer = false,
                HelpText = "Rank commands:\nup - Rank up to the next level\ndown - Rank down to the previous level"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command(RankSystem.Permissions.RankList, RankUtils, "rankutils")
            {
                AllowServer = true,
                HelpText = "Rank Utils commands:\ninfo <rank> - Get information about a rank\nlist - List all available ranks"
            });
        }

        public static void Reload()
        {
            rankConfig = RankSystem.Config.Read();
            config = Skynomi.Config.Read();
        }

        public static void Rank(CommandArgs args)
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
                    if (!Skynomi.Utils.Util.CheckPermission(RankSystem.Permissions.RankUp, args)) return;

                    // rank index start at 1
                    int rank = rankDatabase.GetRank(args.Player.Name);

                    // nextindex start at 0
                    int nextIndex = rank;
                    if (nextIndex < rankConfig.Ranks.Count)
                    {
                        string nextRank = GetRankByIndex(nextIndex);

                        // Check user balance
                        decimal balance = database.GetBalance(args.Player.Name);
                        int rankCost = rankConfig.Ranks[nextRank].Cost;

                        if (balance < rankCost)
                        {
                            args.Player.SendErrorMessage($"Your balance is not enough to level up. ({Skynomi.Utils.Util.CurrencyFormat((int)(rankCost - balance))} more)");
                            return;
                        }
                        // Give Player Rewards
                        int highestRank = Convert.ToInt32(database.CustomString($@"
                            SELECT HighestRank FROM Ranks WHERE Username = @Username
                        ", new { Username = args.Player.Name }));

                        if ((rank + 1) > highestRank)
                        {
                            foreach (var item in rankConfig.Ranks[nextRank].Rewards)
                            {
                                args.Player.GiveItem(Convert.ToInt32(item.Key), item.Value);
                            }
                        }

                        // Set the Highest Level
                        string databaseCommandText = "INSERT INTO Ranks (Username, HighestRank) Values (@Username, @HighestRank) ";
                        if (Skynomi.Database.Database._databaseType == "mysql")
                        {
                            databaseCommandText += "ON DUPLICATE KEY UPDATE HighestRank = @HighestRank";
                        }
                        else if (Skynomi.Database.Database._databaseType == "sqlite")
                        {
                            databaseCommandText += "ON CONFLICT(Username) DO UPDATE SET HighestRank = @HighestRank";
                        }
                        database.CustomVoid(databaseCommandText, new
                        {
                            Username = args.Player.Name,
                            HighestRank = (rank + 1)

                        });
                        database.RemoveBalance(args.Player.Name, rankCost);
                        TShock.UserAccounts.SetUserGroup(TShock.UserAccounts.GetUserAccountByName(args.Player.Name), "rank_" + (rank + 1));
                        rankDatabase.UpdateRank(args.Player.Name, (rank + 1));

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
                    if (!Skynomi.Utils.Util.CheckPermission(RankSystem.Permissions.RankDown, args)) return;

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
                        rankDatabase.UpdateRank(args.Player.Name, (rank - 1));
                        TShock.UserAccounts.SetUserGroup(TShock.UserAccounts.GetUserAccountByName(args.Player.Name), "rank_" + (rank - 1));
                        args.Player.SendInfoMessage($"Your rank has been downgraded to {nextRank} and get {Skynomi.Utils.Util.CurrencyFormat(rankCost)}.");
                    }
                    else
                    {
                        args.Player.SendErrorMessage("You are already at the lowest rank.");
                    }
                }
                else
                {
                    args.Player.SendErrorMessage(usage);
                    return;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
            }

        }

        public static void RankUtils(CommandArgs args)
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
                    if (!Skynomi.Utils.Util.CheckPermission(RankSystem.Permissions.RankInfo, args)) return;
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
                            $"[c/0000FF:Cost:] {Skynomi.Utils.Util.CurrencyFormat(rankCost)}\n" +
                            $"[c/0000FF:Permissioon:] {rankPermission}\n" +
                            $"[c/0000FF:Reward:] {rankReward}";

                        args.Player.SendMessage(detail, Color.White);
                    }
                    else
                    {
                        args.Player.SendErrorMessage($"Rank '{rank}' not found.");
                        return;
                    }
                }
                else if (args.Parameters[0] == "list")
                {
                    if (!Skynomi.Utils.Util.CheckPermission(RankSystem.Permissions.RankList, args)) return;

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
                    return;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
            }
        }

        public static string GetRankByIndex(int index)
        {
            var rankKeys = new List<string>(rankConfig.Ranks.Keys);

            if (index >= 0 && index < rankKeys.Count)
            {
                return rankKeys[index];
            }
            else
            {
                return null;
            }
        }

        public static int GetRankIndex(string rankName)
        {
            var rankKeys = new List<string>(rankConfig.Ranks.Keys);
            return rankKeys.IndexOf(rankName);
        }
    }
}