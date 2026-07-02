using Domain.Contracts.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.Contracts.Results;
using Services.Core;
using Services.Infrastructure.HackerNews.Models;
using Services.Interfaces;

namespace Services.Features.Stories.Queries.GetBestStories
{
    public class GetBestStoriesQueryHandler : BaseQueryHandler,
        IRequestHandler<GetBestStoriesQuery, Result<IEnumerable<StoryResult>>>
    {
        private readonly IHackerNewsClient _hackerNewsClient;
        private readonly IValidator<GetBestStoriesQuery> _validator;
        private readonly ILogger<GetBestStoriesQueryHandler> _logger;

        public GetBestStoriesQueryHandler(
            IHackerNewsClient hackerNewsClient,
            IValidator<GetBestStoriesQuery> validator,
            ILogger<GetBestStoriesQueryHandler> logger)
        {
            _hackerNewsClient = hackerNewsClient;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<StoryResult>>> Handle(GetBestStoriesQuery request, CancellationToken cancellationToken)
        {
            var validationError = await ValidateAsync<GetBestStoriesQuery, IEnumerable<StoryResult>>(_validator, request, cancellationToken);
            if (validationError != null)
                return validationError;

            var ids = await _hackerNewsClient.GetBestStoryIdsAsync(cancellationToken);
            _logger.LogDebug("Fetched {Count} candidate story ids", ids.Count);

            // Score is only known once each item is fetched, so every candidate must be retrieved
            // before the top N can be determined. HackerNewsClient caches items and caps concurrent
            // upstream calls, so this fan-out is cheap on repeated/overlapping requests. Each fetch is
            // isolated so one bad story (network blip, malformed item) doesn't fail the whole request.
            var items = await Task.WhenAll(ids.Select(id => FetchItemSafeAsync(id, cancellationToken)));

            var stories = items
                .Where(item => item is { Deleted: false, Dead: false })
                .Cast<HackerNewsItem>()
                .OrderByDescending(item => item.Score)
                .Take(request.N)
                .Select(ToStoryResult)
                .ToList();

            _logger.LogInformation("Returning {Count} of the requested {Requested} best stories", stories.Count, request.N);

            return Result<IEnumerable<StoryResult>>.Success(stories);
        }

        private async Task<HackerNewsItem?> FetchItemSafeAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                return await _hackerNewsClient.GetItemAsync(id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch Hacker News item {ItemId}; excluding it from the results", id);
                return null;
            }
        }

        private static StoryResult ToStoryResult(HackerNewsItem item) => new(
            Title: item.Title ?? string.Empty,
            Uri: item.Url ?? $"https://news.ycombinator.com/item?id={item.Id}",
            PostedBy: item.By ?? string.Empty,
            Time: DateTimeOffset.FromUnixTimeSeconds(item.Time),
            Score: item.Score,
            CommentCount: item.Descendants);
    }
}
