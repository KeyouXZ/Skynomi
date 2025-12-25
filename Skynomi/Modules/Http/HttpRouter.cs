namespace Skynomi.Modules.Http;

internal class HttpRouter
{
    private readonly Dictionary<string, Func<HttpContext, Task>> _handlers = new();

    public void Register(string path, Func<HttpContext, Task> handler)
    {
        _handlers[path] = handler;
    }

    public bool TryGet(string path, out Func<HttpContext, Task> handler)
        => _handlers.TryGetValue(path, out handler!);
}