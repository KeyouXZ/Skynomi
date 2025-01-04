using Microsoft.Data.Sqlite;
using TShockAPI;

namespace Skynomi.RankSystem
{
    public class Commands
    {
        private static Skynomi.RankSystem.Config rankConfig;
        private static Skynomi.Config config;
        private static Skynomi.Database.Database database = new Database.Database();
        public static void Initialize()
        {
            rankConfig = Skynomi.RankSystem.Config.Read();
            config = Skynomi.Config.Read();

            // Init Commands
            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.Rank, Rank, "rank", "level")
            {
                AllowServer = false,
                HelpText = "Rank commands:\nup - Rank up to the next level\ndown - Rank down to the previous level"
            });
        }

        public static void Reload()
        {
            rankConfig = Skynomi.RankSystem.Config.Read();
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
                    if (!Skynomi.Utils.Util.CheckPermission(Skynomi.Utils.Permissions.RankUp, args)) return;

                    // rank index start at 1
                    int rank = database.GetRank(args.Player.Name);

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
                            args.Player.SendErrorMessage($"Your balance is not enough to level up. ({Skynomi.Utils.Util.CurrencyFormat((int)(rankCost-balance))} more)");
                            return;
                        }
                        // Give Player Rewards
                        int highestRank = Convert.ToInt32(database.CustomString($@"
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
                        string databaseCommandText = "INSERT INTO Ranks (Username, HighestRank) Values (@Username, @HighestRank) ";
                        if (Skynomi.Database.Database._databaseType == "mysql") {
                            databaseCommandText += "ON DUPLICATE KEY UPDATE HighestRank = @HighestRank";
                        } else if (Skynomi.Database.Database._databaseType == "sqlite") {
                            databaseCommandText += "ON CONFLICT(Username) DO UPDATE SET HighestRank = @HighestRank";
                        }
                        database.CustomVoid(databaseCommandText, new
                        {
                            Username = args.Player.Name,
                            HighestRank = (rank+1)

                        });
                        database.RemoveBalance(args.Player.Name, rankCost);
                        TShock.UserAccounts.SetUserGroup(TShock.UserAccounts.GetUserAccountByName(args.Player.Name), "rank_" + (rank+1));
                        database.UpdateRank(args.Player.Name, (rank+1));
                        args.Player.SendInfoMessage($"Your rank has been upgraded to {nextRank}.");
                    }
                    else
                    {
                        args.Player.SendErrorMessage("You are already at the highest rank.");
                    }
                }
                else if (args.Parameters[0] == "down")
                {
                    if (!Skynomi.Utils.Util.CheckPermission(Skynomi.Utils.Permissions.RankDown, args)) return;

                    // start at 1
                    int rank = database.GetRank(args.Player.Name);

                    // start at 0
                    int nextIndex = (rank - 2);
                    if (nextIndex >= 0)
                    {
                        string nextRank = GetRankByIndex(nextIndex);

                        int rankCost = rankConfig.Ranks[GetRankByIndex(nextIndex)].Cost;

                        database.AddBalance(args.Player.Name, rankCost);
                        database.UpdateRank(args.Player.Name, (rank-1));
                        TShock.UserAccounts.SetUserGroup(TShock.UserAccounts.GetUserAccountByName(args.Player.Name), "rank_" + (rank-1));
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