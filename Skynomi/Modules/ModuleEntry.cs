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
}