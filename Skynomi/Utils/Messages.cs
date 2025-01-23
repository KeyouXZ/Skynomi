namespace Skynomi.Utils
{
    public class Messages
    {
        public static readonly string Name = "[Skynomi]";
        
        public static readonly string AutoShopDisabled = $"{Name} Auto shop broadcast is disabled! Add item in config to enable!";
        public static readonly string EmptyNEnableProtectedRegion = $"{Name} Protected region is enabled but no region is set. Please set a region in the config.";

        public static readonly string PermissionError = $"{Name} You don't have permission to use this command";
        public static readonly string Reload = $"{Name} Reloaded configuration file";
        public static readonly string NotLogged = $"You must use this command in-game.";

        public static readonly string UnsupportedDatabaseType = $"{Name} Unsupported database type, falling back to SQLite";
        public static readonly string FallBack = $"{Name} Falling back to SQLite.";
        public static readonly string DifferenctDatabaseType = $"{Name} detected a mismatch between the database type in the TShock configuration and the plugin settings. Please ensure the database types are consistent to avoid potential issues.";

        public static readonly string ParentSettingChanged = $"{Name} Use Parent for Rank setting changed, please restart the server to apply changes.";
    }
}
