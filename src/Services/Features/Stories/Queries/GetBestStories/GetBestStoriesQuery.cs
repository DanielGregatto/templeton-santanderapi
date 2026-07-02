using Domain.Contracts.Common;
using MediatR;
using Services.Contracts.Results;

namespace Services.Features.Stories.Queries.GetBestStories
{
    /// <summary>
    /// Represents a query to retrieve the best N Hacker News stories, ordered by score descending.
    /// </summary>
    public class GetBestStoriesQuery : IRequest<Result<IEnumerable<StoryResult>>>
    {
        /// <summary>
        /// Gets or sets the number of top stories to retrieve.
        /// </summary>
        public int N { get; set; }
    }
}
