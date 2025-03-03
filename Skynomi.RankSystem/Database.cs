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
                // cmd.ExecuteNonQuery();
                cmd.ExecuteNonQueryAsync();

                return;
            }
        }

        public int GetRank(string username)
        {
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = "SELECT Rank FROM Ranks WHERE Username = @Username";
                cmd.Parameters.AddWithValue("@Username", username);

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    cmd.CommandText = "INSERT INTO Ranks (Username, Rank) VALUES (@Username, 0)";
                    // cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQueryAsync();
                    return 0;
                }
            }
        }

        public void UpdateRank(string username, int rank)
        {
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Ranks (Username, Rank)
                    VALUES (@Username, @Rank)";

                if (_databaseType == "mysql")
                {
                    cmd.CommandText += @"
                        ON DUPLICATE KEY UPDATE Rank = @Rank";
                }
                else if (_databaseType == "sqlite")
                {
                    cmd.CommandText += @"
                        ON CONFLICT(Username) DO UPDATE SET Rank = @Rank";
                }

                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Rank", rank);
                // cmd.ExecuteNonQuery();
                cmd.ExecuteNonQueryAsync();
            }
        }
    }
}