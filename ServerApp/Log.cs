using System;

namespace ServerApp
{
    internal static class Logger
    {
        static readonly object logLock = new object();

        public static void Log(string message)
        {
            lock (logLock)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Nit {Environment.CurrentManagedThreadId}] {message}");
            }
        }
    }
}