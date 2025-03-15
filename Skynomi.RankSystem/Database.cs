namespace Skynomi.RankSystem
{
    public class Database
    {
        private static Skynomi.Database.Database db;
        private static string _databaseType = Skynomi.Database.Database._databaseType;
        public static void Initialize()
        {
            db = new Skynomi.Database.Database();

            using (var cmd = db.CreateCommand())
            {
                if (_databaseType == "mysql")
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Ranks (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            Username VARCHAR(255) UNIQUE NOT NULL,
                            Rank INT NOT NULL DEFAULT 0,
                            HighestRank INT NOT NULL DEFAULT 0
                        )";
                    // cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQueryAsync();
                }
                else if (_databaseType == "sqlite")
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Ranks (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT UNIQUE NOT NULL,
                            Rank INTEGER NOT NULL DEFAULT 0,
                            HighestRank INTEGER NOT NULL DEFAULT 0
                        )";
                    // cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQueryAsync();
                }
            }

            RankInitialize();
            HighestRankInitialize();
        }

        public static void CreatePlayer(string username)
        {
            using (var cmd = db.CreateCommand())
            {
                // Create Rank
                cmd.CommandText = "INSERT OR IGNORE INTO Ranks (Username, Rank) VALUES (@Username, 0)";
                if (_databaseType == "mysql")
                {
                    cmd.CommandText = "INSERT IGNORE INTO Ranks (Username, Rank) VALUES (@Username, 0)";
                }
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.ExecuteNonQuery();
            }
            
            if (!Skynomi.Database.CacheManager.Cache["Ranks"].TryGetValue(username, out _))
            {
                Skynomi.Database.CacheManager.Cache["Ranks"].SetValue(username, 0);
            }
        }

        private static void RankInitialize()
        {
            var Rankcache = Skynomi.Database.CacheManager.Cache["Ranks"];
            Rankcache.MysqlQuery = "SELECT Username, Rank FROM Ranks";
            Rankcache.SqliteQuery = Rankcache.MysqlQuery;
            Rankcache.SaveMysqlQuery = "INSERT INTO Ranks (Username, Rank) VALUES (@Param1, @Param2) ON DUPLICATE KEY UPDATE Rank = @Param2";
            Rankcache.SaveSqliteQuery = "INSERT INTO Ranks (Username, Rank) VALUES (@Param1, @Param2) ON CONFLICT(Username) DO UPDATE SET Rank = @Param2";

            Rankcache.Init();
        }

        private static void HighestRankInitialize()
        {
            var Rankcache = Skynomi.Database.CacheManager.Cache["HighestRanks"];
            Rankcache.MysqlQuery = "SELECT Username, HighestRank FROM Ranks";
            Rankcache.SqliteQuery = Rankcache.MysqlQuery;
            Rankcache.SaveMysqlQuery = "INSERT INTO Ranks (Username, HighestRank) VALUES (@Param1, @Param2) ON DUPLICATE KEY UPDATE HighestRank = @Param2";
            Rankcache.SaveSqliteQuery = "INSERT INTO Ranks (Username, HighestRank) VALUES (@Param1, @Param2) ON CONFLICT(Username) DO UPDATE SET HighestRank = @Param2";

            Rankcache.Init();
        }

        public int GetRank(string username)
        {
            var rankValue = Skynomi.Database.CacheManager.Cache["Ranks"].GetValue(username);
            return rankValue != null ? Convert.ToInt32(rankValue) : 0;
        }

        public void UpdateRank(string username, int rank)
        {
            Skynomi.Database.CacheManager.Cache["Ranks"].SetValue(username, rank);
        }
    }
}