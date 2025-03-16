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
                    cmd.ExecuteNonQuery();
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
                    cmd.ExecuteNonQuery();
                }
            }

            RankInitialize();
        }

        public static void CreatePlayer(string username)
        {
            if (!Skynomi.Database.CacheManager.Cache.GetCache<TRank>("Ranks").TryGetValue(username, out _))
            {
                Skynomi.Database.CacheManager.Cache.GetCache<TRank>("Ranks").Update(username, new TRank { Rank = 0, HighestRank = 0 });
            }
        }

        public class TRank
        {
            public int Rank { get; set; }
            public int HighestRank { get; set; }
        }

        private static void RankInitialize()
        {
            var Rankcache = Skynomi.Database.CacheManager.Cache.GetCache<TRank>("Ranks");
            Rankcache.MysqlQuery = "SELECT Username AS 'Key', JSON_OBJECT('Rank', Rank, 'HighestRank', HighestRank) AS 'Value' FROM Ranks";
            Rankcache.SqliteQuery = Rankcache.MysqlQuery;
            Rankcache.SaveMysqlQuery = "INSERT INTO Ranks (Username, Rank, HighestRank) VALUES (@key, @value_Rank, @value_HighestRank) ON DUPLICATE KEY UPDATE Rank = @value_Rank, HighestRank = @value_HighestRank";
            Rankcache.SaveSqliteQuery = "INSERT INTO Ranks (Username, Rank, HighestRank) VALUES (@key, @value_Rank, @value_HighestRank) ON CONFLICT(Username) DO UPDATE SET Rank = @value_Rank, HighestRank = @value_HighestRank";

            Rankcache.Init();
        }

        public int GetRank(string username)
        {
            return Skynomi.Database.CacheManager.Cache.GetCache<TRank>("Ranks").GetValue(username).Rank;
        }

        public void UpdateRank(string username, int rank)
        {
            Skynomi.Database.CacheManager.Cache.GetCache<TRank>("Ranks").Modify(username, e =>
            {
                e.Rank = rank;
                return e;
            });
        }
    }
}