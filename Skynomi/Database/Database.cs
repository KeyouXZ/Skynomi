using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace Skynomi.Database
{
    public class Database
    {
        public static object _connection;
        public static string _databaseType;
        private static Skynomi.Database.Config _config;
        private static bool isFallback = false;

        public void InitializeDatabase()
        {
            _config = Skynomi.Database.Config.Read();
            _databaseType = _config.databaseType.ToLower();

            if (_databaseType == "mysql")
            {
                try
                {
                    TShock.Log.ConsoleInfo($"{Skynomi.Utils.Messages.Name} Connecting to MySQL database...");
                    string MysqlHost = _config.MySqlHost.Contains(":") ? _config.MySqlHost.Split(':')[0] : _config.MySqlHost;
                    string MysqlPort = _config.MySqlHost.Contains(":") ? _config.MySqlHost.Split(':')[1] : "3306";
                    string connectionString = $"Server={MysqlHost};Port={MysqlPort};Database={_config.MySqlDbName};User={_config.MySqlUsername};Password={_config.MySqlPassword};Connection Timeout=30;Default Command Timeout=60;Allow User Variables=true;";

                    _connection = new MySqlConnection(connectionString);
                    ((MySqlConnection)_connection).Open();
                    ((MySqlConnection)_connection).Close();
                    TShock.Log.ConsoleInfo($"{Skynomi.Utils.Messages.Name} Connected to MySQL database.");
                    CreateTables();
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to connect to MySQL database: {ex}");
                    if (_databaseType == "mysql")
                    {
                        isFallback = true;
                        _databaseType = "sqlite";
                        InitializeSqlite();
                    }
                }
            }
            else
            {
                InitializeSqlite();
                _databaseType = "sqlite";
            }

            // Start AutoSave
            Skynomi.Database.CacheManager.StopAutoSave();
            Skynomi.Database.CacheManager.AutoSave(_config.autoSaveInterval);
            TShock.Log.ConsoleInfo($"{Skynomi.Utils.Messages.Name} AutoSave enabled.");
        }

        private void InitializeSqlite()
        {
            try
            {
                string dbPath = Path.Combine(TShock.SavePath, _config.databasePath);
                _connection = new SqliteConnection($"Data Source={dbPath}");
                CreateTables();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to connect to SQLite database: {ex}");
            }
        }

        private void CreateTables()
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = _databaseType == "mysql" ?
                @"
                CREATE TABLE IF NOT EXISTS Accounts (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Username VARCHAR(255) UNIQUE NOT NULL,
                    Balance BIGINT NOT NULL DEFAULT 0
                )" :
                @"
                CREATE TABLE IF NOT EXISTS Accounts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    Balance BIGINT NOT NULL DEFAULT 0
                )";

                cmd.ExecuteNonQuery();
            }
        }

        public static void Close()
        {
            if (_databaseType == "mysql")
            {
                ((MySqlConnection)_connection)?.Close();
            }
            else
            {
                ((SqliteConnection)_connection)?.Close();
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

        public System.Data.IDbCommand CreateCommand()
        {
            Close();
            if (_databaseType == "mysql")
            {
                try
                {
                    ((MySqlConnection)_connection).Open();
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to connect to database. Is it running?");
                    TShock.Log.ConsoleError(ex.ToString());
                    throw new InvalidOperationException($"{Skynomi.Utils.Messages.Name} Fatal error occurred. Unable to proceed with the database operation.");
                }
                return ((MySqlConnection)_connection).CreateCommand();
            }
            else
            {
                ((SqliteConnection)_connection).Open();
                return ((SqliteConnection)_connection).CreateCommand();
            }
        }

        public void CreatePlayer(string username)
        {
            if (!CacheManager.Cache.GetCache<long>("Balance").TryGetValue(username, out _))
            {
                CacheManager.Cache.GetCache<long>("Balance").Update(username, 0);
            }
        }

        public void BalanceInitialize()
        {
            var Balance = CacheManager.Cache.GetCache<long>("Balance");
            Balance.MysqlQuery = "SELECT Username AS 'Key', Balance AS 'Value' FROM Accounts";
            Balance.SqliteQuery = Balance.MysqlQuery;
            Balance.SaveMysqlQuery = "INSERT INTO Accounts (Username, Balance) VALUES (@key, @value) ON DUPLICATE KEY UPDATE Balance = @value";
            Balance.SaveSqliteQuery = @"INSERT INTO Accounts (Username, Balance) VALUES (@key, @value) ON CONFLICT(Username) DO UPDATE SET Balance = @value";
            Balance.Init();
        }

        public long GetBalance(string username)
        {
            return CacheManager.Cache.GetCache<long>("Balance").GetValue(username);
        }

        public void AddBalance(string username, long amount)
        {
            long balance = GetBalance(username);
            CacheManager.Cache.GetCache<long>("Balance").Update(username, balance + amount);
        }

        public void RemoveBalance(string username, long amount)
        {
            long balance = GetBalance(username);
            CacheManager.Cache.GetCache<long>("Balance").Update(username, balance - amount);
        }

        public dynamic CustomVoid(string query, object? param = null, bool output = false)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = query;
                if (param != null)
                {
                    var properties = param.GetType().GetProperties();
                    foreach (var property in properties)
                    {
                        cmd.AddParameter($"@{property.Name}", property.GetValue(param));
                    }
                }

                if (output)
                {
                    var resultList = new List<Dictionary<string, object>>();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
#pragma warning disable CS8601 // Possible null reference assignment.
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
#pragma warning restore CS8601 // Possible null reference assignment.
                            }

                            resultList.Add(row);
                        }
                    }

                    return resultList;
                }
                else
                {
                    cmd.ExecuteNonQuery();
                    return new List<Dictionary<string, object>>();
                }
            }
        }
    }
}