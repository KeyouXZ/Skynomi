using Microsoft.Data.Sqlite;
using TShockAPI;

namespace Skynomi {
    public class SkyDatabase {
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
    }
}