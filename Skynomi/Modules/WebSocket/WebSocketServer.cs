using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Skynomi.Modules.WebSocket;

public sealed class WebSocketServer
{
    private readonly WebSocketRouter _router = new();

    internal WebSocketRouter Router => _router;
    private static List<System.Net.WebSockets.WebSocket> _clients = [];

    internal async Task Handle(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url!.AbsolutePath;

        if (path != "/ws")
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
            return;
        }

        var wsCtx = await ctx.AcceptWebSocketAsync(null);
        var socket = wsCtx.WebSocket;

        lock (_clients)
        {
            _clients.Add(socket);
        }

        Log.Debug("[WS] Client connected");

        // Fires on connect
        await WsCore.OnConnect(socket);
        
        foreach (var mod in _router.Modules)
        {
            await mod.OnConnect(socket);
        }

        var buffer = new byte[4096];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(
                    buffer, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Log.Debug($"[WS] Client message: {msg}");

                using var doc = JsonDocument.Parse(msg);
                if (!doc.RootElement.TryGetProperty("data", out var data) ||
                    !data.TryGetProperty("eventType", out var eventTypeProp))
                {
                    continue;
                }

                var eventType = eventTypeProp.GetString(); // "economy:balanceUpdate"
                if (eventType == null) continue;

                var parts = eventType.Split(':');
                if (parts.Length < 2) continue;

                var moduleName = parts[0];
                var eventName = parts[1];

                if (moduleName == "core")
                {
                 // handle core   
                }

                if (!_router.TryGet(moduleName, out var module))
                {
                    continue;
                }

                await module.OnMessage(socket, msg, eventName);
            }
        }
        finally
        {
            // Fires on disconnect
            foreach (var mod in _router.Modules)
            {
                await mod.OnDisconnect(socket);
            }

            lock (_clients)
            {
                _clients.Remove(socket);
            }

            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "bye",
                CancellationToken.None);
        }
    }

    public static void SendWsData(string? eventType = null, object? data = null)
    {
        try
        {
            dynamic? message;
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                message = new Dictionary<string, object?>
                {
                    { "eventType", eventType },
                    { "data", data }
                };
            }
            else if (data != null)
            {
                message = data;
            }
            else
            {
                return;
            }

            string json = JsonSerializer.Serialize(message);

            lock (_clients)
            {
                foreach (var ws in _clients)
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        _ = ws.SendAsync(
                            Encoding.UTF8.GetBytes(json),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }
}