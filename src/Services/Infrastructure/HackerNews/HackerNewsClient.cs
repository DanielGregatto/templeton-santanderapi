using Domain.Configs;
using Microsoft.Extensions.Options;
using Services.Infrastructure.Caching;
using Services.Infrastructure.HackerNews.Models;
using Services.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace Services.Infrastructure.HackerNews
{
    public class HackerNewsClient : IHackerNewsClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly ICacheService _cache;
        private readonly HackerNewsConfig _config;
        private readonly HackerNewsRequestThrottle _throttle;

        public HackerNewsClient(
            HttpClient httpClient,
            ICacheService cache,
            IOptions<HackerNewsConfig> config,
            HackerNewsRequestThrottle throttle)
        {
            _httpClient = httpClient;
            _cache = cache;
            _config = config.Value;
            _throttle = throttle;
        }

        public Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken) =>
            _cache.GetOrCreateAsync(
                "hn:beststories",
                () => FetchBestStoryIdsAsync(cancellationToken),
                TimeSpan.FromSeconds(_config.BestStoriesCacheSeconds));

        public Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken) =>
            _cache.GetOrCreateAsync(
                $"hn:item:{id}",
                () => FetchItemAsync(id, cancellationToken),
                TimeSpan.FromSeconds(_config.ItemCacheSeconds));

        private async Task<IReadOnlyList<int>> FetchBestStoryIdsAsync(CancellationToken cancellationToken)
        {
            await _throttle.Semaphore.WaitAsync(cancellationToken);
            try
            {
                var ids = await _httpClient.GetFromJsonAsync<int[]>("beststories.json", JsonOptions, cancellationToken);
                return ids ?? Array.Empty<int>();
            }
            finally
            {
                _throttle.Semaphore.Release();
            }
        }

        private async Task<HackerNewsItem?> FetchItemAsync(int id, CancellationToken cancellationToken)
        {
            await _throttle.Semaphore.WaitAsync(cancellationToken);
            try
            {
                return await _httpClient.GetFromJsonAsync<HackerNewsItem>($"item/{id}.json", JsonOptions, cancellationToken);
            }
            finally
            {
                _throttle.Semaphore.Release();
            }
        }
    }
}
