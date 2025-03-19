using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.DB;

namespace Skynomi.Database
{
    public static class CacheManager
    {
        private static Skynomi.Database.Database db = new();
        private static ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();
        private static ConcurrentDictionary<string, object> _lastCache = new ConcurrentDictionary<string, object>();
        public static CacheEntryManager Cache { get; } = new CacheEntryManager();
        private static CacheQueryManager QueryCache { get; } = new();
        private static TypeCacheManager TypeCache { get; } = new();
        private static ToDeleteManager ToRemove { get; } = new();

        public static EventManager Events = new();

        public static IEnumerable<string> GetAllCacheKeys() => _cache.Keys;

        public class CacheEntryManager
        {
            public CacheEntry<T> GetCache<T>(string key)
            {
                if (!_cache.TryGetValue(key, out var existingValue) || existingValue is not ConcurrentDictionary<string, object> cacheDict)
                {
                    cacheDict = new ConcurrentDictionary<string, object>();
                    _cache[key] = cacheDict;
                }

                return new CacheEntry<T>(key, cacheDict);
            }
        }

        private class CacheQueryManager
        {
            private readonly ConcurrentDictionary<string, string> _queries = new();

            public string GetQuery(string cacheKey, string queryType)
            {
                return _queries.TryGetValue($"{cacheKey}_{queryType}", out var query) ? query : string.Empty;
            }

            public void SetQuery(string cacheKey, string queryType, string query)
            {
                _queries[$"{cacheKey}_{queryType}"] = query;
            }
        }

        private class TypeCacheManager
        {
            private readonly ConcurrentDictionary<string, Type> _type = new();
            public void SetType(string cacheKey, Type type)
            {
                if (_type.TryGetValue(cacheKey, out _)) return;
                _type[cacheKey] = type;
            }

            public Type GetType(string cacheKey) => _type.TryGetValue(cacheKey, out Type? type) ? type : null;
        }

        private class ToDeleteManager
        {
            private readonly ConcurrentDictionary<string, List<string>> _trm = new();
            public void Add(string key, string value, bool deleteMethod = true)
            {
                if (!_trm.ContainsKey(key)) _trm[key] = new List<string>();

                List<string> trm = _trm[key];

                if (deleteMethod)
                {
                    if (!trm.Contains(value)) trm.Add(value);
                }
                else
                {
                    trm?.Remove(value);
                }
            }

            public List<string> Get(string key)
            {
                if (!_trm.ContainsKey(key)) return new List<string>();
                return _trm.TryGetValue(key, out List<string>? trm) ? trm?.ToList() : null;
            }
        }

        public class EventManager
        {
            private readonly ConcurrentDictionary<string, EventList> _events = new();

            public EventList this[string key] => _events.GetOrAdd(key, _ => new EventList());

            public class EventList
            {
                public event Action<string, object?>? OnUpdate;
                public event Action<string, object?>? OnAdd;
                public event Action<string>? OnDelete;
                public event Action? OnClear;
                public event Action<string>? OnGet;

                public void InvokeUpdate(string key, object? value) => OnUpdate?.Invoke(key, value);
                public void InvokeAdd(string key, object? value) => OnAdd?.Invoke(key, value);
                public void InvokeDelete(string key) => OnDelete?.Invoke(key);
                public void InvokeClear() => OnClear?.Invoke();
                public void InvokeGet(string key) => OnGet?.Invoke(key);
            }
        }

        public class CacheEntry<T>
        {

            private readonly string _key;
            public EventManager.EventList Events { get; }

            public CacheEntry(string key, ConcurrentDictionary<string, object> cacheDict)
            {
                _key = key;
                Events = CacheManager.Events[_key];
            }

            private ConcurrentDictionary<string, object> GetOrCreateCache()
            {
                if (!_cache.TryGetValue(_key, out var cachedValue) || cachedValue is not ConcurrentDictionary<string, object> cacheDict)
                {
                    cacheDict = new ConcurrentDictionary<string, object>();
                    _cache[_key] = cacheDict;
                }
                return cacheDict;
            }

