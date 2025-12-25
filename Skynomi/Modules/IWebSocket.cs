namespace Skynomi.Modules;

public interface IWebSocket
{
    Task OnConnect(System.Net.WebSockets.WebSocket socket)
        => Task.CompletedTask;

    Task OnMessage(System.Net.WebSockets.WebSocket socket, string message, string eventName);

    Task OnDisconnect(System.Net.WebSockets.WebSocket socket)
        => Task.CompletedTask;
}