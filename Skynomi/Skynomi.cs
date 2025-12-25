using System.Reflection;
using Skynomi.Modules;
using Terraria;
using TerrariaApi.Server;
using TShockAPI.Hooks;

using Skynomi.Modules.Http;

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

    private HttpServer? _webServer;

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
            ServerApi.Hooks.GamePostInitialize.Register(this, PostInitialize);

            // Initialize all modules
            ModuleManager.InitializeAll();

            Log.Success($"Skynomi {Version} initialized at {TimeBoot}");
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
            Log.Info("Reloading Skynomi Modules...");

            // Reload the config
            SkynomiConfig = Config.Read();

            // Reload all modules
            ModuleManager.ReloadAll(e);

            // Reload web server
            var webConfig = SkynomiConfig.Web;
            _webServer?.Reload(webConfig.Address, webConfig.Port, webConfig.Secure);

            Log.Info(Messages.Reload);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to reload Skynomi: {ex}");
        }
    }

    private void PostInitialize(EventArgs args)
    {
        _webServer = new HttpServer();
        var webConfig = SkynomiConfig.Web;

        _webServer.Start(webConfig.Address, webConfig.Port, webConfig.Secure);

        ModuleManager.HandleAllWebServer(_webServer.Router);
        ModuleManager.HandleAllWebSocket(_webServer.WsRouter);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        try
        {
            Log.Info("Disposing Skynomi Modules...");
            // Dispose modules
            ModuleManager.DisposeAll();
            _webServer?.Dispose();

            // Deregister All Hooks
            GeneralHooks.ReloadEvent -= Reload;
            ServerApi.Hooks.GamePostInitialize.Deregister(this, PostInitialize);

            Log.Success(Messages.Dispose);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to dispose Skynomi: {ex}");
        }
    }
}