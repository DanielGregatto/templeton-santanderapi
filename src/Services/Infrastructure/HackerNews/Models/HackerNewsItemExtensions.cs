using Services.Contracts.Results;

namespace Services.Infrastructure.HackerNews.Models
{
    public static class HackerNewsItemExtensions
    {
        public static StoryResult ToStoryResult(this HackerNewsItem item) => new(
            Title: item.Title ?? string.Empty,
            Uri: item.Url ?? $"https://news.ycombinator.com/item?id={item.Id}",
            PostedBy: item.By ?? string.Empty,
            Time: DateTimeOffset.FromUnixTimeSeconds(item.Time),
            Score: item.Score,
            CommentCount: item.Descendants);
    }
}
