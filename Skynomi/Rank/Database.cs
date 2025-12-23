using Skynomi.Modules;

namespace Skynomi.Rank;

public class Database
{
    public class RankData
    {
        public RankData(string name)
        {
            Name = name;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Rank { get; set; }
        public int HighestRank { get; set; }
    }

    public RankData? GetRankData(string name)
    {
        var db = ModuleManager.Get<Skynomi.Database.DatabaseModule>();

        var data = db.Db.GetCollection<RankData>().FindOne(x => x.Name == name);

        return data;
    }

    public void CreateRankData(string name)
    {
        var db = ModuleManager.Get<Skynomi.Database.DatabaseModule>();
        var col = db.Db.GetCollection<RankData>();

        if (col.FindOne(x => x.Name == name) is not null) return;

        var data = new RankData(name);

        col.Insert(data);
        col.EnsureIndex(x => x.Name, unique: true);
    }
    
    public bool UpdateRank(string name, Action<RankData> updateAction)
    {
        var dbModule = ModuleManager.Get<Skynomi.Database.DatabaseModule>();
        var col = dbModule.Db.GetCollection<RankData>();

        var data = col.FindOne(x => x.Name == name);
        if (data is null)
            return false;
        
        updateAction(data);

        // Auto update the highest rank
        if (data.Rank > data.HighestRank)
            data.HighestRank = data.Rank;

        col.Update(data);
        return true;
    }
}