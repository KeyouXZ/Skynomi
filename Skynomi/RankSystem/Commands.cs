using Microsoft.Data.Sqlite;
using TShockAPI;

namespace Skynomi.RankSystem
{
    public class Commands
    {
        private static Skynomi.RankSystem.Config rankConfig;
        private static Skynomi.Config config;
        private static SqliteConnection _connection;

        public static void Initialize()
        {
            rankConfig = Skynomi.RankSystem.Config.Read();
            config = Skynomi.Config.Read();

            // Init Commands
            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.Rank, Rank, "rank", "level"));
        }

        public static void Reload()
        {
            rankConfig = Skynomi.RankSystem.Config.Read();
            config = Skynomi.Config.Read();
        }

        public static bool CheckIfLogin(CommandArgs args)
        {
            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage(Skynomi.Utils.Messages.NotLogged);
                return false;
            }
            return true;
        }

        public static bool CheckPermission(string perm, CommandArgs args)
        {
            if (!args.Player.HasPermission(perm))
            {
                args.Player.SendErrorMessage(Skynomi.Utils.Messages.PermissionError, TShockAPI.Commands.Specifier);
                return false;
            }

            return true;
        }

        public static void Rank(CommandArgs args)
        {
            try
            {
                if (!CheckIfLogin(args)) return;

                string usage = "Usage: /rank <up/down>";

                if (args.Parameters.Count == 0)
                {
                    args.Player.SendErrorMessage(usage);
                    return;
                }

                if (args.Parameters[0] == "up")
                {
                    string prefix = Skynomi.Database.GetLevel(args.Player.Name);

                    Skynomi.RankSystem.Config.Rank rankDetails = null;
                    if (rankConfig.Ranks.ContainsKey("prefix"))
                    {
                        rankDetails = rankConfig.Ranks[prefix];
                    }
                    else
                    {
                        rankDetails = null;
                    }

                    int index = GetRankIndex(prefix);

                    int nextIndex = index + 1;
                    if (nextIndex < rankConfig.Ranks.Count)
                    {
                        string nextRank = GetRankByIndex(nextIndex);

                        // Check user balance
                        decimal balance = Skynomi.Database.GetBalance(args.Player.Name);
                        int rankCost = rankConfig.Ranks[nextRank].Cost;

                        if (balance < rankCost)
                        {
                            args.Player.SendErrorMessage($"Your balance is not enough to level up. ({Skynomi.Utils.Util.CurrencyFormat(rankCost)} more)");
                            return;
                        }
                        // Give Player Rewards
                        int highestRank = Convert.ToInt32(Skynomi.Database.CustomString($@"
                            SELECT HighestRank FROM Ranks WHERE Username = @Username
                        ", new { Username = args.Player.Name }));

                        if (nextIndex > highestRank)
                        {
                            foreach (var item in rankConfig.Ranks[nextRank].Rewards)
                            {
                                args.Player.GiveItem(Convert.ToInt32(item.Key), item.Value);
                            }
                        }

                        // Set the Highest Level
                        Skynomi.Database.CustomVoid(@$"
                            INSERT INTO Ranks (Username, HighestRank)
                            Values (@Username, @HighestRank)
                            ON CONFLICT(Username) DO UPDATE SET HighestRank = @HighestRank
                        ", new
                        {
                            Username = args.Player.Name,
                            HighestRank = nextIndex

                        });
                        Skynomi.Database.RemoveBalance(args.Player.Name, rankCost);
                        Skynomi.Database.UpdateLevel(args.Player.Name, nextRank);
                        args.Player.SendInfoMessage($"Your rank has been upgraded to {nextRank}.");
                    }
                    else
                    {
                        args.Player.SendErrorMessage("You are already at the highest rank.");
                    }
                }
                else if (args.Parameters[0] == "down")
                {
                    string prefix = Skynomi.Database.GetLevel(args.Player.Name);

                    Skynomi.RankSystem.Config.Rank rankDetails = null;
                    if (rankConfig.Ranks.ContainsKey("prefix"))
                    {
                        rankDetails = rankConfig.Ranks[prefix];
                    }
                    else
                    {
                        rankDetails = null;
                    }

                    int index = GetRankIndex(prefix);

                    int nextIndex = index - 1;
                    if (nextIndex >= 0)
                    {
                        string nextRank = GetRankByIndex(nextIndex);
                        int rankCost = rankConfig.Ranks[GetRankByIndex(index)].Cost;

                        Skynomi.Database.AddBalance(args.Player.Name, rankCost);
                        Skynomi.Database.UpdateLevel(args.Player.Name, nextRank);
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