            public void Update(string subKey, T value)
            {
                var cacheDict = GetOrCreateCache();

#pragma warning disable CS8601 // Possible null reference assignment.
                cacheDict[subKey] = value;
#pragma warning restore CS8601 // Possible null reference assignment.

                bool isNewEntry = !cacheDict.ContainsKey(subKey);

                if (isNewEntry)
                    Events.InvokeAdd(subKey, value);
                else
                    Events.InvokeUpdate(subKey, value);
            }

            public void Modify(string key, Func<T, T> updater)
            {
                if (TryGetValue(key, out var existingValue))
                {
                    Update(key, updater(existingValue));
                }
            }

            public bool TryGetValue(string subKey, out T value)
            {
                var cacheDict = GetOrCreateCache();
                if (cacheDict.TryGetValue(subKey, out var obj) && obj is T typedValue)
                {
                    Events.InvokeGet(subKey);
                    value = typedValue;
                    return true;
                }
#pragma warning disable CS8601 // Possible null reference assignment.
                value = default;
#pragma warning restore CS8601 // Possible null reference assignment.
                return false;
            }

            public T GetValue(string subKey)
            {
                return GetOrCreateCache().TryGetValue(subKey, out var value) && value is T typedValue ? typedValue : default;
            }

            public T[] GetAllValues()
            {
                return GetOrCreateCache()
                    .Select(x => x.Value)
                    .Select(v =>
                    {
                        if (v is T tValue)
                            return tValue;

                        if (v is string json)
                        {
                            try
                            {
                                return typeof(T).IsPrimitiveOrString() ? (T)(object)json : JsonConvert.DeserializeObject<T>(json);
                            }
                            catch
                            {
                                Console.WriteLine($"[ERROR] Failed to deserialize JSON: {json}");
                                return default;
                            }
                        }

                        return default;
                    })
                    .Where(v => v != null)
                    .ToArray()!;
            }

            public string[] GetAllKeys()
            {
                return GetOrCreateCache().Keys.OfType<string>().ToArray();
            }

            public bool DeleteValue(string subKey)
            {
                if (!TryGetValue(subKey, out _)) return false;

                bool status = GetOrCreateCache().TryRemove(subKey, out _);

                ToRemove.Add(_key, subKey);

                if (_lastCache.TryGetValue(_key, out var lastCacheObj) && lastCacheObj is ConcurrentDictionary<string, object> lastCacheDict)
                {
                    lastCacheDict.TryRemove(subKey, out _);
                }

                if (status) Events.InvokeDelete(subKey);

                return status;
            }

            public void Clear()
            {
                Events.InvokeClear();
                GetOrCreateCache().Clear();
            }

            public string MysqlQuery { get => QueryCache.GetQuery(_key, "MysqlQuery"); set => QueryCache.SetQuery(_key, "MysqlQuery", value); }
            public string SqliteQuery { get => QueryCache.GetQuery(_key, "SqliteQuery"); set => QueryCache.SetQuery(_key, "SqliteQuery", value); }
            public string SaveMysqlQuery { get => QueryCache.GetQuery(_key, "SaveMysqlQuery"); set => QueryCache.SetQuery(_key, "SaveMysqlQuery", value); }
            public string SaveSqliteQuery { get => QueryCache.GetQuery(_key, "SaveSqliteQuery"); set => QueryCache.SetQuery(_key, "SaveSqliteQuery", value); }

            public bool Init()
            {
                try
                {
                    Console.WriteLine(Skynomi.Utils.Messages.Name + " Initializing " + _key + " cache...");

                    TypeCache.SetType(_key, typeof(T));
                    Type t = TypeCache.GetType(_key);

                    string query = CacheManager.QueryCache.GetQuery(_key, Skynomi.Database.Database._databaseType == "sqlite" ? "SqliteQuery" : "MysqlQuery");

                    if (string.IsNullOrEmpty(query))
                    {
                        TShock.Log.ConsoleError("No init query set");
                        return false;
                    }

                    var result = db.CustomVoid(query, output: true);

                    // Reset the cache
                    GetOrCreateCache().Clear();

                    foreach (var row in result)
                    {
                        string key = row["Key"];
                        object value = t.IsPrimitiveOrString() ? Convert.ChangeType(row["Value"], t) : JsonConvert.DeserializeObject(row["Value"], t);
                        GetOrCreateCache()[key] = value;
                    }

                    var deepCopy = ((ConcurrentDictionary<string, object>)_cache[_key])
                    .ToDictionary(entry => entry.Key, entry =>
                        entry.Value != null
                            ? (entry.Value.GetType().IsPrimitive || entry.Value is string || entry.Value is decimal
                                ? entry.Value
                                : JsonConvert.DeserializeObject(JsonConvert.SerializeObject(entry.Value), entry.Value.GetType()))
                            : default(object));

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                    _lastCache[_key] = new ConcurrentDictionary<string, object>(deepCopy);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

                    return true;
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"Failed to init cache {_key}: {ex.Message}");
                    return false;
                }
            }

