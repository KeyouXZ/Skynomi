namespace Skynomi.Utils
{
    public class Messages
    {
        public static readonly string Name = $"[Skynomi]";

        public static readonly string PermissionError = "You don't have permission to use this command";
        public static readonly string Reload = "Reloaded configuration file";
        public static readonly string NotLogged = $"You must use this command in-game.";

        #region Database
        public static readonly string UnsupportedDatabaseType = "Unsupported database type, falling back to SQLite";
        public static readonly string FallBack = "Falling back to SQLite.";
        public static readonly string DifferenctDatabaseType = "detected a mismatch between the database type in the TShock configuration and the plugin settings. Please ensure the database types are consistent to avoid potential issues.";
        #endregion

        #region CacheManager
        public static readonly string CacheReloaded = "Cache reloaded";
        public static readonly string CacheSaving = "Saving cache(s):";
        public static readonly string CacheSaved = "Cache(s) saved";
        #endregion
    }
}
