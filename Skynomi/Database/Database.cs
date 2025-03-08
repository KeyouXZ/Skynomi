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

        public async Task InitializeDatabaseAsync()
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
                    string connectionString = $"Server={MysqlHost};Port={MysqlPort};Database={_config.MySqlDbName};User={_config.MySqlUsername};Password={_config.MySqlPassword};Pooling=true;Allow User Variables=true;Max Pool Size=100;";

                    _connection = new MySqlConnection(connectionString);
                    await ((MySqlConnection)_connection).OpenAsync();
                    TShock.Log.ConsoleInfo($"{Skynomi.Utils.Messages.Name} Connected to MySQL database.");
                }
                else
                {
                    await InitializeSqliteAsync();
                    _databaseType = "sqlite";
                }

                await CreateTablesAsync();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to connect to MySQL database: {ex.Message}");
                if (_databaseType == "mysql")
                {
                    isFallback = true;
                    _databaseType = "sqlite";
                    await InitializeSqliteAsync();
                }
            }
        }

        public void InitializeDatabase()
        {
            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeSqliteAsync()
        {
            try
            {
                string dbPath = Path.Combine(TShock.SavePath, _config.databasePath);
                _connection = new SqliteConnection($"Data Source={dbPath}");
                await ((SqliteConnection)_connection).OpenAsync();
                await CreateTablesAsync();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to connect to SQLite database: {ex.Message}");
            }
        }

        private void InitializeSqlite()
        {
            InitializeSqliteAsync().GetAwaiter().GetResult();
        }

        private async Task CreateTablesAsync()
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = _databaseType == "mysql" ?
                @"
                CREATE TABLE IF NOT EXISTS Accounts (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Username VARCHAR(255) UNIQUE NOT NULL,
                    Balance DECIMAL(10, 2) NOT NULL DEFAULT 0
                )" :
                @"
                CREATE TABLE IF NOT EXISTS Accounts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    Balance DECIMAL(10, 2) NOT NULL DEFAULT 0
                )";

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public static void Close()
        {
            if (_keepAliveTimer != null)
            {
                _keepAliveTimer.Stop();
                _keepAliveTimer.Dispose();
            }

            if (_databaseType == "mysql")
            {
                TShock.Log.ConsoleInfo($"{Skynomi.Utils.Messages.Name} Closing database connection...");
                ((MySqlConnection)_connection).CloseAsync();
            }
            else
            {
                ((SqliteConnection)_connection).Close();
            }
        }

        public static void PostInitialize()
        {
            if (_config.databaseType != "sqlite" && _config.databaseType != "mysql")
            {
                TShock.Log.ConsoleWarn(Skynomi.Utils.Messages.UnsupportedDatabaseType);
                isFallback = true;
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

        public dynamic CreateCommand()
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

        public async Task CreatePlayerAsync(string username)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "INSERT OR IGNORE INTO Accounts (Username, Balance) VALUES (@Username, 0)";
                if (_databaseType == "mysql")
                {
                    cmd.CommandText = "INSERT IGNORE INTO Accounts (Username, Balance) VALUES (@Username, 0)";
                }
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Username", username);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public void CreatePlayer(string username)
        {
            CreatePlayerAsync(username).GetAwaiter().GetResult();
        }

        public async Task<decimal> GetBalanceAsync(string username)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = "SELECT Balance FROM Accounts WHERE Username = @Username";
                cmd.Parameters.AddWithValue("@Username", username);

                object result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
                }
                else
                {
                    cmd.CommandText = "INSERT INTO Accounts (Username, Balance) VALUES (@Username, 0)";
                    await cmd.ExecuteNonQueryAsync();
                    return 0;
                }
            }
        }

        public decimal GetBalance(string username)
        {
            return GetBalanceAsync(username).GetAwaiter().GetResult();
        }

        public async Task AddBalanceAsync(string username, int amount)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = _databaseType == "mysql" ?
                    @"INSERT INTO Accounts (Username, Balance) VALUES (@Username, @Amount)
                      ON DUPLICATE KEY UPDATE Balance = Balance + @Amount" :
                    @"INSERT INTO Accounts (Username, Balance) VALUES (@Username, @Amount)
                      ON CONFLICT(Username) DO UPDATE SET Balance = Balance + @Amount";

                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Amount", amount);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public void AddBalance(string username, int amount)
        {
            AddBalanceAsync(username, amount).GetAwaiter().GetResult();
        }

        public async Task RemoveBalanceAsync(string username, decimal amount)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = _databaseType == "mysql" ?
                    @"INSERT INTO Accounts (Username, Balance) VALUES (@Username, @Amount)
                      ON DUPLICATE KEY UPDATE Balance = Balance - @Amount" :
                    @"INSERT INTO Accounts (Username, Balance) VALUES (@Username, @Amount)
                      ON CONFLICT(Username) DO UPDATE SET Balance = Balance - @Amount";
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Amount", amount);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public void RemoveBalance(string username, decimal amount)
        {
            RemoveBalanceAsync(username, amount).GetAwaiter().GetResult();
        }

        public async Task<dynamic> CustomVoidAsync(string query, object? param = null, bool output = false)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = query;
                AddParameters(cmd, param);

                if (output)
                {
                    var resultList = new List<Dictionary<string, object>>();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }

                            resultList.Add(row);
                        }
                    }

                    return resultList;
                }
                else
                {
                    await cmd.ExecuteNonQueryAsync();
                    return new List<Dictionary<string, object>>();
                }
            }
        }

        public dynamic CustomVoid(string query, object? param = null, bool output = false)
        {
            return CustomVoidAsync(query, param, output).GetAwaiter().GetResult();
        }

        public async Task<string> CustomStringAsync(string query, object? param = null)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = query;
                AddParameters(cmd, param);

                object result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToString(result) ?? string.Empty : string.Empty;
            }
        }

        public string CustomString(string query, object? param = null)
        {
            return CustomStringAsync(query, param).GetAwaiter().GetResult();
        }

        public async Task<decimal> CustomDecimalAsync(string query, object? param = null)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = query;
                AddParameters(cmd, param);

                object result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
        }

        public decimal CustomDecimal(string query, object? param = null)
        {
            return CustomDecimalAsync(query, param).GetAwaiter().GetResult();
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