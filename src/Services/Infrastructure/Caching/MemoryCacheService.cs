using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Infrastructure.Caching
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
        {
            if (_cache.TryGetValue(key, out T? cached))
            {
                _logger.LogDebug("Cache hit for {Key}", key);
                return cached!;
            }

            var keyLock = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await keyLock.WaitAsync();
            try
            {
                // Another caller may have populated the cache while we were waiting for the lock.
                if (_cache.TryGetValue(key, out cached))
                {
                    _logger.LogDebug("Cache hit for {Key} after waiting for an in-flight fetch", key);
                    return cached!;
                }

                _logger.LogDebug("Cache miss for {Key}; fetching (ttl {TtlSeconds}s)", key, ttl.TotalSeconds);
                var value = await factory();
                _cache.Set(key, value, ttl);
                return value;
            }
            finally
            {
                keyLock.Release();
            }
        }
    }
}
