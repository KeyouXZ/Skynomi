using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using System.Timers;

namespace Skynomi.Database
{
    public class Database
    {
        private static object _connection;
        public static string _databaseType;
        private static Skynomi.Database.Config _config;
        private static bool isFallback = false;
        private static System.Timers.Timer _keepAliveTimer;

        public void InitializeDatabase()
        {
            _config = Skynomi.Database.Config.Read();
            _databaseType = _config.databaseType.ToLower();

            try
            {
                if (_databaseType == "mysql")
                {
                    TShock.Log.ConsoleInfo($"{Skynomi.Utils.Messages.Name} Connecting to MySQL database...");
                    string MysqlHost = _config.MySqlHost.Contains(":") ? _config.MySqlHost.Split(':')[0] : _config.MySqlHost;
                    string MysqlPort = _config.MySqlHost.Contains(":") ? _config.MySqlHost.Split(':')[1] : "3306";
                    string connectionString = $"Server={MysqlHost};Port={MysqlPort};Database={_config.MySqlDbName};User={_config.MySqlUsername};Password={_config.MySqlPassword};Pooling=true;Allow User Variables=true;Max Pool Size=100;MARS=True;";

                    _connection = new MySqlConnection(connectionString);
                    ((MySqlConnection)_connection).Open();
                    TShock.Log.ConsoleInfo($"{Skynomi.Utils.Messages.Name} Connected to MySQL database.");
                    StartKeepAliveTimer();
                }
                else
                {
                    InitializeSqlite();
                    _databaseType = "sqlite";
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
                if (_databaseType == "mysql")
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Accounts (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            Username VARCHAR(255) UNIQUE NOT NULL,
                            Balance DECIMAL(10, 2) NOT NULL DEFAULT 0
                        )";
                    // cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQueryAsync();

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
                        CREATE TABLE IF NOT EXISTS Accounts (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT UNIQUE NOT NULL,
                            Balance DECIMAL(10, 2) NOT NULL DEFAULT 0
                        )";
                    // cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQueryAsync();

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

        private void StartKeepAliveTimer()
        {
            _keepAliveTimer = new System.Timers.Timer(3000);
            _keepAliveTimer.Elapsed += KeepAlive;
            _keepAliveTimer.AutoReset = true;
            _keepAliveTimer.Enabled = true;
        }

        private void KeepAlive(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (_connection is MySqlConnection mySqlConnection && mySqlConnection.State == System.Data.ConnectionState.Open)
                {
                    using (var cmd = mySqlConnection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT 1";
                        cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleWarn($"{Skynomi.Utils.Messages.Name} KeepAlive failed: {ex.Message}");
                ((MySqlConnection)_connection).Close();

                TShock.Log.ConsoleInfo($"{Skynomi.Utils.Messages.Name} Reconnecting to MySQL database...");
                string MysqlHost = _config.MySqlHost.Contains(":") ? _config.MySqlHost.Split(':')[0] : _config.MySqlHost;
                string MysqlPort = _config.MySqlHost.Contains(":") ? _config.MySqlHost.Split(':')[1] : "3306";
                string connectionString = $"Server={MysqlHost};Port={MysqlPort};Database={_config.MySqlDbName};User={_config.MySqlUsername};Password={_config.MySqlPassword};Pooling=true;";

                _connection = new MySqlConnection(connectionString);
                
                ((MySqlConnection)_connection).Open();
                TShock.Log.ConsoleInfo($"{Skynomi.Utils.Messages.Name} Connected to MySQL database.");
            }
        }

        public static void Close()
        {
            TShock.Log.ConsoleInfo($"{Skynomi.Utils.Messages.Name} Closing database connection...");
            if (_keepAliveTimer != null)
            {
                _keepAliveTimer.Stop();
                _keepAliveTimer.Dispose();
            }

            if (_databaseType == "mysql")
            {
                ((MySqlConnection)_connection).CloseAsync();
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

            if (isFallback)
            {
                TShock.Log.ConsoleInfo(Skynomi.Utils.Messages.FallBack);
                _databaseType = "sqlite";
            }

            if (TShock.Config.Settings.StorageType.ToLower() != _databaseType.ToLower())
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

        public void CreatePlayer(string username)
        {
            using (var cmd = CreateCommand())
            {
                // Create Account
                cmd.CommandText = "INSERT OR IGNORE INTO Accounts (Username, Balance) VALUES (@Username, 0)";
                if (_databaseType == "mysql")
                {
                    cmd.CommandText = "INSERT IGNORE INTO Accounts (Username, Balance) VALUES (@Username, 0)";
                }
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Username", username);
                // cmd.ExecuteNonQuery();
                cmd.ExecuteNonQueryAsync();

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
                    // cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQueryAsync();
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
                    VALUES (@Username, @Amount)";

                if (_databaseType == "mysql")
                {
                    cmd.CommandText += @"
                        ON DUPLICATE KEY UPDATE Balance = Balance + @Amount";
                }
                else if (_databaseType == "sqlite")
                {
                    cmd.CommandText += @"
                        ON CONFLICT(Username) DO UPDATE SET Balance = Balance + @Amount";
                }


                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Amount", amount);
                // cmd.ExecuteNonQuery();
                cmd.ExecuteNonQueryAsync();
            }
        }

        public void RemoveBalance(string username, decimal amount)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Accounts (Username, Balance)
                    VALUES (@Username, @Amount)";

                if (_databaseType == "mysql")
                {
                    cmd.CommandText += @"
                        ON DUPLICATE KEY UPDATE Balance = Balance - @Amount";
                }
                else if (_databaseType == "sqlite")
                {
                    cmd.CommandText += @"
                        ON CONFLICT(Username) DO UPDATE SET Balance = Balance - @Amount";
                }

                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Amount", amount);
                // cmd.ExecuteNonQuery();
                cmd.ExecuteNonQueryAsync();
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
                    // cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQueryAsync();
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

        public void CustomVoid(string query, object? param = null)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = query;
                AddParameters(cmd, param);
                // cmd.ExecuteNonQuery();
                cmd.ExecuteNonQueryAsync();
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
