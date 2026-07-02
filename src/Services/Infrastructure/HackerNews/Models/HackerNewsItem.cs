using System.Text.Json.Serialization;

namespace Services.Infrastructure.HackerNews.Models
{
    /// <summary>
    /// Raw shape of https://hacker-news.firebaseio.com/v0/item/{id}.json — mirrors every field
    /// documented at https://github.com/HackerNews/API#items, not just the ones this app currently
    /// maps into <see cref="Services.Contracts.Results.StoryResult"/>.
    /// </summary>
    public class HackerNewsItem
    {
        public int Id { get; set; }

        /// <summary>"job", "story", "comment", "poll", or "pollopt".</summary>
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? By { get; set; }

        /// <summary>Unix timestamp (seconds).</summary>
        public long Time { get; set; }
        public int Score { get; set; }

        /// <summary>Total comment count. Only present on stories/polls.</summary>
        public int Descendants { get; set; }
        public bool Dead { get; set; }
        public bool Deleted { get; set; }

        /// <summary>The comment, Ask HN, or poll text, as HTML.</summary>
        public string? Text { get; set; }

        /// <summary>The comment's parent: either another comment or the relevant story.</summary>
        public int? Parent { get; set; }

        /// <summary>The pollopt's associated poll.</summary>
        public int? Poll { get; set; }

        /// <summary>Comment ids, in display order.</summary>
        public int[]? Kids { get; set; }

        /// <summary>A poll's related pollopts, in display order.</summary>
        public int[]? Parts { get; set; }
    }
}