            public bool Reload(bool save = false)
            {
                try
                {
                    if (save) Save();
                    return Init();
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"Failed to reload cache: {ex.Message}");
                    return false;
                }
            }

            public bool Save()
            {
                try
                {
                    string query = CacheManager.QueryCache.GetQuery(_key, Skynomi.Database.Database._databaseType == "sqlite" ? "SaveSqliteQuery" : "SaveMysqlQuery");

                    if (string.IsNullOrEmpty(query))
                    {
                        TShock.Log.ConsoleError($"No save query set for {_key}");
                        return false;
                    }

                    var cacheDict = GetOrCreateCache();

                    if (!_lastCache.TryGetValue(_key, out var lastCacheObj) || lastCacheObj is not ConcurrentDictionary<string, object> lastCacheDict)
                    {
                        lastCacheDict = new ConcurrentDictionary<string, object>();
                        _lastCache[_key] = lastCacheDict;
                    }

                    saveToFile();

                    bool toSave = false;
                    foreach (var kvp in cacheDict) { if (!lastCacheDict.TryGetValue(kvp.Key, out var lastValue) || !ReflectionHelper.AreObjectsEqual(lastValue, kvp.Value)) { toSave = true; break; } }
                    ;
                    if (ToRemove.Get(_key).Any()) toSave = true;
                    if (!toSave) return true;

                    Console.WriteLine(Skynomi.Utils.Messages.Name + " Saving " + _key + "...");

                    var con = Skynomi.Database.Database._connection;
                    System.Data.Common.DbConnection? database = Skynomi.Database.Database._databaseType == "sqlite"
                        ? con as Microsoft.Data.Sqlite.SqliteConnection
                        : con as MySql.Data.MySqlClient.MySqlConnection;

                    // Reopen connection
                    database?.Close();
                    database?.Open();

                    using (var transaction = database?.BeginTransaction())
                    {
                        try
                        {
                            using (var cmd = database?.CreateCommand())
                            {
                                foreach (string key in ToRemove.Get(_key))
                                {
                                    string aquery = CacheManager.QueryCache.GetQuery(_key, Skynomi.Database.Database._databaseType == "sqlite" ? "SqliteQuery" : "MysqlQuery");
                                    var match = Regex.Match(aquery, @"FROM\s+([\w\d_]+)", RegexOptions.IgnoreCase);
                                    string table = match.Groups[1].Value;
                                    var bmatch = Regex.Match(aquery, @"([\w]+)\s+AS\s+'Key'", RegexOptions.IgnoreCase);
                                    string keyColumn = bmatch.Success ? bmatch.Groups[1].Value : "key";

                                    cmd.CommandText = "DELETE FROM " + table + " WHERE " + keyColumn + " = @key";
                                    cmd.Parameters.Clear();
                                    cmd.AddParameter("@key", key);
                                    cmd.ExecuteNonQuery();

                                    ToRemove.Add(_key, key, false);
                                }

                                foreach (var kvp in cacheDict)
                                {
                                    if (!lastCacheDict.TryGetValue(kvp.Key, out var lastValue) || !ReflectionHelper.AreObjectsEqual(lastValue, kvp.Value))
                                    {
                                        cmd.CommandText = query;
                                        cmd.Parameters.Clear();
                                        cmd.AddParameter("@key", kvp.Key);

                                        if (TypeCache.GetType(_key).IsPrimitiveOrString())
                                        {
                                            cmd.AddParameter("@value", kvp.Value);
                                        }
                                        else
                                        {
                                            string[] exs = new string[] { };
                                            var matches = Regex.Matches(query, @"@value_([\w\d_]+)");
                                            foreach (Match match in matches)
                                            {
                                                if (exs.Contains(match.Value))
                                                {
                                                    continue;
                                                }
                                                exs = exs.Concat(new string[] { match.Value }).ToArray();

                                                string propertyName = match.Groups[1].Value;
                                                object value = ReflectionHelper.GetPropertyValue(kvp.Value, propertyName);
                                                cmd.AddParameter(match.Value, value ?? DBNull.Value);
                                            }
                                        }

                                        // #region Debug
                                        // Console.WriteLine($"Executing Query: {cmd.CommandText}");
                                        // foreach (System.Data.Common.DbParameter param in cmd.Parameters)
                                        // {
                                        //     Console.WriteLine($"{param.ParameterName}: {param.Value}");
                                        // }
                                        // #endregion

                                        cmd.ExecuteNonQuery();

                                        lastCacheDict[kvp.Key] = kvp.Value;
                                    }
                                }
                            }
                            transaction?.Commit();
                            database?.Close();
                        }
                        catch (Exception)
                        {
                            transaction?.Rollback();
                            database?.Close();
                            throw;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"Failed to save cache {_key}: {ex.ToString()}");
                    return false;
                }
            }

            private void saveToFile()
            {
                var toSaveCacheDict = GetOrCreateCache().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                string json = JsonConvert.SerializeObject(toSaveCacheDict, Formatting.Indented);
                string path = Path.Combine(TShock.SavePath + "/Skynomi/cache/" + _key + "/");
                Directory.CreateDirectory(path);
                string filePath = Path.Combine(path, $"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.json");
                File.WriteAllText(filePath, json);
            }
        }


