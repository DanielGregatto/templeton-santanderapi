using Domain.Configs;
using Microsoft.Extensions.Options;
using System;
using System.Threading;

namespace Services.Infrastructure.HackerNews
{
    /// <summary>
    /// Caps how many requests to the Hacker News API can be in flight at once, shared across all
    /// callers regardless of how many inbound API requests are being served concurrently.
    /// </summary>
    public class HackerNewsRequestThrottle
    {
        public SemaphoreSlim Semaphore { get; }

        public HackerNewsRequestThrottle(IOptions<HackerNewsConfig> config)
        {
            var maxConcurrency = Math.Max(1, config.Value.MaxConcurrentUpstreamRequests);
            Semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        }
    }
}
