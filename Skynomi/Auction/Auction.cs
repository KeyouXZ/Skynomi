using Skynomi.Modules;
using TerrariaApi.Server;
using TShockAPI.Hooks;
using IDisposable = Skynomi.Modules.IDisposable;

namespace Skynomi.Auction;

public class Auction : IModule, IDependent, IReloadable, IDisposable
{
    public string Name => "Auction";
    public string Description => "Live auction system for players to buy and sell items";
    public Version Version => new(1, 0, 0);
    public string Author => "Folvrix";

    public IReadOnlyList<Type> RequiredModules =>
    [
        typeof(Utils.UtilsModule),
        typeof(Skynomi.Database.DatabaseModule),
        typeof(Economy.EconomyModule)
    ];

    public Config AuctionConfig = null!;

    public void Initialize()
    {
        AuctionConfig = Config.Read();
        AuctionManager.Initialize(AuctionConfig);
        Commands.Initialize();
    }

    public void Reload(ReloadEventArgs args)
    {
        AuctionConfig = Config.Read();
        AuctionManager.Reload(AuctionConfig);
    }

    public void Dispose()
    {
        AuctionManager.Dispose();
    }
}
