using Skynomi.Modules.Http;
using Skynomi.Modules.WebSocket;
using TShockAPI.Hooks;

namespace Skynomi.Modules;

internal sealed class ModuleEntry(IModule module)
{
    public IModule Module { get; } = module;
    public bool Initialized { get; private set; }

    public void Initialize(
        Func<Type, ModuleEntry> resolver,
        HashSet<Type>? stack = null)
    {
        stack ??= [];

        if (!stack.Add(Module.GetType()))
            throw new InvalidOperationException(
                $"Circular dependency detected at {Module.Name}");

        if (Initialized)
            return;

        // ReSharper disable once SuspiciousTypeConversion.Global
        if (Module is IDependent dependent)
        {
            foreach (var depType in dependent.RequiredModules)
            {
                resolver(depType).Initialize(resolver, stack);
            }
        }

        Module.Initialize();
        Initialized = true;
    }


    public void Reload(ReloadEventArgs e)
    {
        if (Module is not IReloadable reloadable) return;

        reloadable.Reload(e);
        Log.Info(
            $"Reloaded module: {Module.Name} v{Module.Version.ToString()} by {Module.Author}");
    }

    public void Dispose()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (Module is not IDisposable disposable) return;

        disposable.Dispose();
        Log.Info(
            $"Disposed module: {Module.Name} v{Module.Version.ToString()} by {Module.Author}");
    }

    public void RegisterWebServer(HttpRouter router)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (Module is not IHttpServer webServer) return;

        var moduleName = Module.Name
            .ToLowerInvariant()
            .Replace(" ", "");

        var webRoot = Path.Combine(
            SkynomiPlugin.SkynomiConfig.Web.Root,
            moduleName
        );

        // router.Register(path, webServer.HandleHttpServer);
        router.Register("/" + moduleName, async ctx =>
        {
            await webServer.HandleHttpServer(ctx);

            switch (ctx.IsHandled)
            {
                case false when Directory.Exists(webRoot):
                    await HttpStatic.Serve(ctx.Raw, webRoot);
                    return;
                case false:
                    ctx.Raw.Response.StatusCode = 404;
                    break;
            }
        });
    }


    public void RegisterWebSocket(WebSocketRouter router)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (Module is not IWebSocket ws) return;

        router.Register(Module.Name
            .ToLowerInvariant()
            .Replace(" ", ""), ws);
    }
}