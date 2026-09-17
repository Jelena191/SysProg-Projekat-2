using System;

namespace ServerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                WebServer server = new WebServer();
                server.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server se nije mogao pokrenuti: {ex.Message}");
            }
        }
    }
}