using System.Reflection;
using Skynomi.Modules;
using Terraria;
using TerrariaApi.Server;
using TShockAPI.Hooks;

namespace Skynomi;

[ApiVersion(2, 1)]
public class SkynomiPlugin(Main game) : TerrariaPlugin(game)
{
    public override string Name => "Skynomi Core";
    public override string Description => "Terraria Economy System Core";
    public override string Author => "Keyou";

    public override Version Version =>
        Assembly.GetExecutingAssembly().GetName().Version
        ?? new Version(1, 0, 0);

    public static SkynomiPlugin Instance = null!;

    public static Config SkynomiConfig { get; private set; } = null!;
    public static readonly string TimeBoot = DateTime.Now.ToString("yyyy-MM-dd-HH-mm");

    public override void Initialize()
    {
        try
        {
            // Register self
            Instance = this;
            
            // Read config
            SkynomiConfig = Config.Read();
                
            // Register modules
            ModuleManager.AutoRegister();
            
            // Register all hooks
            GeneralHooks.ReloadEvent += Reload;
            
            // Initialize all modules
            ModuleManager.InitializeAll();

            Log.Info($"Skynomi {Version} initialized at {TimeBoot}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to initialize Skynomi {ex}");
        }
    }

    private void Reload(ReloadEventArgs e)
    {
        try
        {
            // Reload the config
            SkynomiConfig = Config.Read();
            
            // Reload all modules
            ModuleManager.ReloadAll(e);

            Log.Info(Messages.Reload);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to reload Skynomi: {ex}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        try
        {
            // Dispose modules
            ModuleManager.DisposeAll();
            
            // Deregister All Hooks
            GeneralHooks.ReloadEvent -= Reload;

            Log.Info(Messages.Dispose);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to dispose Skynomi: {ex}");
        }
    }
}