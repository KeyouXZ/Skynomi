using Skynomi.Modules.Http;

namespace Skynomi.Modules;

public interface IHttpServer
{
    Task HandleHttpServer(HttpContext ctx);
}