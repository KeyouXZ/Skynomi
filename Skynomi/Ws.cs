using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Skynomi.Modules;
using Skynomi.Modules.WebSocket;
using TShockAPI;

namespace Skynomi;

public abstract class Ws
{
    public static void Send(string eventType, object data)
    {
        WebSocketServer.SendWsData(eventType, data);
    }
}

internal abstract class WsCore
{
    public static async Task OnConnect(WebSocket socket)
    {
        try
        {
            var data = new
            {
                Modules = ModuleManager.GetModules(),
                Player = TShock.Players
                    .Where(p => p != null)
                    .Select(p => new
                    {
                        p.Name,
                        Group = p.Group?.Name ?? "none"
                    })
                    .ToList()
            };
            var json = JsonSerializer.Serialize(new { eventType = "core:connect", data });
            var buffer = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }
}