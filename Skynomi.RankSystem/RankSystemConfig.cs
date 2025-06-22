namespace Skynomi.RankSystem
{
    public abstract class Permissions
    {
        public static readonly string Rank = "skynomi.rank";
        public static readonly string RankUp = "skynomi.rank.up";
        public static readonly string RankDown = "skynomi.rank.down";
        public static readonly string RankList = "skynomi.rank.list";
        public static readonly string RankInfo = "skynomi.rank.info";
        public static readonly string ResetRank = "skynomi.rank.resetrank";
        public static readonly string ResetHighestRank = "skynomi.rank.resethighestrank";
    }

    public abstract class Messages
    {
        public static readonly string ParentSettingChanged = "Use Parent for Rank setting changed, please restart the server to apply changes.";
    }
}