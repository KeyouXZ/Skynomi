using Microsoft.Data.Sqlite;
using TShockAPI;

namespace Skynomi {
    public class Database {
        private static SqliteConnection _connection;

        public SqliteConnection Connection => _connection;
        public static void InitializeDatabase()
        {
            string dbPath = Path.Combine(TShock.SavePath, "Skynomi.sqlite3");

            _connection = new SqliteConnection($"Data Source={dbPath}");
            _connection.Open();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Accounts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        Balance REAL NOT NULL DEFAULT 0
                    )";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Ranks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        Rank INTEGER NOT NULL DEFAULT 0,
                        HighestRank INTEGER NOT NULL DEFAULT 0
                    )
                ";
                cmd.ExecuteNonQuery();
            }
        }
        public static decimal GetBalance(string username)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Balance FROM Accounts WHERE Username = @Username";
                cmd.Parameters.AddWithValue("@Username", username);

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
                }
                else
                {
                    // Create new account if not exists
                    cmd.CommandText = "INSERT INTO Accounts (Username, Balance) VALUES (@Username, 0)";
                    cmd.ExecuteNonQuery();
                    return 0;
                }
            }
        }

        public static void AddBalance(string username, int amount)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Accounts (Username, Balance)
                    VALUES (@Username, @Amount)
                    ON CONFLICT(Username) DO UPDATE SET Balance = Balance + @Amount";
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.ExecuteNonQuery();
            }
        }

        public static void RemoveBalance(string username, decimal amount)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Accounts (Username, Balance)
                    VALUES (@Username, @Amount)
                    ON CONFLICT(Username) DO UPDATE SET Balance = Balance - @Amount";
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.ExecuteNonQuery();
            }
        }

        public static int GetRank(string username)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Rank FROM Ranks where Username = @Username
                ";
                cmd.Parameters.AddWithValue("@Username", username);

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    // Create new account if not exists
                    cmd.CommandText = "INSERT INTO Ranks (Username, Rank) VALUES (@Username, 0)";
                    cmd.ExecuteNonQuery();
                    return 0;
                }
            }
        }
        public static void UpdateRank(string username, int rank)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Ranks (Username, Rank)
                    VALUES (@Username, @Rank)
                    ON CONFLICT(Username) DO UPDATE SET Rank = @Rank";
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Rank", rank);
                cmd.ExecuteNonQuery();
            }
        }

        public static void CustomVoid(string query,object param = null)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;

                if (param != null) {
                    var prop = param.GetType().GetProperties();
                    foreach (var key in prop)
                    {
                        cmd.Parameters.AddWithValue($"@{key.Name}", key.GetValue(param));
                    }
                }
                cmd.ExecuteNonQuery();
            }
        }

        public static string CustomString(string query, object param = null)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;

                if (param != null)
                {
                    var prop = param.GetType().GetProperties();
                    foreach (var key in prop)
                    {
                        cmd.Parameters.AddWithValue($"@{key.Name}", key.GetValue(param));
                    }
                }

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToString(result);
                }
                else
                {
                    return null;
                }
            }
        }

        public static decimal CustomDecimal(string query)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}