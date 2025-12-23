using Skynomi.Modules;

namespace Skynomi.Economy;

public class Database
{
    public class Wallet
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public long Balance { get; set; }
    }

    public class BankAccount
    {
        public required string AccountId { get; set; }
        public required string Username { get; set; }
        public required int Pin { get; set; }
        public long Balance { get; set; }
        public long Limit { get; set; }
    }

    public Wallet? GetWalletData(string name)
    {
        var db = ModuleManager.Get<Skynomi.Database.DatabaseModule>();

        var data = db.Db.GetCollection<Wallet>().FindOne(x => x.Name == name);
        return data;
    }

    public void CreateAccount(string name, long balance = 0)
    {
        var db = ModuleManager.Get<Skynomi.Database.DatabaseModule>();
        var col = db.Db.GetCollection<Wallet>();

        if (col.FindOne(x => x.Name == name) is not null) return;

        var account = new Wallet
        {
            Name = name,
            Balance = balance
        };

        col.Insert(account);
        col.EnsureIndex(x => x.Name, unique: true);
    }

    public long? GetWalletBalance(string name)
    {
        var wallet = GetWalletData(name);

        return wallet?.Balance;
    }

    public bool UpdateWalletBalance(string name, Action<Wallet> update)
    {
        var dbModule = ModuleManager.Get<Skynomi.Database.DatabaseModule>();
        var col = dbModule.Db.GetCollection<Wallet>();

        var data = col.FindOne(x => x.Name == name);
        if (data is null)
        {
            return false;
        }

        update(data);
        
        col.Update(data);
        return true;
    }
}