        public static bool SaveAll()
        {
            try
            {
                foreach (var key in _cache.Keys)
                {
                    Type type = CacheManager.TypeCache.GetType(key);
                    var getCacheMethod = typeof(CacheManager.CacheEntryManager)
                        .GetMethod("GetCache")
                        ?.MakeGenericMethod(type);
                    var cacheEntry = getCacheMethod?.Invoke(CacheManager.Cache, new object[] { key });
                    var saveMethod = cacheEntry?.GetType().GetMethod("Save");
                    saveMethod?.Invoke(cacheEntry, null);
                }
                return true;
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Failed to save all cache: {ex.Message}");
                return false;
            }
        }

        private static CancellationTokenSource? _autoSaveCancellationTokenSource;

        public static bool AutoSave(int intervalInSeconds)
        {
            StopAutoSave();
            _autoSaveCancellationTokenSource = new CancellationTokenSource();
            var token = _autoSaveCancellationTokenSource.Token;

            try
            {
                Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(intervalInSeconds * 1000, token);
                        Console.WriteLine(Skynomi.Utils.Messages.CacheSaving);
                        SaveAll();
                        TShock.Log.ConsoleInfo(Skynomi.Utils.Messages.CacheSaved);
                    }
                }, token);
                return true;
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Failed to auto save cache: {ex.Message}");
                return false;
            }
        }

        public static void StopAutoSave()
        {
            _autoSaveCancellationTokenSource?.Cancel();
        }
    }

    public static class TypeExtensions
    {
        public static bool IsPrimitiveOrString(this Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal);
        }
    }

    public static class ReflectionHelper
    {
        public static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null) return null;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            PropertyInfo prop = obj.GetType().GetProperty(propertyName);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            return prop?.GetValue(obj, null);
        }
        public static bool AreObjectsEqual(object obj1, object obj2)
        {
            if (obj1 == null || obj2 == null) return obj1 == obj2;

            if (obj1.GetType().IsPrimitive || obj1 is string)
            {
                return Equals(obj1, obj2);
            }

            Type type = obj1.GetType();
            if (type != obj2.GetType()) return false;

            foreach (var prop in type.GetProperties())
            {
                var val1 = prop.GetValue(obj1);
                var val2 = prop.GetValue(obj2);
                if (!object.Equals(val1, val2)) return false;
            }
            return true;
        }
    }
}