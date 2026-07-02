using Services.Infrastructure.HackerNews.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IHackerNewsClient
    {
        /// <summary>IDs of the current best stories, as returned by beststories.json.</summary>
        Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken);

        /// <summary>Details for a single story/item, or null if it doesn't exist.</summary>
        Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken);
    }
}
