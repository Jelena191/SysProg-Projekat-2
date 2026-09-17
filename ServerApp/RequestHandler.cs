using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp
{
    internal static class RequestHandler
    {
        private static readonly AsyncCache cache = new AsyncCache(Config.CacheDuration);
        public static async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            if (request.Url != null && request.Url.AbsolutePath == "/favicon.ico")
            {
                context.Response.StatusCode = 204;
                context.Response.OutputStream.Close();
                return;
            }
            if (!string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"Odbijena metoda: {request.HttpMethod}");
                SendError(context, 405, "Dozvoljena je samo GET metoda.");
                return;
            }
            string city = (request.QueryString["city"] ?? "").Trim();
            string state = (request.QueryString["state"] ?? "").Trim();
            string country = (request.QueryString["country"] ?? "").Trim();
            Logger.Log($"Zahtev: city='{city}', state='{state}', country='{country}'");
            if (string.IsNullOrWhiteSpace(city))
            {
                SendError(context, 400,
                    "Nedostaje obavezan parametar 'city'.<br>" +
                    "Primer: http://localhost:5050/?city=Los%20Angeles&state=California&country=USA<br>" +
                    "Primer: http://localhost:5050/?city=Belgrade&state=Central%20Serbia&country=Serbia<br>" +
                    "Primer: http://localhost:5050/?city=Novi%20Sad&state=Autonomna%20Pokrajina%20Vojvodina&country=Serbia<br>" +
                    "Primer: http://localhost:5050/?city=Nis&state=Central%20Serbia&country=Serbia<br>");
                return;
            }
            string cacheKey = $"{city}|{state}|{country}".ToLowerInvariant();
            Task<string> resultTask = cache.GetOrAddAsync(cacheKey,
                () =>
                {
                    Logger.Log($"Poziv IQAir API-ja za: {city}, {state}, {country}");
                    return AirVisualClient.FetchAirQualityAsync(city, state, country);
                });
            await resultTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception?.InnerException ?? t.Exception!;
                    if (ex is AirVisualException)
                    {
                        Logger.Log($"Greska u obradi ({cacheKey}): {ex.Message}");
                        SendError(context, 404, ex.Message);
                    }
                    else
                    {
                        Logger.Log($"Neocekivana greska ({cacheKey}): {ex.Message}");
                        SendError(context, 500, "Doslo je do interne greske na serveru.");
                    }
                }
                else
                {
                    SendResponse(context, t.Result);
                    Logger.Log($"Uspesno posluzen zahtev: {cacheKey}");
                }
            });
        }
        private static void SendResponse(HttpListenerContext context, string html)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
        private static void SendError(HttpListenerContext context, int statusCode, string message)
        {
            string html =
                "<!DOCTYPE html><html lang='sr'><head><meta charset='utf-8'><title>Greska</title>" +
                "<style>body{font-family:Arial,sans-serif;background:#f4f4f4;margin:40px;}" +
                ".card{background:#fff;max-width:520px;margin:auto;padding:24px;border-radius:12px;" +
                "box-shadow:0 2px 8px rgba(0,0,0,.1);border-left:6px solid #cc0033;}</style></head>" +
                $"<body><div class='card'><h2>Greska {statusCode}</h2><p>{message}</p></div></body></html>";

            byte[] buffer = Encoding.UTF8.GetBytes(html);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
    }
}