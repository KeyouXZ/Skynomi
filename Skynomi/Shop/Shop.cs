using System.Timers;
using Skynomi.Database;
using Skynomi.Economy;
using Skynomi.Modules;
using Skynomi.Utils;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using IDisposable = Skynomi.Modules.IDisposable;
using Timer = System.Timers.Timer;

namespace Skynomi.Shop;

public class Shop : IModule, IReloadable, IDisposable, IDependent
{
    public string Name => "Shop";
    public string Description => "Shop module";
    public Version Version => new(0, 1, 0);
    public string Author => "Keyou";

    public IReadOnlyList<Type> RequiredModules =>
    [
        typeof(UtilsModule), typeof(DatabaseModule), typeof(EconomyModule)
    ];
    
    public Config ShopConfig = null!;
    public Timer BroadcastTimer = new();

    public void Initialize()
    {
        ShopConfig = Config.Read();
        Commands.Initialize();
        
        ServerApi.Hooks.GamePostInitialize.Register(SkynomiPlugin.Instance, PostInitialize);
    }

    private void PostInitialize(EventArgs args)
    {
        if (!ShopConfig.AutoBroadcastShop || _List() == "No items available") return;
        
        Log.Warn(Messages.AutoShopDisabled);
        BroadcastTimer = new Timer(ShopConfig.BroadcastIntervalInSeconds * 1000);
        BroadcastTimer.Elapsed += OnBroadcastTimerElapsed;
        BroadcastTimer.AutoReset = true;
        BroadcastTimer.Start();
    }

    public void Reload(ReloadEventArgs args)
    {
        if (ShopConfig.AutoBroadcastShop)
        {
            BroadcastTimer.Stop();
        }

        Skynomi.Config.Read();
        ShopConfig = Config.Read();

        if (!ShopConfig.AutoBroadcastShop || _List() == "No items available") return;
        
        Log.Warn(Messages.AutoShopDisabled);
        BroadcastTimer = new Timer(ShopConfig.BroadcastIntervalInSeconds * 1000);
        BroadcastTimer.Elapsed += OnBroadcastTimerElapsed;
        BroadcastTimer.AutoReset = true;
        BroadcastTimer.Start();
    }

    public void Dispose()
    {
        ServerApi.Hooks.GamePostInitialize.Deregister(SkynomiPlugin.Instance, PostInitialize);
    }
    
    private string _List()
    {
        var utils = ModuleManager.Get<UtilsModule>();
        // shop list
        string message = "Shop Items";
        int i = 0;
        foreach (var item in ShopConfig.ShopItems)
        {
            i++;
            message += $"\n{i}. [i:{item.Key}] ({item.Key}) - B: {utils.CurrencyFormat(item.Value.buyPrice)} | S: {utils.CurrencyFormat(item.Value.sellPrice)}";
        }

        if (message == "Shop Items")
        {
            message = "No items available";
        }

        return message;
    }
    
    private void OnBroadcastTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        TSPlayer.All.SendInfoMessage(_List());
    }
}