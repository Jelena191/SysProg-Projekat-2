using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServerApp
{
    internal class AsyncCache
    {
        private class Entry
        {
            public string Value = "";
            public DateTime ExpiresAt;
        }
        private readonly Dictionary<string, Entry> store = new Dictionary<string, Entry>();
        private readonly Dictionary<string, Task<string>> inFlight = new Dictionary<string, Task<string>>();
        private readonly object gate = new object();
        private readonly TimeSpan duration;
        public AsyncCache(TimeSpan duration)
        {
            this.duration = duration;
        }
        public Task<string> GetOrAddAsync(string key, Func<Task<string>> factory)
        {
            lock (gate)
            {
                if (store.TryGetValue(key, out var e))
                {
                    if (DateTime.Now < e.ExpiresAt)
                    {
                        Logger.Log($"Kes pogodak: {key}");
                        return Task.FromResult(e.Value);
                    }
                    store.Remove(key);
                }

                if (inFlight.TryGetValue(key, out var existing))
                {
                    Logger.Log($"Isti zahtev se vec obradjuje, task ceka na rezultat: {key}");
                    return existing;
                }
                var tcs = new TaskCompletionSource<string>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                inFlight[key] = tcs.Task;
                Logger.Log($"Kes promasaj, pokrecem obradu: {key}");

                try
                {
                    Task<string> fetch = factory();
                    _ = fetch.ContinueWith(t =>
                    {
                        string? result = null;
                        Exception? error = null;
                        bool cancelled = t.IsCanceled;

                        lock (gate)
                        {
                            inFlight.Remove(key);
                            if (t.IsCompletedSuccessfully)
                            {
                                result = t.Result;
                                store[key] = new Entry
                                {
                                    Value = result,
                                    ExpiresAt = DateTime.Now.Add(duration)
                                };
                                Logger.Log($"Rezultat kesiran: {key}");
                            }
                            else if (!cancelled)
                            {
                                error = t.Exception?.GetBaseException() ??
                                        new Exception("Nepoznata greska u asinhronoj obradi.");
                                Logger.Log($"Obrada nije uspela, rezultat se ne kesira: {key}");
                            }
                        }

                        if (cancelled)
                            tcs.TrySetCanceled();
                        else if (error != null)
                            tcs.TrySetException(error);
                        else
                            tcs.TrySetResult(result!);
                    }, TaskScheduler.Default);
                }
                catch (Exception ex)
                {
                    inFlight.Remove(key);
                    tcs.TrySetException(ex);
                }

                return tcs.Task;
            }
        }
    }
}