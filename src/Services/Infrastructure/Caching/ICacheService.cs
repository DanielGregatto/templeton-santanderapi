using System;
using System.Threading.Tasks;

namespace Services.Infrastructure.Caching
{
    public interface ICacheService
    {
        /// <summary>
        /// Returns the cached value for <paramref name="key"/>, or invokes <paramref name="factory"/> to
        /// produce and cache it. Concurrent callers that miss the cache for the same key are coalesced
        /// into a single call to <paramref name="factory"/> (cache-stampede protection).
        /// </summary>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);
    }
}
