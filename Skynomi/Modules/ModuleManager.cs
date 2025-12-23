using TShockAPI.Hooks;

namespace Skynomi.Modules;

public static class ModuleManager
{
    private static readonly Dictionary<Type, ModuleEntry> Modules = new();

    public static void Register(IModule module)
    {
        if (Modules.ContainsKey(module.GetType()))
        {
            Log.Error($"Module {module.Name} already registered. Skipped");
            return;
        }

        Modules[module.GetType()] = new ModuleEntry(module);
        Log.Info($"Registered module {module.Name} v{module.Version.ToString()} by {module.Author}");
    }

    public static void AutoRegister()
    {
        var modules = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IModule).IsAssignableFrom(t)
                        && !t.IsInterface
                        && !t.IsAbstract);

        foreach (var type in modules)
        {
            try
            {
                var module = (IModule)Activator.CreateInstance(type)!;
                Register(module);
            }
            catch (Exception e)
            {
                Log.Error($"Error loading module {type.Name}: {e}");
            }
        }
    }

    private static ModuleEntry Resolve(Type type)
    {
        if (!Modules.TryGetValue(type, out var entry))
            throw new InvalidOperationException(
                $"Required module '{type.Name}' not registered");

        return entry;
    }

    public static void InitializeAll()
    {
        var visited = new HashSet<Type>();

        foreach (var entry in Modules.Values)
            InitializeWithDeps(entry, visited);
    }

    private static void InitializeWithDeps(
        ModuleEntry entry,
        HashSet<Type> stack)
    {
        if (entry.Initialized)
            return;

        var type = entry.Module.GetType();

        if (!stack.Add(type))
            throw new InvalidOperationException(
                $"Circular dependency detected at {entry.Module.Name}");

        // ReSharper disable once SuspiciousTypeConversion.Global
        if (entry.Module is IDependent dependent)
        {
            foreach (var depType in dependent.RequiredModules)
            {
                if (!Modules.TryGetValue(depType, out var dep))
                    throw new InvalidOperationException(
                        $"Missing dependency {depType.Name} for {entry.Module.Name}");

                InitializeWithDeps(dep, stack);
            }
        }

        entry.Initialize(Resolve);
        stack.Remove(type);
    }
    
    public static T Get<T>() where T : class, IModule
    {
        return (T)Modules[typeof(T)].Module;
    }

    public static void ReloadAll(ReloadEventArgs e)
    {
        foreach (var entry in Modules.Values)
        {
            entry.Reload(e);
        }
    }

    public static void DisposeAll()
    {
        foreach (var entry in Modules.Values)
        {
            entry.Dispose();
        }
    }
}