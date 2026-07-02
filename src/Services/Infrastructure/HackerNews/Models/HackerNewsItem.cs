using System.Text.Json.Serialization;

namespace Services.Infrastructure.HackerNews.Models
{
    /// <summary>Raw shape of https://hacker-news.firebaseio.com/v0/item/{id}.json</summary>
    public class HackerNewsItem
    {
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? By { get; set; }

        /// <summary>Unix timestamp (seconds).</summary>
        public long Time { get; set; }
        public int Score { get; set; }
        public int Descendants { get; set; }
        public bool Dead { get; set; }
        public bool Deleted { get; set; }
    }
}
