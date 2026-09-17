using System;

namespace ServerApp
{
    // Centralna konfiguracija servera.
    internal static class Config
    {
        public static string ApiKey = "bf5a1a22-42d9-425b-83f2-8700eb87f64a";
        public const string ApiBaseUrl = "http://api.airvisual.com/v2/city";
        public const string ServerPrefix = "http://localhost:5050/";
        public const int WorkerCount = 4;
        public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    }
}