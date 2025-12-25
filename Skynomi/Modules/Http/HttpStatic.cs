using System.Net;

namespace Skynomi.Modules.Http;

public static class HttpStatic
{
    public static async Task Serve(
        HttpListenerContext ctx,
        string root)
    {
        var path = ctx.Request.Url!.AbsolutePath;

        var rel = path.TrimStart('/');
        var file = Path.Combine(root, rel);
        
        Log.Debug($"[HTTP] Serving {file}");

        if (Directory.Exists(file))
            file = Path.Combine(file, "index.html");

        if (!File.Exists(file))
        {
            ctx.Response.StatusCode = 404;
            return;
        }

        ctx.Response.ContentType = GetMime(file);

        await using var fs = File.OpenRead(file);
        await fs.CopyToAsync(ctx.Response.OutputStream);
    }

    private static string GetMime(string file) =>
        Path.GetExtension(file) switch
        {
            ".html" => "text/html",
            ".js" => "application/javascript",
            ".css" => "text/css",
            ".json" => "application/json",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
}
