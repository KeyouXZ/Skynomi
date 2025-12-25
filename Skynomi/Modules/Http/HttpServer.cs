using System.Net;
using Skynomi.Modules.WebSocket;

namespace Skynomi.Modules.Http;

internal sealed class HttpServer : IDisposable
{
    private readonly HttpRouter _router = new();
    private readonly WebSocketServer _wsServer = new();

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    public HttpRouter Router => _router;
    public WebSocketRouter WsRouter => _wsServer.Router;


    public void Start(string address, int port, bool secure, bool reload = false)
    {
        if (reload) Stop();

        Log.Info(reload ? "[HTTP] Reloading web server..." : "[HTTP] Starting web server...");

        var scheme = secure ? "https" : "http";
        var prefix = $"{scheme}://{address}:{port}/";

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        _listener.Start();

        _cts = new CancellationTokenSource();
        Task.Run(() => ListenLoop(_cts.Token));

        Log.Success($"[HTTP] Started at {prefix}");
    }

    public void Reload(string address, int port, bool secure)
    {
        Start(address, port, secure, true);
    }

    private void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();

        _cts = null;
        _listener = null;
    }

    private async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var raw = await _listener!.GetContextAsync();
                var ctx = new HttpContext(raw);
                _ = Task.Run(() => Handle(ctx), token);
            }
            catch when (token.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task Handle(HttpContext ctx)
    {
        var raw = ctx.Raw;
        var req = raw.Request;
        var res = raw.Response;
        var path = req.Url!.AbsolutePath;

        // 1️⃣ WebSocket
        Log.Debug($"[HTTP] {req.HttpMethod} {path}");

        if (req.IsWebSocketRequest)
        {
            await _wsServer.Handle(raw);
            return;
        }

        if (path.TrimStart('/').Contains('/'))
        {
            res.StatusCode = 403;
            res.Close();
            return;
        }

        /*switch (path)
        {
            // 2️⃣ Root "/"
            case "/":
            case "/core.js":
            case "/core.css":
                await HttpStatic.Serve(raw, SkynomiPlugin.SkynomiConfig.Web.Root);
                res.Close();
                return;
        }*/
        await HttpStatic.Serve(raw, SkynomiPlugin.SkynomiConfig.Web.Root);
        res.Close();
        return;

        // 5️⃣ Everything else → router
        /*if (_router.TryGet(path, out var h))
        {
            await h(ctx);
            res.Close();
            return;
        }*/
    }

    public void Dispose()
    {
        Log.Info("[HTTP] Stopping web server...");
        Stop();
    }
}