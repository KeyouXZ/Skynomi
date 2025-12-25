namespace Skynomi.Modules.WebSocket;

internal sealed class WebSocketRouter
{
    private readonly Dictionary<string, IWebSocket> _modules = new();

    public void Register(string path, IWebSocket module)
        => _modules[path] = module;

    public bool TryGet(string path, out IWebSocket module)
        => _modules.TryGetValue(path, out module!);
    
    public List<IWebSocket> Modules => _modules.Values.ToList();
}