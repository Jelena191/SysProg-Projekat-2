using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ServerApp
{
    internal class WebServer
    {
        private readonly HttpListener listener = new HttpListener();
        private readonly Queue<HttpListenerContext> requestQueue = new Queue<HttpListenerContext>();
        private readonly object queueLock = new object();
        private readonly SemaphoreSlim itemsAvailable = new SemaphoreSlim(0);
        private volatile bool running = false;
        public void Start()
        {
            listener.Prefixes.Add(Config.ServerPrefix);
            listener.Start();
            running = true;

            Logger.Log($"Server pokrenut na {Config.ServerPrefix}");
            Logger.Log($"Broj konzumentskih taskova: {Config.WorkerCount}");
            Logger.Log("Primer: " + Config.ServerPrefix + "?city=LosAngeles&state=California&country=USA");
            for (int i = 0; i < Config.WorkerCount; i++)
            {
                int id = i + 1;
                _ = Task.Run(() => ConsumerLoopAsync(id));
            }
            while (running)
            {
                HttpListenerContext context;
                try
                {
                    context = listener.GetContext();
                }
                catch (Exception ex)
                {
                    if (!running) break;
                    Logger.Log($"Greska pri prijemu zahteva: {ex.Message}");
                    continue;
                }
                lock (queueLock)
                {
                    requestQueue.Enqueue(context);
                }
                itemsAvailable.Release();
            }
        }
        private async Task ConsumerLoopAsync(int id)
        {
            Logger.Log($"Konzumentski task {id} pokrenut.");
            while (running)
            {
                await itemsAvailable.WaitAsync();

                HttpListenerContext context;
                lock (queueLock)
                {
                    if (requestQueue.Count == 0)
                        continue;
                    context = requestQueue.Dequeue();
                }

                try
                {
                    await RequestHandler.ProcessRequestAsync(context);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Neuhvacena greska u tasku {id}: {ex.Message}");
                }
            }
        }
    }
}