using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClientApp
{
    class Program
    {
        const string ServerUrl = "http://localhost:5050/";
        static readonly HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Klijent za pregled kvaliteta vazduha (async)");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("1 - Jedan zahtev");
                Console.WriteLine("2 - Vise paralelnih zahteva (test konkurentnosti)");
                Console.WriteLine("exit - izlaz");
                Console.Write("Izbor: ");
                string? choice = Console.ReadLine();
                if (choice == null || choice.Trim().ToLower() == "exit")
                    break;

                choice = choice.Trim();
                if (choice != "1" && choice != "2")
                {
                    Console.WriteLine("Nepoznat izbor.");
                    continue;
                }
                Console.Write("Grad (city): ");
                string city = Console.ReadLine() ?? "";
                Console.Write("Region (state): ");
                string state = Console.ReadLine() ?? "";
                Console.Write("Drzava (country): ");
                string country = Console.ReadLine() ?? "";

                string url = $"{ServerUrl}?city={Uri.EscapeDataString(city.Trim())}" +
                             $"&state={Uri.EscapeDataString(state.Trim())}" +
                             $"&country={Uri.EscapeDataString(country.Trim())}";

                if (choice == "1")
                {
                    await SendRequestAsync(url, 0);
                }
                else
                {
                    Console.Write("Broj paralelnih zahteva: ");
                    if (!int.TryParse(Console.ReadLine(), out int n) || n < 1)
                        n = 5;

                    var tasks = new List<Task>();
                    for (int i = 0; i < n; i++)
                    {
                        int id = i + 1;
                        tasks.Add(SendRequestAsync(url, id));
                    }
                    await Task.WhenAll(tasks);
                }
            }
        }
        static async Task SendRequestAsync(string url, int id)
        {
            string prefix = id == 0 ? "" : $"[Zahtev {id}] ";
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                string body = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"{prefix}HTTP {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"{prefix}{StripHtml(body)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{prefix}Greska: {ex.Message}");
            }
        }
        static string StripHtml(string html)
        {
            string noStyle = Regex.Replace(html, "<style.*?</style>", " ", RegexOptions.Singleline);
            string text = Regex.Replace(noStyle, "<.*?>", " ");
            text = Regex.Replace(text, "\\s+", " ").Trim();
            return text;
        }
    }
}