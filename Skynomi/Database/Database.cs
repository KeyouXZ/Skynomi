using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;

namespace Skynomi.Database
{
    public class Database
    {
        private static object _connection;
        private static string _databaseType;
        private static Skynomi.Database.Config _config;
        private static bool isFallback = false;

        public void InitializeDatabase()
        {
            _config = Skynomi.Database.Config.Read();
            _databaseType = _config.databaseType.ToLower();

            try
            {
                if (_databaseType == "mysql")
                {
                    string connectionString = $"Server={_config.MySqlHost};Database={_config.MySqlDbName};User={_config.MySqlUsername};Password={_config.MySqlPassword};";
                    _connection = new MySqlConnection(connectionString);
                    ((MySqlConnection)_connection).Open();
                }
                else
                {
                    InitializeSqlite();
                }

                CreateTables();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to connect to MySQL database: {ex.Message}");
                if (_databaseType == "mysql")
                {
                    isFallback = true;
                    _databaseType = "sqlite";
                    InitializeSqlite();
                }
            }
        }

        private void InitializeSqlite()
        {
            try
            {
                string dbPath = Path.Combine(TShock.SavePath, _config.databasePath);
                _connection = new SqliteConnection($"Data Source={dbPath}");
                ((SqliteConnection)_connection).Open();
                CreateTables();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to connect to SQLite database: {ex.Message}");
            }
        }

        private void CreateTables()
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Accounts (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Username VARCHAR(255) UNIQUE NOT NULL,
                        Balance DECIMAL(10, 2) NOT NULL DEFAULT 0
                    )";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Ranks (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Username VARCHAR(255) UNIQUE NOT NULL,
                        Rank INT NOT NULL DEFAULT 0,
                        HighestRank INT NOT NULL DEFAULT 0
                    )";
                cmd.ExecuteNonQuery();
            }
        }

        public static void Close()
        {
            if (_databaseType == "mysql")
            {
                ((MySqlConnection)_connection).Close();
            }
            else
            {
                ((SqliteConnection)_connection).Close();
            }
        }

        public static void PostInitialize()
        {
            if (_databaseType != "sqlite" && _databaseType != "mysql")
            {
                TShock.Log.ConsoleWarn(Skynomi.Utils.Messages.UnsupportedDatabaseType);
            }
            else if (isFallback)
            {
                TShock.Log.ConsoleInfo(Skynomi.Utils.Messages.FallBack);

            }
            else if (TShock.Config.Settings.StorageType.ToLower() != _databaseType.ToLower())
            {
                TShock.Log.ConsoleWarn(Skynomi.Utils.Messages.DifferenctDatabaseType);
            }
        }

        private dynamic CreateCommand()
        {
            if (_databaseType == "mysql")
            {
                return ((MySqlConnection)_connection).CreateCommand();
            }
            else
            {
                return ((SqliteConnection)_connection).CreateCommand();
            }
        }

        public decimal GetBalance(string username)
        {
            using (var cmd = CreateCommand())
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
                    cmd.CommandText = "INSERT INTO Accounts (Username, Balance) VALUES (@Username, 0)";
                    cmd.ExecuteNonQuery();
                    return 0;
                }
            }
        }

        public void AddBalance(string username, int amount)
        {
            using (var cmd = CreateCommand())
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

        public void RemoveBalance(string username, decimal amount)
        {
            using (var cmd = CreateCommand())
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

        public int GetRank(string username)
        {
            using (var cmd = CreateCommand())
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
                    cmd.ExecuteNonQuery();
                    return 0;
                }
            }
        }

        public void UpdateRank(string username, int rank)
        {
            using (var cmd = CreateCommand())
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

        public void CustomVoid(string query, object? param = null)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = query;
                AddParameters(cmd, param);
                cmd.ExecuteNonQuery();
            }
        }

        public string CustomString(string query, object? param = null)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = query;
                AddParameters(cmd, param);

                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToString(result) ?? string.Empty : string.Empty;
            }
        }

        public decimal CustomDecimal(string query, object? param = null)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = query;
                AddParameters(cmd, param);

                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
        }

        private void AddParameters(dynamic cmd, object param)
        {
            if (param != null)
            {
                var properties = param.GetType().GetProperties();
                foreach (var property in properties)
                {
                    cmd.Parameters.AddWithValue($"@{property.Name}", property.GetValue(param));
                }
            }
        }
    }
}
