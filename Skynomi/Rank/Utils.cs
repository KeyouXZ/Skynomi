using Skynomi.Modules;

namespace Skynomi.Rank;

public abstract class Utils
{
    public static string? GetRankByIndex(int index)
    {
        var rankModule = ModuleManager.Get<Rank>();
        var rankKeys = new List<string>(rankModule.RankConfig.Ranks.Keys);

        if (index >= 0 && index < rankKeys.Count)
        {
            return rankKeys[index];
        }

        return null;
    }

    public static int GetRankIndex(string rankName)
    {
        var rankModule = ModuleManager.Get<Rank>();
        var rankKeys = new List<string>(rankModule.RankConfig.Ranks.Keys);
        return rankKeys.IndexOf(rankName);
    }
}