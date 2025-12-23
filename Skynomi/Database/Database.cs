using LiteDB;
using TShockAPI;

namespace Skynomi.Database;

public sealed class DatabaseModule : Modules.IModule, Modules.IDisposable
{
    public string Name => "Database";
    public string Description => "LiteDB provider for Skynomi modules.";
    public Version Version => new(0, 1, 0);
    public string Author => "Keyou";

    public LiteDatabase Db { get; private set; } = null!;

    public void Initialize()
    {
        var path = Path.Join(TShock.SavePath, "skynomi.db");
        Db = new LiteDatabase(path);
    }

    public void Dispose()
    {
        Db?.Dispose();
    }
}