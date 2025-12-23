using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Skynomi.Economy;

public class EconomyModule : Modules.IModule, Modules.IDependent
{
    public string Name => "Economy";
    public string Description => "Economy module";
    public Version Version => new(0, 1, 0);

    public IReadOnlyList<Type> RequiredModules => [typeof(Utils.UtilsModule), typeof(Skynomi.Database.DatabaseModule)];

    public string Author => "Keyou";

    public Database Db = new();

    public void Initialize()
    {
        // Hooks
        ServerApi.Hooks.NetGreetPlayer.Register(SkynomiPlugin.Instance, OnPlayerJoin);
        AccountHooks.AccountCreate += OnPlayerRegister;
        Commands.Initialize();
    }

    private void OnPlayerJoin(GreetPlayerEventArgs e)
    {
        var player = TShock.Players[e.Who];
        
        if (player.IsLoggedIn && Db.GetWalletData(player.Account.Name) is not null) return;
        
        Db.CreateAccount(player.Account.Name);
        Log.Info($"Created new account for {player.Account.Name}");
    }

    private void OnPlayerRegister(AccountCreateEventArgs e)
    {
        if (Db.GetWalletData(e.Account.Name) is not null) return;
        
        Db.CreateAccount(e.Account.Name);
        Log.Info($"Reg: Created new account for {e.Account.Name}");
    }
}