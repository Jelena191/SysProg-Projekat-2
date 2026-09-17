using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServerApp
{
    internal class AirVisualException : Exception
    {
        public AirVisualException(string message) : base(message) { }
    }
    internal static class AirVisualClient
    {
        private static readonly HttpClient http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        public static async Task<string> FetchAirQualityAsync(string city, string state, string country)
        {
            if (Config.ApiKey == "YOUR_API_KEY" || string.IsNullOrWhiteSpace(Config.ApiKey))
                throw new AirVisualException(
                    "API kljuc nije podesen. Otvorite ServerApp/Config.cs i unesite svoj IQAir API kljuc.");

            string url =
                $"{Config.ApiBaseUrl}?city={Uri.EscapeDataString(city)}" +
                $"&state={Uri.EscapeDataString(state)}" +
                $"&country={Uri.EscapeDataString(country)}" +
                $"&key={Uri.EscapeDataString(Config.ApiKey)}";

            string body;
            try
            {
                HttpResponseMessage resp = await http.GetAsync(url);
                body = await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new AirVisualException($"Neuspela komunikacija sa IQAir API-jem: {ex.Message}");
            }

            using JsonDocument doc = ParseJson(body);
            JsonElement root = doc.RootElement;
            string status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";
            if (status != "success")
            {
                string apiMessage = "nepoznata greska";
                if (root.TryGetProperty("data", out var dataErr) &&
                    dataErr.ValueKind == JsonValueKind.Object &&
                    dataErr.TryGetProperty("message", out var msg))
                {
                    apiMessage = msg.GetString() ?? apiMessage;
                }
                throw new AirVisualException($"IQAir API greska: {apiMessage}");
            }
            JsonElement data = root.GetProperty("data");
            JsonElement current = data.GetProperty("current");
            JsonElement pollution = current.GetProperty("pollution");

            int aqiUs = pollution.GetProperty("aqius").GetInt32();
            int aqiCn = pollution.GetProperty("aqicn").GetInt32();
            string mainUs = PollutantName(pollution.TryGetProperty("mainus", out var mu) ? mu.GetString() : null);
            string ts = pollution.TryGetProperty("ts", out var t) ? (t.GetString() ?? "") : "";
            string cityName = data.TryGetProperty("city", out var c) ? (c.GetString() ?? city) : city;
            string stateName = data.TryGetProperty("state", out var s) ? (s.GetString() ?? state) : state;
            string countryName = data.TryGetProperty("country", out var co) ? (co.GetString() ?? country) : country;
            string weatherHtml = "";
            if (current.TryGetProperty("weather", out var weather) && weather.ValueKind == JsonValueKind.Object)
            {
                int temp = weather.TryGetProperty("tp", out var tp) ? tp.GetInt32() : 0;
                int hum = weather.TryGetProperty("hu", out var hu) ? hu.GetInt32() : 0;
                double wind = weather.TryGetProperty("ws", out var ws) ? ws.GetDouble() : 0;
                weatherHtml =
                    $"<tr><td>Temperatura</td><td>{temp} &deg;C</td></tr>" +
                    $"<tr><td>Vlaznost</td><td>{hum} %</td></tr>" +
                    $"<tr><td>Brzina vetra</td><td>{wind} m/s</td></tr>";
            }
            return BuildHtml(cityName, stateName, countryName, aqiUs, aqiCn, mainUs, ts, weatherHtml);
        }
        private static JsonDocument ParseJson(string body)
        {
            try
            {
                return JsonDocument.Parse(body);
            }
            catch (Exception)
            {
                throw new AirVisualException("Neispravan odgovor IQAir API-ja (nije validan JSON).");
            }
        }
        private static string PollutantName(string? code)
        {
            return code switch
            {
                "p2" => "PM2.5",
                "p1" => "PM10",
                "o3" => "Ozon (O3)",
                "n2" => "Azot-dioksid (NO2)",
                "s2" => "Sumpor-dioksid (SO2)",
                "co" => "Ugljen-monoksid (CO)",
                _ => code ?? "nepoznato"
            };
        }

        private static (string label, string color) AqiCategory(int aqi)
        {
            if (aqi <= 50) return ("Dobar", "#009966");
            if (aqi <= 100) return ("Umeren", "#ffde33");
            if (aqi <= 150) return ("Nezdrav za osetljive grupe", "#ff9933");
            if (aqi <= 200) return ("Nezdrav", "#cc0033");
            if (aqi <= 300) return ("Veoma nezdrav", "#660099");
            return ("Opasan", "#7e0023");
        }
        private static string BuildHtml(string city, string state, string country,
            int aqiUs, int aqiCn, string mainUs, string ts, string weatherHtml)
        {
            var (label, color) = AqiCategory(aqiUs);
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang='sr'><head><meta charset='utf-8'>");
            sb.Append("<title>Kvalitet vazduha</title>");
            sb.Append("<style>body{font-family:Arial,sans-serif;background:#f4f4f4;margin:40px;}");
            sb.Append(".card{background:#fff;max-width:520px;margin:auto;padding:24px;border-radius:12px;box-shadow:0 2px 8px rgba(0,0,0,.1);}");
            sb.Append("table{width:100%;border-collapse:collapse;margin-top:12px;}");
            sb.Append("td{padding:8px;border-bottom:1px solid #eee;}");
            sb.Append(".badge{display:inline-block;padding:6px 14px;border-radius:20px;color:#000;font-weight:bold;}");
            sb.Append("</style></head><body><div class='card'>");
            sb.Append($"<h2>Kvalitet vazduha: {city}, {state}, {country}</h2>");
            sb.Append($"<p>Status: <span class='badge' style='background:{color}'>{label}</span></p>");
            sb.Append("<table>");
            sb.Append($"<tr><td>US AQI</td><td><b>{aqiUs}</b></td></tr>");
            sb.Append($"<tr><td>CN AQI</td><td>{aqiCn}</td></tr>");
            sb.Append($"<tr><td>Glavni zagadjivac</td><td>{mainUs}</td></tr>");
            sb.Append(weatherHtml);
            sb.Append($"<tr><td>Vreme merenja</td><td>{ts}</td></tr>");
            sb.Append("</table></div></body></html>");
            return sb.ToString();
        }
    }
}