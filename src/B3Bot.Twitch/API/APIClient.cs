using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using B3Bot.Core;

namespace B3Bot.Twitch.API
{
    public class APIClient : IDisposable
    {
        private const string LOGGER_CATEGORY = "B3Bot.Twitch.API";
        private const string TWITCH_API_URL = "https://api.twitch.tv";
        private const int MAX_QUEUED = 5;

        private readonly static SemaphoreSlim _rateLimitLock = new SemaphoreSlim(1);
        private object _queueLock = new object();
        private int _queuedRequests = 0;
        private int _waitingRequests = 0;
        private static short _rateLimitRemaining = 1;
        private static long _rateLimitResetTicks = DateTime.MaxValue.Ticks;

        public ILogger Logger { get; } 
        internal HttpClient httpClient { get; private set; }

        public APIClient(HttpClient client, ILoggerFactory loggerFactory) :
            this(client, loggerFactory.CreateLogger(LOGGER_CATEGORY))
        {
        }

        private APIClient(HttpClient client, ILogger logger)
        {
            Logger = logger;

            client.BaseAddress = new Uri(TWITCH_API_URL);
            client.DefaultRequestHeaders.Add("Client-ID", Constants.TwitchAPIClientId);

            httpClient = client;
        }

        public async Task<HttpResponseMessage> GetFromEndpoint(string url)
        {
            await WaitForSlot();

            HttpResponseMessage result;
            await _rateLimitLock.WaitAsync();
            try
            {
                result = await httpClient.GetAsync(url);

                var remaining = short.Parse(result.Headers.GetValues("RateLimit-Remaining").First());
                var reset = long.Parse(result.Headers.GetValues("RateLimit-Reset").First());

                lock (_queueLock)
                {
                    _rateLimitRemaining = remaining;
                    _waitingRequests--;
                    Volatile.Write(ref _rateLimitResetTicks, reset.ToDateTime().Ticks);
                }
                Logger.LogTrace($"{DateTime.UtcNow}: Twitch Rate - {remaining} until {reset.ToDateTime()}");
            }
            finally
            {
                _rateLimitLock.Release();
            }

            result.EnsureSuccessStatusCode();

            return result;
        }

        private async Task WaitForSlot()
        {
            var isQueued = false;
            do
            {
                // Check rate-limit
                lock (_queueLock)
                {
                    if (_rateLimitRemaining - _waitingRequests > 0)
                    {
                        _waitingRequests++;
                        if (isQueued)
                        {
                            _queuedRequests--;
                            isQueued = false;
                        }
                    }
                    else
                    {
                        if (!isQueued)
                        {
                            if (_queuedRequests + 1 > MAX_QUEUED)
                            {
                                throw new TimeoutException("Too many requests waiting");
                            }
                            _queuedRequests++;
                            isQueued = true;
                        }
                    }
                }

                if (isQueued)
                {
                    await Task.Delay(new DateTime(Volatile.Read(ref _rateLimitResetTicks)).Subtract(DateTime.UtcNow));
                }
            }
            while (isQueued);
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}
