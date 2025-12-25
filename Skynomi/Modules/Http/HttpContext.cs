using System.Net;

namespace Skynomi.Modules.Http;

public sealed class HttpContext
{
    public HttpListenerContext Raw { get; }
    public bool IsHandled { get; private set; }

    public HttpContext(HttpListenerContext raw)
    {
        Raw = raw;
    }

    public void MarkHandled()
        => IsHandled = true;
}