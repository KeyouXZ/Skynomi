namespace Skynomi.Utils
{
    public class Messages
    {
        public static readonly string Name = $"[Skynomi]";

        public static readonly string PermissionError = $"{Name} You don't have permission to use this command";
        public static readonly string Reload = $"{Name} Reloaded configuration file";
        public static readonly string NotLogged = $"You must use this command in-game.";

        #region Database
        public static readonly string UnsupportedDatabaseType = $"{Name} Unsupported database type, falling back to SQLite";
        public static readonly string FallBack = $"{Name} Falling back to SQLite.";
        public static readonly string DifferenctDatabaseType = $"{Name} detected a mismatch between the database type in the TShock configuration and the plugin settings. Please ensure the database types are consistent to avoid potential issues.";
        #endregion

        #region CacheManager
        public static readonly string CacheReloaded = $"{Name} Cache reloaded";
        public static readonly string CacheSaving = $"{Name} Saving database:";
        public static readonly string CacheSaved = $"{Name} Database saved";
        #endregion
    }
}
