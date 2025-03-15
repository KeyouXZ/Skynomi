using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TShockAPI;

namespace Skynomi.Database
{
    public static class CacheManager
    {
        private static Skynomi.Database.Database db = new();
        private static ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();
        private static ConcurrentDictionary<string, object> _lastCache = new ConcurrentDictionary<string, object>();

        public static CacheEntryManager Cache { get; } = new CacheEntryManager();

        public static IEnumerable<string> GetAllCacheKeys()
        {
            return _cache.Keys;
        }

        public class CacheEntryManager
        {
            public CacheEntry this[string key]
            {
                get
                {
                    if (!_cache.TryGetValue(key, out var existingValue) || existingValue is not ConcurrentDictionary<string, object>)
                    {
                        var newDict = new ConcurrentDictionary<string, object>();
                        _cache[key] = newDict;
                    }
                    return new CacheEntry(key);
                }
            }
        }

        public class CacheEntry
        {
            private readonly string _key;

            public CacheEntry(string key)
            {
                _key = key;
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

            public void SetValue(string subKey, object value)
            {
                var cacheDict = GetOrCreateCache();
                cacheDict[subKey] = value;
            }

            public bool TryGetValue(string subKey, out object value)
            {
                var cacheDict = GetOrCreateCache();
                return cacheDict.TryGetValue(subKey, out value);
            }

            public object? GetValue(string subKey)
            {
                var cacheDict = GetOrCreateCache();
                return cacheDict.TryGetValue(subKey, out var value) ? value : null;
            }

            public string SqliteQuery
            {
                get => GetOrCreateCache().TryGetValue("SqliteQuery", out var value) ? value as string : null;
                set
                {
                    GetOrCreateCache()["SqliteQuery"] = value;
                }
            }

            public string MysqlQuery
            {
                get => GetOrCreateCache().TryGetValue("MysqlQuery", out var value) ? value as string : null;
                set
                {
                    GetOrCreateCache()["MysqlQuery"] = value;
                }
            }

            public bool Init()
            {
                try
                {
                    string query = Skynomi.Database.Database._databaseType == "sqlite" ? SqliteQuery : MysqlQuery;

                    if (string.IsNullOrEmpty(query))
                    {
                        TShock.Log.ConsoleError("No init query set");
                        return false;
                    }

                    if (db == null)
                    {
                        TShock.Log.ConsoleError("Failed to init cache, Database is not initialized");
                        return false;
                    }

                    Console.WriteLine(Skynomi.Utils.Messages.Name + " Init " + _key + " cache...");
                    var result = db.CustomVoid(query, output: true);

                    var match = Regex.Match(query, @"SELECT\s*(.*?)\s*FROM", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        var columns = match.Groups[1].Value.Split(',').Select(column => column.Trim()).ToArray();

                        if (columns.Length < 2)
                        {
                            TShock.Log.ConsoleError("Invalid init query");
                            return false;
                        }

                        foreach (var kvp in result)
                        {
                            SetValue(kvp[columns[0]], kvp[columns[1]]);
                        }
                    }
                    _lastCache[_key] = new ConcurrentDictionary<string, object>(
                        ((ConcurrentDictionary<string, object>)_cache[_key]).ToDictionary(entry => entry.Key, entry => entry.Value)
                    );
                    return true;
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"Failed to init cache: {ex}");
                    return false;
                }
            }

            public string SaveSqliteQuery
            {
                get => GetOrCreateCache().TryGetValue("SaveSqliteQuery", out var value) ? value as string : null;
                set
                {
                    GetOrCreateCache()["SaveSqliteQuery"] = value;
                }
            }

            public string SaveMysqlQuery
            {
                get => GetOrCreateCache().TryGetValue("SaveMysqlQuery", out var value) ? value as string : null;
                set
                {
                    GetOrCreateCache()["SaveMysqlQuery"] = value;
                }
            }
            public bool Save()
            {
                try
                {
                    Console.WriteLine(Skynomi.Utils.Messages.Name + " Saving " + _key + "...");
                    var query = Skynomi.Database.Database._databaseType == "sqlite" ? SaveSqliteQuery : SaveMysqlQuery;

                    if (string.IsNullOrEmpty(query))
                    {
                        TShock.Log.ConsoleError("No save query set");
                        return false;
                    }

                    if (!_lastCache.TryGetValue(_key, out var lastCacheObj) || lastCacheObj is not ConcurrentDictionary<string, object> lastCacheDict)
                    {
                        TShock.Log.ConsoleInfo($"Initializing _lastCache for key: {_key}");
                        lastCacheDict = new ConcurrentDictionary<string, object>();
                        _lastCache[_key] = lastCacheDict;
                    }

                    var cacheDict = GetOrCreateCache();

                    int counter = 0;
                    var excludedKeys = new HashSet<string> { "MysqlQuery", "SaveMysqlQuery", "SqliteQuery", "SaveSqliteQuery" };
                    foreach (var kvp in cacheDict.Where(kvp => !excludedKeys.Contains(kvp.Key)))
                    {
                        if (!lastCacheDict.TryGetValue(kvp.Key, out var lastValue) || !Equals(lastValue, kvp.Value))
                        {
                            counter++;
                            using (var cmd = db.CreateCommand())
                            {
                                cmd.CommandText = query;

                                cmd.Parameters.Clear();
                                cmd.Parameters.AddWithValue("@Param1", kvp.Key);
                                cmd.Parameters.AddWithValue("@Param2", kvp.Value);

                                cmd.ExecuteNonQuery();
                            }

                            lastCacheDict[kvp.Key] = kvp.Value;
                        }
                    }

                    var toSavecacheDict = GetOrCreateCache()
                        .Where(kvp => !excludedKeys.Contains(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    string json = JsonConvert.SerializeObject(toSavecacheDict, Formatting.Indented);
                    string path = Path.Combine(TShock.SavePath + "/Skynomi/cache/" + _key + "/");
                    Directory.CreateDirectory(path);
                    string filePath = Path.Combine(path, $"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.json");
                    File.WriteAllText(filePath, json);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"Failed to save cache: {ex.Message}");
                    return false;
                }
            }


            public bool Reload(bool save = false)
            {
                try
                {
                    if (save)
                    {
                        Save();
                    }

                    bool status = Init();
                    return status;
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"Failed to reload cache: {ex.Message}");
                    return false;
                }
            }
        }

        public static bool SaveAll()
        {
            try
            {
                foreach (var key in _cache.Keys)
                {
                    if (Cache[key] is CacheEntry cacheEntry)
                    {
                        cacheEntry.Save();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Failed to save all cache: {ex.Message}");
                return false;
            }
        }

        public static bool AutoSave(int intervalInSeconds)
        {
            try
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(intervalInSeconds * 1000);
                        Console.WriteLine(Skynomi.Utils.Messages.CacheSaving);
                        Skynomi.Database.CacheManager.SaveAll();
                        TShock.Log.ConsoleInfo(Skynomi.Utils.Messages.CacheSaved);
                    }
                });
                return true;

            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Failed to auto save cache: {ex.Message}");
                return false;
            }
        }
    }
}
